# AMD Schola — Meeting Minutes

**Focus:** Unity port architecture, MVP scope, and implementation planning  
**Date:** October 1, 2026

## Attendees

**AMD**
- Alex Cann
- Tian Yue

**Student team**
- Mohammad Mahi Ali Mukati
- Talha Asif
- Anwar Khan
- Yahyaa Yasin
- Raahim Chaghtai
- Muzzammil Siddiqui

**Attribution:** Speaker 1 is identified as Alex Cann, following the previous meeting's naming. Other unidentified speakers are referred to as student team members. The team lead's identity is not specified in these notes.

## 1. Architecture and Starting Points

Alex described two modular workstreams that can be developed separately, sequentially, or in parallel:

- **Training:** Build the Unity environment and its interfaces, including agents, sensors, actuators, endpoints, and spaces, then connect to Schola's existing Python training code.
- **Inference:** Load an exported ONNX model and run it inside Unity through a policy interface that does not depend on an environment.

Both are valid starting points. The team should first decide how to divide the work.

For a training-first approach, the team can initially test communication using manually constructed messages before building the developer-facing Unity integration. For an inference-first approach, the team can begin with an existing exported model and implement the components needed to run it.

## 2. Python Training and Native Inference

A student team member asked whether Python handles both training and inference through gRPC. Alex clarified that, in the existing Unreal implementation:

- Training runs in Python, where the relevant machine learning libraries are available.
- Inference runs entirely in Unreal's C++ implementation using an exported model.
- Training components, including communication, are isolated so they can be removed without affecting inference.

The Unity port should reuse the existing Python training code. During gameplay, inference should run within the engine's own timing and execution model, without a separate Python process.

## 3. Relationship to Unity ML-Agents and Unity Design

A student team member asked how Schola differs from Unity ML-Agents. Alex described substantial overlap in functionality, with Schola's intended distinction being a shared Python front end across multiple game engines. The Unity port would extend that approach beyond Unreal.

The integration should retain Schola's composable design while following Unity conventions. Familiar concepts across engines are useful, but engine-specific implementation details should not be copied directly. For example, Unreal threading constructs such as `FRunnable` should be replaced with an appropriate Unity-side approach.

Schola exposes building blocks rather than prescribing one workflow. Developers can use an environment interface directly or combine observers, actuators, agents, and environments as needed. Alex emphasized that the developer-facing API is itself an important part of the user interface.

## 4. User Stories and Prioritization

The team presented six user stories from its course deliverable:

| User story | Intended outcome / acceptance criteria |
| --- | --- |
| Agent observations | Configure observations; values update with new scenes and retain a consistent format. |
| Agent actions | Connect policy outputs to game actions; valid actions change the object's behaviour. |
| Training control | Control episode count; report rewards and episode status to Python and reset correctly between episodes. |
| Training through Schola | Exchange observations and actions using the existing gRPC contract and complete a short training run. |
| Running a trained policy | Load a compatible model, supply observations, and apply actions during gameplay with Python stopped. |
| Integration setup | Provide a guide that lets a new team member run training and use a trained model. |

The team asked whether it should begin with Gymnasium and later add libraries such as Stable-Baselines3. Alex explained that Schola's existing Python side already handles the supported framework integrations. Implementing the RPC contract correctly should allow the Unity port to use those integrations without rebuilding them.

A student team member proposed starting with the Unity–Python communication story. Alex confirmed this as a valid starting point; model deployment remains the other option. A final workstream split was not recorded.

AMD could provide a model trained in Unreal to help the team develop inference independently of training. Engine unit differences may make its behaviour unsuitable initially, but it could still help validate model loading and execution. No delivery date was committed.

## 5. Feasibility and MVP Scope

Alex considered the six user stories achievable over four months, based on a six-person team, access to AI tools, and an existing reference implementation. He distinguished a rough functional implementation from the additional work required to polish it.

The agreed MVP direction is:

- A working sample environment that trains in Unity.
- A reusable framework that supports creating additional environments.
- A walkthrough explaining how a developer builds a new environment using that framework.

A hard-coded demonstration for only one environment would not be sufficient. The implementation must show that developers can extend it without an unnecessarily cumbersome process.

The complete user-story set also includes native inference. Alex's specific MVP description focused on the training example and reusable framework; the notes do not explicitly settle whether inference must be included in the first MVP milestone.

## 6. Unity UI Mockup Feedback

The team presented a non-functional Unity plugin mockup that mirrors Unreal's controls and settings. It was prepared for a course deliverable; no functionality is connected, and the team is still investigating some settings.

### Training settings

A separate training configuration window is reasonable. Many controls correspond directly to settings in the underlying RL libraries, such as SB3 algorithm parameters, and mirror command-line options.

Launching training from the editor by starting a Python process is supported in the existing workflow. Alex noted that this had not been AMD's preferred way to train in practice. He also explained that Unreal's earlier singleton-subsystem approach was engine-specific and had been abandoned there.

### Scene and object configuration

Agents, sensors, actuators, and environments should be configured through Unity's scene and object workflows rather than maintained independently in a settings window.

Environment information should flow from the scene. Otherwise, developers would need to keep scene changes synchronized with a separate environment tab. Sensors such as raycasts also need to be attached to an object to be meaningful.

The team discussed using the Inspector for configuration and live feedback. A visual display of sensor outputs would be useful for debugging, but should use Unity's existing Inspector or debugging extension mechanisms rather than a new custom visualization framework.

Alex considered the mockup panels a useful starting point for the underlying classes, subject to these changes in how configuration is exposed.

## 7. Project Credit, Coding Standards, and IP

### Resume and LinkedIn descriptions

Alex advised students to describe the work as a **student project with or for AMD**, placed in the projects section of a resume. Students should not claim employment at AMD.

AMD may publish a blog post crediting the students, as it has for previous university projects. Alex also anticipated that the work could be upstreamed into the existing MIT-licensed repository with contributor attribution. These were expectations rather than confirmed publication commitments.

Alex will check specific wording for student credit with AMD's PR team.

### Coding standards

AI-assisted coding is acceptable, but team members should read, understand, and take responsibility for generated code. Alex discouraged blindly accepting generated output, particularly in a course intended to develop students' understanding.

The team should:

- Use formatters and follow Unity naming conventions, including PascalCase and camelCase where appropriate.
- Document the code clearly.
- Remove misleading or irrelevant generated comments, including unsupported backward-compatibility claims and explanations of alternatives that would never make sense.

### Intellectual property

Alex indicated that the proposed work was acceptable under the MIT license. The course checklist covering MVP agreement, coding standards, IP, and a reasonable plan was reviewed and considered covered during the meeting. No separate written IP terms were included in the source notes.

## 8. Meeting Cadence and Next Presentation

Alex proposed establishing a recurring weekly meeting time from the following week through approximately the end of the semester.

The team lead will circulate a scheduling link so everyone can provide availability, including the member who was absent.

At the next meeting, the team should present:

- How the product will develop toward the end goal.
- The first functionality it plans to deliver and demonstrate working.

## 9. Action Items

| Owner | Action | Timing |
| --- | --- | --- |
| Student team lead | Circulate a scheduling link and collect availability for recurring weekly meetings. | Before the next recurring meeting is scheduled. |
| Student team | Prepare a staged implementation plan and identify the first working deliverable. | Next meeting. |
| Alex Cann | Check with AMD PR on wording for student project credit on resumes and LinkedIn. | Not specified. |

**Development follow-up from the discussion:** Decide how to divide training and inference work; revise the mockup so scene-related configuration uses Unity's scene/Inspector workflows; ensure the MVP demonstrates a reusable framework as well as a working example.

**Potential AMD support:** Supply an exported Unreal-trained model for inference development. Availability was offered, but no delivery commitment or date was recorded.

## 10. Items Still to Be Confirmed

- Identity of the student team lead responsible for circulating the scheduling link.
- Recurring weekly meeting time.
- Final workstream allocation and first working deliverable.
- Whether native inference is required in the first MVP milestone.
- PR-approved wording for student project credit.
- Whether and when the team will request an exported model from AMD.
