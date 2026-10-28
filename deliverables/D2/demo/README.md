# D2 Demo: Return to Base

Return to Base demonstrates AMD Schola's Unity inference workflow. Six cube-shaped drones start at random positions and use the included trained ONNX model to move back to a blue base at the world origin. Clicking or dragging a drone moves it away from home; the policy then guides it back.

## Video walkthrough

[Watch the demo setup and gameplay on YouTube](https://youtu.be/utxF_vXdSw8)

## What the demo demonstrates

Each drone is a real `ScholaAgent` connected to a `PositionSensor` and a `TranslateActuator`. A shared `SentisPolicy` runs [move_to_origin.onnx](move_to_origin.onnx) through Unity Inference Engine, while `SimpleStepper` runs the **observe → think → act** loop for every drone.

| Stage | What happens |
| --- | --- |
| Observe | `PositionSensor` supplies the drone's three-dimensional position. |
| Think | `SentisPolicy` evaluates the trained model to produce a movement action. |
| Act | `TranslateActuator` applies the movement, with each action coordinate clamped to ±0.2. |
| Repeat | The loop runs at 15 steps per second by default, responding to the drone's current position. |

Orange drones are away from home. They turn green when they are within 0.4 Unity units of the origin, and the on-screen **Drones home** counter updates. All drones share the same model but receive their own observations. They can overlap as they converge on the same destination.

This demo runs an already-trained policy inside Unity. It does not train a model during Play Mode or demonstrate the Unity–Python training bridge. No Python process is required to run it.

## Requirements

- Unity **6000.3**; this setup used **6000.3.24f1**.
- A checkout of this repository containing the current AMD Schola package at [`Unity/com.amd.schola`](../../../Unity/com.amd.schola). Older branches may lack the inference, actuator, or stepper classes required by this demo.
- The two demo files in this folder: [ReturnToBaseGame.cs](ReturnToBaseGame.cs) and [move_to_origin.onnx](move_to_origin.onnx).

## Set up and run

1. Create or open a **3D Unity project**. A Universal 3D project works for this demo.
2. Open **Window → Package Management → Package Manager**.
3. Click **+ → Install package from disk…** and select **`Unity/com.amd.schola/package.json`** from this repository. If AMD Schola is already installed from this checkout, skip this step.
4. Wait for package installation and script compilation to finish. AMD Schola declares **Unity Inference Engine 2.6.1** as a dependency, so Unity installs it automatically. Confirm **AMD Schola** appears under **In Project**. Keep the repository in place because this is a local package reference.
5. In the Unity **Project** window, create a folder such as **`Assets/Demo`**. Copy **`ReturnToBaseGame.cs`** and **`move_to_origin.onnx`** into it. The screenshots and this README are not needed inside Unity.
6. Wait for Unity to import both files and finish compiling. Check the **Console** for compile errors before continuing.
7. Open a basic scene containing a **Main Camera** and a **Directional Light**. Ensure the camera has the **MainCamera** tag. The script positions that camera automatically.
8. Choose **GameObject → Create Empty** and name the object **Demo**.
9. Drag **`ReturnToBaseGame.cs`** onto the Demo object's Inspector to add the **Return To Base Game** component.
10. Drag **`move_to_origin.onnx`** from the Project window into the component's **Model** field. Leave **Drone Count = 6** and **Steps Per Second = 15** for the default demonstration.
11. Save the scene, then press **Play** and select the **Game** tab. The script creates the drones, base, and ground at runtime; no manual placement is needed.
12. Verify that the overlay reads **`Brain: Trained ONNX model (move_to_origin)`**. Watch the drones return to base and turn green.

## Controls

Click inside the **Game** view so it receives input.

| Input | Result |
| --- | --- |
| Click a drone | Repositions that drone to a random location; it moves back toward the base. |
| Drag a drone, then release | Moves it along a horizontal plane at its current height; the policy takes over again on release. |
| Space | Scatters all drones to new random positions. |

The **Knocks** and **Drags** counters record interactions during the current run.

## Screenshots

### Drones away from the base

Six orange drones are spread around the scene. The overlay identifies the trained ONNX model and shows **0 / 6** drones home.

![Six orange drones spread around the blue base, with zero drones home.](d2demo1.jpeg)

### Returning to the base

The drones gather near the base. One has reached the home threshold and turned green; the overlay shows **1 / 6** home.

![Drones gathered near the blue base, with one green drone and five orange drones.](d2demo2.jpeg)

### All drones home

All six drones have reached the home threshold and turned green. They overlap near the shared destination, and the counter confirms **6 / 6** home.

![Green drones overlapping at the blue base, with the counter showing all six home.](d2demo3.jpeg)

## Troubleshooting

- **“Can't add script … script class cannot be found”:** Open the Console and resolve compile errors first. The file must be named `ReturnToBaseGame.cs`. Missing `SimpleStepper`, `SentisPolicy`, `TranslateActuator`, or `IPolicy` usually means the installed Schola checkout is too old. Update it to include the current package code, return to Unity, and use **Assets → Refresh** if necessary.
- **`Unity.InferenceEngine` or `ModelAsset` is missing:** Check that Package Manager finished resolving AMD Schola's Inference Engine dependency. Confirm you selected this repository's current package manifest.
- **The overlay says “Stand-in policy”:** Stop Play Mode, assign the ONNX model to the component's Model field, and run again. The script includes a simple fallback controller when no model is assigned or initialization fails, so moving drones alone do not prove the trained model is running. If a model is already assigned, inspect the Console for the initialization error.
- **No useful camera view:** Keep an enabled camera tagged **MainCamera** in the scene. The demo repositions it when Play Mode starts.
- **Mouse or Space does nothing:** Select the Game view and confirm Play Mode is active. The script supports both Unity's new Input System and the legacy input API.

## AI usage

I used GPT-6 Astra to create this README's setup and run instructions, troubleshooting section, requirements section, controls, and to format the whole README.
