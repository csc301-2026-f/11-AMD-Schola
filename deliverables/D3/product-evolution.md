# Schola for Unity (Team 11, Oscorp) - Product Evolution (D3)

## 1. What Needed to Improve

After D2, Schola for Unity could observe, act and run a trained model, but it could not do the thing that makes Schola useful: **train an agent from Unity**. Our own D2 retrospective said it plainly: "The training connection (US4) has not started." Without it, a developer had no way to get from "an agent in my scene" to "a trained agent in my game". The only model we could run, in the Return to Base demo, was made by hand outside the product. A second, smaller gap sat right behind it. Nobody had checked that a model trained through Schola would actually run in Unity, so the promise "train, then play with no Python" was unproven.

We found the gap in three ways. First, the D2 review (Q2, Q3a) listed US4 as the main thing that went wrong and named it our main focus. Second, AMD described the MVP as "a sample environment that trains in Unity", and the D2 demo, which hard-coded part of the flow, made the missing piece obvious. Third, the 3 November meeting with AMD showed that the training sequence, reset behaviour and training-state connection were what AMD wanted to review next ([minutes](../team/minutes/26-11-03-minutes.md)).

## 2. What You Did

We built the full loop: **train in Python, export, run in Unity**.

**Training connection (US4).** `schola unity train` starts a small adapter that makes a running Unity scene look like any other Schola environment, so Schola's existing Python trainers (PPO through `VecEnv`) work unchanged. Unity side, a `TrainingServer` component accepts one local connection and queues each request, and `TrainingSession` applies it on Unity's main thread by calling the `ScholaEnvironment` we built for US3. Python side, the adapter turns Schola's gRPC messages into small JSON messages. Each message carries a length prefix and is limited to 4 MiB. Reset handling follows what AMD asked about: the three Schola reset modes (reset on the same step, reset on the next step, or only on request) are supported, and the final observation of an episode is kept when a reset happens in the same step. The work was done by Talha in PRs [#17](https://github.com/csc301-2026-f/11-AMD-Schola/pull/17) and, after a revert, [#20](https://github.com/csc301-2026-f/11-AMD-Schola/pull/20).

**Export and run (US5).** `schola unity train --export-onnx` saves the trained policy as an ONNX file. A new `PolicyRunner` component runs that file on a chosen agent with no Python: add the component, assign the agent and the model, press Play. Anwar built this in [#21](https://github.com/csc301-2026-f/11-AMD-Schola/pull/21).

**Process and documentation.** Every GitHub Actions job now has a time limit and the Unity test limit dropped from 30 to 10 minutes ([#22](https://github.com/csc301-2026-f/11-AMD-Schola/pull/22)). Fahad added architecture diagrams for AMD ([`us1-us4-uml.md`](../../Unity/com.amd.schola/Documentation~/architecture/us1-us4-uml.md)), and the README now has a full "train and run your own agent" guide.

**Key design decisions.**
- *Why JSON over a local socket instead of gRPC inside Unity?* gRPC inside Unity needs native libraries that are hard to ship in a package. Keeping the Unity side plain C# means the package installs cleanly, and the Python side reuses Schola's existing gRPC clients unchanged.
- *Why only the main thread?* Unity allows only its main thread to change a scene. The background thread only reads and queues messages, and `Update()` does the work, one request per frame. This is safe, but it makes training slow, and we state that limit in the docs.
- *Why clamp model outputs?* See the integration challenges below.

**Integration challenges.**
1. *The first training PR was merged and then reverted* (#17 then #18) and was restored unchanged in #20. The lesson was about process: the revert PR had a one-line description, so the reason is not written down anywhere a reviewer can find it.
2. *A trained model crashed on its first step.* When Anwar ran the exported model in Unity, it produced values around ±2, outside the cube's allowed range, and `TranslateActuator` correctly rejected them and threw. The fix was to clamp Box actions to the declared bounds in `SentisPolicy`, the way Stable-Baselines3 clips actions during training. We would not have found this without running the whole loop end to end.
3. *Dictionary-shaped actions.* Schola trains with merged actions, but Unity expects a dictionary of named actions. The exporter now receives the original unmerged action space so names and shapes line up with what the agent defines.
4. *Names and shapes must match.* A model only works if its input and output names and sizes match the agent's sensors and actuators. `SentisPolicy` and `PolicyRunner` check this and log a plain explanation instead of failing silently.

## 3. Before vs. After

| | After D2 | Now (D3) |
| --- | --- | --- |
| Train from Unity | Not possible. `Training/` held only code generation. | `schola unity train` trains PPO in a running scene. |
| Source of the model | Hand-made `move_to_origin.onnx`. | A model trained from Unity, exported with `--export-onnx`. |
| Run a model | Wire up `SentisPolicy` and the stepper by hand (see the demo script). | Add a `PolicyRunner`, assign agent and model, press Play. |
| Bad model or bad action | A model with the wrong shape was rejected, but out-of-range outputs crashed the first step. | Mismatches log a clear reason and out-of-range outputs are clamped. Invalid actions from Python are rejected before the scene changes. |
| Failure handling in training | Not applicable. | Unity replies with a short error message that Python maps to a gRPC status. After a failed step the environment must be reset, so training never continues from a half-applied step. |
| Automated tests | Unity EditMode tests for US1 to US3 and the inference stepper only. | More than 70 Unity EditMode tests, 17 Python adapter and live tests (see [training guide](../../Unity/com.amd.schola/Documentation~/training.md#verification)), all run in CI. |
| CI time limits | Four jobs without a limit and a 30-minute Unity limit. | Every job has a limit. Unity tests are limited to 10 minutes. |
| AMD feedback | UML diagram requested. | UML added and reviewed on 3 November. AMD's naming and reset feedback is on the to-do list. |

Evidence: the training guide records a validation run in which 65 EditMode tests and 17 Python adapter tests passed against a real batch Unity Editor, including a 32-step PPO optimizer run and a checkpoint reload. The CI run on PR #20 was green, and the README now walks a TA through the loop in a few minutes.

**Demo video:** [start-to-finish walkthrough on YouTube](https://www.youtube.com/watch?v=TPu1wXgE_mE), recorded by Anwar, showing training for 2,000 steps, then running the result in Unity.

**Result of the recorded run:** in the video, after only 2,000 training steps, the exported model moved the cube to the target ball with Python closed. That is the first time the full loop (train in Unity, export, run in Unity) has been shown to work with a model we trained ourselves.

What we do **not** claim: this is one recorded run, so we cannot say how reliably the cube learns to reach the target across different seeds or step counts, and we did not measure training speed.

## 4. Reflection

**What worked well.** Building on shared interfaces paid off again: the training server needed only the `ScholaEnvironment` from US3, and the runner only the `IAgent` and `SentisPolicy` from US5, with almost no changes to existing code. Writing the limits down (one agent, one request per frame, not tested in a built player) in the docs meant AMD's questions on 3 November were about design, not surprises. Running the whole loop end to end found the clamping bug that unit tests had missed.

**What was harder than expected.** Threading: keeping all scene work on Unity's main thread while a background thread waits on the network took the most care, and it is why training is slow. Matching model names and shapes between Python's exporter and Unity was a second source of friction. The process was also harder than the code: the revert of #17 cost us time, and PR #21 was merged without a review recorded on GitHub, which breaks our own rule. We would add branch protection that requires one approval, as our D2 retrospective planned.

**Path to D4.** Remaining work, in order: (1) US6, a packaged setup sample and tutorial that covers training and not only inference, which we agreed with AMD on 3 November; (2) the interface changes AMD suggested, for example clearer names than `IReward` and `IEpisodeStatus`, and explaining how the training-state connection is configured; (3) checking multiple environments and moving state collection into the service, as AMD recommended; (4) publishing a first tagged release and, if time allows, publishing the package automatically when `main` changes; and (5) optional batched inference. The training connection is the riskiest part of the project and it is now working, so we think this plan is feasible by the course deadline in mid-December.
