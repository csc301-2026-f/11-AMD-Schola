# Unity Training (gRPC)

This folder will hold the Unity-side training connection to Schola's Python client
(gRPC `GymService` from `Proto/`).

## AGENT TODO: command-line port (`-ScholaPort`)

Smoke-test CI starts the headless player with:

```text
SmokeTest.x86_64 -batchmode -nographics -ScholaPort=50051 -logFile ...
```

See `.github/workflows/smoke-test.yml` (`SMOKE_PORT` / `run` job) and
`.github/scripts/unity_smoke_test.py`.

**Required behavior (match Unreal):**

1. When the Unity gRPC gym connector / training server starts, parse the player
   command line for `-ScholaPort=<int>` (same spelling and `=` form as Unreal).
2. If the flag is present, bind the gRPC server to that port.
3. If the flag is missing, fall back to a documented default (Unreal uses
   `ServerSettings.Port` when parse fails).
4. Serve Schola's existing `GymService` protocol so
   `python .github/scripts/unity_smoke_test.py --port <port>` can connect with
   `GrpcProtocol(..., credential_mode="insecure")` and `ExternalSimulator`
   (no changes under `Resources/python`).

**Unreal reference (do not invent a different flag name):**

- `Source/ScholaProtobuf/Private/GymConnectors/gRPC/gRPCGymConnector.cpp`
  uses `FParse::Value(..., TEXT("ScholaPort="), Port)`.

**Do not commit `Unity/SmokeTestProject` until:**

1. This port parsing is implemented and tested locally against the smoke script, and
2. `.github/scripts/check_meta_files.py` skips project `ProjectSettings/` and
   `Packages/` (see the AGENT TODO in that file). Otherwise `checks.yml` fails.

Once both are done and the smoke-test project exists, the `build` and `run` jobs
in `smoke-test.yml` turn on with no workflow edits.
