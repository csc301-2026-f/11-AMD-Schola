# Individual Contributions - D2

# **Anwar Khan**

**Role**: Implemented and tested Schola's Unity ONNX inference policy for running trained agents with Unity Inference Engine.

- **Unity package setup:** created the `com.amd.schola` package skeleton (`package.json`, `Runtime/`, `Inference/`, `Training/`) that the team builds on.
- **US5 policy (PR #8):** built `SentisPolicy`, which loads a Schola-exported ONNX model with Unity's Inference Engine and turns agent observations into actions. Added `IPolicy`, `InteractionDefinition`, `DiscreteSpace`, `DiscretePoint` and the `AMD.Schola.Inference` assembly.
- **Test model:** trained a CartPole PPO model with Stable-Baselines3 and exported it with Schola's ONNX exporter for the policy tests.
- **Tests:** 8 EditMode tests in `SentisPolicyTests` covering correct actions from the trained model and clear errors for mismatched keys, shapes and value counts.
- **Reviews:** requested changes on PR #11 (US5 inference stepper) before merging it; merged PRs #3, #13 and #16.

# **Fahad Moinuddin**

**Role**: Implemented the Unity agent’s I/O and local training loop (US1–US3): sensors that turn the scene into a keyed observation, actuators that apply a policy-shaped action, and the environment `Step` / `Reset` cycle that calls both.

- **US1 define agent inputs (PR #2):** added `ISensor` (`InitSensor` / `GetObservationSpace` / `CollectObservations`) and `PositionSensor` (`Runtime/Sensors/`), the only Unity sensor today. It reads world position into a 3-float `BoxPoint` under the key `"position"`. `ScholaAgent.Observe` / `Define` (`Runtime/Agent/ScholaAgent.cs`) aggregate attached sensors into a stable-keyed `DictPoint` / `DictSpace`. Shared types: `Point`, `BoxPoint`, `DictPoint` (`Runtime/Points/`); `BoxSpace`, `DictSpace` (`Runtime/Spaces/`); `InteractionDefinition` (`Runtime/Common/`) publishes the obs and action spaces. The inspector still stores `List<PositionSensor>`, not `List<ISensor>`.
- **US2 apply agent actions (PR #10):** added `IActuator` (`InitActuator` / `GetActionSpace` / `TakeAction`) and `TranslateActuator` (`Runtime/Actuators/`), the only Unity actuator today (key `"translate"`). It applies a 3-float `BoxPoint` delta to `Transform.position`. `ScholaAgent.Act` / `DefineActionSpace` fan a policy-shaped `DictPoint` out by `ActionKey`. Invalid actions throw a clear error; all keys are checked before any actuator runs, so a missing key leaves the scene unchanged. Local inference uses this same `Act` path via `SimpleStepper` → `IPolicy.Think` → `Act`. The inspector still stores `List<TranslateActuator>`, not `List<IActuator>`.
- **US3 control training episodes (PR #15):** added `IEnvironment` / `ScholaEnvironment` (`Runtime/Environment/`) with `Initialize` / `Step` / `Reset`, plus `IReward` (`ComputeReward` / `OnEpisodeReset`) and `IEpisodeStatus` (`GetStatus(terminated, truncated)` / `OnEpisodeReset`). `Step` runs Act (US2) → reward → status → Observe (US1) and returns `AgentState`; `Reset` restores the initialize pose snapshot (including actuators outside the agent hierarchy), notifies episode hooks, and returns `InitialAgentState` (`Runtime/TrainingDataTypes/`). `ITrainingStateSink` is a US4 placeholder used in-memory in tests and left null in production. Unity-local episode control is in place; there is no Unity gRPC server yet, so Python never sees that state.
- **Tests:** 3 EditMode tests in `PositionSensorObservationTests` and `ScholaAgentObserveTests` (observations update with the scene; stable dict keys and space format); 10 in `TranslateActuatorTests` and `ScholaAgentActTests` (valid translate, wrong type/shape, missing keys, no half-apply, duplicate action keys); 18 in `ScholaEnvironmentLifecycleTests`, `ScholaEnvironmentResetTests`, `ScholaEnvironmentRewardTests`, `ScholaEnvironmentTerminationTests`, and `AgentStateTests` (published spaces, Reset → Step → Reset, snapshot restore, independent terminated/truncated flags, sink reporting).
- **Reviews:** opened PRs #2 (US1), #10 (US2), and #15 (US3).

# **Raahim Chaghtai**

**Role**: Implemented and tested Schola's Unity inference stepper (US5), which repeats the observe, think, act loop for agents running a trained policy, and contributed to the CI/CD pipeline.

- **US5 inference stepper (PR #11):** added `IAgent` (`Observe` / `Act`), `IStepper` (`Init` / `Step`) and `SimpleStepper`, which collects every agent's observation first, asks the policy for one action per agent, then applies the actions. If the policy fails for any agent, nobody acts that step.
- **Step runner:** added `StepperRunner`, a MonoBehaviour that runs a stepper every N frames and stops, with a logged error, when a step fails or throws instead of repeating the error every frame.
- **Review fixes:** after review on PR #11, the stepper now keeps its own copy of the agent list, skips agents whose GameObjects were destroyed, and the runner stops on exceptions; documented that the caller owns and disposes the policy.
- **Connecting agents to the stepper (PR #13):** made `ScholaAgent` implement `IAgent` so a real agent can be driven by the stepper.
- **Tests:** 12 EditMode tests in `SimpleStepperTests` (input validation, observe-before-act ordering, per-agent actions, policy failure, repeated steps, list changes, destroyed agents) and 3 in `ScholaAgentSteppingTests`, which run a real `ScholaAgent` with `PositionSensor` and `TranslateActuator` through the stepper.
- **CI/CD:** implemented the Python regression-test subset workflow (Task 5, PR #7) and the release workflow with a tag and package-version check, with tests (Task 8, PR #14).
- **Reviews:** reviewed and merged PRs #4, #6, #8, #9, #10 and #12 (CI/CD Tasks 3, 4, 6 and 7, the US5 Sentis policy, and US2).

# **Yahyaa Yasin**

**Role**: Training communication lead; built the CI entry point and the end-to-end smoke-test workflow so Unity and Python can be checked together later.

- **CI entry point (Task 1, PR #1):** implemented `.github/workflows/ci.yml` with path-based change detection (`dorny/paths-filter`), so Unity, Python, and proto jobs only run when their areas change; added the `ci-ok` gate that fails if any required job failed or was cancelled. Manual `workflow_dispatch` runs every job.
- **CI scaffolding:** set up the shared CI/CD work breakdown and placeholder workflows/scripts so teammates could own Tasks 2–8 without editing the same files.
- **End-to-end smoke test (Task 7, PR #9):** implemented `smoke-test.yml` and `.github/scripts/unity_smoke_test.py`. The `mock` job always drives AMD’s CartPole mock gRPC server with Schola’s Python client (`GymVectorEnv` + insecure `GrpcProtocol`). `detect` / `build` / `run` stay skipped until `Unity/SmokeTestProject` exists, then turn on without workflow rewrites. Added a `smoke` path filter in `ci.yml` so script-only changes do not also trigger the Unity license job.
- **Hand-off notes for training integration:** documented in `Unity/com.amd.schola/Training/README.md` and related CI comments that Unity must parse `-ScholaPort=<port>` (same as Unreal) and that `check_meta_files.py` must skip project `ProjectSettings/` / `Packages/` before committing a smoke-test project.
- **US6 (in progress):** owning the rest of setup integration on `US6-setup-integration` — merged `main`; finishing Editor validation and the guide/sample train → trained-model path.
- **Planning and partner process:** wrote team roles and meeting cadence (Q7 & Q8) in `deliverables/D1/planning.md`, and recorded partner/kickoff meeting minutes under `deliverables/team/minutes`.
- **D2 demo support:** prepared and walked the EditMode test demo path (observe → act → ONNX policy → stepper) for partner review once the package was installed locally.

# **Talha Asif**

**Role**: Implemented Schola’s Unity-to-Python training integration (US4, currently awaiting review) and contributed the CI checks, Unity test workflow, and integration of the team’s CI/CD pipeline.

- **US4 train through Schola ([PR #17](https://github.com/csc301-2026-f/11-AMD-Schola/pull/17), open):** added `TrainingServer`, `TrainingSession`, and `TrainingWire` under `Unity/com.amd.schola/Training/`. These connect US3’s environment lifecycle to Python, sending observations, rewards, and episode status to the trainer and applying returned actions. Network I/O runs in the background; environment operations run on Unity’s main thread. Added action validation, message-size limits, timeouts, disconnect handling, and shutdown cancellation.
- **Python training bridge:** implemented `schola.unity.bridge`, which exposes Schola’s existing protobuf `GymService` over local gRPC and communicates with Unity through length-prefixed JSON. This reuses the existing Python clients and Stable-Baselines3 wrappers without requiring a native Unity gRPC dependency. Supports `Box`/FLOAT32, `Dict`, and `Discrete` spaces, plus same-step, next-step, and explicit reset modes while preserving terminal observations.
- **Training commands and sample:** added `schola unity train` for PPO training and checkpoint saving, and `schola unity bridge` for separately managed trainers. Created an importable **Training Demo** with a scene builder, reward and episode-status hooks, and a training guide. Added optional finite action bounds to `TranslateActuator` for PPO. The current implementation supports one agent/environment per endpoint with synchronous Transform-based stepping.
- **Tests:** added Unity training-session and transport tests, Python adapter tests, and optional live Unity integration tests. PR #17 records **65 passing Unity EditMode tests across the package** and **17 passing Python adapter/live tests**, covering communication, episode boundaries, reset modes, and checkpoint save/reload. Also verified the installed training command against a real Unity Editor host for 128 timesteps.
- **CI fast checks (Task 2, [PR #3](https://github.com/csc301-2026-f/11-AMD-Schola/pull/3)):** implemented CSharpier formatting checks, missing/orphaned Unity `.meta` validation, package-manifest validation, and Conventional Commit PR-title checks. Added seven validator regression tests, pinned the formatter version, and documented contribution conventions.
- **Contributor workflow (Task 3, [PR #4](https://github.com/csc301-2026-f/11-AMD-Schola/pull/4); [PR #5](https://github.com/csc301-2026-f/11-AMD-Schola/pull/5)):** added the Unity testing checklist to the PR template and formatted six existing C# files to resolve CI failures blocking other contributions.
- **Unity test automation (Task 4, [PR #6](https://github.com/csc301-2026-f/11-AMD-Schola/pull/6)):** implemented the GameCI EditMode workflow, pinned Unity `6000.3.24f1`, configured test-result checks and coverage reports, and added artifact uploads that run even when tests fail.
- **CI/CD integration ([PR #16](https://github.com/csc301-2026-f/11-AMD-Schola/pull/16)):** brought all eight teammates’ CI/CD tasks together into `main`, including fast checks, Unity and Python tests, protobuf-generation verification, smoke tests, and versioned releases. Verified that every task branch was included and documented remaining prerequisites for licensed Unity runs, the smoke-test player, and release publishing.

# **Mohammad Mahi Ali Mukati**

**Role**: CI/CD code generation checks, UML diagrams, D2 demo, documentation, and led partner communication.

- **CI/CD (Task 6, [PR #12](https://github.com/csc301-2026-f/11-AMD-Schola/pull/12)):** wrote a script that generates C# files from the project's protobuf definitions and added it to CI. Once the generated files are committed, CI checks that they stay up to date with those definitions.
- **Tests and instructions:** added nine tests to check that the script catches missing or outdated files and handles failures without damaging existing files. Added instructions for running it locally to the Unity training README.
- **Merge support:** merged [PR #5](https://github.com/csc301-2026-f/11-AMD-Schola/pull/5), which fixed Unity C# formatting issues.
- **UML:** created component/class and episode-sequence diagrams explaining the Unity agent workflow for US1–US3.
- **Demo:** created a [Return to Base](demo/ReturnToBaseGame.cs) demo scene, connecting agents, sensors, actuators, and the inference stepper to the ONNX model. Created a simple interactive scene with click, drag, and scatter controls to control drones returning to a base point.
- **Setup and documentation:** set up the demo in Unity and wrote the [README](demo/README.md) with run instructions, troubleshooting, and three gameplay screenshots, with the help of GPT-6 Astra.
- **Video:** recorded a [setup and gameplay walkthrough](https://youtu.be/utxF_vXdSw8) and linked it in the README.
- **Partner communication:** coordinated all communication with Alex on the AMD team.
