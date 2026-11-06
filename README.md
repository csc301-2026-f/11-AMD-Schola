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

**Quick links:** [About](#about-this-project) · [What's new in D3](#whats-new-in-d3) · [Try the demo](#try-it-the-return-to-base-demo) · [Train and run your own agent](#train-and-run-your-own-agent) · [Key use cases](#key-use-cases) · [Deployment](#deployment) · [Non-functional requirements](#non-functional-requirements) · [Unity package (developers)](#unity-package-developers) · [Git/GitHub workflow](#gitgithub-workflow) · [License](#license) · [Unreal setup](#getting-started-unreal)

## About this project

**Schola for Unity** is a course project (CSC301, Team 11 "Oscorp") built with our partner AMD. It ports AMD's open-source **Schola** toolkit from Unreal Engine to **Unity**.

**What is reinforcement learning (RL)?** An agent, such as a game character, learns by trial and error: it gets rewards for useful actions and gradually learns what to do. The result of training is a *model*, a file that can look at a situation and choose an action.

**What Schola does.** Schola already connects Unreal Engine games to popular Python RL tools (Gymnasium, Stable-Baselines3, RLlib). Unity had no equivalent, so Unity developers had to build that connection themselves. Our Unity package lets a developer:

1. describe what an agent can **see** (sensors) and **do** (actuators) in their scene,
2. **train** the agent with Schola's existing Python tools, and
3. **run the trained model inside Unity**, with no Python running.

**Why it matters.** Developers who already use Schola in Unreal can keep the same training tools in Unity, and Unity developers get a ready-made path from "an agent in my scene" to "a trained agent in my game" instead of writing their own integration. It is an alternative to Unity ML-Agents for teams that want Schola's workflow, and it supports AMD's plan to make Schola available across several game engines.

**Who it is for.** Unity game developers and RL researchers who already use, or want to use, Schola's Python training tools. You do not need to know reinforcement learning to try the demo.

**Where we are.** Both halves now work: an agent can be **trained** from Unity through Schola's Python tools, and the resulting model can be **run inside Unity** with no Python. Course planning documents live in [`deliverables/`](deliverables/).

## What's new in D3

Since D2 (tag `D2`) the main gap, that training from Unity did not exist, is closed:

- **Train from Unity (US4, PR [#20](https://github.com/csc301-2026-f/11-AMD-Schola/pull/20)):** `schola unity train` trains a PPO agent in a running Unity scene using Schola's existing Python tools.
- **Train, export, run (US5, PR [#21](https://github.com/csc301-2026-f/11-AMD-Schola/pull/21)):** the same command can export the trained policy as an ONNX file (`--export-onnx`), and the new `PolicyRunner` component runs it in Unity with Python closed. Out-of-range model outputs are clamped to the action bounds instead of crashing the scene.
- **Process:** every CI job now has a timeout (PR [#22](https://github.com/csc301-2026-f/11-AMD-Schola/pull/22)), and architecture diagrams requested by AMD were added to [`Documentation~/architecture`](Unity/com.amd.schola/Documentation~/architecture/us1-us4-uml.md).

Where to find it: [Train and run your own agent](#train-and-run-your-own-agent) below, which also links a [start-to-finish video walkthrough](https://www.youtube.com/watch?v=TPu1wXgE_mE). The full analysis, with before and after, is in [`deliverables/D3/product-evolution.md`](deliverables/D3/product-evolution.md).

### How we addressed D2 feedback

| Feedback | What we did |
| --- | --- |
| AMD (D2 demo) asked for an organized UML diagram of the interfaces and design decisions. | Added [`us1-us4-uml.md`](Unity/com.amd.schola/Documentation~/architecture/us1-us4-uml.md). At the 3 November meeting AMD reviewed it and asked for clearer names (for example `IReward`, `IEpisodeStatus`) and an explanation of how the training connection is configured. Both are still on our to-do list; see the [minutes](deliverables/team/minutes/26-11-03-minutes.md). |
| Our own D2 review: US4 had not started. | US4 is implemented and merged ([#20](https://github.com/csc301-2026-f/11-AMD-Schola/pull/20)). |
| Our own D2 review: CI arrived late and some PRs were merged without review. | CI now runs on every PR with time limits. Recent PRs have written descriptions, and PRs [#19](https://github.com/csc301-2026-f/11-AMD-Schola/pull/19), [#20](https://github.com/csc301-2026-f/11-AMD-Schola/pull/20) and [#22](https://github.com/csc301-2026-f/11-AMD-Schola/pull/22) each carry a teammate's approval on GitHub. |
| Our own D2 review: meeting minutes stopped after 1 October. | Minutes for 3 November are in [`deliverables/team/minutes/`](deliverables/team/minutes/). |

## Try it: the Return to Base demo

There is no website or hosted version: Schola for Unity is a Unity package, so you try it by opening it in the Unity Editor (see [Deployment](#deployment)). The demo needs **no Python, no coding and no training**. It shows a model that was trained beforehand steering six drones back to their base.

**You need:** a computer with [Unity Hub](https://unity.com/download) and **Unity 6000.3** (we tested with 6000.3.24f1), [Git](https://git-scm.com/downloads) (or use *Code → Download ZIP* on GitHub), and an internet connection the first time so Unity can download one package.

1. **Get the code.** Run `git clone https://github.com/csc301-2026-f/11-AMD-Schola.git` (or download and unzip the ZIP). Remember where you put the folder.
2. **Create a project.** In Unity Hub choose **New project**, pick the editor 6000.3, a **3D** template, and name it anything (for example `ScholaDemo`).
3. **Add the Schola package.** In Unity open **Window → Package Management → Package Manager**, click **+ → Install package from disk…** and select `Unity/com.amd.schola/package.json` from the folder you downloaded. Wait until the spinner at the bottom right finishes. Keep the downloaded folder where it is.
4. **Add the demo files.** In Unity's **Project** window create a folder `Assets/Demo`, then drag `ReturnToBaseGame.cs` and `move_to_origin.onnx` from [`deliverables/D2/demo/`](deliverables/D2/demo/) into it.
5. **Set up the scene.** Use a scene that has a **Main Camera** and a **Directional Light**. Choose **GameObject → Create Empty**, name it `Demo`, drag `ReturnToBaseGame.cs` onto it, then drag `move_to_origin.onnx` into the component's **Model** field.
6. **Press Play** and click the **Game** tab.

**What you should see:** six orange cube drones start away from a blue base and fly back by themselves. A drone turns **green** when it reaches home, and the on-screen counter goes from `0 / 6` to `6 / 6`. The overlay should read `Brain: Trained ONNX model (move_to_origin)`. If it says `Stand-in policy`, the model is not assigned, so repeat the last part of step 5.

**Things to try:** click a drone to knock it to a random spot, drag a drone and let go, or press **Space** to scatter all drones. The drones always find their way back.

More detail, troubleshooting and a [video walkthrough](https://youtu.be/utxF_vXdSw8) are in the [full demo guide](deliverables/D2/demo/README.md).

![Six orange drones spread around the blue base, with 0 of 6 home.](deliverables/D2/demo/d2demo1.jpeg)
![Drones gathering at the base; one has turned green.](deliverables/D2/demo/d2demo2.jpeg)
![All six drones are green and home.](deliverables/D2/demo/d2demo3.jpeg)

## Train and run your own agent

This is the full workflow added in D3: train a cube to reach a target, export what it learned, then run it with Python closed. It takes about 15 minutes plus a short training run.

**Video walkthrough (start to finish):** [D3 demo video on YouTube](https://www.youtube.com/watch?v=TPu1wXgE_mE). It shows the whole loop: after a short 2,000-step training run the cube reaches the target with Python closed. The commands below use 10,000 steps for a safer margin; use `--timesteps 2000` to match the video.


**You need (in addition to the demo requirements):** Python **3.10 to 3.12** and a terminal.

**Step 1: install.** Install the package in Unity as in steps 1 to 3 of the demo, then, from the repository root, install the Python tools:

```bash
python -m pip install -e "./Resources/python[sb3]"
```

**Step 2: train.**
1. In Unity's **Package Manager**, select **AMD Schola** and import the **Training Demo** sample.
2. Choose **Tools → Schola → Create Training Demo Scene**, save the scene, then in **Edit → Project Settings → Player** turn on **Run In Background**.
3. Press **Play** in Unity. The cube waits for Python.
4. In your terminal run:

```bash
schola unity train --timesteps 10000 --checkpoint ckpt/unity_ppo --export-onnx
```

You will see training progress in the terminal while the cube moves. In our recorded run, 2,000 steps were enough for the cube to learn to reach the target, but results can vary between runs, so a very short run does not guarantee it. When it finishes, the terminal prints `Saved Unity policy to ckpt/unity_ppo` and `Exported ONNX policy to ckpt/unity_ppo.onnx`.

**Step 3: run what it learned, with Python closed.**
1. Stop Play Mode and close the terminal.
2. Drag `ckpt/unity_ppo.onnx` into the Unity **Project** window.
3. Select a GameObject, click **Add Component**, search for **Policy Runner** (it also adds a Stepper Runner automatically), add the cube's `ScholaAgent` to its **Agents** list and drag the `.onnx` file into **Model**.
4. Press **Play**. The cube is now controlled by the model with no Python running. If the model does not fit the agent (wrong input or output names or sizes), the Console shows a message saying why instead of failing silently.

Good to know: training runs about one step per Unity frame, so it is slow, and it supports one agent per environment. More options and limits are in the [training guide](Unity/com.amd.schola/Documentation~/training.md).

### Configuration

There are no API keys or accounts to set up. These are the only settings, and none of them is a secret. Defaults are used if you change nothing.

| Setting | Default | Where to change it |
| --- | --- | --- |
| Unity training server port | `8001` (loopback only) | **Training Server** component in Unity, and `--unity-port` on the command |
| Python adapter port | `8000` for `schola unity bridge` (`schola unity train` picks a free one) | `--port` on `schola unity bridge` |
| Request timeout | 30 seconds | **Request Timeout Seconds** on the Training Server, and `--timeout` on the command |
| Training length and seed | `--timesteps 10000`, `--seed 0` | Command options |
| Model files | `ckpt/unity_ppo.zip` and `.onnx` | `--checkpoint` and `--onnx-path` |
| CI secrets (maintainers only) | `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` | GitHub repository secrets. Never commit their values |

## Key use cases

Everything is built around one loop that repeats while the game runs: **observe** (sensors read the game) → **think** (the model chooses an action) → **act** (actuators change the game).

| # | Use case | What a developer does | Status |
| --- | --- | --- | --- |
| 1 | **Define what an agent sees** (US1) | Add a `ScholaAgent` and sensors such as `PositionSensor`. Observations are read fresh from the scene and keep a consistent format. | Done |
| 2 | **Apply an agent's actions** (US2) | Add actuators such as `TranslateActuator`. Valid actions move the object, and invalid actions are rejected with a clear error. | Done |
| 3 | **Control training episodes** (US3) | Use `ScholaEnvironment` to report rewards, episode endings (terminated or truncated) and resets, so an agent can learn from repeated attempts. | Done; reused by the Python training connection |
| 4 | **Train through Schola** (US4) | Connect the Unity environment to Schola's Python tools and run `schola unity train`. Optionally export the result as ONNX with `--export-onnx`. | New in D3. Works for one agent per environment with synchronous Transform-based stepping |
| 5 | **Run a trained policy** (US5) | Load an ONNX model with `SentisPolicy` (Unity Inference Engine), or just add a `PolicyRunner` component. `SimpleStepper` and `StepperRunner` repeat observe, think, act for every agent. A model whose names or shapes do not fit the agent is rejected with a clear error, and out-of-range outputs are clamped to the action bounds. | Working. The Return to Base demo runs a pre-made model, and [Train and run your own agent](#train-and-run-your-own-agent) runs one you trained |
| 6 | **Set up and try the package** (US6) | Follow a guide and sample scene to install the package and use a trained model. | In progress. The [demo guide](deliverables/D2/demo/README.md) is the first version |

Typical developer path: build the agent (1, 2), define rewards and resets (3), train in Python and export an ONNX model (4), then run it in the game (5).

**What to try if you are evaluating the project:** (a) the [Return to Base demo](#try-it-the-return-to-base-demo) for a 5-minute look at a finished result, then (b) [Train and run your own agent](#train-and-run-your-own-agent) for the new D3 workflow, and (c) the [EditMode and Python tests](#run-the-tests) for engineering quality.

## Deployment

**There is no deployed URL.** Schola for Unity is a library that runs inside a developer's own Unity Editor and Python environment, so there is no website or server to host. What a user "deploys" is the package itself, and the fastest way to evaluate it is the local setup in [Try it](#try-it-the-return-to-base-demo). The demo model and the Training Demo scene builder play the role of seed data, so nothing needs to be created or configured by hand.

How the package is built and shared:
- **Install from disk** (supported for evaluation): point Unity's Package Manager at `Unity/com.amd.schola/package.json`.
- **GitHub Release:** pushing a tag such as `unity-v0.1.0` runs [`release.yml`](.github/workflows/release.yml), which checks the tag matches `package.json`, runs the Unity tests and publishes an installable tarball. No release has been published yet.
- **Not automatic yet:** merging to `main` runs the full CI checks but does not publish anything. Publishing is a deliberate version tag.

## Non-functional requirements

Schola for Unity is a developer package, not a web app, so we applied the standards for APIs and backends (input validation, clear errors, secure local communication), plus basic accessibility for the one piece of UI we ship.

| Area | What we did | Where to see it |
| --- | --- | --- |
| **Input validation** | Every action from Python is checked against the declared space before anything in the scene moves: matching keys, shapes and bounds, and no NaN or infinite numbers. Messages are limited to 4 MiB and value trees to 10,000 nodes. `TranslateActuator.MaxDisplacement` bounds the action range. Model outputs that exceed the bounds are clamped. | `Training/TrainingWire.cs`, `Runtime/Actuators/TranslateActuator.cs`, `Inference/SentisPolicy.cs`; tests in `Tests/Training` and `Tests/EditMode/Policies` |
| **Meaningful errors** | Failures come back as a short message, not a stack trace: Unity replies `{ok:false, error:"…"}` and the Python adapter turns that into the right gRPC status (invalid argument, failed precondition, deadline exceeded, unavailable). Models that do not fit an agent, or a `PolicyRunner` with no agent or model, log what is wrong and stop cleanly. After a failed step the environment must be reset before stepping again, so training never continues from a half-applied step. | `Resources/python/schola/unity/bridge.py`, `Training/TrainingSession.cs`, `Inference/PolicyRunner.cs` |
| **Secure communication** | Both listeners bind to `127.0.0.1` only, the gRPC server uses local credentials, one trainer connects at a time, and every exchange has a timeout. Nothing is sent over the internet. | `TrainingServer.cs`, `bridge.py` |
| **Secrets** | The repository contains no keys or passwords. CI reads the Unity license from GitHub secrets, never from workflow files. | [Configuration](#configuration), `.github/workflows/` |
| **Reliability** | Scene changes run only on Unity's main thread. A failed exchange closes the connection so a stale reply is never read as the next answer. Stopping Play Mode releases all queued work. | `TrainingServer.cs`, `Tests/Training/TrainingServerTests.cs` |
| **Production-readiness** | Every GitHub Actions job has a time limit. CI runs formatting, `.meta`-file, Unity, Python and codegen checks on every pull request. Over 70 Unity EditMode tests and the Python adapter tests run in CI. | `.github/workflows/`, [Run the tests](#run-the-tests) |
| **Accessibility** | There is no end-user UI to audit with Lighthouse. The only on-screen UI is the demo overlay, and it never relies on colour alone: a drone turns green when home and the text counter also shows `n / 6`. README images have alt text. We have not yet tested with assistive tools. | `deliverables/D2/demo/ReturnToBaseGame.cs` |

Known limits: training runs at about one step per Unity frame, supports one agent per environment, and has not been validated in a built player (IL2CPP).

## Unity package (developers)

Package path: [`Unity/com.amd.schola`](Unity/com.amd.schola) (`com.amd.schola`, Unity **6000.3**). It depends on `com.unity.ai.inference` and `com.unity.test-framework`.

### Install into a Unity project

1. Install **Unity 6000.3** (team CI uses **6000.3.24f1**; other 6000.3.x patches usually work for local work).
2. Create an empty 3D project in Unity Hub.
3. **Window → Package Management → Package Manager** → **+** → **Install package from disk…** → select `Unity/com.amd.schola/package.json` from this checkout.
4. Wait for import/compile to finish. **AMD Schola** should appear under Package Manager → **In Project**.

Keep the repo checkout in place while the package is installed from disk. Pushing a `unity-v<version>` tag publishes a GitHub Release with an installable tarball (see [Deployment](#deployment)); no release has been published yet.

### Run the tests

**Unity (EditMode, more than 70 tests).**

1. In the consumer project's `Packages/manifest.json`, add (alongside `dependencies`):

```json
"testables": [ "com.amd.schola" ]
```

2. **Window → Analysis → Test Runner** (or search "Test Runner") → **EditMode**.
3. Expand **AMD.Schola.Tests.EditMode** and **Run All**.

These tests exercise sensors/observations, actuators/actions (including rejecting bad values), `ScholaEnvironment` rewards, termination and resets, `SentisPolicy` and `PolicyRunner` with ONNX models, the observe, think, act stepper, and the training server and session. Some tests intentionally log errors to verify failure handling; green checks in Test Runner are what matter.

**Python (training adapter).** From the repository root, install the test tools (the same way CI does) and run the tests:

```bash
python -m pip install --group Resources/python/pyproject.toml:test -e "./Resources/python[sb3]"
python -m pytest Test/unity --confcutdir=Test/unity --import-mode=importlib -n 0 --timeout=60
```

The tests that need a live Unity Editor skip themselves when none is running, so this command is safe anywhere. Setup for the live tests is in the [training guide](Unity/com.amd.schola/Documentation~/training.md#verification).

Both suites also run automatically on every pull request (see [Git/GitHub workflow](#gitgithub-workflow)).

### Train Unity agents (US4)

The step-by-step guide is in [Train and run your own agent](#train-and-run-your-own-agent). The packaged adapter connects Unity to Schola's existing Python environment wrappers, so no user-written training connection is required. See the [Unity training guide](Unity/com.amd.schola/Documentation~/training.md) for the separate `schola unity bridge` command, reset modes and limitations. This implementation supports one agent per environment with synchronous Transform-based stepping; it does not advance physics by a fixed interval.

### What is / is not included yet

| Available on `main` | Not finished yet |
| --- | --- |
| Runtime agents, sensors, actuators, spaces/points | General multi-agent and physics-stepping training |
| Local episode/env APIs (`ScholaEnvironment`) | Packaged Setup Demo sample and full setup guide (US6); today the demo files live in [`deliverables/D2/demo/`](deliverables/D2/demo/) |
| ONNX inference (`SentisPolicy`, `PolicyRunner`) and steppers (`SimpleStepper`, `StepperRunner`) | Headless smoke player project for CI build/run jobs |
| Return to Base demo with a trained ONNX model | Batched inference and naming/interface changes requested by AMD on 3 November |
| Unity/Python training server, adapter, PPO CLI, ONNX export and Training Demo (US4, US5) | Player/IL2CPP validation for the training connection |
| CI/CD: tests, formatting, meta-file checks, codegen check, release workflow, job timeouts | Automatic publishing of the package when `main` changes (today it happens on version tags) |

Training notes and codegen for Unity C# bindings: [`Unity/com.amd.schola/Training/README.md`](Unity/com.amd.schola/Training/README.md). CI workflows live under [`.github/workflows/`](.github/workflows/).

## Git/GitHub workflow

We forked AMD's open-source Schola repository into our course organization, and all work happens in that fork. The process is:

1. **Track the work.** Each task is a card on our shared Trello board (lists: *Backlog → Ready → In Progress → In Review → Done*). Each card has one owner and acceptance criteria copied from its user story (US1-US6).
2. **Branch from `main`.** One short-lived branch per task, named after the story or task, for example `US5-inference-stepper`, `feature/us2-apply-agent-actions` or `CICD-task8`. Nobody commits directly to `main`.
3. **Commit with Conventional Commits.** For example `feat(unity): add StepperRunner to drive a stepper each frame`.
4. **Open a pull request into `main`** and fill in the [PR template](.github/pull_request_template.md): what changed, how it was tested, and a checklist. The PR **title** must start with one of `feat`, `fix`, `docs`, `test`, `refactor`, `build`, `chore` or `ci`, and CI checks it.
5. **Get a teammate's review.** Every PR needs at least one reviewer who is not the author, including for AI-assisted code. The author must be able to explain every change. Review comments are fixed on the same branch.
6. **Automated checks must pass** (GitHub Actions, summarised by one `ci-ok` job). Every job has a `timeout-minutes` limit so a stuck job cannot use up shared CI time:
   - C# formatting with CSharpier, Unity `.meta` file completeness, package manifest and PR-title checks,
   - Unity EditMode tests, run with GameCI,
   - Python regression tests, a protobuf-to-C# codegen check, and an end-to-end smoke test against a mock server.
7. **Merge into `main`** with the GitHub *Merge pull request* button once the review is approved and checks are green. If a merge turns out to be wrong, we revert it with a new PR rather than rewriting history.
8. **Release (on a version tag, not on every merge).** The version lives in `Unity/com.amd.schola/package.json`. Pushing a tag such as `unity-v0.1.0` runs a workflow that checks the tag matches that version, runs the Unity tests, and publishes a GitHub Release with the package tarball.

New source files carry AMD's copyright header, and every new Unity file or folder is committed together with its `.meta` file. More detail is in [CONTRIBUTING.md](CONTRIBUTING.md).

## License

This project is released under the **MIT License**. See [LICENSE.txt](LICENSE.txt) (Copyright (c) 2023-2026 Advanced Micro Devices, Inc.).

**Why MIT:**
- Upstream Schola is MIT-licensed. Our work is a fork and extension of it, so we keep the same license and copyright notice.
- At our 1 October 2026 meeting, AMD's representative said the proposed work is acceptable under the MIT license, and AMD anticipated that it could later be merged back into the upstream repository with contributor attribution.
- It is permissive: studios and researchers can use, modify and ship Schola for Unity in their own games and tools, with no extra obligations beyond keeping the notice.
- Our [planning document](deliverables/D1/planning.md) records that the code is open source under MIT and can be shared publicly. By contributing, you agree that your contributions are licensed under the same terms.

Third-party dependencies such as Unity Inference Engine, gRPC and Protobuf keep their own licenses.

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
To use the Unreal plugin in an existing Unreal Engine project, copy this repository to the `/Plugins` folder of your project, and pip install the Schola python package in `/Resources/python` using `pip install -e <path-to-plugin>/Resources/python[all]` (the folder that contains `pyproject.toml`). For Unity, see [Unity package](#unity-package-developers) above.

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

## AI Usage

Claude Opus 5.5  was used to co-develop the Unity Sentis Policy