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
