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

# **Raahim Chaghtai**

**Role**: Implemented and tested Schola's Unity inference stepper (US5), which repeats the observe, think, act loop for agents running a trained policy, and contributed to the CI/CD pipeline.

- **US5 inference stepper (PR #11):** added `IAgent` (`Observe` / `Act`), `IStepper` (`Init` / `Step`) and `SimpleStepper`, which collects every agent's observation first, asks the policy for one action per agent, then applies the actions. If the policy fails for any agent, nobody acts that step.
- **Step runner:** added `StepperRunner`, a MonoBehaviour that runs a stepper every N frames and stops, with a logged error, when a step fails or throws instead of repeating the error every frame.
- **Review fixes:** after review on PR #11, the stepper now keeps its own copy of the agent list, skips agents whose GameObjects were destroyed, and the runner stops on exceptions; documented that the caller owns and disposes the policy.
- **Connecting agents to the stepper (PR #13):** made `ScholaAgent` implement `IAgent` so a real agent can be driven by the stepper.
- **Tests:** 12 EditMode tests in `SimpleStepperTests` (input validation, observe-before-act ordering, per-agent actions, policy failure, repeated steps, list changes, destroyed agents) and 3 in `ScholaAgentSteppingTests`, which run a real `ScholaAgent` with `PositionSensor` and `TranslateActuator` through the stepper.
- **CI/CD:** implemented the Python regression-test subset workflow (Task 5, PR #7) and the release workflow with a tag and package-version check, with tests (Task 8, PR #14).
- **Reviews:** reviewed and merged PRs #4, #6, #8, #9, #10 and #12 (CI/CD Tasks 3, 4, 6 and 7, the US5 Sentis policy, and US2).
