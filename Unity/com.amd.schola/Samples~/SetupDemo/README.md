# Setup Demo

A deterministic cube-to-target installation demonstration using real `ScholaAgent` and `PositionSensor` observations. The controller sends its chosen displacement through `ScholaAgent.Act()` to a `TranslateActuator`; Schola applies the movement. No Python, trained model, rewards, or episode system is required or demonstrated.

1. Use Unity 6.3 (the repository CI target is **6000.3.24f1**) and a **3D Built-In Render Pipeline** project.
2. Import **Setup Demo** from AMD Schola's Package Manager Samples section.
3. After compilation, choose **Tools > AMD Schola > Create Setup Demo Scene**.
4. Save as **Assets/ScholaSetupDemo.unity**. Save any current scene when prompted.
5. Press Play. The cube should approach the sphere in about three seconds. The Game overlay shows `agent_position`, `target_position`, and **Target reached**. Move Target while playing to see it follow again.

If you imported the earlier observation-only demo, back up customizations before reimporting **Setup Demo**, then create a new scene with the updated builder. Existing imported sample scripts and scenes are not updated automatically. To keep an existing scene, add `TranslateActuator` to Schola Cube, set its **Action Key** to `translate`, and set Schola Agent's **Actuators** list to contain that component.

The scene is generated and serialized by your Unity Editor. It was not created or verified in the implementation environment, where Unity was unavailable.

In Project, open **Packages > AMD Schola** and view `Documentation~/setup.md` on disk for the full installation, Inspector, adaptation, troubleshooting, branch findings, and validation guide. In the repository, it is at **Unity/com.amd.schola/Documentation~/setup.md**. The repository root README also links it. A `~` folder is intentionally hidden in Unity's asset browser; use your file browser or repository viewer to read it.

`Runtime/SetupDemoController.cs` validates sensor/actuator wiring, collects `Observe()` values, sends `DictPoint`/`BoxPoint` displacements through `Act()`, and displays current observations. `Editor/SetupDemoSceneBuilder.cs` creates the scene and references, including the registered actuator. `Tests/Editor/SetupDemoTests.cs` checks movement, observation freshness, missing actuators, rejected action keys, and scene save/reload. These tests become discoverable after sample import; they are not automatically imported by package-mode CI.

Duplicate the generated scene before adapting it. Keep one imported sample copy to avoid duplicate assembly names. Stop Play Mode before creating another scene. Changes made during Play Mode normally revert when it stops.
