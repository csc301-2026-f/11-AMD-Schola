# Unity setup and adaptation guide (US6)

This guide targets **`US6-setup-integration`**, extending the existing `com.amd.schola` package. The **Setup Demo** makes a cube approach a sphere using real Schola position observations and a small deterministic controller. It needs no Python process or trained model. It demonstrates installation and observation wiring; its movement is sample code, not Schola actions, reinforcement learning, episode management, or model inference.

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

## Inspector settings

The builder assigns these automatically. The table also serves as a manual wiring recipe. Unity displays serialized `m_` fields with human-readable labels; the source field names below are authoritative.

| Object / component | Inspector label (source field) | Value |
| --- | --- | --- |
| Schola Cube / Transform | Position | `(-3, 0.5, 0)` |
| Schola Cube / Position Sensor | Observation Key (`m_ObservationKey`) | `agent_position` |
| Schola Cube / Schola Agent | Sensors (`m_Sensors`), Size | `2` |
| Same list | Element 0 | Position Sensor on **Schola Cube** |
| Same list | Element 1 | Position Sensor on **Target** |
| Target / Transform | Position / Scale | `(3, 0.5, 0)` / `(0.4, 0.4, 0.4)` |
| Target / Position Sensor | Observation Key (`m_ObservationKey`) | `target_position` |
| Schola Cube / Setup Demo Controller | Agent (`m_Agent`) | Schola Agent on **Schola Cube** |
| Same controller | Target Sensor (`m_TargetSensor`) | Position Sensor on **Target** |
| Same controller | Speed (`m_Speed`) | `2` world units per second |
| Same controller | Arrival Distance (`m_ArrivalDistance`) | `0.1` world units |

No Rigidbody is used. Colliders come from the primitives, but the controller moves the Transform directly and does not implement collision response. Do not add a dynamic Rigidbody and expect physics-driven movement. The controller intentionally requires the two sensors in the order shown; extra sensors require adapting its validation.

## Press Play and recognize success

1. Open **Game** and **Console**, then press **Play**.
2. The Console should print **`Schola Setup Demo: deterministic movement; no training or inference.`**
3. The cube should move from x = -3 toward the small sphere at x = 3, stopping within `Arrival Distance` in about three seconds at the default Speed.
4. The Game overlay should show **`agent_position`** changing and **`target_position`** remaining near `(3, 0.5, 0)`, followed by **Target reached**. Console also logs arrival once.
5. While playing, move **Target** in the Inspector to `(0, 0.5, 2)`. Its observation should change and the cube should follow it again.
6. Stop Play Mode. Normal Unity scene edits made while playing revert; press Play again to repeat the demonstration. This is not a Schola episode reset.

These are expected results awaiting Editor verification, not a claim that Play Mode was run during implementation.

## How observations and movement work

`AMD.Schola.Runtime` is the package's core assembly. A `PositionSensor` implements `ISensor`: it publishes a three-dimensional `BoxSpace`, and `CollectObservations()` reads its GameObject's **world** `Transform.position` into a `BoxPoint` containing `[x, y, z]`.

`ScholaAgent.SetSensors(...)` stores the two sensor references. `Define()` describes their values in a `DictSpace`, keyed by `ObservationKey`. `Observe()` returns current values in a matching `DictPoint`. Keys must be unique: this implementation uses dictionary assignment, so duplicate keys overwrite earlier observations.

The sample lives in a separate imported assembly, `AMD.Schola.Samples.SetupDemo`. On `Start`, its controller validates scene references and keys, calls each sensor's `InitSensor()`, and calls `Define()`. Each `Update` then calls `Step(Time.deltaTime)`:

```csharp
var observation = (DictPoint)m_Agent.Observe();
var current = ReadPosition(observation, AgentPositionKey);
var target = ReadPosition(observation, TargetPositionKey);
```

The controller uses the observed positions to choose bounded movement:

```csharp
m_Agent.transform.position = Vector3.MoveTowards(
    current, target, m_Speed * Mathf.Max(0f, deltaTime)
);
```

It observes again after movement, so the overlay displays the resulting scene state. `MoveTowards` prevents overshooting the target for a large frame interval. The arrival threshold simply stops sample movement; it does not assign a reward, terminate an episode, or reset an environment.

**There is no `Act`, actuator, policy, or stepper on this implementation branch.** Consequently the direct Transform assignment above is explicitly sample-only movement. No parallel action interface is introduced. See the US2 migration point below.

## Adapt it to your project

1. Duplicate the generated scene under `Assets` before editing. Imported samples are ordinary project assets; reimporting a sample can replace your changes.
2. Replace the cube's visual mesh, or add `PositionSensor`, `ScholaAgent`, and `SetupDemoController` to your own object. Keep the agent's position sensor on the same Transform the controller moves. Remove the old controller so only one object is driven.
3. Add a `PositionSensor` to your goal object, set the two keys as above, and reconnect the agent's **Sensors** list and controller references. Adjust **Speed** and **Arrival Distance** for your world's scale.
4. Keep world coordinates in mind: `PositionSensor` reads `position`, not `localPosition`. The sample allows movement in all three axes. Use a common y coordinate for ground movement or change the movement calculation to constrain an axis.
5. To add observations, add sensors, assign unique keys, and extend the controller's configuration validation and parsing. The current agent accepts `PositionSensor` specifically, even though `ISensor` exists; arbitrary new sensor implementations also require changes to that agent API.
6. For physics or collision-aware locomotion, use an appropriate movement component/actuator and timing for your game instead of relying on this Transform demonstration.

### Once US2 is deliberately integrated

On `feature/us2-apply-agent-actions` and current remote `main`, `TranslateActuator` accepts a **three-float displacement per action**, not velocity. Add it to the controlled object, keep `ActionKey = "translate"`, and configure the agent's actuator list with `SetActuators(...)` or its Inspector. Initialize the actuator and use `DefineActionSpace()` as appropriate.

Replace the direct Transform assignment with a `DictPoint` containing a `BoxPoint` under the actuator key, then call `ScholaAgent.Act(...)`. Compute `delta = nextPosition - currentPosition` using the existing speed/time calculation. Conceptually:

```csharp
// Future integration only: these APIs are absent from the US6 implementation base.
agent.Act(new DictPoint {
    Points = { ["translate"] = new BoxPoint {
        Values = { delta.x, delta.y, delta.z }
    } }
});
```

Do not paste this into the current branch: it requires the US2 implementation. Validate the actual combined branch and actuator keys when integrating. Keep the sample controller and a policy stepper from moving the same object at the same time.

## Branch findings and US3–5 follow-up

Remote refs were fetched and source/PRs inspected on **2026-10-26**. These findings describe code presence, not independently verified execution:

| Branch / evidence | Source findings and implications |
| --- | --- |
| `US6-setup-integration`, base `8397c92` | Has US1 `PositionSensor`, spaces/points, and `ScholaAgent.Define/Observe`; no action API, training bridge, or inference runtime. Compared with remote `main`, 19 commits belong only to main and 9 only to US6. Existing CI work and local deletions were preserved. |
| `feature/us-1-define-agent-inputs`, `22889c1` ([PR #2](https://github.com/csc301-2026-f/11-AMD-Schola/pull/2)) | Observation implementation already present here, with subsequent formatting on US6. |
| `feature/us2-apply-agent-actions`, `49f354a` ([PR #10](https://github.com/csc301-2026-f/11-AMD-Schola/pull/10)) | `IActuator`, `TranslateActuator`, `SetActuators`, `DefineActionSpace`, `Act`; merged to main, absent here. Tests are present on that branch; not executed in this environment. |
| `feature/us-3-control-training-episodes`, `7fc8da4` | Currently points to the same commit as remote main. It contains the merged action/inference work but no additional Unity reward, episode-ending, or reset implementation. US3 remains follow-up work. |
| US4 discovery | No US4 branch or PR found among fetched branches and all 14 repository PRs. `Training` contains only `.gitkeep`; the smoke-test workflow/client on US6 are placeholders. Generated protocol tooling on `CICD-task6` is not evidence of a functioning Unity/Python training connection. |
| `US5-Unity-Sentis-Policy`, `12478f1` ([PR #8](https://github.com/csc301-2026-f/11-AMD-Schola/pull/8)) | Adds `SentisPolicy : IPolicy, IDisposable`, `InteractionDefinition`, and `com.unity.ai.inference` 2.6.1. Source maps named model inputs/outputs to Dict keys and checks spaces/shapes. Requires a compatible model; presence of source/tests does not verify this sample's inference. |
| `US5-inference-stepper` ([PR #11](https://github.com/csc301-2026-f/11-AMD-Schola/pull/11)) | Adds `IAgent`, `IStepper`, `SimpleStepper` and `StepperRunner`. `Setup(...)` starts the runner; `SimpleStepper` performs Observe > Think > Act. Runner setup requires application code and an initialized policy. |
| `US5-scholaagent-implements-iagent` ([PR #13](https://github.com/csc301-2026-f/11-AMD-Schola/pull/13)) | Makes the real ScholaAgent implement `IAgent`, adds stepping tests; merged into main (`7fc8da4`), absent here. |

Teammates' code was inspected without merging or cherry-picking. The package intentionally keeps this branch's existing dependencies. A later team integration should wire the actual episode lifecycle (US3), verified Unity/Python transport and reset/step contract (US4), and compatible model plus policy lifetime/runner setup (US5).

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
| Cube stays still / configuration error | Check controller enabled, both references, exact keys, and the two Sensors entries in order. Ensure Game view is playing and `Time.timeScale` is not zero. |
| Empty/missing observation key | `InitSensor()` warns on empty keys; the sample rejects unexpected or duplicate keys. Set both keys exactly as in the table. |
| Pink or invisible geometry | Use the Built-In baseline project or assign materials compatible with your project's render pipeline; check the Camera is enabled and frames the objects. |
| Cube passes through obstacles | Expected for direct Transform movement; this demonstration has no collision-aware actuator. |
| Training UI or inference menu is absent | No such UI exists on this branch. The separate `deliverables` UI mockup is not a functional integration. |
| Sample tests not listed | Import the sample first. Open **Window > General > Test Runner**, choose EditMode. For package tests, add `"testables": ["com.amd.schola"]` at the project manifest root, alongside `dependencies`. |

For exact checks performed during implementation and the remaining Editor acceptance steps, see [validation.md](validation.md).
