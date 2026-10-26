# Individual Contributions - D2

# **Anwar Khan**

**Role**: Implemented and tested Schola's Unity ONNX inference policy for running trained agents with Unity Inference Engine.

- **Unity package setup:** created the `com.amd.schola` package skeleton (`package.json`, `Runtime/`, `Inference/`, `Training/`) that the team builds on.
- **US5 policy (PR #8):** built `SentisPolicy`, which loads a Schola-exported ONNX model with Unity's Inference Engine and turns agent observations into actions. Added `IPolicy`, `InteractionDefinition`, `DiscreteSpace`, `DiscretePoint` and the `AMD.Schola.Inference` assembly.
- **Test model:** trained a CartPole PPO model with Stable-Baselines3 and exported it with Schola's ONNX exporter for the policy tests.
- **Tests:** 8 EditMode tests in `SentisPolicyTests` covering correct actions from the trained model and clear errors for mismatched keys, shapes and value counts.
- **Reviews:** requested changes on PR #11 (US5 inference stepper) before merging it; merged PRs #3, #13 and #16.
