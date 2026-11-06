# Schola for Unity (Team 11, Oscorp) - Individual Contributions (D3)

> For each team member: name, role, and specific contributions. Be precise about what you did, not how much you wrote.

**Raahim Chaghtai**, Inference stepper (US5) and CI/CD

> - **CI/CD.** Added `timeout-minutes` to every GitHub Actions job that had none (`checks`, `changes`, `ci-ok`, `detect`) in PR [#22](https://github.com/csc301-2026-f/11-AMD-Schola/pull/22). I then tightened the limits to fit real run times, so the Unity tests now stop after 8 minutes (down from 30), the Python tests, checks and mock smoke test after 5, and the Unity smoke-test build and run jobs after 20 and 10 (down from 45 and 20).
> - **Documentation.** Rewrote `README.md` for D3 (what's new, train-and-run guide, configuration table, deployment justification, non-functional requirements and D2 feedback table) and wrote `deliverables/D3/product-evolution.md`.
> - **Reviews.** Reviewed and approved PR [#20](https://github.com/csc301-2026-f/11-AMD-Schola/pull/20), which restores the US4 training connection, after checking that its code was identical to the reverted PR #17 and that CI was green, and then merged it.
> - **Presentation.** Presented the training-connection slide in the deliverable presentation.

**Fahad Moinuddin**, Architecture UML (US1–US4)
> - **Architecture.** Authored `Unity/com.amd.schola/Documentation~/architecture/us1-us4-uml.md`, a Mermaid UML architecture doc covering system context, agent composition (US1–US2), the episode environment (US3), dual-loop sequences for training vs inference (US3 vs US5), protocol nesting vs Unity cardinality (US3–US4), the US4 `GymService` target and Unreal delta (including reverted/spike paths), and a type inventory that marks each type as implemented, placeholder, adjacent, Unreal, or not on `main`.
> - **Planning.** Created 5 tasks on the team Trello from the partner team meeting with AMD and assigned them to teammates.

**Anwar Khan**, Running trained policies in Unity (US5)

> - **PolicyRunner (PR [#21](https://github.com/csc301-2026-f/11-AMD-Schola/pull/21)).** Added a component that runs a trained ONNX model on agents without Python. You assign the agents and the model and press Play. If the model is missing or doesn't fit the agent, it logs a clear error.
> - **Action clamping.** Exported models output values around ±2 while the actuator allows ±0.1, so `SentisPolicy` now clamps actions to their bounds, like Stable-Baselines3 does in training.
> - **ONNX export.** Added `--export-onnx` and `--onnx-path` to `schola unity train`, and made sure the exported model uses the same input and output names as the agent's sensors and actuators so Unity can load it.
> - **Tests.** 11 Unity tests (clamping and `PolicyRunner`, including a trained model reaching its target) and 2 Python tests for the export.
> - **Demo.** Recorded the [D3 walkthrough video](https://www.youtube.com/watch?v=TPu1wXgE_mE) and made the D3 slides.
