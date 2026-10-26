# US6 validation record

Updated **2026-10-26** for the explicitly requested US2 integration on `US6-setup-integration`, starting from the owner's clean committed US6 state **`9ef5127`**. The original setup implementation was based on `8397c92`. No commit, push, merge, cherry-pick, or PR was made by this follow-up. It reuses the existing US2 runtime/tests and metadata from **`49f354a`**, then wires the sample through the action API. No applicable `AGENTS.md` was found. Repository instructions, package code/tests, and relevant branch diffs were inspected before editing.

## Checks actually performed

| Check | Result |
| --- | --- |
| `git fetch origin` | Passed; refreshed refs without changing the checkout. Discovered the US3 branch advanced to `722840d`; no US3 code was integrated. |
| US2 source inspection | `git diff origin/feature/us2-apply-agent-actions origin/main -- Unity/com.amd.schola/Runtime/Actuators Unity/com.amd.schola/Runtime/Agent/ScholaAgent.cs` shows that main's only additional change in this action runtime is `ScholaAgent : IAgent`. Reused US2's self-contained version to avoid importing US5 dependencies. |
| Existing `.github/scripts/tests/test_checks.py` suite | **7 tests passed** with Python's `unittest` runner. The absent `check_meta_files.py` was loaded from `git show 8397c92:.github/scripts/check_meta_files.py` into an in-memory module; the file was not restored. |
| `.github/scripts/check_package_json.py` validation against the changed manifest | Passed. |
| Existing metadata validator against `Unity/` | Passed using its `8397c92` version in memory. This validator intentionally ignores `Samples~`, so a separate sample inspection was also run. |
| Additional sample layout inspection | Passed: registered sample directory exists; every sample file/subfolder has metadata; no orphan metadata; **44 package/sample metadata files** have valid unique 32-character GUIDs. US2 metadata retains its existing GUIDs. |
| Assembly inspection | Passed: all **5** package/sample `.asmdef` files parse as JSON, have unique names and resolving explicit assembly references. Sample builder/tests target Editor only. |
| CSharpier **1.3.0** | Formatted the 8 integrated/changed C# files, then checked **19 C# files** under `Unity/`; passed with no syntax/format errors. Used the cached .NET 10 tool DLL and the formatter configuration preserved from `8397c92` outside the repository because the checkout has no formatter tool/config files. This parses C#; it is not a Unity compilation or API/type check. |
| Controller action-path inspection | Passed: controller source calls `m_Agent.Act(...)` and has no direct position assignments. The builder registers a cube-local `TranslateActuator`; the controller initializes actuators and defines the action space. This source inspection is not a runtime test. |
| `git diff --check` and inspection of new files for trailing whitespace | Passed. |
| Clean Unity installation/import, compilation, scene generation/save/reload, EditMode execution, Play Mode and visuals | **Not run: Unity Editor was not available.** No `.unity` scene is presented as Editor-verified. |
| Python/Unreal regression suites | Not run: the changes do not modify Python training or Unreal code. |

This follow-up's static checks passed. Python temporary files were directed to the allowed task artifact directory, reusing the workaround from the initial implementation's sandbox access errors. The initial implementation also corrected an unsupported CSharpier `check --no-cache` invocation; that argument was not used for this follow-up's check.

The original working tree had deletions of `.config/dotnet-tools.json`, `.csharpierrc.yaml`, and `.github/scripts/check_meta_files.py`. They remain absent in the owner's committed `9ef5127` state and were not restored. The normal CI commands below rely on those files, so the equivalent local checks above used their preserved baseline contents. CI/tooling restoration is a separate decision for the owner.

## Reproduce standard checks with intact repository tooling

From the repository root, with Python and the formatter runtime available:

```powershell
dotnet tool restore
dotnet csharpier check Unity/
python -m unittest discover -s .github/scripts/tests
python .github/scripts/check_meta_files.py
python .github/scripts/check_package_json.py
git diff --check
```

These are the existing branch CI entry points, not a claim that those literal commands succeeded with the absent tooling. The metadata validator does not traverse `Samples~`. The implementation additionally inspected that tree, GUIDs, and assembly definitions with Python standard-library assertions. Unity source tests are present but **not run**: **10 reused US2 action tests** and **5 sample tests**, plus the existing observation tests.

## Unity acceptance checklist still to run

Use **Unity 6000.3.24f1** and a new **3D Built-In** project, following [setup.md](setup.md):

- [ ] Install **from disk**, selecting this checkout's `Unity/com.amd.schola/package.json`. Confirm AMD Schola 0.1.0 resolves with no Console errors.
- [ ] Import **Setup Demo** through Package Manager. Confirm the sample's scripts compile, and its Tools menu appears.
- [ ] Back up customizations and reimport an older Setup Demo before using the updated builder. Create a new scene or add/register its missing actuator as described in the guide.
- [ ] Run **Tools > AMD Schola > Create Setup Demo Scene** and save under `Assets`. Confirm all five expected objects, real Schola components, keys, and Inspector references match the guide; no Missing Script entries. Schola Cube must have a Translate Actuator with Action Key `translate`, registered as the agent's single Actuators entry.
- [ ] Close and reopen the saved scene. Confirm references persist and the Game camera frames the cube and target.
- [ ] Press Play. Confirm the setup-demo message, moving observation values, cube arrival in about three seconds, and the arrival message without errors.
- [ ] Move Target to `(0, 0.5, 2)` while playing. Confirm its observation changes and the cube follows. Stop and restart Play Mode to repeat from the saved state.
- [ ] In **Window > General > Test Runner > EditMode**, run `AMD.Schola.Samples.SetupDemo.Tests`: **5 tests** cover observation/action-driven movement, no overshoot/following a moved target, invalid observation keys, missing actuators, action-key rejection before movement, and scene save/reload of sensor/actuator references.
- [ ] Add `"testables": ["com.amd.schola"]` at the root of the project's `Packages/manifest.json` alongside `dependencies`, then run the package observation tests and the **10 US2 tests** in `ScholaAgentActTests` / `TranslateActuatorTests`. Imported sample tests live under Assets and do not depend on `testables`.
- [ ] Test canceling the builder's save dialog and creating a second scene after saving current changes. Check your existing scene is preserved.
- [ ] Adapt a duplicated scene to another controlled object and goal; reconnect the two sensors, registered actuator and controller references and repeat the demonstration.

Optional Editor test automation after importing the sample (replace the project path):

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.24f1\Editor\Unity.exe'
$demoProject = 'C:\Projects\ScholaSetupDemo'
& $unityEditor -batchmode -nographics -projectPath $demoProject `
    -runTests -testPlatform EditMode `
    -testResults "$demoProject\editmode-results.xml" `
    -logFile "$demoProject\editmode.log"
```

Close the interactive Editor for that project first. This command is supplied for the remaining check and was not run here. Inspect the XML/log for actual results; it does not exercise the visible Play Mode demonstration.

## CI coverage and acceptance limits

The branch's GameCI workflow targets package EditMode tests using `packageMode: true`, which includes the reused US2 package tests. It does **not** automatically import `Samples~`; therefore the five sample tests require import into a consumer project. The end-to-end training smoke workflow/client are existing placeholders and cannot verify US6, US4, or training.

US6 supplies the installable sample sources, a Unity scene-creation command, metadata/assembly isolation, and the installation/adaptation guide. The explicitly requested US2 integration now routes deterministic decisions through the real `Observe > Act > TranslateActuator` path. Acceptance remains pending for actual Editor compilation, sample import, generated scene reference integrity, tests, and Play Mode behavior. Rewards/resets, training connectivity, and compatible trained-model execution remain outside this branch's setup demonstration. See the source/PR findings in [setup.md](setup.md).
