# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

"""Commands for connecting and training a running Unity environment."""

from pathlib import Path

from cyclopts import App

app = App(name="unity", help="Connect and train Unity environments through Schola.")


@app.command
def bridge(unity_port: int = 8001, port: int = 8000, timeout: float = 30):
    """Expose Unity to existing Schola trainers; stop with Ctrl+C."""
    from schola.unity.bridge import UnityBridgeServer

    with UnityBridgeServer(unity_port, port, timeout) as adapter:
        print(f"Schola Unity adapter listening on 127.0.0.1:{adapter.port}", flush=True)
        try:
            adapter.server.wait_for_termination()
        except KeyboardInterrupt:
            pass


@app.command
def train(
    unity_port: int = 8001,
    timesteps: int = 10000,
    checkpoint: Path = Path("ckpt/unity_ppo"),
    seed: int = 0,
    timeout: float = 30,
):
    """Train PPO using Schola's existing SB3 environment and save a checkpoint.

    Start Unity Play Mode with a TrainingServer first. Continuous action spaces
    must have finite bounds (set TranslateActuator.MaxDisplacement above zero).
    """
    if timesteps < 1:
        raise ValueError("timesteps must be positive")
    import gymnasium as gym
    import numpy as np
    from stable_baselines3 import PPO
    from stable_baselines3.common.vec_env import VecMonitor

    from schola.core.protocols.protobuf.grpc_protocol import GrpcProtocol
    from schola.core.simulators.external_simulator import ExternalSimulator
    from schola.sb3.env import VecEnv
    from schola.sb3.utils import VecMergeDictActionWrapper
    from schola.unity.bridge import UnityBridgeServer

    with UnityBridgeServer(unity_port, port=0, timeout=timeout) as adapter:
        env = VecEnv(ExternalSimulator(), GrpcProtocol("127.0.0.1", adapter.port))
        try:
            if isinstance(env.action_space, gym.spaces.Dict):
                env = VecMergeDictActionWrapper(env)
            if isinstance(env.action_space, gym.spaces.Box) and (
                not np.isfinite(env.action_space.low).all()
                or not np.isfinite(env.action_space.high).all()
            ):
                raise ValueError(
                    "Set finite action bounds, e.g. TranslateActuator.MaxDisplacement = 0.1"
                )
            env = VecMonitor(env)
            policy = (
                "MultiInputPolicy"
                if isinstance(env.observation_space, gym.spaces.Dict)
                else "MlpPolicy"
            )
            model = PPO(policy, env, n_steps=128, batch_size=64, seed=seed, verbose=1)
            model.learn(total_timesteps=timesteps)
            checkpoint.parent.mkdir(parents=True, exist_ok=True)
            model.save(checkpoint)
            print(f"Saved Unity policy to {checkpoint}", flush=True)
        finally:
            env.close()


if __name__ == "__main__":
    app()
