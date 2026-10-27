# US4 startup transport experiment

This is an **opt-in Windows x64 prototype**, not the completed Unity training integration.
It implements startup acknowledgements and close/error acknowledgements. Definitions,
scene actions, rewards, resets, and training are deliberately not implemented.

## Why this transport?

Schola's Python code calls a native gRPC server using the existing GymService contract.
Grpc.Core 2.46.6 provides a server API usable from a .NET Standard assembly; its native
Windows x64 library is restored with the managed assemblies. It is in maintenance mode:
https://grpc.io/blog/grpc-csharp-future/
This experiment does not establish it as the final production dependency.
Grpc.Tools 2.72.0 generates the C# bindings; Google.Protobuf 3.31.1 reads/writes messages.
Compiler and runtime versions need not equal Python's package versions: the shared wire
schema must agree. All seven required schemas are generated without changing Proto/.

## Prepare dependencies (Windows x64, Python 3.10–3.12)

From the repository root:

```powershell
python Unity/tools/prepare_startup.py
python -m venv .venv
.venv/Scripts/python -m pip install -r Unity/tools/requirements-startup.txt
```

The preparation script downloads pinned NuGet packages over HTTPS, verifies their
SHA-256 values against startup-packages.lock.json, restores local DLLs and Unity
plugin metadata, and regenerates C# files. Re-running it is supported.
Run this only after reviewing the script and dependency choices as normal.
It does not install software system-wide. Compiler downloads are cached in
Unity/.startup-cache; restored plugins and caches are ignored by Git.
The local restored-packages.json records versions and hashes for inspection.
Do not commit downloaded binaries. Check generated C# changes when schemas change.

The training assembly is disabled unless SCHOLA_STARTUP_SPIKE is defined.
Its managed plugins are explicitly referenced, rather than automatically added
to every assembly. This keeps this experiment out of the default inference path.
Dependencies should be restored before opting in.

## Install Unity and run the actual Editor check

1. Install Unity Hub from https://unity.com/download and a Unity 6.3 editor (6000.3).
   The current package declares 6000.3; using the team's exact patch version is preferable.
2. Create an empty 3D Unity project outside this package repository.
3. In Package Manager, use Install package from disk and select
   Unity/com.amd.schola/package.json from this checkout.
4. Use the Windows desktop target. In Project Settings > Player > Other Settings,
   add SCHOLA_STARTUP_SPIKE to Scripting Define Symbols and apply it.
5. Create an empty GameObject and add AMD Schola > Training > Gym Startup Probe.
6. Press Play. The Console should say it is listening on 127.0.0.1:50051.
7. In a separate shell at the repository root:

```powershell
.venv/Scripts/python Unity/tools/probe_startup.py
```

8. Expect PASS output for each startup mode, invalid mode rejection,
   unimplemented definitions, close/error acknowledgements, and two client sessions.
   Unity logs startup counts from its main thread.
9. Stop Play, press Play again, and repeat the Python command. Verify there is no
   address-in-use or native-plugin error. Repeat with Enter Play Mode domain reload
   disabled if that configuration is used by the team.
10. Stop Play. The probe should fail within its deadline:

```powershell
.venv/Scripts/python Unity/tools/probe_startup.py --timeout 0.5 --repeats 1
```

Do not run the standalone harness while Play mode is using the same port.
Do not interpret a successful startup as a successful training run.

## Verify the server core without Unity

Install a .NET 8 SDK locally if needed, then:

```powershell
dotnet build Unity/tools/StartupHarness/StartupHarness.csproj
.venv/Scripts/python Unity/tools/test_startup_transport.py
```

The harness compiles the **same GymStartupServer.cs and generated bindings** used
by the component. It uses the existing Schola GrpcProtocol, not a replacement Python
server. Checks include all startup modes, invalid mode rejection, unimplemented
definitions, close/error acknowledgements, client reconnects, occupied-port failure,
server process restart on the same port, and deadline failure after shutdown.

This is evidence for C# / Python transport interoperability, **not** evidence that
Unity imports the DLLs, loads the native plugin, or cleans up across Play mode changes.

## Scope and lifecycle

- IPv4 loopback only; no authentication or remote deployment is claimed.
- Startup records the reset mode but does not perform an environment reset.
- Status-only CLOSED/ERROR is acknowledged; it does not stop the listener, permitting reconnects.
- Component disable/destruction/application quit stops the server; Stop is idempotent.
- Handlers never call Unity APIs or wait for the main thread. Update logs counters safely.
- Only Windows x64 is restored. Player builds, IL2CPP, macOS/Linux, Android/iOS,
  and dependency conflicts with other packages remain unverified.
- Full reset-mode semantics belong to subsequent environment integration.
- Choosing a replacement transport or expanding platform scope needs a separate decision.

## Validation recorded October 26, 2026

- .NET SDK 8.0.416: server harness builds with zero warnings/errors.
- Python 3.12.14, grpcio 1.85.0, protobuf 6.33.6, Gymnasium 1.4.0, NumPy 2.5.4.
- Transport runner passed its two server lifecycles, with six startup requests per
  lifecycle (three modes across two client sessions).
- Unity Editor was not installed. Package import, Play mode startup/shutdown/restart,
  and Unity native-plugin compatibility remain unverified.

