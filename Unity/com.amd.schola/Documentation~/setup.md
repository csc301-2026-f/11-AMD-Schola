# Unity setup and adaptation guide (US6)

This guide targets **`US6-setup-integration`**, extending the existing `com.amd.schola` package. The **Setup Demo** makes a cube approach a sphere using real Schola position observations and actions. A small deterministic controller chooses displacements and calls `ScholaAgent.Act()`; `TranslateActuator` applies them. It needs no Python process or trained model. It demonstrates installation, observation collection, and action wiring; it does not demonstrate reinforcement learning, episode management, or model inference.

## Prerequisites and version evidence

| Item | Requirement and evidence |
| --- | --- |
| Unity Editor | Unity **6.3**: `package.json` declares minimum `6000.3`. The branch's `.github/workflows/unity-tests.yml` targets **6000.3.24f1**; use that exact version for reproducibility. This is a configured target, not a locally verified compatibility claim. |
| Project | A new **3D project using the Built-In Render Pipeline** is the baseline for this sample's default primitive materials. No URP/HDRP package is required. Other pipelines need their own compatible materials. |
| Unity dependencies | `com.unity.test-framework` **1.4.5**, declared in this branch's package manifest and resolved by Package Manager. |
| Git | Required to clone or install through a Git URL. Run `git --version` in PowerShell. |
| Not required | Unreal Engine, Python, ONNX models, a GPU backend, inference packages, or a training server. |

No Unity Editor was found in the implementation environment. The scene is therefore created by Unity in your Editor using the included builder, rather than supplied as hand-written, unverified scene YAML. See [validation status and checklist](validation.md).

## Install the package

### Local checkout: use this to try the uncommitted implementation

1. Open your Unity 6.3 project.
2. Open **Window > Package Management > Package Manager**.
3. Choose **+ > Install package from disk…** (some Editor versions label this **Add package from disk…**).
4. Select **`Unity/com.amd.schola/package.json`** in this checkout. Do not select the repository root or the UI mockup package under `deliverables`.
5. Wait for compilation and package resolution. The package should appear as **AMD Schola 0.1.0**.

Keep the checkout in place: a disk installation references the source folder. Local edits are immediately available to Unity after recompilation. Do not install a second copy of `com.amd.schola` under `Assets` or `Packages`.

If starting from a fresh clone after these changes have been committed and pushed by the owner:

```powershell
git clone --branch US6-setup-integration https://github.com/csc301-2026-f/11-AMD-Schola.git
Set-Location .\11-AMD-Schola
Resolve-Path .\Unity\com.amd.schola\package.json
```

### Git installation: available once the owner pushes US6

Use **+ > Install package from git URL…** with:

```text
https://github.com/csc301-2026-f/11-AMD-Schola.git?path=/Unity/com.amd.schola#US6-setup-integration
```

The `?path=` selects the package subdirectory and must precede `#revision`. The repository root is an Unreal plugin, not a UPM package. For reproducible sharing, replace the branch after `#` with a commit SHA **containing the US6 changes**. The remote branch does not contain these local edits until you publish them. Nothing is pushed by this implementation.

Unity documents [Git subdirectory installation](https://docs.unity3d.com/6000.0/Documentation/Manual/upm-git.html) and [package sample registration/import](https://docs.unity3d.com/6000.0/Documentation/Manual/cus-samples.html).

## Import, create, and open the sample

1. Select **AMD Schola** in Package Manager and expand **Samples**.
2. Import **Setup Demo**. Unity copies it to **`Assets/Samples/AMD Schola/0.1.0/Setup Demo`**. `Samples~` stays inactive inside the package until imported.
3. Wait for the imported runtime and Editor assemblies to compile.
4. Choose **Tools > AMD Schola > Create Setup Demo Scene**.
5. Save as **`Assets/ScholaSetupDemo.unity`**. The command asks you to save modified scenes, creates a new scene, and saves it with Unity's own serializer. Canceling either dialog leaves your current scene alone. Stop Play Mode before running this command.
6. The saved scene is open. To reopen it later, double-click **`Assets/ScholaSetupDemo.unity`** in Project.

The builder creates **Schola Cube**, **Target**, **Ground**, **Main Camera**, and **Directional Light**. It uses Unity primitives and default materials, so there are no downloaded assets, custom shaders, or serialized external scene references to repair.

**Upgrading the earlier observation-only demo:** Package changes do not automatically update scripts already copied into `Assets/Samples`. Back up customizations, reimport **Setup Demo**, and create a new scene with the updated builder. To retain an existing scene, add a `TranslateActuator` to **Schola Cube**, set **Action Key** to `translate`, and register it in Schola Agent's **Actuators** list as shown below. An old scene without the actuator is rejected with a configuration error.

## Inspector settings

The builder assigns these automatically. The table also serves as a manual wiring recipe. Unity displays serialized `m_` fields with human-readable labels; the source field names below are authoritative.

| Object / component | Inspector label (source field) | Value |
| --- | --- | --- |
| Schola Cube / Transform | Position | `(-3, 0.5, 0)` |
| Schola Cube / Position Sensor | Observation Key (`m_ObservationKey`) | `agent_position` |
| Schola Cube / Schola Agent | Sensors (`m_Sensors`), Size | `2` |
| Same list | Element 0 | Position Sensor on **Schola Cube** |
| Same list | Element 1 | Position Sensor on **Target** |
| Schola Cube / Translate Actuator | Action Key (`m_ActionKey`) | `translate` |
| Schola Cube / Schola Agent | Actuators (`m_Actuators`), Size | `1` |
| Same list | Element 0 | Translate Actuator on **Schola Cube** |
| Target / Transform | Position / Scale | `(3, 0.5, 0)` / `(0.4, 0.4, 0.4)` |
| Target / Position Sensor | Observation Key (`m_ObservationKey`) | `target_position` |
| Schola Cube / Setup Demo Controller | Agent (`m_Agent`) | Schola Agent on **Schola Cube** |
| Same controller | Target Sensor (`m_TargetSensor`) | Position Sensor on **Target** |
| Same controller | Speed (`m_Speed`) | `2` world units per second |
| Same controller | Arrival Distance (`m_ArrivalDistance`) | `0.1` world units |

No Rigidbody is used. Colliders come from the primitives, but `TranslateActuator` changes the Transform position and does not implement collision response. Do not add a dynamic Rigidbody and expect physics-driven movement. The controller intentionally requires the two sensors in the order shown and exactly one translate actuator on the controlled object; extra sensors or actuators require adapting its validation.

## Press Play and recognize success

1. Open **Game** and **Console**, then press **Play**.
2. The Console should print **`Schola Setup Demo: deterministic Schola actions; no training or inference.`**
3. The cube should move from x = -3 toward the small sphere at x = 3, stopping within `Arrival Distance` in about three seconds at the default Speed.
4. The Game overlay should show **`agent_position`** changing and **`target_position`** remaining near `(3, 0.5, 0)`, followed by **Target reached**. Console also logs arrival once.
5. While playing, move **Target** in the Inspector to `(0, 0.5, 2)`. Its observation should change and the cube should follow it again.
6. Stop Play Mode. Normal Unity scene edits made while playing revert; press Play again to repeat the demonstration. This is not a Schola episode reset.

These are expected results awaiting Editor verification, not a claim that Play Mode was run during implementation.

## How observations and movement work

`AMD.Schola.Runtime` is the package's core assembly. A `PositionSensor` implements `ISensor`: it publishes a three-dimensional `BoxSpace`, and `CollectObservations()` reads its GameObject's **world** `Transform.position` into a `BoxPoint` containing `[x, y, z]`.

`ScholaAgent.SetSensors(...)` stores the two sensor references. `Define()` describes their values in a `DictSpace`, keyed by `ObservationKey`. `Observe()` returns current values in a matching `DictPoint`. Keys must be unique: this implementation uses dictionary assignment, so duplicate keys overwrite earlier observations.

`ScholaAgent.SetActuators(...)` registers the cube's `TranslateActuator`. `DefineActionSpace()` describes a `DictSpace` with a three-dimensional `BoxSpace` under the `translate` key. `Act(Point)` expects a `DictPoint`, looks up each actuator's `ActionKey`, and forwards the nested point to `TakeAction(Point)`. The actuator validates that the nested value is a three-float `BoxPoint`, then adds that world-space displacement to its Transform.

The sample lives in a separate imported assembly, `AMD.Schola.Samples.SetupDemo`. On `Start`, its controller validates scene references and keys, calls each sensor's `InitSensor()` and actuator's `InitActuator()`, and calls `Define()` and `DefineActionSpace()`. Each `Update` then calls `Step(Time.deltaTime)`:

```csharp
var observation = (DictPoint)m_Agent.Observe();
var current = ReadPosition(observation, AgentPositionKey);
var target = ReadPosition(observation, TargetPositionKey);
```

The controller uses the observed positions to choose the next position, converts it to a displacement, and sends a Schola action:

```csharp
var nextPosition = Vector3.MoveTowards(
    current, target, m_Speed * Mathf.Max(0f, deltaTime)
);
var delta = nextPosition - current;
m_Agent.Act(new DictPoint {
    Points = { [TranslateKey] = new BoxPoint {
        Values = { delta.x, delta.y, delta.z }
    } }
});
```

It observes again after movement, so the overlay displays the resulting scene state. `MoveTowards` prevents overshooting the target for a large frame interval. The arrival threshold simply stops sample movement; it does not assign a reward, terminate an episode, or reset an environment.

The runtime flow is **PositionSensor > ScholaAgent.Observe > deterministic controller > ScholaAgent.Act > TranslateActuator.TakeAction > updated Transform > Observe/display**. The controller never writes the Transform position directly. No replacement action interface, policy, or stepper is introduced. The controller is deterministic because it uses a programmed rule; the observation and action paths are real Schola functionality.

## Adapt it to your project

1. Duplicate the generated scene under `Assets` before editing. Imported samples are ordinary project assets; reimporting a sample can replace your changes.
2. Replace the cube's visual mesh, or add `PositionSensor`, `TranslateActuator`, `ScholaAgent`, and `SetupDemoController` to your own object. Keep the agent's position sensor and translate actuator on the same Transform. Remove the old controller so only one object is driven.
3. Add a `PositionSensor` to your goal object, set the two observation keys as above, and reconnect the agent's **Sensors** list and controller references. Set the actuator's **Action Key** to `translate` and register it in the agent's **Actuators** list. Adjust **Speed** and **Arrival Distance** for your world's scale.
4. Keep world coordinates in mind: `PositionSensor` reads `position`, not `localPosition`. The sample allows movement in all three axes. Use a common y coordinate for ground movement or change the movement calculation to constrain an axis.
5. To add observations, add sensors, assign unique keys, and extend the controller's configuration validation and parsing. The current agent accepts `PositionSensor` specifically, even though `ISensor` exists; arbitrary new sensor implementations also require changes to that agent API.
6. For physics or collision-aware locomotion, use an appropriate movement component/actuator and timing for your game instead of relying on `TranslateActuator`'s Transform movement. The current agent accepts `TranslateActuator` specifically, so a different `IActuator` implementation also requires extending the agent API.

### Action units and future policy integration

The integrated US2 `TranslateActuator` accepts a **three-float displacement per action**, not velocity. The sample multiplies speed by elapsed time once, then sends `[delta.x, delta.y, delta.z]`. The actuator does not multiply by time again. Match the `DictPoint` key to `ActionKey` and preserve its three-value shape when changing the controller.

With a future policy integration, replace the deterministic decision rule with a policy that produces compatible actions, and let the real stepper drive the agent. Keep the sample controller and a policy stepper from sending actions to the same object at the same time. The required US5 interfaces and dependencies are not included by this US2-only update.

## Branch findings and US3–5 follow-up

Remote refs were fetched and source/PRs inspected on **2026-10-26**. The original pass used base `8397c92`; this US2 follow-up started from the owner's clean committed US6 state `9ef5127`. These findings describe code presence, not independently verified execution:

| Branch / evidence | Source findings and implications |
| --- | --- |
| `US6-setup-integration` | Has US1 observations and now the existing US2 action runtime/tests, integrated with the sample. No training bridge or inference runtime is included. The initial pre-US6 divergence was 19 commits only on main and 9 only on US6; this update preserves the branch history and existing CI work. |
| `feature/us-1-define-agent-inputs`, `22889c1` ([PR #2](https://github.com/csc301-2026-f/11-AMD-Schola/pull/2)) | Observation implementation already present here, with subsequent formatting on US6. |
| `feature/us2-apply-agent-actions`, `49f354a` ([PR #10](https://github.com/csc301-2026-f/11-AMD-Schola/pull/10)) | `IActuator`, `TranslateActuator`, `SetActuators`, `DefineActionSpace`, `Act`; already merged to main. This follow-up reuses those source files, metadata, and 10 action test cases on US6 with formatting changes only. No US5 interfaces/dependencies are imported. Unity execution remains unverified here. |
| `feature/us-3-control-training-episodes`, refreshed `722840d` | Since the initial inspection this branch has advanced beyond main: `65736cd` adds environment step/reward/reset functionality and associated tests; the tip marks US3 complete awaiting review. Source includes `ScholaEnvironment`, reward/status interfaces and training-state types. This code was not integrated or executed in this US2 follow-up. |
| US4 discovery | No US4 branch or PR found among fetched branches and all 14 repository PRs. `Training` contains only `.gitkeep`; the smoke-test workflow/client on US6 are placeholders. Generated protocol tooling on `CICD-task6` is not evidence of a functioning Unity/Python training connection. |
| `US5-Unity-Sentis-Policy`, `12478f1` ([PR #8](https://github.com/csc301-2026-f/11-AMD-Schola/pull/8)) | Adds `SentisPolicy : IPolicy, IDisposable`, `InteractionDefinition`, and `com.unity.ai.inference` 2.6.1. Source maps named model inputs/outputs to Dict keys and checks spaces/shapes. Requires a compatible model; presence of source/tests does not verify this sample's inference. |
| `US5-inference-stepper` ([PR #11](https://github.com/csc301-2026-f/11-AMD-Schola/pull/11)) | Adds `IAgent`, `IStepper`, `SimpleStepper` and `StepperRunner`. `Setup(...)` starts the runner; `SimpleStepper` performs Observe > Think > Act. Runner setup requires application code and an initialized policy. |
| `US5-scholaagent-implements-iagent` ([PR #13](https://github.com/csc301-2026-f/11-AMD-Schola/pull/13)) | Makes the real ScholaAgent implement `IAgent`, adds stepping tests; merged into main (`7fc8da4`), absent here. |

The initial pass inspected teammates' code without integrating it. The owner then explicitly requested US2 integration. This update copies only US2's existing action implementation/tests and metadata, preserving the rest of the checkout and branch history; it does not merge or cherry-pick commits. The package keeps this branch's existing dependencies. A later team integration should wire the reviewed episode lifecycle (US3), verified Unity/Python transport and reset/step contract (US4), and compatible model plus policy lifetime/runner setup (US5).

## Optional Python and inference work

The repository has existing Python tools under `Resources/python`, primarily documented for the Unreal integration. Installing those tools does not create a Unity training bridge. Do not run the root Unreal setup steps as a prerequisite for this demo. There is no verified Unity training command, port, or trained-model workflow to offer on this branch.

After US3–5 are integrated, separately verify training transport, rewards and resets, model export, input/output keys and shapes, and inference-engine compatibility. `SentisPolicy` must be disposed by its owner; `StepperRunner` does not own the policy. A test model from another environment is not automatically compatible with two position observations and a three-dimensional translation action.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| Package Manager cannot find `package.json` | Use the disk path ending `Unity/com.amd.schola/package.json`, or include `?path=/Unity/com.amd.schola` in the Git URL. |
| Git install succeeds but sample is missing | The local US6 changes may not be pushed. Use **from disk** for this checkout. Confirm the selected revision contains the `samples` manifest entry. |
| Git executable not found | Install Git, check `git --version`, and restart Hub/Editor so they inherit the updated PATH. |
| Duplicate assembly/type errors | Remove duplicate package/sample copies. Keep only one imported Setup Demo version if old/new sample copies coexist. |
| No Tools menu | Import Setup Demo, wait for compilation, and resolve the first Console compiler error. Its builder is in the imported Editor assembly, not the core package. |
| No `.unity` file in the imported folder | Expected: use **Create Setup Demo Scene**, then save under `Assets`. The source ships a builder because Editor serialization was unavailable during implementation. |
| Cube stays still / configuration error | Check controller enabled, both references, exact keys, the two Sensors entries in order, and one registered Translate Actuator on the cube with Action Key `translate`. Reimport/regenerate an older sample as described above. Ensure Game view is playing and `Time.timeScale` is not zero. |
| Action rejected / missing `translate` key | `Act` matches `DictPoint` entries to actuator keys. Keep `SetupDemoController.TranslateKey` and the actuator's `ActionKey` aligned. The nested `BoxPoint` must have exactly three floats. |
| Empty/missing observation key | `InitSensor()` warns on empty keys; the sample rejects unexpected or duplicate keys. Set both keys exactly as in the table. |
| Pink or invisible geometry | Use the Built-In baseline project or assign materials compatible with your project's render pipeline; check the Camera is enabled and frames the objects. |
| Cube passes through obstacles | Expected: `TranslateActuator` changes Transform position without collision response. Use collision-aware locomotion for your own environment. |
| Training UI or inference menu is absent | No such UI exists on this branch. The separate `deliverables` UI mockup is not a functional integration. |
| Sample tests not listed | Import the sample first. Open **Window > General > Test Runner**, choose EditMode. For package tests, add `"testables": ["com.amd.schola"]` at the project manifest root, alongside `dependencies`. |

For exact checks performed during implementation and the remaining Editor acceptance steps, see [validation.md](validation.md).
