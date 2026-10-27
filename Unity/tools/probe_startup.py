# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""Call the startup spike through Schola's existing Python client (no trainer required)."""

import argparse
from pathlib import Path
import sys

# Use this checkout without installing the unrelated ONNX/training dependencies.
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "Resources/python"))

import grpc
from schola.core.protocols.base_protocol import AutoResetType
from schola.core.protocols.protobuf.grpc_protocol import GrpcProtocol
from schola.generated import GymConnector_pb2, StateUpdates_pb2


def probe(port, timeout):
    protocol = GrpcProtocol(
        "127.0.0.1",
        port,
        credential_mode="insecure",
        environment_start_timeout=timeout,
        grpc_close_timeout=timeout,
    )
    protocol.start()
    try:
        for mode in AutoResetType:
            protocol.send_startup_msg(mode)
            print(f"PASS startup: {mode.value}", flush=True)
        try:
            protocol.gym_stub.StartGymConnector(
                GymConnector_pb2.GymConnectorStartRequest(autoreset_type=99),
                timeout=timeout,
            )
        except grpc.RpcError as error:
            if error.code() != grpc.StatusCode.INVALID_ARGUMENT:
                raise
            print("PASS invalid reset mode rejected", flush=True)
        else:
            raise AssertionError("Invalid reset mode was accepted")
        try:
            protocol.gym_stub.RequestTrainingDefinition(
                GymConnector_pb2.TrainingDefinitionRequest(), timeout=timeout
            )
        except grpc.RpcError as error:
            if error.code() != grpc.StatusCode.UNIMPLEMENTED:
                raise
            print("PASS definitions explicitly unimplemented", flush=True)
        else:
            raise AssertionError("Spike unexpectedly implements definitions")
        for status in (StateUpdates_pb2.CLOSED, StateUpdates_pb2.ERROR):
            protocol.gym_stub.UpdateState(
                StateUpdates_pb2.StateUpdate(status=status), timeout=timeout
            )
        print("PASS close/error acknowledgements", flush=True)
    finally:
        protocol.close()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=50051)
    parser.add_argument("--timeout", type=float, default=5)
    parser.add_argument(
        "--repeats",
        type=int,
        default=2,
        help="Reconnect without restarting the server.",
    )
    args = parser.parse_args()
    for index in range(args.repeats):
        probe(args.port, args.timeout)
        print(f"PASS session {index + 1}", flush=True)
