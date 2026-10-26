# US6 validation record

Recorded **2026-10-26** on `US6-setup-integration`, based on `8397c92`. No commit, push, merge, cherry-pick, or PR was made. No applicable `AGENTS.md` was found in the checkout or its ancestor directories. `CONTRIBUTING.md`, the package source/tests/manifest, CI workflows, mockup version documentation, remote branch files/diffs, and all repository PRs were inspected before editing.

## Checks actually performed

| Check | Result |
| --- | --- |
| `git fetch origin` | Passed after an initial sandbox denial writing `.git/FETCH_HEAD`; fetched refs without changing the checkout. |
| `git rev-list --left-right --count origin/main...HEAD` | `19 9`; kept the existing US6 branch and its divergent CI work. |
| Existing `.github/scripts/tests/test_checks.py` suite | **7 tests passed** with Python's `unittest` runner. The pre-existing deleted `check_meta_files.py` was loaded from `git show HEAD:...` into an in-memory module; the deleted file was not restored. |
| `.github/scripts/check_package_json.py` validation against the changed manifest | Passed. |
| Existing metadata validator against `Unity/` | Passed using its tracked HEAD version in memory. This validator intentionally ignores `Samples~`, so a separate sample inspection was also run. |
| Additional sample layout inspection | Passed: registered sample directory exists; every sample file/subfolder has metadata; no orphan metadata; **38 package/sample metadata files** have valid unique 32-character GUIDs. |
| Assembly inspection | Passed: all **5** package/sample `.asmdef` files parse as JSON, have unique names and resolving explicit assembly references. Sample builder/tests target Editor only. |
| CSharpier **1.3.0** | Formatted the 3 new C# files, then checked **15 C# files** under `Unity/`; passed with no syntax/format errors. Used the cached .NET 10 tool DLL and the tracked HEAD formatter configuration copied outside the repository because the working-tree tool/config files were already deleted. This parses C#; it is not a Unity compilation or API/type check. |
| `git diff --check` and inspection of new files for trailing whitespace | Passed. |
| Clean Unity installation/import, compilation, scene generation/save/reload, EditMode execution, Play Mode and visuals | **Not run: Unity Editor was not available.** No `.unity` scene is presented as Editor-verified. |
| Python/Unreal regression suites | Not run: the changes do not modify Python training or Unreal code. |

The first validator run encountered Windows sandbox temporary-directory access errors. Redirecting `tempfile.tempdir` to the allowed task artifact directory resolved them; all seven tests then passed. An initial CSharpier `check` invocation used the unsupported `--no-cache` argument; rerunning with the documented `check` options passed.

The working tree already had deletions of `.config/dotnet-tools.json`, `.csharpierrc.yaml`, and `.github/scripts/check_meta_files.py`. Those are preserved. The normal CI commands below rely on those files, so the equivalent local checks above used their tracked contents without changing the user's deletions. CI/tooling restoration is a separate decision for the owner.

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

These are the existing branch CI entry points, not a claim that those literal commands succeeded with the pre-existing local deletions. The metadata validator does not traverse `Samples~`. The implementation additionally inspected that tree, GUIDs, and assembly definitions with Python standard-library assertions.

## Unity acceptance checklist still to run

Use **Unity 6000.3.24f1** and a new **3D Built-In** project, following [setup.md](setup.md):

- [ ] Install **from disk**, selecting this checkout's `Unity/com.amd.schola/package.json`. Confirm AMD Schola 0.1.0 resolves with no Console errors.
- [ ] Import **Setup Demo** through Package Manager. Confirm the sample's scripts compile, and its Tools menu appears.
- [ ] Run **Tools > AMD Schola > Create Setup Demo Scene** and save under `Assets`. Confirm all five expected objects, real Schola components, keys, and Inspector references match the guide; no Missing Script entries.
- [ ] Close and reopen the saved scene. Confirm references persist and the Game camera frames the cube and target.
- [ ] Press Play. Confirm the setup-demo message, moving observation values, cube arrival in about three seconds, and the arrival message without errors.
- [ ] Move Target to `(0, 0.5, 2)` while playing. Confirm its observation changes and the cube follows. Stop and restart Play Mode to repeat from the saved state.
- [ ] In **Window > General > Test Runner > EditMode**, run `AMD.Schola.Samples.SetupDemo.Tests`: **3 tests** cover observation-driven movement/no overshoot/following a moved target, invalid keys, and real scene save/reload reference integrity.
- [ ] Add `"testables": ["com.amd.schola"]` at the root of the project's `Packages/manifest.json` alongside `dependencies`, then run the existing package observation tests too. Imported sample tests live under Assets and do not depend on `testables`.
- [ ] Test canceling the builder's save dialog and creating a second scene after saving current changes. Check your existing scene is preserved.
- [ ] Adapt a duplicated scene to another controlled object and goal; reconnect the two sensors and controller references and repeat the demonstration.

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

The branch's GameCI workflow targets package EditMode tests using `packageMode: true`. It does **not** automatically import `Samples~`; therefore the three new sample tests require import into a consumer project. The end-to-end training smoke workflow/client are existing placeholders and cannot verify US6, US4, or training.

US6 now supplies the installable sample sources, a Unity scene-creation command, metadata/assembly isolation, and the installation/adaptation guide. Acceptance remains pending for actual Editor compilation, sample import, generated scene reference integrity, tests, and Play Mode behavior. Full Schola actions require deliberate US2 integration; rewards/resets, training connectivity, and compatible trained-model execution remain outside this branch's setup demonstration. See the source/PR findings in [setup.md](setup.md).
