# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

"""Tests schola unity train --export-onnx without Unity, using a fake Unity connection."""

import contextlib

import gymnasium as gym
import numpy as np
import onnx
import pytest
from stable_baselines3 import PPO
from stable_baselines3.common.vec_env import DummyVecEnv

import schola.core.protocols.protobuf.grpc_protocol as grpc_protocol
import schola.sb3.env as sb3_env
import schola.unity.bridge as bridge
from schola.scripts.unity import train


class _ReachTarget(gym.Env):
    def __init__(self):
        box = gym.spaces.Box(-10.0, 10.0, shape=(3,), dtype=np.float32)
        self.observation_space = gym.spaces.Dict(
            {"agent_position": box, "target_position": box}
        )
        self.action_space = gym.spaces.Dict(
            {"translate": gym.spaces.Box(-0.1, 0.1, shape=(3,), dtype=np.float32)}
        )
        self.agent = np.ones(3, np.float32)

    def _obs(self):
        return {"agent_position": self.agent, "target_position": np.zeros(3, np.float32)}

    def reset(self, *, seed: int | None = None, options: dict | None = None):
        super().reset(seed=seed)
        self.agent = np.ones(3, np.float32)
        return self._obs(), {}

    def step(self, action):
        self.agent = (self.agent + np.asarray(action, np.float32)).astype(np.float32)
        return self._obs(), -float(np.linalg.norm(self.agent)), False, False, {}


# Replaces the Unity connection with _ReachTarget so train runs without Unity.
@pytest.fixture
def fake_unity(monkeypatch):
    @contextlib.contextmanager
    def fake_bridge(unity_port, port=0, timeout=30):
        yield type("Adapter", (), {"port": 0})()

    monkeypatch.setattr(bridge, "UnityBridgeServer", fake_bridge)
    monkeypatch.setattr(grpc_protocol, "GrpcProtocol", lambda *args, **kwargs: None)
    monkeypatch.setattr(sb3_env, "VecEnv", lambda *args: DummyVecEnv([_ReachTarget]))


def _io_names(path):
    model = onnx.load(str(path))
    return [i.name for i in model.graph.input], [o.name for o in model.graph.output]


def test_export_onnx_names_inputs_and_outputs_after_unity_keys(fake_unity, tmp_path):
    checkpoint = tmp_path / "unity_ppo"

    train(timesteps=128, checkpoint=checkpoint, export_onnx=True)

    assert (tmp_path / "unity_ppo.zip").exists()
    inputs, outputs = _io_names(tmp_path / "unity_ppo.onnx")
    assert sorted(inputs) == ["agent_position", "target_position"]
    assert outputs == ["translate"]


def test_checkpoint_remembers_unmerged_action_space_for_sb3_export(fake_unity, tmp_path):
    train(timesteps=128, checkpoint=tmp_path / "unity_ppo")

    model = PPO.load(tmp_path / "unity_ppo.zip", device="cpu")
    action_space = getattr(model, "__unmerged_action_space", None)
    assert isinstance(action_space, gym.spaces.Dict)
    assert list(action_space.spaces) == ["translate"]
