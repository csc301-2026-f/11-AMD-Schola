# Individual Contributions - D2

# **Anwar Khan**

**Role**: Implemented and tested Schola's Unity ONNX inference policy for running trained agents with Unity Inference Engine.

- **Unity package setup:** created the `com.amd.schola` package skeleton (`package.json`, `Runtime/`, `Inference/`, `Training/`) that the team builds on.
- **US5 policy (PR #8):** built `SentisPolicy`, which loads a Schola-exported ONNX model with Unity's Inference Engine and turns agent observations into actions. Added `IPolicy`, `InteractionDefinition`, `DiscreteSpace`, `DiscretePoint` and the `AMD.Schola.Inference` assembly.
- **Test model:** trained a CartPole PPO model with Stable-Baselines3 and exported it with Schola's ONNX exporter for the policy tests.
- **Tests:** 8 EditMode tests in `SentisPolicyTests` covering correct actions from the trained model and clear errors for mismatched keys, shapes and value counts.
- **Reviews:** requested changes on PR #11 (US5 inference stepper) before merging it; merged PRs #3, #13 and #16.

# **Fahad Moinuddin**

**Role**: Implemented Agent inputs(US1), actions(US2), and added control for training episodes functionality(US3)

- **Define Agent Inputs (US1):** Developed standardized agent inputs, sensors, and stable-keyed observation aggregation for consistent and fresh scene data Unity UPM package com.amd.schola.
- **Apply Agent Actions (US2):** Added functionaluty for agents to apply policy-shaped actions to scene actuators, and reject invalid actions mirroring US1 sensor pattern (IActuator + TranslateActuator + ScholaAgent.Act / DefineActionSpace).
- **Control training episodes:** Introduced a single-agent ScholaEnvironment in Unity for local RL training, managing rewards, episode lifecycles, and in-memory protocol-shaped state reporting without external wire protocols.
- **Reviews:** Created 3 PR's for define agent inputs US1, apply agent actions US2, and control training episodes US3.
