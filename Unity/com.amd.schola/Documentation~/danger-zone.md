# Danger Zone training and inference

The blue agent learns to reach the orange target while avoiding the red circle. Every episode randomizes the agent and target z coordinates in [-1.6, 1.6]. The agent starts at x = -2 and the target at x = 2. The red zone stays at the origin. Position observations and bounded translation actions travel through the existing Schola bridge to Stable-Baselines3 PPO.

## Install and open in Unity

Use Unity 6000.3.24f1 and a 3D Built-In project. Install the package from disk by selecting `Unity/com.amd.schola/package.json` in Package Manager. Select AMD Schola and import **Training Demo**. If already imported, back up customizations before reimporting to receive the new files.

Open `Assets/Samples/AMD Schola/0.1.0/Training Demo/Scenes/ScholaDangerZoneTraining.unity`. You can regenerate a scene with **Tools > Schola > Create Danger Zone Training Scene**. The inference builder is **Create Danger Zone Inference Scene**. Save generated scenes under Assets.

Enable **Run In Background** in Player settings. Wait for compilation before entering Play Mode.

## Prepare Python once

In PowerShell, from the repository root, use Python 3.12 (3.10 and 3.11 are also supported). Skip virtual-environment creation if `.venv` already exists.

```powershell
py -3.12 -m venv .venv
.\.venv\Scripts\python.exe -m pip install -e "./Resources/python[sb3]"
```

## Train through the Unity Inspector

1. Stop Play Mode and select **Schola Danger Zone Environment** in the Hierarchy.
2. In **Training Server**, keep Port = 8001 and enable **Run Script On Play**.
3. Choose Script Type = Python, Env Type = Custom Python Path, and set Custom Python Path to the full path of your `.venv/Scripts/python.exe`.
4. Choose Python Script Type = Builtin Schola Unity Training Script.
5. Set Timesteps = 100000, Seed = 0, Checkpoint Path = `ckpt/danger_zone`, and enable Export Final Model To Onnx. Leave Onnx Path empty.
6. Save the scene, open the Console, and press Play. Python launches automatically and logs appear with `[Schola script]`. The HUD counts completed successes/failures and shows episode steps and reward.
7. Wait for **Saved Unity policy** and **Exported ONNX policy** before stopping Play Mode. UI-launched relative paths resolve from the Unity project's working directory. Look for `ckpt/danger_zone.zip` and `ckpt/danger_zone.onnx` there.

The shipped scene leaves Run Script On Play unchecked so you can choose your Python installation before launching. Training duration depends on actual steps per second. A 100000-step run at 100 steps/s takes approximately 17 minutes, plus startup and export. Use a short run to check the connection, then allow the full run for learning. PPO completes 128-step batches. Each run starts a fresh model. Stopping Play Mode kills the child process and can interrupt saving.

To launch from a terminal instead, keep Run Script On Play unchecked, enter Play Mode, and run from the repository root:

```powershell
.\.venv\Scripts\schola.exe unity train --unity-port 8001 --seed 0 --timesteps 100000 --checkpoint ckpt/danger_zone --export-onnx
```

## Run the trained model

1. Stop training/Play Mode and import your exported `danger_zone.onnx` into an Assets folder in Unity.
2. Open the sample's **Scenes/ScholaDangerZoneInference.unity**.
3. Select **Schola Danger Zone Environment**. In **Danger Zone Inference**, assign your imported asset to **Model**. This sample uses Danger Zone Inference to apply the policy through `ScholaEnvironment.Step`, including reward, danger detection, terminal status, and randomized resets.
4. Keep Step Interval = 0.05 for a visible 20 actions/s demonstration. Press Play.
5. The policy runs on CPU. After success, danger entry, leaving the arena, or the step limit, the scene holds for one second and starts another randomized episode. The HUD reports the outcome and cumulative counts.

The inference scene includes a reference policy for this task. Replace its Model assignment with your newly exported model to showcase your own training result.

The original reach-target model has two position inputs. The danger-zone policy needs three inputs named `agent_position`, `target_position`, and `danger_position`, each containing three floats, plus the three-float output `translate`. Use the policy exported from this danger-zone task.

## Manual scene wiring and adaptation

The builder assigns these references for you. For a scene assembled by hand:

| Object/component | Configuration |
| --- | --- |
| Root | Schola Environment, Danger Zone Task, and Training Server for training |
| Agent | Position Sensor key `agent_position`, Translate Actuator key `translate` with Max Displacement 0.1, and Schola Agent |
| Target | Position Sensor key `target_position` |
| Danger Zone | Position Sensor key `danger_position`, at (0, 0.02, 0) |
| Schola Agent | Sensors list contains agent, target, and danger sensors; Actuators list contains agent's Translate Actuator |
| Schola Environment | Agent points to the Schola Agent |
| Danger Zone Task | Agent, Target, and Danger point to their transforms; Danger Radius = 0.8, Arrival Distance = 0.3, Max Steps = 256, Layout Seed = 7 |
| Inference root | Use Danger Zone Inference with environment, agent, and exported Model assigned |

The arena extends to x/z = +/-2.8. The danger visual is a flat cylinder of diameter 1.6. Danger detection checks the complete movement segment against a radius-0.8 circle, so jumping through the circle is also a failure. The agent center determines danger entry. Movement stays at y = 0.25, with the y action projected away before collecting reward/observations. The existing three-value action contract is preserved.

Reward is previous goal distance minus current goal distance, minus 0.005 per step. Success adds 5. Danger entry or leaving the arena subtracts 5 and terminates. Reaching 256 steps truncates. All distances use the x/z plane.

Layout Seed controls the sample's deterministic random layout sequence. The UI's Seed controls PPO. ScholaEnvironment.Seed is currently a no-op, so changing the PPO seed alone does not change the layout sequence. Changing hazard radius, arena dimensions, keys, or reset distributions changes the task and calls for retraining.

## Code changes

- `DangerZoneTask.cs` implements rewards, swept danger detection, planar projection, randomized resets, counters, and HUD. Its explicit episode-status reset hook avoids randomizing twice when ScholaEnvironment notifies both interfaces.
- `DangerZoneInference.cs` uses the existing SentisPolicy and ScholaEnvironment.Step/Reset APIs for policy execution and automatic episode cycling.
- `DangerZoneSceneBuilder.cs` builds the training/inference scenes and persistent colored materials. Training uses the existing Training Server UI and Python command.
- `DangerZoneTests.cs` covers reset behavior, observations, swept hazard entry, success, projection, bounds, and truncation.
- The sample runtime assembly references Unity.InferenceEngine for ModelAsset. Scene, material, script metadata and model assets preserve Unity references.

## Verified results

The reference policy completed 100096 training steps in 357 seconds on the batch validation host. It reached the target on 80 of 100 evaluation layouts with a separate seed. Performance varies with training and scene changes. All 128 Unity checks passed, including Play Mode inference and hazard-task tests. The automatic Training Server launch path also completed a separate 128-step training/export check. See [validation.md](validation.md) for logs, model provenance, and test details.
