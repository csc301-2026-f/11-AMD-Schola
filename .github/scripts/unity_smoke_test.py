# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
"""End-to-end smoke test: drive a running Schola gRPC server with Schola's Python client.

Run by smoke-test.yml against AMD's mock CartPole server and, once it exists, the
headless Unity build:
``python .github/scripts/unity_smoke_test.py --port 50051``

Kept out of Test/ on purpose: pytest.ini collects everything under Test/, and this script
needs a running server.
"""

import argparse
import sys
import traceback

import grpc
import numpy as np

from schola.core.protocols.protobuf.grpc_protocol import GrpcProtocol
from schola.core.simulators.external_simulator import ExternalSimulator
from schola.gym.env import GymVectorEnv


class SmokeTestFailure(Exception):
    pass


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--host", default="localhost")
    parser.add_argument("--port", type=int, default=50051)
    parser.add_argument("--steps", type=int, default=500)
    parser.add_argument(
        "--timeout",
        type=int,
        default=120,
        help="Seconds to wait for the server to accept the startup message.",
    )
    parser.add_argument("--min-episodes", type=int, default=1)
    parser.add_argument("--seed", type=int, default=0)
    return parser.parse_args(argv)


def describe(value: object) -> str:
    if isinstance(value, np.ndarray):
        return f"array(shape={value.shape}, dtype={value.dtype}, value={value})"
    return repr(value)


def check_observation(env: GymVectorEnv, obs: object, where: str) -> None:
    if not env.observation_space.contains(obs):
        raise SmokeTestFailure(
            f"{where}: observation {describe(obs)} is not in observation space "
            f"{env.observation_space}"
        )


def run(env: GymVectorEnv, steps: int, seed: int) -> tuple[int, list[float]]:
    env.action_space.seed(seed)
    obs, _ = env.reset(seed=seed)
    check_observation(env, obs, "reset")

    # Same-step autoreset: when an agent's episode ends, the server resets it and the
    # returned observation already belongs to the next episode.
    episode_returns: list[float] = []
    running_returns = np.zeros(env.num_envs, dtype=np.float64)
    for step in range(1, steps + 1):
        obs, rewards, terminated, truncated, _ = env.step(env.action_space.sample())
        check_observation(env, obs, f"step {step}")
        rewards = np.asarray(rewards, dtype=np.float64)
        if not np.all(np.isfinite(rewards)):
            raise SmokeTestFailure(f"step {step}: rewards {rewards} are not all finite")
        running_returns += rewards
        for slot in np.flatnonzero(np.logical_or(terminated, truncated)):
            episode_returns.append(float(running_returns[slot]))
            running_returns[slot] = 0.0
    return steps, episode_returns


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    address = f"{args.host}:{args.port}"
    print(f"Connecting to Schola server at {address} (timeout {args.timeout}s) ...")

    env = None
    try:
        protocol = GrpcProtocol(
            url=args.host,
            port=args.port,
            environment_start_timeout=args.timeout,
            credential_mode="insecure",
        )
        env = GymVectorEnv(ExternalSimulator(), protocol)
        print(f"Connected to {env.num_envs} agent slot(s).")
        print(f"Observation space: {env.single_observation_space}")
        print(f"Action space: {env.single_action_space}")

        steps, episode_returns = run(env, args.steps, args.seed)
        if len(episode_returns) < args.min_episodes:
            raise SmokeTestFailure(
                f"only {len(episode_returns)} episode(s) finished in {steps} steps; "
                f"expected at least {args.min_episodes}"
            )
    except grpc.RpcError as error:
        if error.code() == grpc.StatusCode.DEADLINE_EXCEEDED and env is None:
            print(
                f"FAIL: no Schola server at {address} after {args.timeout}s",
                file=sys.stderr,
            )
        else:
            print(
                f"FAIL: gRPC error {error.code().name}: {error.details()}",
                file=sys.stderr,
            )
        return 1
    except SmokeTestFailure as error:
        print(f"FAIL: {error}", file=sys.stderr)
        return 1
    except Exception:
        traceback.print_exc()
        print("FAIL: unexpected exception (traceback above)", file=sys.stderr)
        return 1
    finally:
        if env is not None:
            env.close()

    mean_return = (
        sum(episode_returns) / len(episode_returns) if episode_returns else 0.0
    )
    print(
        f"PASS: {steps} steps, {len(episode_returns)} episode(s), "
        f"mean episode return {mean_return:.2f}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
