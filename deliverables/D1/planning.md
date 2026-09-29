# YOUR PRODUCT/TEAM NAME
> _Note:_ This document will evolve throughout your project. You commit regularly to this file while working on the project (especially edits/additions/deletions to the _Highlights_ section). 
 > **This document will serve as a master plan between your team, your partner and your TA.**

## Product Details
 
#### Q1: What is the product?

 > Short (1 - 2 min' read)
 * Start with a single sentence, high-level description of the product.
 * Be clear - Describe the problem you are solving in simple terms.
 * Specify if you have a partner, who they are (role/title), and the organization information.
 * Be concrete. For example:
    * What are you planning to build? Is it a website, mobile app, browser extension, command-line app, etc.?      
    * When describing the problem/need, give concrete examples of common use cases.
    * Assume the reader knows nothing about the partner or the problem domain and provide the necessary context. 
 * Focus on *what* your product does, and avoid discussing *how* you're going to implement it.      
   For example: This is not the time or the place to talk about which programming language and/or framework you are planning to use.
 * **Feel free (and very much encouraged) to include useful diagrams, mock-ups and/or links**.


#### Q2: Who are your target users?

  > Short (1 - 2 min' read max)
 * Be specific (e.g. a 'a third-year university student taking CSC301 and studying Computer Science' and not 'a student')
 * **Feel free to use personas. You can create your personas as part of this Markdown file, or add a link to an external site (for example, [Xtensio](https://xtensio.com/user-persona/)).**

#### Q3: Why would your users choose your product? What are they using today to solve their problem/need?

> Short (1 - 2 min' read max)
 * We want you to "connect the dots" for us - Why does your product (as described in your answer to Q1) fits the needs of your users (as described in your answer to Q2)?
 * Explain the benefits of your product explicitly & clearly. For example:
    * Save users time (how and how much?)
    * Allow users to discover new information (which information? And, why couldn't they discover it before?)
    * Provide users with more accurate and/or informative data (what kind of data? Why is it useful to them?)
    * Does this application exist in another form? If so, how does your differ and provide value to the users?
    * How does this align with your partner's organization's values/mission/mandate?

#### Q4: What are the user stories that make up the Minumum Viable Product (MVP)?

 * At least 5 user stories concerning the main features of the application - note that this can broken down further
 * You must follow proper user story format (as taught in lecture) ```As a <user of the app>, I want to <do something in the app> in order to <accomplish some goal>```
 * User stories must contain acceptance criteria. Examples of user stories with different formats can be found here: https://www.justinmind.com/blog/user-story-examples/. **It is important that you provide a link to an artifact containing your user stories**.
 * If you have a partner, these must be reviewed and accepted by them. You need to include the evidence of partner approval (e.g., screenshot from email) or at least communication to the partner (e.g., email you sent)

#### Q5: Have you decided on how you will build it? Share what you know now or tell us the options you are considering.

> Short (1-2 min' read max)
 * What is the technology stack? Specify languages, frameworks, libraries, PaaS products or tools to be used or being considered. 
 * How will you deploy the application?
 * Describe the architecture - what are the high level components or patterns you will use? Diagrams are useful here. 
 * Will you be using third party applications or APIs? If so, what are they?

----
## Intellectual Property Confidentiality Agreement 
> Note this section is **not marked** but must be completed briefly if you have a partner. If you have any questions, please ask on Piazza.
>  
**By default, you own any work that you do as part of your coursework.** However, some partners may want you to keep the project confidential after the course is complete. As part of your first deliverable, you should discuss and agree upon an option with your partner. Examples include:
1. You can share the software and the code freely with anyone with or without a license, regardless of domain, for any use.
2. You can upload the code to GitHub or other similar publicly available domains.
3. You will only share the code under an open-source license with the partner but agree to not distribute it in any way to any other entity or individual. 
4. You will share the code under an open-source license and distribute it as you wish but only the partner can access the system deployed during the course.
5. You will only reference the work you did in your resume, interviews, etc. You agree to not share the code or software in any capacity with anyone unless your partner has agreed to it.

**Your partner cannot ask you to sign any legal agreements or documents pertaining to non-disclosure, confidentiality, IP ownership, etc.**

Briefly describe which option you have agreed to.

----

## Teamwork Details

#### Q6: Have you met with your team?

Do a team-building activity in-person or online. This can be playing an online game, meeting for bubble tea, lunch, or any other activity you all enjoy.
* Get to know each other on a more personal level.
* Provide a few sentences on what you did and share a picture or other evidence of your team building activity.
* Share at least three fun facts from members of you team (total not 3 for each member).


#### Q7: What are the roles & responsibilities on the team?

Describe the different roles on the team and the responsibilities associated with each role (e.g., frontend, database). 
 * Roles should reflect the structure of your team and be appropriate for your project. One person may have multiple roles.  
 * Add role(s) to your Team-[Team_Number]-[Team_Name].csv file on the main folder.
 * At least one person must be identified as the dedicated partner liaison. They need to have great organization and communication skills.
 * Everyone must contribute to code. Students who don't contribute to code enough will receive a lower mark at the end of the term.

List each team member and:
 * A description of their role(s) and responsibilities including the components they'll work on and non-software related work
 * Why did you choose them to take that role? Specify if they are interested in learning that part, experienced in it, or any other reasons. Do no make things up. This part is not graded but may be reviewed later.

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

Describe meetings (and other events) you are planning to have. 
 * When and where? Recurring or ad hoc? In-person or online?
 * What's the purpose of each meeting?
 * Other events could be coding sessions, code reviews, quick weekly sync meeting online, etc.
 * You should have 2 meetings with your project partner (if you have one) before D1 is due. Describe them here:
   * You must keep track of meeting minutes and add them to your repo under "deliverables/minutes" folder
   * You must have a regular meeting schedule established for the rest of the term.  

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

1. **Kickoff — [TODO: confirm date], online.** Attendees: Alex Cann and Tian Yue (AMD) and all seven team members. We covered communication channels (Teams preferred, email for async), a proposed weekly check-in cadence, provisional Unity allocation, how to approach a design mockup for a library, Schola's training and inference architecture, a suggested Unity approach (ONNX inference and a gRPC service using existing protos), key risks, and AMD's development practices. Action items: send weekly meeting options, select a formal point of contact, and report our preference on Unity/Godot/application. [Minutes](../team/minutes/26-10-22-minutes.txt).
2. **Weekly check-in — Thursday [TODO: date], morning, online on Microsoft Teams.** Attendees: [TODO]. [TODO: topics discussed and decisions]. [TODO: link to minutes].
  
#### Q9: How will you organize your team?

List/describe the artifacts you will produce to organize your team. (We strongly recommend that you use standard collaboration tools like Linear.app, Jira, Slack, Discord, GitHub.)       

 * Artifacts can be To-Do lists, Task boards, schedule(s), meeting minutes, etc.
 * We want to understand:
   * How do you keep track of what needs to get done? (You must grant your TA and partner access to systems you use to manage work)
   * **How do you prioritize tasks?**
   * How do tasks get assigned to team members?
   * How do you determine the status of work from inception to completion?

#### Q10: What are the rules regarding how your team works?

**Communications:**
 * What is the expected frequency? What methods/channels will be used? 
 * If you have a partner project, what is your process for communicating with your partner? Who is responsible?
 
**Collaboration:**
 * How are people held accountable for attending meetings, completing action items? What is your process?
 * How will you address the issue if one person doesn't contribute or is not responsive?

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
