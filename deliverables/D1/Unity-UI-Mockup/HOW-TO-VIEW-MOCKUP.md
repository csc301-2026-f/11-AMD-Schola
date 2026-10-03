# Schola Unity UI Mockup

**This is an editor-only UI mockup for review.** It shows a proposed Unity interface for Schola; it does not train agents, run inference, connect to Python, or save configuration. It is not an AMD Unity integration.

## View the screenshots

No installation is needed to review these:

- [Training](Screenshots/training.png)
- [Agents](Screenshots/agents.png)
- [Sensors](Screenshots/sensors.png)
- [Inference](Screenshots/inference.png)
- [Light theme](Screenshots/training-light.png)
- [Narrow Inspector](Screenshots/inspector-320.png)

These captures show the mockup before its labels were changed from “UI Preview” to “UI Mockup”.

## Install and open (optional)

Requires **Unity 6.3** (6000.3.24f1). This folder contains a local Unity package, not a standalone Unity project.

1. Clone or download this repository and extract it if needed.
2. In Unity Hub, create a new empty Unity 6.3 project and open it.
3. Open **Window → Package Management → Package Manager**.
4. Choose **+ → Install package from disk…** and select `Unity-UI-Mockup/com.amd.schola.ui-mockup/package.json` from this repository.
5. Open **Window → AMD Schola → UI Mockup**. No Play mode is needed.

Keep the downloaded folder in place while the package is installed. No Python, Unreal Engine, models, or additional packages are needed.

Browse the tabs, dropdowns, and foldouts to explore the example screens. Configuration fields are read-only and training/inference buttons are disabled. **Show in Inspector** displays a temporary preview object; it creates no scene object or asset. Closing the window resets the preview choices.

To uninstall, close the mockup and remove **AMD Schola — Unity UI Mockup** in Package Manager.

## Files

- `com.amd.schola.ui-mockup/`: editor UI code, styles, example data, and Unity package metadata.
- `Screenshots/`: all six original Unity Editor screenshots.

The package retains the [MIT license and AMD attribution](com.amd.schola.ui-mockup/LICENSE.md).
