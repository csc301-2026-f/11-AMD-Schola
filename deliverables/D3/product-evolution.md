# Schola for Unity (Team 11, Oscorp) - Product Evolution (D3)

## 1. What Needed to Improve

After D2, Schola for Unity could observe a scene, act on it and run a model, but it could not do the thing that makes Schola useful, which is **training an agent from Unity**. Our own D2 retrospective put it plainly when it said that "the training connection (US4) has not started", and at that point the `Training/` folder held only code-generation setup. Without training, a developer had no way to get from "an agent in my scene" to "a trained agent in my game". The only model we could run, in the Return to Base demo, was made by hand outside the product.

We identified the gap in three ways. First, the D2 review listed US4 as the main thing that went wrong and made it our main focus for the next iteration. Second, AMD had described the MVP as a sample environment that trains in Unity, and our D2 demo had to hard-code part of that flow, which made the missing piece obvious. Third, at the 3 November meeting many of AMD's questions were about training, such as how resets and episode endings reach the Python trainer ([minutes](../team/minutes/26-11-03-minutes.md)).

## 2. What You Did

We built the full loop, so that a developer can **train in Python, export the result, and run it in Unity**.

**Training connection (US4).** The `schola unity train` command starts a small adapter that makes a running Unity scene look like any other Schola environment, so Schola's existing Python trainers work unchanged. In Unity, a `TrainingServer` component accepts one local connection and queues each request, and a `TrainingSession` then applies it on Unity's main thread by calling the `ScholaEnvironment` we built for US3. On the Python side, the adapter turns Schola's gRPC messages into small JSON messages, each with a length prefix and a 4 MiB size limit. The session supports the three Schola reset modes, which reset in the same step, reset on the next step, or wait for an explicit reset. When an episode ends and resets in the same step, the final observation is still sent to the trainer alongside the new starting observation. Talha built this in PR [#17](https://github.com/csc301-2026-f/11-AMD-Schola/pull/17), and after a revert it was restored in [#20](https://github.com/csc301-2026-f/11-AMD-Schola/pull/20).

**Export and run (US5).** With `--export-onnx`, the same command also saves the trained policy as an ONNX file. A new `PolicyRunner` component runs that file on the chosen agents without Python, so the developer only adds the component, assigns the agents and the model, and presses Play. Anwar built this in [#21](https://github.com/csc301-2026-f/11-AMD-Schola/pull/21).

**Process and documentation.** Every GitHub Actions job now has a time limit ([#22](https://github.com/csc301-2026-f/11-AMD-Schola/pull/22)), and we later tightened the limits to match real run times, so the Unity tests now stop after 8 minutes. The README also gained a full "train and run your own agent" guide.

**Key design decisions**
- *Why JSON over a local socket instead of gRPC inside Unity?* Running gRPC inside Unity would mean shipping native libraries with the package. Keeping the Unity side in plain C# lets the package install cleanly, while the Python side still reuses Schola's existing gRPC clients unchanged.
- *Why only the main thread?* Unity only lets its main thread change a scene. The background thread therefore only reads and queues messages, and `Update()` does the real work, one request per frame. This keeps the scene safe, although it also makes training slow, and the training guide states that limit.
- *Why clamp model outputs?* This came out of the integration work described below.

**Integration challenges**
1. *The first training PR was merged and then reverted.* PR #17 was reverted in #18 and restored unchanged in #20. The lesson was about process, because the revert PR had a one-line description and the reason is not written down anywhere a reviewer can find it.
2. *A trained model crashed on its first step.* When Anwar ran the exported model in Unity, it produced values around ±2, which is well outside the cube's allowed range, so `TranslateActuator` correctly rejected them and threw an error. The fix was to clamp Box actions to the declared limits inside `SentisPolicy`, which is also how Stable-Baselines3 clips actions during training. We would not have found this without running the whole loop end to end.
3. *Merged versus named actions.* Schola's trainer works with all actions merged into one list, whereas the Unity agent expects a dictionary of named actions. The export step therefore receives the original, unmerged action space, so that the model's outputs line up with what the agent defines.
4. *Names and sizes must match.* A model only works when its input and output names and sizes match the agent's sensors and actuators. `SentisPolicy` checks this when it starts, and `PolicyRunner` stops with a logged explanation instead of failing silently.

## 3. Before vs. After

| | After D2 | Now (D3) |
| --- | --- | --- |
| Train from Unity | Not possible, because `Training/` held only code generation | `schola unity train` trains PPO in a running scene |
| Source of the model | A hand-made `move_to_origin.onnx` | A model trained from Unity and exported with `--export-onnx` |
| Run a model | Set up `SentisPolicy` and the stepper by hand, as the demo script does | Add a `PolicyRunner`, assign the agent and the model, and press Play |
| Bad model or bad action | A model with the wrong shape was rejected, but outputs past the limits crashed the first step | Mismatches log a clear reason, outputs past the limits are clamped, and invalid actions from Python are rejected before the scene changes |
| Failure during training | Not applicable | Unity replies with a short error that Python maps to a gRPC status, and the environment must be reset before the next step |
| Automated tests | Tests for sensors, actuators, episodes, the Sentis policy and the stepper | 76 Unity tests, now also covering the training server, the training session and `PolicyRunner`, plus 10 Python test functions for the adapter and export, all run in CI |
| CI time limits | Four jobs had no limit, and Unity tests could run for 30 minutes | Every job has a limit, and the Unity tests stop after 8 minutes |

**Demo video.** Anwar recorded a [start-to-finish walkthrough on YouTube](https://www.youtube.com/watch?v=TPu1wXgE_mE) that trains for 2,000 steps and then runs the result in Unity.

**Result of the recorded run.** After only 2,000 training steps, the exported model moved the cube to the target ball with Python closed. This is the first time the full loop of training in Unity, exporting and running in Unity has worked with a model we trained ourselves. The CI run on PR #20 was also green.

**What we do not claim.** This was one recorded run, so we cannot yet say how reliably the cube learns across different seeds or step counts, and we have not measured training speed.

## 4. Reflection

**What worked well.** Building on shared interfaces paid off again. The training server needed only the `ScholaEnvironment` from US3, and the runner needed only `IAgent` and `SentisPolicy` from US5, so almost no existing code had to change. Writing down the limits early, such as one agent per environment, one request per frame and no testing in a built player, meant that AMD's questions on 3 November were about design rather than surprises. Running the whole loop end to end also found the clamping bug that unit tests had missed.

**What was harder than expected.** Threading took the most care, because all scene work has to stay on Unity's main thread while a background thread waits on the network, and that design is also why training is slow. Matching model names and sizes between Python's exporter and Unity caused a second round of friction. The process turned out to be harder than the code, since the revert of #17 cost us time and PR #21 was merged without a review recorded on GitHub, which breaks our own rule. Branch protection that requires one approval, which our D2 retrospective already planned, would prevent that.

**Path to D4.** The remaining work, in order, starts with US6, a packaged setup sample and tutorial that covers training as well as inference, which we agreed with AMD on 3 November. After that come the interface changes AMD suggested, such as clearer names than `IReward` and `IEpisodeStatus`, followed by support for several environments with state collected centrally by the service, as AMD recommended. We then plan to publish a first tagged release and, if time allows, to publish the package automatically when `main` changes, and batched inference is optional. Since the training connection was the riskiest part of the project and it now works, we think this plan is feasible before the course deadline in mid-December.
