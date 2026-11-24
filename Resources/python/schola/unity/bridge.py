# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

"""Expose a Unity TrainingServer through Schola's existing GymService contract.

The adapter owns a loopback-only, length-prefixed JSON connection to Unity. It
uses the existing protobuf schema and gRPC clients without native Unity plugins.
One adapter serves Unity's registered environments in index order and their declared
agents. A single-agent Unity environment declares one agent, ``SingleAgent``.
"""

from concurrent.futures import ThreadPoolExecutor
import json
import math
import socket
import struct
import threading
import time
from typing import Any

import grpc

from schola.generated import Definitions_pb2 as definitions
from schola.generated import GymConnector_pb2 as connector
from schola.generated import GymConnector_pb2_grpc as services
from schola.generated import Points_pb2 as points
from schola.generated import Spaces_pb2 as spaces
from schola.generated import State_pb2 as states
from schola.generated import StateUpdates_pb2 as updates

# Wire format version; must equal TrainingWire.Version in the Unity package.
# Version 2 keys every definition, action and state by agent id.
VERSION = 2
MAX_MESSAGE_BYTES = 4 * 1024 * 1024


class UnityError(RuntimeError):
    """Unity rejected an operation; reset the environment before stepping again."""


def _shape(node: dict, count: int) -> list[int]:
    shape = node.get("shape") or [count]
    if not shape or any(type(size) is not int or size <= 0 for size in shape):
        raise ValueError("Invalid Box shape")
    if math.prod(shape) != count:
        raise ValueError("Box shape does not match its data")
    return shape


def _decode_tree(nodes: list[dict], *, space: bool):
    if not nodes or len(nodes) > 10000 or nodes[0].get("parent") != -1:
        raise ValueError("Invalid Unity value tree")
    result = []
    children: dict[int, set[str]] = {}
    for index, node in enumerate(nodes):
        value = spaces.Space() if space else points.Point()
        kind = node["kind"]
        if kind == "dict":
            (value.dict_space if space else value.dict_point).SetInParent()
        elif kind == "box":
            if space:
                box = value.box_space
                dimensions = node.get("dimensions") or []
                box.shape_dimensions.extend(_shape(node, len(dimensions)))
                for item in dimensions:
                    dimension = box.dimensions.add()
                    if item.get("hasLow"):
                        dimension.low = item["low"]
                    if item.get("hasHigh"):
                        dimension.high = item["high"]
            else:
                box = value.box_point
                data = node.get("values") or []
                if not all(math.isfinite(x) for x in data):
                    raise ValueError("Non-finite Unity Box value")
                box.values.extend(data)
                box.shape.extend(_shape(node, len(data)))
            box.dtype = 0  # FLOAT32, matching Unity's float-based Box types.
        elif kind == "discrete":
            if space:
                if node["high"] <= 0:
                    raise ValueError("Discrete space must have positive size")
                value.discrete_space.high = node["high"]
            else:
                value.discrete_point.value = node["value"]
        elif kind == "multi_binary":
            if space:
                shape = node.get("shape")
                if (
                    not isinstance(shape, list)
                    or len(shape) != 1
                    or type(shape[0]) is not int
                    or shape[0] <= 0
                ):
                    raise ValueError("Invalid MultiBinary shape")
                value.multi_binary_space.shape = shape[0]
            else:
                data = node.get("boolValues") or []
                if not isinstance(data, list) or not all(
                    type(flag) is bool for flag in data
                ):
                    raise ValueError("MultiBinary values must be booleans")
                value.multi_binary_point.values.extend(data)
        else:
            raise ValueError(f"Unsupported Unity type: {kind}")
        result.append(value)
        if index:
            parent = node["parent"]
            if type(parent) is not int or not 0 <= parent < index:
                raise ValueError("Invalid Unity tree parent")
            if nodes[parent]["kind"] != "dict":
                raise ValueError("Unity tree parent must be a Dict")
            key = node["key"]
            if not isinstance(key, str) or key in children.setdefault(parent, set()):
                raise ValueError("Invalid or duplicate Unity key")
            children[parent].add(key)
    # Attach children in reverse order so each copied protobuf subtree is complete.
    for index in range(len(nodes) - 1, 0, -1):
        node = nodes[index]
        parent = result[node["parent"]]
        target = parent.dict_space.spaces if space else parent.dict_point.values
        target[node["key"]].CopyFrom(result[index])
    return result[0]


def _encode_point(point: points.Point) -> list[dict]:
    nodes = []

    def visit(value, parent=-1, key=None, depth=0):
        if depth > 32 or len(nodes) >= 10000:
            raise ValueError("Action tree is too deep or too large")
        node = {"parent": parent, "key": key}
        index = len(nodes)
        nodes.append(node)
        kind = value.WhichOneof("point")
        if kind == "dict_point":
            node["kind"] = "dict"
            for child_key in sorted(value.dict_point.values):
                visit(value.dict_point.values[child_key], index, child_key, depth + 1)
        elif kind == "box_point":
            if value.box_point.dtype != 0:
                raise ValueError("Unity Box actions require FLOAT32")
            data = list(value.box_point.values)
            if not all(math.isfinite(x) for x in data):
                raise ValueError("Actions must be finite")
            node.update(kind="box", values=data, shape=list(value.box_point.shape))
            _shape(node, len(data))
        elif kind == "discrete_point":
            node.update(kind="discrete", value=value.discrete_point.value)
        elif kind == "multi_binary_point":
            node.update(
                kind="multi_binary",
                boolValues=list(value.multi_binary_point.values),
            )
        else:
            raise ValueError(f"Unsupported Unity action type: {kind}")

    visit(point)
    return nodes


def _info(entries) -> dict[str, str]:
    return {entry["key"]: entry["value"] for entry in entries or []}


class UnityConnection:
    """Serialized local transport. A failed exchange closes the stream to prevent stale replies."""

    def __init__(self, port: int = 8001, timeout: float = 30):
        if not 1 <= port <= 65535 or not math.isfinite(timeout) or timeout <= 0:
            raise ValueError("Invalid Unity port or timeout")
        self.port = port
        self.timeout = timeout
        self._socket: socket.socket | None = None
        self._lock = threading.Lock()

    @staticmethod
    def _read_exact(
        stream: socket.socket, size: int, deadline: float | None = None
    ) -> bytes:
        data = bytearray()
        while len(data) < size:
            if deadline is not None:
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    raise TimeoutError("Unity request deadline expired")
                stream.settimeout(remaining)
            chunk = stream.recv(size - len(data))
            if not chunk:
                raise ConnectionError("Unity disconnected")
            data.extend(chunk)
        return bytes(data)

    def request(self, command: str, *, timeout: float | None = None, **kwargs) -> dict:
        timeout = min(self.timeout, timeout) if timeout is not None else self.timeout
        deadline = time.monotonic() + timeout
        if timeout <= 0 or not self._lock.acquire(timeout=timeout):
            raise TimeoutError("Unity request deadline expired")
        try:
            timeout = deadline - time.monotonic()
            if timeout <= 0:
                raise TimeoutError("Unity request deadline expired")
            if self._socket is None:
                self._socket = socket.create_connection(
                    ("127.0.0.1", self.port), timeout
                )
                self._socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
            self._socket.settimeout(timeout)
            data = json.dumps(
                dict(version=VERSION, command=command, **kwargs), allow_nan=False
            ).encode("utf-8")
            if len(data) > MAX_MESSAGE_BYTES:
                raise ValueError("Unity request is too large")
            self._socket.sendall(struct.pack("!I", len(data)) + data)
            (size,) = struct.unpack("!I", self._read_exact(self._socket, 4, deadline))
            if not 0 < size <= MAX_MESSAGE_BYTES:
                raise ConnectionError("Invalid Unity response length")
            response = json.loads(self._read_exact(self._socket, size, deadline))
            if response.get("version") != VERSION:
                raise ConnectionError(
                    "Unsupported Unity training protocol version "
                    f"{response.get('version')}; this adapter speaks version {VERSION}. "
                    "Update the schola Python package and the com.amd.schola Unity "
                    "package to the same release."
                )
            if not response.get("ok"):
                raise UnityError(response.get("error") or "Unity rejected the request")
            return response
        except UnityError:
            raise
        except Exception:
            self._disconnect()
            raise
        finally:
            self._lock.release()

    def _disconnect(self):
        if self._socket is not None:
            self._socket.close()
            self._socket = None

    def close(self):
        with self._lock:
            self._disconnect()


class UnityGymService(services.GymServiceServicer):
    """Translate the existing three Schola RPCs to Unity's main-thread session."""

    def __init__(self, connection: UnityConnection):
        self.connection = connection
        # Agent ids Unity declared for this session; fetched on first use.
        self._agent_ids: list[frozenset[str]] | None = None
        self._environment_count = 1

    def _request(self, context, command, **kwargs):
        try:
            return self.connection.request(
                command, timeout=context.time_remaining(), **kwargs
            )
        except TimeoutError as error:
            context.abort(grpc.StatusCode.DEADLINE_EXCEEDED, str(error))
        except UnityError as error:
            context.abort(grpc.StatusCode.FAILED_PRECONDITION, str(error))
        except (ValueError, KeyError, TypeError) as error:
            context.abort(grpc.StatusCode.INVALID_ARGUMENT, str(error))
        except (OSError, ConnectionError) as error:
            context.abort(grpc.StatusCode.UNAVAILABLE, str(error))

    def StartGymConnector(self, request, context):
        # A new session may be a different Unity scene with different agents.
        self._agent_ids = None
        response = self._request(context, "start", autoreset=request.autoreset_type)
        count = response.get("environmentCount", 1)
        if type(count) is not int or count < 1:
            context.abort(
                grpc.StatusCode.FAILED_PRECONDITION, "Invalid Unity environment count"
            )
        self._environment_count = count
        return connector.GymConnectorStartResponse()

    def _validate_environment_ids(self, environments, context):
        if (
            not isinstance(environments, list)
            or len(environments) != self._environment_count
            or any(
                not isinstance(item, dict)
                or type(item.get("id")) is not int
                or item["id"] != i
                for i, item in enumerate(environments)
            )
        ):
            context.abort(
                grpc.StatusCode.FAILED_PRECONDITION, "Invalid Unity environment ids"
            )

    @staticmethod
    def _agent_names(entries, context):
        if (
            not isinstance(entries, list)
            or not entries
            or any(
                not isinstance(item, dict)
                or not isinstance(item.get("id"), str)
                or not item["id"].strip()
                for item in entries
            )
        ):
            context.abort(
                grpc.StatusCode.FAILED_PRECONDITION, "Invalid Unity agent ids"
            )
        names = frozenset(item["id"] for item in entries)
        if len(names) != len(entries):
            context.abort(
                grpc.StatusCode.FAILED_PRECONDITION, "Duplicate Unity agent ids"
            )
        return names

    def _definition(self, context) -> list[dict]:
        response = self._request(context, "definition")
        environments = response.get("environments")
        if environments is None:
            environments = [dict(id=0, agents=response["agents"])]
        self._validate_environment_ids(environments, context)
        self._agent_ids = [
            self._agent_names(item.get("agents"), context) for item in environments
        ]
        return environments

    def RequestTrainingDefinition(self, request, context):
        definition = definitions.TrainingDefinition()
        for item in self._definition(context):
            environment = definition.environment_definitions.add()
            for entry in item["agents"]:
                agent = environment.agent_definitions[entry["id"]]
                agent.agent_type = "Unity"
                agent.obs_space.CopyFrom(
                    _decode_tree(entry["observationSpace"], space=True)
                )
                agent.action_space.CopyFrom(
                    _decode_tree(entry["actionSpace"], space=True)
                )
        return definition

    def _environments(self, response, context):
        environments = response.get("environments")
        if environments is None and self._environment_count == 1:
            environments = [dict(response, id=0)]
        self._validate_environment_ids(environments, context)
        if self._agent_ids is not None:
            for i, environment in enumerate(environments):
                fields = (
                    ["states", "initialStates"]
                    if environment.get("hasInitial")
                    else ["states"]
                )
                for field in fields:
                    if (
                        self._agent_names(environment.get(field), context)
                        != self._agent_ids[i]
                    ):
                        context.abort(
                            grpc.StatusCode.FAILED_PRECONDITION,
                            "Unity returned unexpected agent ids",
                        )
        return environments

    @staticmethod
    def _initial(target, agent_states):
        for item in agent_states:
            agent = target.agent_states[item["id"]]
            agent.observations.CopyFrom(_decode_tree(item["observations"], space=False))
            agent.info.update(_info(item.get("info")))

    def UpdateState(self, request, context):
        if request.status == updates.CLOSED:
            self._request(context, "close")
            return states.State()
        if request.status != updates.GOOD:
            context.abort(
                grpc.StatusCode.INVALID_ARGUMENT, "Unsupported communicator status"
            )
        kind = request.WhichOneof("update")
        result = states.State()
        if kind == "reset":
            if any(
                env_id < 0 or env_id >= self._environment_count
                for env_id in request.reset.environments
            ):
                context.abort(
                    grpc.StatusCode.INVALID_ARGUMENT, "Unknown Unity environment id"
                )
            entries = []
            for env_id in range(self._environment_count):
                entry = dict(id=env_id)
                settings = request.reset.environments.get(env_id)
                if settings is not None:
                    entry.update(
                        hasSeed=settings.HasField("seed"),
                        seed=settings.seed,
                        options=[
                            dict(key=key, value=value)
                            for key, value in settings.options.items()
                        ],
                    )
                entries.append(entry)
            if self._environment_count == 1:
                kwargs = {
                    key: value for key, value in entries[0].items() if key != "id"
                }
            else:
                kwargs = dict(environments=entries)
            response = self._request(context, "reset", **kwargs)
            for env_id, entry in enumerate(self._environments(response, context)):
                self._initial(
                    result.initial_state.environment_states[env_id], entry["states"]
                )
        elif kind == "step":
            if self._agent_ids is None:
                self._definition(context)
            if len(request.step.environments) != self._environment_count:
                context.abort(
                    grpc.StatusCode.INVALID_ARGUMENT,
                    "Expected one step per Unity environment",
                )
            entries = []
            for env_id, environment in enumerate(request.step.environments):
                unknown = sorted(set(environment.updates) - self._agent_ids[env_id])
                if unknown:
                    context.abort(
                        grpc.StatusCode.INVALID_ARGUMENT,
                        f"Unknown Unity agent id: {', '.join(unknown)}",
                    )
                try:
                    actions = [
                        dict(
                            id=agent_id,
                            action=_encode_point(environment.updates[agent_id]),
                        )
                        for agent_id in sorted(environment.updates)
                    ]
                except ValueError as error:
                    context.abort(grpc.StatusCode.INVALID_ARGUMENT, str(error))
                entries.append(dict(id=env_id, actions=actions))
            kwargs = (
                dict(actions=entries[0]["actions"])
                if self._environment_count == 1
                else dict(environments=entries)
            )
            response = self._request(context, "step", **kwargs)
            for env_id, entry in enumerate(self._environments(response, context)):
                environment = result.training_state.environment_states.add()
                for item in entry["states"]:
                    agent = environment.agent_states[item["id"]]
                    agent.observations.CopyFrom(
                        _decode_tree(item["observations"], space=False)
                    )
                    agent.reward = item["reward"]
                    agent.terminated = item["terminated"]
                    agent.truncated = item["truncated"]
                    agent.info.update(_info(item.get("info")))
                if entry.get("hasInitial"):
                    self._initial(
                        result.initial_state.environment_states[env_id],
                        entry["initialStates"],
                    )
        else:
            context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Expected reset or step")
        return result


class UnityBridgeServer:
    """Context-managed gRPC adapter bound to loopback with local credentials.

    Parameters
    ----------
    unity_port : int
        Port of the Unity TrainingServer component.
    port : int
        Python gRPC listening port; zero chooses an available port.
    timeout : float
        Maximum time to wait for Unity per request, in seconds.
    """

    def __init__(self, unity_port: int = 8001, port: int = 8000, timeout: float = 30):
        if not 0 <= port <= 65535:
            raise ValueError("Invalid gRPC port")
        self.connection = UnityConnection(unity_port, timeout)
        self._executor = ThreadPoolExecutor(max_workers=1)
        self.server = grpc.server(
            self._executor,
            options=[
                ("grpc.max_receive_message_length", MAX_MESSAGE_BYTES),
                ("grpc.max_send_message_length", MAX_MESSAGE_BYTES),
            ],
        )
        services.add_GymServiceServicer_to_server(
            UnityGymService(self.connection), self.server
        )
        self.port = self.server.add_secure_port(
            f"127.0.0.1:{port}", grpc.local_server_credentials()
        )
        if not self.port:
            self._executor.shutdown(wait=False)
            raise OSError("Could not bind Unity gRPC adapter")

    def __enter__(self):
        self.server.start()
        return self

    def __exit__(self, *_):
        self.server.stop(0).wait()
        self._executor.shutdown(wait=True)
        self.connection.close()
