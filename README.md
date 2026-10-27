<p align="center">
  <img src="./Docs/Sphinx/_static/AMD_Schola_Lockup_RGB_Blk.png" alt="Schola Lockup" width="50%"/>
</p>

<div align="center">

[![GitHub License](https://img.shields.io/github/license/GPUOpen-LibrariesAndSDKs/Schola)](https://opensource.org/license/MIT)
[![Discord](https://img.shields.io/badge/Discord-Join_Us-5865F2?style=flat&logo=discord&logoColor=white)](https://discord.gg/amd-dev)
[![Codestyle](https://img.shields.io/badge/code%20style-black-000000.svg)](https://github.com/psf/black)
[![Python Version from PEP 621 TOML](https://img.shields.io/python/required-version-toml?tomlFilePath=https%3A%2F%2Fraw.githubusercontent.com%2FGPUOpen-LibrariesAndSDKs%2FSchola%2Frefs%2Fheads%2FAlex%2FBeautify-Readme%2FResources%2Fpython%2Fpyproject.toml)](https://www.python.org/downloads/)
[![Unreal Engine](https://img.shields.io/badge/Unreal%20Engine%20-%205.5%20%7C%205.6%20%7C%205.7%20-%20white?style=flat&logo=unrealengine&color=F26522)](https://www.unrealengine.com/download)
[![Unity](https://img.shields.io/badge/Unity-6000.3-black?style=flat&logo=unity)](https://unity.com/download)


</div>

---

The Schola project is a toolkit for controlling agents in game engines with reinforcement learning. Upstream Schola targets **Unreal Engine** and connects environments to Python RL frameworks (Gymnasium, RLlib, Stable-Baselines3). This repository is a course fork that also adds a **Unity** package under [`Unity/com.amd.schola`](Unity/com.amd.schola).

**Unity status (this fork):** agents can observe, act, manage local episodes, and run ONNX policies with Unity Inference Engine (`SentisPolicy`). EditMode tests cover these paths. A full Unity↔Python training bridge (gRPC) and a Package Manager Setup Demo sample are still in progress (see `US4` / `US6-setup-integration` branches). Course planning lives under [`deliverables/`](deliverables/).

## Unity package

Package path: [`Unity/com.amd.schola`](Unity/com.amd.schola) (`com.amd.schola`, Unity **6000.3**). Depends on `com.unity.ai.inference` and `com.unity.test-framework`.

### Install into a Unity project

1. Install **Unity 6000.3** (team CI uses **6000.3.24f1**; other 6000.3.x patches usually work for local work).
2. Create an empty 3D project in Unity Hub.
3. **Window → Package Management → Package Manager** → **+** → **Install package from disk…** → select `Unity/com.amd.schola/package.json` from this checkout.
4. Wait for import/compile to finish. **AMD Schola** should appear under Package Manager → **In Project**.

Keep the repo checkout in place while the package is installed from disk.

### Run EditMode tests

1. In the consumer project’s `Packages/manifest.json`, add (alongside `dependencies`):

```json
"testables": [ "com.amd.schola" ]
```

2. **Window → Analysis → Test Runner** (or search “Test Runner”) → **EditMode**.
3. Expand **AMD.Schola.Tests.EditMode** and **Run All**.

These tests exercise sensors/observations, actuators/actions, `ScholaEnvironment` episode hooks, `SentisPolicy` with a CartPole ONNX model, and the observe→think→act stepper. Some tests intentionally log errors to verify failure handling; green checks in Test Runner are what matter.

### What is / is not included yet

| Available on `main` | Not finished yet |
| --- | --- |
| Runtime agents, sensors, actuators, spaces/points | Unity gRPC training server ↔ Schola Python (US4) |
| Local episode/env APIs (`ScholaEnvironment`) | Package Manager **Setup Demo** sample + setup guide (US6 branch) |
| ONNX inference (`SentisPolicy`) and steppers | Headless smoke player project for CI build/run jobs |

Training notes and codegen for Unity C# bindings: [`Unity/com.amd.schola/Training/README.md`](Unity/com.amd.schola/Training/README.md). CI workflows live under [`.github/workflows/`](.github/workflows/).

## Getting Started (Unreal)

### Install Unreal Engine
As the upstream Schola plugin is an Unreal Engine project, you will need to first install Unreal Engine. Refer to the below table to identify the correct version of Unreal Engine for each version of Schola. 

>[!NOTE]
> Each Schola release may be compatible with other versions of Unreal Engine beyond the ones listed here, however these are the version(s) tested for each release.

| Schola version | Unreal Version |
| -------------- | -------------- |
| **2.1** | **5.5-5.7** |
| 2.0 | 5.5-5.6 |
| 1.3 | 5.5-5.6 |
| 1.2 | 5.5 |
| 1.1 | 5.5 |
| 1.0 | 5.4 |


### Installing Schola Into Your Unreal Project
To use the Unreal plugin in an existing Unreal Engine project, copy this repository to the `/Plugins` folder of your project, and pip install the Schola python package in `/Resources/python` using `pip install -e <path-to-plugin>/Resources/python[all]` (the folder that contains `pyproject.toml`). For Unity, see [Unity package](#unity-package) above.

> [!IMPORTANT]
> Since Schola is provided as C++ source you must recompile your project after adding it. Otherwise, you will receive a warning about Schola being built for another version of Unreal Engine regardless of what version you are using.

### Dependencies

#### Python

See `Resources/python/pyproject.toml` for a comprehensive list of dependencies. The following **optional dependency extras** are available when installing with pip (for example `pip install -e ./Resources/python[sb3]`):

| Extra | Description |
| ----- | ----------- |
| `sb3` | Dependencies for running training with Stable Baselines 3 |
| `rllib` | Dependencies for running training with RLlib |
| `minari` | Dependencies for collecting Minari datasets with Schola |
| `all` | Equivalent to installing `sb3`, `rllib`, and `minari` together |
| `docs` | Dependencies for building documentation with Sphinx |

Test dependencies are declared under `[dependency-groups]` in `pyproject.toml`, not as a pip extra. Install them with **`pip install --group test`** (for example `pip install --group test -e ./Resources/python[all]` from your project, or the same with `cd` into `Resources/python` first).

#### C++

All C++ dependencies for using Schola are bundled with the plugin under `/Source/ThirdParty` and do not need to be installed separately. These consist of `gRPC`, `protobuf` and `absl`(dependency of gRPC).

## Build and Test

>[!IMPORTANT]
> Schola comes with all dependencies included. Only run these if you encounter issues during the setup.

### Building Third Party Dependencies

Third party dependencies, specifically gRPC and Protobuf can be built using `Schola\Plugins\Schola\Resources\Build\windows_dependencies.bat` or `Schola\Plugins\Schola\Resources\Build\linux_dependencies.sh` depending on your OS. This will update the plugin ThirdParty folder to include copies of the dependencies including .lib/.a files, and copy protoc, and relevant plugins to the tools directory.

### Generating gRPC/Protobuf Code

To generate code for gRPC and Protobuf run `schola compile-proto`. This will generate `*.pb.cc`, `*.pb.h` and `*.pb.py` files to the correct folders as well as fix several bugs in the default generator (e.g. ignore warnings in C++ code, and fix relative imports for python)

### Generating Documentation

Documentation for Schola is build using a combination of Doxygen + Sphinx + Breathe.

1. Install Doxygen from [the website](https://www.doxygen.nl/) 
2. Install documentation requirements for Schola using pip, for example `pip install -e "./Resources/python[docs]"` from the plugin root (or `pip install -e ".[docs]"` after `cd` into `Resources/python`).
3. Run the command `schola build-docs --builder html` from the root of this project (or supply the path to the plugin folder)

## Contact

For questions about Schola, contact the team at [Schola@amd.com](mailto:Schola@amd.com).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for issue and pull request guidelines, coding standards, and testing expectations.