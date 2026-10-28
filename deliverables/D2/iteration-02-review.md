# Team 11 Oscorp - AMD Schola for Unity

## Iteration 2 - Review & Retrospect

- When: 2026-10-26
- Where: Online - Microsoft Teams

## Process - Reflection

#### Q1. What worked well

1. **Splitting work by user story on top of shared types.** Early on we agreed on the `Space`/`Point` types and a few small interfaces (`IAgent`, `IPolicy`, `IStepper`, `IActuator`, `ISensor`) based on Schola's Unreal design. Because every story built on the same pieces, several stories were worked on at the same time without waiting on each other, and joining them later was easy. For example, once `ScholaAgent` implemented `IAgent`, it worked with the stepper with no other changes.

2. **Checking risky parts early.** Before building the bigger features we tested the parts most likely to go wrong, like whether a model exported by Schola actually runs in Unity's Inference Engine. Finding that out in the first couple of weeks meant the later work was built on things we already knew worked.

3. **Reviews found real bugs.** Reviews caught problems before they reached main, for example a missing shape check that would have let bad input fail much later, and cases where destroyed objects or exceptions would break the frame loop. Every fix came with a test.

4. **Breaking CI into small tasks.** CI/CD was split into eight tasks (Unity tests, Python regression tests, smoke test, protobuf codegen check, formatting and meta checks, release), each with its own branch and PR. Several people could work on it at once, and each piece was small enough to review.

#### Q2. What did not work well

1. **The training connection (US4) has not started.** Because we focused on inference and the agent pieces, nobody has written the gRPC service yet. The `Training/` folder only has the codegen setup. This matters because the MVP AMD described is a sample environment that trains in Unity, and the setup guide (US6) needs training to work end to end.

2. **CI reached main after most of the features.** The CI branch was merged on Oct 26 (PR #16), after US1, US2 and US5 were already in. Those PRs were never checked automatically, and the US3 PR needed several formatting-fix commits once CSharpier started running on it.

3. **We didn't always follow our own review rule.** Our planning doc says every PR needs a teammate's review, but several PRs near the end of the iteration were merged with no review recorded on GitHub, and some early setup was pushed straight to main.

4. **Meeting minutes stopped after October 1.** The repo only has minutes for Sep 22 and Oct 1, even though we kept meeting. Decisions from later meetings (who does US4, what AMD wants in the demo) are only in chat, which makes them easy to lose.

#### Q3(a). Planned changes

1. **Make US4 the main focus.** The sub-team for the training connection will start with a minimal step/reset exchange with Schola's Python client (matching the `-ScholaPort` behaviour from Unreal) and post progress in the team chat twice a week, so blockers come up early.

2. **Turn on branch protection for main.** Require one approving review and a passing CI run before merging, so the review rule doesn't depend on people remembering it.

3. **Merge shared changes first.** Changes to CI or shared types go into main before feature PRs that depend on them, so features get checked from the start.

4. **Write minutes for every partner meeting.** Whoever runs the meeting commits the minutes to `deliverables/team/minutes/` within a day.

#### Q3(b). Integration & Next steps

We split into sub-teams by user story (US1, US2, US3, US5) plus one for CI/CD. Everything connects through the shared `Space`/`Point` types and the `IAgent`/`IPolicy`/`IStepper` interfaces, and was merged into main through PRs. Next we will build the training connection (US4), finish US5 so a trained model can control an agent in a scene, and then add the sample scene and setup guide (US6).

## Product - Review

#### Q4. How was your product demo?

We developed the demo by integrating completed user stories (US1, US2, US3) with our mock UI and hard-coding incomplete functionality to mimic anticipated functionality. 
We showcased the functionality of US1, US2, US3 and a high level overview of our codebase through an interactive html page. It highlighted completed progress and how each user story relates to each other.
The partner was satisfied with the features showcased in the demo, however requested an organized UML diagram featuring our current interfaces and design decisions.
We learned that visual demo's are valuable in conveying information intuitively. 
