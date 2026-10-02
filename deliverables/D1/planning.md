# AMD Schola — Unity Integration / Oscorp
> _Note:_ This document will evolve throughout your project. You commit regularly to this file while working on the project (especially edits/additions/deletions to the _Highlights_ section). 
 > **This document will serve as a master plan between your team, your partner and your TA.**

## Product Details
 
#### Q1: What is the product?

AMD Schola - Unity Integration is a Unity package that brings AMD’s Schola reinforcement learning tools to Unity. It lets developers define training environments, train agents using Schola’s existing Python tools, and run trained models inside their games.

Reinforcement learning allows an agent, such as a game character, to learn through trial and error by receiving rewards for useful actions. For example, a developer could train a character to reach a destination while avoiding obstacles. Our package will let developers define what the character observes, which actions it can take, and what earns rewards.

Our partner is AMD, a company that develops processors, graphics hardware, and related software. Alexander Cann, Member of Technical Staff on AMD’s Schola team, is our partner representative and primary point of contact. Schola currently supports Unreal Engine, and our project will bring that workflow to Unity.

#### Q2: Who are your target users?

Our primary users are Unity game developers who want to train NPCs or simulation agents without building their own connection to reinforcement learning tools. This includes developers at small game studios who can build scenes and character behaviours but need support integrating agent training into their games.

We also target reinforcement learning researchers and engineers who already use Schola and want to work with Unity environments while keeping their existing training tools.

#### Q3: Why would your users choose your product? What are they using today to solve their problem/need?

Unity developers can currently use Unity ML-Agents or build custom training integrations. Users would choose our product when they want to use Schola’s existing tools within Unity, especially if they already work with Schola in Unreal Engine.

The main advantage over Unity ML-Agents would be compatibility with Schola’s training workflow. Teams could keep familiar tools while working across both engines, and Unity developers would not need to build the Schola connection themselves. Our package aims to reduce repeated integration work while fitting into Unity’s existing editor workflow. This supports AMD’s plans to make Schola available across multiple game engines.

#### Q4: What are the user stories that make up the Minumum Viable Product (MVP)?

User stories agreed upon in initial meeting with partner.

These stories describe the minimum end-to-end product: a Unity developer can define an RL environment, train through Schola’s Python ecosystem, export an ONNX policy, and run it in Unity without Python. Trello tracks implementation tasks.

#### US1: Define a reinforcement-learning environment

**Story:** As a Godot developer, I want to define a reinforcement-learning environment through a small engine-independent interface so that I can make an environment trainable without writing networking code.

**Acceptance criteria:**

- A developer can implement or configure the environment's initialization, reset, observation, reward, and terminal-state behavior.
- The environment can contain at least one agent.
- Environment code does not directly manage sockets, RPC calls, or serialized protocol messages.
- The environment accepts a reproducible random seed and optional reset configuration.
- Invalid or incomplete environment configuration produces a clear error.

#### US2: Declare observation and action spaces

**Story:** As a Godot developer, I want to define observation and action spaces using reusable types so that Schola can validate and communicate the agent's available inputs and outputs.

**Acceptance criteria:**

- The core supports Box, Discrete, MultiDiscrete, and MultiBinary spaces and their corresponding point values.
- Spaces expose their shape, bounds, and data type where applicable.
- An omitted Box bound represents an unbounded dimension rather than zero.
- An observation or action that does not match its declared space is rejected with a useful error.
- Space definitions can be translated to the representation expected by the existing Schola Python package.
- Round-trip tests serialize and deserialize spaces, points, interaction definitions, and agent states without changing their values.

#### US3: Connect to existing Python training tools

**Story:** As an ML practitioner, I want a Godot environment to connect to Schola's existing Python training tools so that I can train policies without maintaining a separate Godot-specific Python workflow.

**Acceptance criteria:**

- The Godot integration completes the connection and environment-definition exchange with the existing Python client.
- The integration uses Schola's existing protocol and gRPC services unless an alternative is approved by AMD.
- The transport implements `StartGymConnector`, `RequestTrainingDefinition`, and `UpdateState` from the existing Gym connector protocol.
- Python can discover the available environment, agents, observation spaces, and action spaces.
- The transport is isolated behind a core interface so the engine-independent code does not depend directly on gRPC.
- Connection failures and incompatible protocol data produce actionable errors instead of hanging the game or training process.

#### US4: Execute the episode lifecycle

**Story:** As an ML practitioner, I want Schola to coordinate observations, actions, rewards, terminal states, and resets so that training proceeds correctly across complete episodes.

**Acceptance criteria:**

- For each step, Godot supplies an observation and accepts a compatible action from Python.
- Each step returns the resulting observation, reward, and termination or truncation state.
- Reset restores the demonstration environment to a valid initial state and returns an initial observation.
- The connector supports the existing disabled, same-step, and next-step auto-reset modes with the same externally observable behavior as Schola's Python API.
- The connector can coordinate more than one environment in a running scene.
- An integration test completes multiple episodes without lifecycle deadlock or state leakage between episodes.

#### US5: Configure Schola through Godot-native tools

**Story:** As a Godot developer, I want to configure agents and environments through nodes and the Inspector so that I can use familiar Godot workflows instead of editing protocol or networking code.

**Acceptance criteria:**

- A developer can add the required Schola components to a scene using Godot's normal node workflow.
- Essential settings are visible and editable in the Inspector with understandable names and defaults.
- The demonstration project can be configured without modifying Schola's internal source code.
- The demonstration agent receives a small reward for moving backward, a larger reward for moving forward, and a penalty for remaining still.
- A configurable maximum step count truncates an episode that does not otherwise terminate.
- Running the scene reports missing or conflicting configuration clearly.

#### US6: Export a trained policy to ONNX

**Story:** As a Godot developer, I want to export a trained policy to ONNX so that I can transfer the learned policy from the Python training process into Godot.

**Acceptance criteria:**

- A policy trained with Stable-Baselines3 can be exported through Schola's existing Python export workflow.
- The resulting file is a valid ONNX model that can be opened by an independent model-inspection tool.
- The model's input and output names, shapes, and data types are documented for the inference implementation.
- A policy trained for the Godot demonstration environment is exported with inputs and outputs matching that environment's declared spaces.

#### US7: Run and ship an ONNX policy

**Story:** As a Godot developer, I want a trained policy to drive my agent with Python closed and to exclude training-only dependencies from exported games so that I can ship an autonomous agent without unnecessary training infrastructure.

**Acceptance criteria:**

- Godot loads the exported ONNX model and validates that its inputs and outputs match the agent's declared spaces.
- On each physics step, the agent follows an observe-infer-act loop that applies model output as its action.
- The demonstration agent performs the intended learned behavior while Python is not running.
- Missing, invalid, or incompatible model files produce a clear error.
- Training and transport code is packaged separately from the core environment API and inference code.
- A Godot export containing the demonstration environment runs its trained policy without Python or a live gRPC connection.
- The exported build excludes the training add-on without preventing the project from loading or using inference.
- The documentation identifies which modules are required for training and which are required in a shipped game.

#### Partner review

The team will send this artifact and the accompanying architecture to AMD through the shared Microsoft Teams channel. Evidence of that communication and any requested revisions will be linked here after the review.

#### Q5: Have you decided on how you will build it? Share what you know now or tell us the options you are considering.

Technology Stack: C# (Unity plugin), Python 3.10–3.12 (training client). gRPC/Protobuf for communication. Gymnasium, Ray RLlib, Stable-Baselines3. ONNX via Unity Sentis.

Deployment: Distributed as a Unity Package Manager (UPM) package (Git URL/tarball). Includes sample project, tests, and README. No web deployment.

Architecture:

Unity: Environment Manager, Observation/Action/Policy interfaces.

Training Adapter: Implements gRPC/Protobuf service to connect Unity to Python RL client.

Inference Adapter: Loads compatible ONNX policy locally, applies actions without Python.

Flow: Unity scene → Environment interfaces → gRPC adapter ↔ Python RL client → ONNX → Unity inference adapter → actions.

Third-Party Applications & APIs: gRPC (Google.Protobuf, Grpc.Tools, YetAnotherHttpHandler), Unity Sentis/Barracuda, Python RL libraries.

## Teamwork Details

#### Q6: Have you met with your team?

Q6: Yes. Team-building activity: played Volleyball and met online. Evidence: attached screenshot of the online team call. Fun facts:

Fahad does Brazilian Jiu Jitsu.

Anwar skipped grade 11.

Mahi has a business registered under his name.
Updated Q6 now includes activity, evidence, and three fun facts.
![Team Bonding Photo](Team_Bonding_Image.jpeg)


#### Q7: What are the roles & responsibilities on the team?

Our roles follow the main parts of the project: the Unity-facing API, the training connection to Schola's Python side (gRPC), local inference (ONNX), and the testing and example work that ties them together. Every member owns at least one code component. Non-code roles (liaison, coordinator) are added on top of coding work, not in place of it.

| Role | Responsibilities |
| --- | --- |
| Partner liaison | Formal point of contact for AMD email; monitors the Teams chat; schedules partner check-ins; sends agendas, Q4/Q5 and IP questions; records AMD decisions and meeting minutes. |
| Team coordinator (Scrum master) | Runs internal meetings; maintains the task board and sprint priorities; follows up on action items; tracks deliverable deadlines. |
| Unity API & editor integration | Designs the Unity-facing abstractions (agent, observers, actuators, policy, decision triggers) and their Inspector/editor workflow; leads the design review with AMD. |
| Training communication (gRPC) | Implements the Unity-side gRPC service against Schola's existing Protocol Buffers definitions; handles step/reset and episode flow; checks compatibility with the unchanged Python client. |
| Inference (ONNX) | Runs policies exported from Schola as ONNX inside Unity; builds the policy component; documents runtime, operator, version and platform limits. |
| Testing & CI | Sets up Unity test infrastructure and automated checks early; writes shape/contract tests that keep training and inference paths consistent; coordinates pull request reviews. |
| Example environment & documentation | Builds the sample Unity scene used for the end-to-end demo; writes setup guides and API documentation. |

**Mohammad Mahi Ali Mukati — Partner liaison; Unity API & editor integration lead**
* _Responsibilities:_ Designs the Unity-facing Environment, Agent, observer (sensor), actuator and policy components and how developers configure them in the Unity editor; leads the interface design review with AMD; supports Unity-side testing. As liaison, is the formal email contact for AMD, schedules check-ins, relays requirements and feedback to the team, and ensures minutes are recorded.
* _Why:_ Previously ran a Unity game studio, is fluent in C#, and has written NPC AI and gameplay algorithms in Unity. A JavaFX project using MVC and design patterns gives experience structuring reusable, modular components. As a developer and QA tester at REP, took part in client meetings and turned feedback into development priorities, which fits the liaison role.

**Yahyaa Yasin — Training communication (gRPC) lead**
* _Responsibilities:_ Implements the Unity-side gRPC service in C# against Schola's existing Protocol Buffers definitions, including step/reset and episode handling; compares behaviour with the Unreal reference implementation to reach feature parity with the unchanged Python client.
* _Why:_ Built and optimized production server-side C#/.NET systems at Freedom Mobile. Wrote a concurrent TCP client/server system in C with multiplexed socket communication, which is the same problem as exchanging structured messages between two runtimes in a tight loop. Has hands-on Python/PyTorch experience, including reproducing a research paper from scratch, so is comfortable on both sides of the bridge.

**Raahim Chaghtai — Training communication (gRPC) & Python bridge**
* _Responsibilities:_ Works with Yahyaa on the engine-to-Python bridge: verifies the Unity service against the Python client's contract, adds logging at request/response boundaries, and profiles latency and failures in the training loop. Picks up Unity-side C# tasks as he ramps up.
* _Why:_ Profiled backend latency across AWS Lambda, AI processing and databases at Safi Data; integrated an LLM through a server-side service layer at Dominarlo; built FastAPI REST APIs at FrontYard, so has experience defining clean contracts between separate systems. Wrote a Unix shell in C covering process control, file descriptors and signals, which is relevant to process/IPC issues. Is currently doing Unity's "Create with Code" path to build C# and Unity foundations.

**Talha Asif — Python integration & training validation; Testing & CI lead**
* _Responsibilities:_ Connects the Unity environment to Schola's Python training stack and sets up reproducible training runs; builds evaluation tooling to check that the example agent actually learns; sets up automated checks and end-to-end smoke tests, including a Docker-based setup; contributes to setup documentation.
* _Why:_ During an AI/ML engineering internship at DevFortress, built configurable training pipelines and evaluated models through controlled experiments. Built APIs and deployed applications with Docker through the Provision project and agent/MCP server development. Wants to build familiarity with Unity and reinforcement learning.

**Anwar Khan — Inference (ONNX) lead**
* _Responsibilities:_ Runs ONNX policies exported from Schola inside Unity; builds the Unity policy component and connects it to the shared observer/actuator components so inference and training use the same mappings; documents runtime and platform limits.
* _Why:_ Has experience with game engines and C# for Unity-side work, plus hands-on ML from an ASL classifier (Python, OpenCV, TensorFlow/Keras). At ND Research, changed an existing AI system without disrupting its architecture, which is similar to extending Schola. Wants to learn more about reinforcement learning.

**Fahad Moinuddin — Team coordinator (Scrum master); Inference (ONNX) & end-to-end validation**
* _Responsibilities:_ Runs internal meetings, maintains the task board and sprint priorities, and follows up on action items and deadlines. On the code side, handles model export from the Python side and checks exported model inputs/outputs with Anwar, and validates end-to-end training and inference on the benchmark environment.
* _Why:_ Led a 6-person backend team through 7 weekly scrums and directed a 4-person team through a 4-week sprint. Has trained neural networks on large image datasets and built real-time 3D/computer vision systems, which suits model export and validation.

**Muzzammil — Example environment & documentation lead**
* _Responsibilities:_ Builds the sample Unity scene used to demonstrate training and inference; trains and evaluates the example agent; writes setup guides, tutorials and API documentation.
* _Why:_ Wrote Unity and C# lessons for Ultimate Coders, breaking down Unity's component/scripting model for students, which suits documentation and examples. Trained and evaluated a PPO agent on CartPole with Gymnasium and Stable-Baselines3, so has practical experience with the RL loop the example must demonstrate. Designed, tested and shipped a React Native app to the App Store, and is learning gRPC.


#### Q8: How will you work as a team?

**Recurring meetings and events**

| Event | When / where | Purpose |
| --- | --- | --- |
| Weekly AMD check-in | Weekly, Tuesday or Thursday morning depending on availability, online on Microsoft Teams. Adjusted around midterms as agreed with AMD. | Progress update, blockers, next steps, and AMD feedback on design decisions. No formal prep is required; the liaison sends a short agenda beforehand and minutes afterward. |
| Internal team meeting | Weekly, Tuesday or Thursday morning depending on availability, online on Microsoft Teams. | Sprint planning and review, task assignment, prioritization, and preparing questions for AMD. |
| Async updates | Ongoing, in the team's Microsoft Teams group chat. | Day-to-day communication; members post progress and blockers between meetings so issues are raised without waiting for the next meeting. |
| Code review | Continuous, on GitHub. | Every pull request needs at least one human reviewer from the team before merge, following AMD's Schola practice. |
| Coding / integration sessions | Ad hoc, online on Microsoft Teams. | Pair work on high-risk pieces (gRPC exchange, ONNX spike, test setup) and integration before deliverables. |

Minutes for partner meetings are stored in [`deliverables/team/minutes`](../team/minutes).

**Partner meetings before D1**

1. **Kickoff — Tuesday, 22nd September, morning 9:30, online.** Attendees: Alex Cann and Tian Yue (AMD) and all seven team members. We covered communication channels (Teams preferred, email for async), a proposed weekly check-in cadence, provisional Unity allocation, how to approach a design mockup for a library, Schola's training and inference architecture, a suggested Unity approach (ONNX inference and a gRPC service using existing protos), key risks, and AMD's development practices. Action items: send weekly meeting options, select a formal point of contact, and report our preference on Unity/Godot/application. [Minutes](../team/minutes/26-10-22-minutes.txt).
2. **Weekly check-in — Thursday, 1st October, morning 9:30, online on Microsoft Teams.** Attendees: All team members. Show our progress and get feedback. [TODO: link to minutes].
  
#### Q9: How will you organize your team?

**Tracking work.** We use a GitHub Projects board linked to our team repository. Each of the six MVP user stories in Q4 is an epic, and it is broken into issues with one owner, a short description, a size label, and acceptance criteria copied from the story. Our Mentor TA and our AMD contacts are given access to the board. Fahad Moinuddin (team coordinator) maintains the board. The columns are **Backlog → Ready → In Progress → In Review → Done**.

**Repository setup.** We forked AMD's open-source Schola repository, and every member works from a clone of that fork. Work happens on feature branches and reaches the fork's main branch through pull requests. Once AMD confirms their contribution process, reviewed changes are proposed upstream to AMD's repository. Talha Asif keeps the fork in sync with upstream so our work does not drift from AMD's changes.

**Sizing and velocity.** On AMD's advice, we do not estimate in hours, because teams tend to overestimate how much focused work fits in a block, especially when using AI tools. We size tasks as small, medium, or large, and track how many of each we finish per week. That tells us our real velocity, and we use it to plan the next sprint and to tell AMD honestly what will fit in the term.

**Prioritization.** We rank tasks by two criteria:
1. Does it unblock the end-to-end MVP workflow (Unity agent → training connection → ONNX export → Unity inference)?
2. How risky is it? The three areas AMD flagged as trouble spots (getting ONNX inference working, the gRPC communication layer, and designing a non-opinionated engine abstraction) go first, along with test infrastructure, because a late failure there would cost the most.

Stretch goals from Q4 (multi-agent support, concurrent environments, broader RL framework coverage, custom editor windows) stay in the Backlog until the core workflow works, matching AMD's preference for a small, well-designed core. Priorities are set at our weekly internal meeting and adjusted after each AMD check-in based on their feedback.

**Task assignment.** At the weekly internal meeting, members pick up tasks from *Ready* based on the roles in Q7 and their current workload, and every member takes at least one coding task. Each task has exactly one owner. Large tasks are split into vertical slices that can be finished in about a week, and pair work on high-risk pieces is arranged in our ad hoc coding sessions. If two people want the same task, or a task has no clear owner, Fahad makes the final call.

**Tracking status from start to finish.**
- *In Progress*: the owner has started work on a feature branch.
- *In Review*: a pull request is open and linked to the issue.
- *Done*: the PR is merged after review by at least one teammate, the acceptance criteria are met, and automated checks pass (set up by Talha Asif, testing and CI lead).
- A user story is complete when all of its issues are Done and its acceptance criteria from Q4 have been demonstrated in the example scene.
- Blocked tasks get a "blocked" label and a note in the team Teams chat explaining the blocker.

**Other artifacts.**
- Meeting minutes for every partner meeting in `deliverables/team/minutes/`, written up by a rotating note-taker.
- The team CSV and `Stakeholders.txt` in `deliverables/team/`.
- The README, updated as setup and tooling change.
- A decision log recording each open question for AMD, who owns it, and AMD's answer with a date. Mahi Ali Mukati (partner liaison) maintains it.


#### Q10: What are the rules regarding how your team works?

**Communications:**
* **Internal:** Our team group chat on Microsoft Teams is our main channel. Members check it at least once a day and reply to direct requests within 24 hours. Urgent items get an @mention. Members post progress and blockers there between meetings rather than waiting for the next one.
* **With AMD:** Mahi Ali Mukati, our partner liaison, is our formal point of contact for AMD for the whole semester. He handles email, scheduling the weekly check-in, sending the agenda beforehand and minutes afterward, and raising requests for decisions such as MVP and user story approval, upstream contribution process, and IP terms. AMD created a shared Microsoft Teams group chat with our team, and any member can post technical questions, bugs, and blockers there directly without routing them through the liaison.
* **Meetings:** A weekly AMD check-in and a weekly internal meeting, both on Microsoft Teams in the Tuesday or Thursday morning slot (see Q8).

**Collaboration:**
* **Accountability:** Every meeting has a rotating note-taker, and action items are recorded with an owner and a date. Members who cannot attend tell the team beforehand and read the minutes. Fahad reviews open action items at the start of each internal meeting and follows up on anything overdue.
* **Code standards:** Each member works on a feature branch in our fork, and the main branch is protected. Every change goes through a pull request that a human teammate reviews before merging, following AMD's Schola practice. This applies to AI-assisted code as well: whoever opens the PR is responsible for understanding it and must be able to explain it in review. We set up automated tests early so that AI-assisted changes are checked automatically. Commits and PRs link to their issue.
* **If someone isn't contributing or responding:**
  1. Fahad or a teammate checks in privately within two days to find out what is blocking them.
  2. If it continues, we discuss it as a team and agree on a smaller, clearly defined task with a deadline.
  3. If there is still no change, we tell our Mentor TA so the issue is documented and handled early.

  We use GitHub commit, review, and issue history to see how work is distributed.

## Organisation Details

#### Q11. How does your team fit within the overall team organisation of the partner?

We are a student **feature-development and integration team** extending AMD Schola to a new engine. AMD maintains the existing Unreal plugin, Python package and protocol; our primary ownership is the Unity-facing implementation, integration example, tests and documentation. This requires coordination with AMD on API shape, compatibility, licensing and upstream review rather than independent changes to the existing Python client. A Python-side change would be proposed in a separate reviewed pull request only if the existing contract proves insufficient.

AMD described parallel projects potentially covering Unity, Godot and an application built with Schola. Alexander Cann represented AMD at kickoff, and Tian Yue is expected to become the main contact as work progresses. The final project allocation and review responsibilities are not settled. If another team is also assigned Unity, AMD and course staff will determine whether work is divided by feature or pursued as distinct designs; we will revise ownership before two teams edit overlapping components.

#### Q12. How does your project fit within the overall product from the partner?

Schola already links Unreal environments to Python RL tooling. Our project adds a Unity engine path that can use the existing training protocol and run an exported policy in Unity. The shared Python side and protocol are upstream dependencies; our Unity interfaces, adapters and sample are the proposed new feature set. The example and package should show how a developer configures an agent and how observations, actions, rewards and resets pass through the training path, then how local inference uses the trained model.

The partner's early success criteria, as understood from the kickoff, are a flexible Unity API that fits game developers' workflows, a working training connection to the existing Python side, and useful local inference. The immediate design milestone is an AMD-reviewed abstraction that does not impose per-frame decisions. The precise end-of-term acceptance threshold, supported platforms and expectations for upstream merging must be confirmed at the next review; we will record AMD's response against Q4's criteria.

## Potential Risks

#### Q13. What are some potential risks to your project?

1. **Interface design may be too rigid or awkward.** An API that couples observers to the wrong Unity object or assumes every agent acts each frame would make the library hard to reuse, even if a demo works.
2. **ONNX inference may be incompatible or difficult to package.** Exported model shapes, supported operators, runtime selection and Unity platform/version constraints could delay local policy execution.
3. **gRPC and dependency setup may stall training.** Generated code, engine dependency management and two-process debugging add integration and reproducibility risk.
4. **Training and inference paths may drift.** Different observation/action mapping in the two modes would create a model that trains successfully but behaves incorrectly in Unity.
5. **Project ownership and partner decisions remain open.** Contact assignments, the Unity/Godot split, MVP approval and IP terms could change scope or prevent timely review.
6. **Term time and testing constraints.** Midterms, engine test setup and optimistic estimates may leave insufficient time for a fully documented, reliable example.

#### Q14. What are some potential mitigation strategies for the risks you identified?

| Risk | Response and evidence of progress |
| --- | --- |
| Interface design | Present an interactive Unity-facing prototype and a small sample to AMD first. Review observation/action/policy contracts and a turn-based trigger before scaling to more sensors or agents. |
| ONNX inference | Spike one exported model with known inputs and outputs early; record Unity runtime, supported operators, version and platform limits. Keep the example narrow and adjust model/runtime choice with AMD if needed. |
| gRPC integration | First prove a minimal step/reset exchange using the existing proto contract, log request/response boundaries, pin setup versions, then add full episode handling. Escalate any required schema changes to AMD. |
| Path drift | Share observation/action definitions; use one example agent in training and inference, with tests checking shapes and an end-to-end smoke demonstration. |
| Unclear ownership or decisions | Appoint the liaison, schedule weekly reviews, send Q4/Q5 and IP questions for explicit AMD feedback, record the decision owner and date, and replan when the other-team split is announced. |
| Time and testability | Size work relatively, implement small vertical slices, set up automated checks as soon as feasible, and keep extra environments, automatic builds and custom editor UI outside the MVP until the core workflow works. |
