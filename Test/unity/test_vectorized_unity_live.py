# Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

"""Real Unity regression cases for TrainingIntegrationHost with SCHOLA_TEST_UNITY_ENVS=2."""

import os

import grpc
import numpy as np
import pytest

from schola.core.protocols.base_protocol import AutoResetType
from schola.core.protocols.protobuf.grpc_protocol import GrpcProtocol
from schola.core.simulators.external_simulator import ExternalSimulator
from schola.unity.bridge import UnityBridgeServer

pytestmark = pytest.mark.skipif(
    not os.environ.get("SCHOLA_TEST_UNITY_PORT")
    or os.environ.get("SCHOLA_TEST_UNITY_ENVS") != "2",
    reason="Two-environment Unity host not requested",
)


@pytest.fixture
def adapter():
    with UnityBridgeServer(int(os.environ["SCHOLA_TEST_UNITY_PORT"]), port=0) as server:
        yield server


@pytest.mark.parametrize(
    "mode", [AutoResetType.SAME_STEP, AutoResetType.NEXT_STEP, AutoResetType.DISABLED]
)
def test_real_unity_independent_termination_truncation_and_resets(adapter, mode):
    protocol = GrpcProtocol("127.0.0.1", adapter.port)
    protocol.start()
    try:
        protocol.send_startup_msg(mode)
        ids, _, _, spaces = protocol.get_definition()
        assert ids == [["SingleAgent"], ["SingleAgent"]]
        protocol.send_reset_msg()
        pending = [False, False]
        positions = [0.0, 0.0]
        steps = [0, 0]
        for _ in range(12):
            actions = {
                i: (
                    {}
                    if pending[i]
                    else {
                        "SingleAgent": {
                            "translate": np.array(
                                [0.1 if i == 0 else 0, 0, 0], dtype=np.float32
                            )
                        }
                    }
                )
                for i in range(2)
            }
            observations, rewards, terminated, truncated, _, initial, _ = (
                protocol.send_action_msg(actions, spaces[0])
            )
            initial_ids = set()
            for i in range(2):
                reset_step = pending[i] and mode == AutoResetType.NEXT_STEP
                frozen = pending[i] and mode == AutoResetType.DISABLED
                if reset_step:
                    positions[i] = 0
                    steps[i] = 0
                elif not frozen:
                    positions[i] += 0.1 if i == 0 else 0
                    steps[i] += 1
                done = steps[i] >= (2 if i == 0 else 4)
                np.testing.assert_allclose(
                    observations[i]["SingleAgent"]["position"],
                    [positions[i], 0, i],
                    atol=1e-6,
                )
                assert rewards[i]["SingleAgent"] == pytest.approx(positions[i])
                assert terminated[i]["SingleAgent"] == (done and i == 0)
                assert truncated[i]["SingleAgent"] == (done and i == 1)
                if done and mode == AutoResetType.SAME_STEP:
                    initial_ids.add(i)
                    np.testing.assert_allclose(
                        initial[i]["SingleAgent"]["position"], [0, 0, i]
                    )
                    positions[i] = 0
                    steps[i] = 0
                pending[i] = done and mode != AutoResetType.SAME_STEP
            assert set(initial) == initial_ids
    finally:
        protocol.close()


def test_real_unity_invalid_second_action_requires_reset_before_recovery(adapter):
    protocol = GrpcProtocol("127.0.0.1", adapter.port)
    protocol.start()
    try:
        protocol.send_startup_msg()
        _, _, _, spaces = protocol.get_definition()
        protocol.send_reset_msg()
        actions = {
            i: {"SingleAgent": {"translate": np.array([x, 0, 0], dtype=np.float32)}}
            for i, x in enumerate([0.1, 2])
        }
        with pytest.raises(grpc.RpcError) as failure:
            protocol.send_action_msg(actions, spaces[0])
        assert failure.value.code() == grpc.StatusCode.FAILED_PRECONDITION
        actions[1]["SingleAgent"]["translate"][:] = 0
        with pytest.raises(grpc.RpcError):
            protocol.send_action_msg(actions, spaces[0])
        protocol.send_reset_msg()
        observations, *_ = protocol.send_action_msg(actions, spaces[0])
        np.testing.assert_allclose(
            observations[0]["SingleAgent"]["position"], [0.1, 0, 0], atol=1e-6
        )
        np.testing.assert_allclose(
            observations[1]["SingleAgent"]["position"], [0, 0, 1], atol=1e-6
        )
    finally:
        protocol.close()


def test_real_unity_sb3_keeps_reset_observations_and_terminal_observations_separate(
    adapter,
):
    pytest.importorskip("stable_baselines3")
    from schola.sb3.env import VecEnv
    from schola.sb3.utils import VecMergeDictActionWrapper

    env = VecMergeDictActionWrapper(
        VecEnv(ExternalSimulator(), GrpcProtocol("127.0.0.1", adapter.port))
    )
    try:
        assert env.num_envs == 2
        env.reset()
        actions = np.array([[0.1, 0, 0], [0, 0, 0]], dtype=np.float32)
        for tick in range(1, 5):
            observations, _, done, info = env.step(actions)
            assert list(done) == [tick % 2 == 0, tick == 4]
            if done[0]:
                np.testing.assert_allclose(
                    info[0]["terminal_observation"]["position"], [0.2, 0, 0], atol=1e-6
                )
                np.testing.assert_allclose(observations["position"][0], [0, 0, 0])
                assert not info[0]["TimeLimit.truncated"]
            if done[1]:
                assert info[1]["TimeLimit.truncated"]
                np.testing.assert_allclose(observations["position"][1], [0, 0, 1])
    finally:
        env.close()


def test_real_unity_vectorized_ppo_saves_reloadable_checkpoint(adapter, tmp_path):
    sb3 = pytest.importorskip("stable_baselines3")
    from schola.sb3.env import VecEnv
    from schola.sb3.utils import VecMergeDictActionWrapper

    env = VecMergeDictActionWrapper(
        VecEnv(ExternalSimulator(), GrpcProtocol("127.0.0.1", adapter.port))
    )
    try:
        assert env.num_envs == 2
        model = sb3.PPO(
            "MultiInputPolicy",
            env,
            n_steps=16,
            batch_size=16,
            n_epochs=1,
            seed=7,
            device="cpu",
        )
        model.learn(total_timesteps=64)
        assert model.num_timesteps == 64
        assert model._n_updates == 2
        checkpoint = tmp_path / "vectorized_unity_ppo.zip"
        model.save(checkpoint)
        loaded = sb3.PPO.load(checkpoint, env=env, device="cpu")
        action, _ = loaded.predict(env.reset(), deterministic=True)
        assert action.shape == (2, 3) and np.isfinite(action).all()
        observations, rewards, _, _ = env.step(action)
        assert observations["position"].shape == (2, 3)
        assert np.isfinite(observations["position"]).all()
        assert np.isfinite(rewards).all()
    finally:
        env.close()
