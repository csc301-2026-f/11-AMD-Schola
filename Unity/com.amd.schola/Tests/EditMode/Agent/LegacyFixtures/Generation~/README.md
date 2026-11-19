# Legacy concrete-list fixtures

`../LegacyConcreteBindings.unity` and `../LegacyConcreteBindings.prefab` were
saved by Unity **6000.3.24f1** on November 18, 2026 in a separate project containing
the unmodified runtime from commit
`a3cb9a7386bf0f3eb1a197af9a824e60d8b6e074` (`7054247^`, before the interface refactor).
The runtime source files and their original `.meta` GUIDs were extracted from Git.
The current interface implementation was never loaded in the generator project.

At generation time `ScholaAgent` stored `List<PositionSensor> m_Sensors` and
`List<TranslateActuator> m_Actuators`. The generator checks these exact field types
before writing either asset. Both assets contain a root agent, root and child
sensor/actuator references, an empty slot in each list, nondefault keys, and
nonzero transforms.

The migration tests copy the original bytes into a temporary Assets folder and
load them with the current implementation **without first saving them**. They
check reference identity, list order, null slots, keys, observation/action spaces,
and observation/action behavior. This differs from the separate current-format
scene/prefab round-trip tests.

Do not resave these fixture assets in the current editor. To regenerate them,
run the following from the repository root, supplying your Unity executable:

```sh
fixture_project="$(mktemp -d -t schola-legacy-fixtures)"
mkdir -p "$fixture_project/Assets/LegacyRuntime" "$fixture_project/Assets/Editor" \
  "$fixture_project/Packages" "$fixture_project/ProjectSettings"
git archive a3cb9a7386bf0f3eb1a197af9a824e60d8b6e074 Unity/com.amd.schola/Runtime \
  | tar -x -C "$fixture_project/Assets/LegacyRuntime" --strip-components=3
cp Unity/com.amd.schola/Tests/EditMode/Agent/LegacyFixtures/Generation~/GenerateLegacyFixtures.cs \
  "$fixture_project/Assets/Editor/"
printf '%s\n' '{"dependencies": {}}' > "$fixture_project/Packages/manifest.json"
printf '%s\n' 'm_EditorVersion: 6000.3.24f1' > "$fixture_project/ProjectSettings/ProjectVersion.txt"
"/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit -projectPath "$fixture_project" \
  -executeMethod GenerateLegacyFixtures.Generate \
  -logFile "$fixture_project/generation.log"
```

The output is under `$fixture_project/Assets/GeneratedLegacyFixtures/`. Copy its
two assets and their `.meta` files back here only after checking that generation
succeeded. Unity may assign different file IDs on regeneration, so compare
structure and rerun the migration tests rather than expecting identical hashes.

SHA-256 of the original committed fixture content:

- Scene: `4767f8a73a5c2c4be6a4de384165bc8595b890cec512e128c8b9eb47976f272c`
- Prefab: `dece8c2924c14a94d819457d863f2a9dc5b177b1e520b478a24120ad2560634c`

This `Generation~` folder is excluded from Unity asset imports, so the legacy-only
generator does not compile into the current package or test assemblies.
