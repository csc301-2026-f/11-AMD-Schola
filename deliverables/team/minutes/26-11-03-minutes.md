# AMD Schola — Meeting Minutes

**Focus:** Unity architecture review, training and inference progress, and demo feedback  
**Date:** November 3, 2026  
**Time:** 9:37 AM (as recorded in the transcript)  
**Duration:** Approximately 35 minutes

## Attendees

**AMD**
- Alex Cann

**Student team**
- Mohammad Mahi Ali Mukati
- Talha Asif
- Anwar Khan
- Yahyaa Yasin
- Raahim Chaghtai
- Muzzammil Siddiqui
- Fahad Moinuddin


## 1. Observation and Action Spaces

The team reviewed the Unity interfaces and UML diagrams with Alex. The discussion included differences between box, discrete, and binary spaces.

Alex explained that representing the same values does not make these spaces interchangeable. Their metadata affects how a training framework constructs probability distributions and encodes choices. A box space containing zero and one does not convey the same meaning as a binary space; discrete encodings can also introduce unnecessary overhead for binary choices.

## 2. Environment Interfaces and Naming

Alex questioned names such as `IReward` and `IEpisodeStatus`: they can sound like interfaces for a reward or status value, even though they describe objects that calculate rewards or report episode status. He recommended names that make those responsibilities clearer.

The team also discussed `ITrainingStateSink`. Alex suggested considering a name such as receiver or transmitter that better communicates its role in passing state onward. No replacement name was finalized.

The diagrams did not fully explain how the training-state connection was configured. Team members said this belonged to US4 and needed clarification from that implementation. They offered to follow up with the details or an updated UML diagram.

The review also raised whether the implementation should depend on the environment interface rather than a concrete class where appropriate.

## 3. Multiple Environments and Training Communication

Multi-environment support was not confirmed during the meeting. The team described its current setup as focused on one environment and one agent and agreed to check the wider support.

Alex recommended having the Gym service manage the environments, call their methods, collect their states, and send the results as a batch. He cautioned against giving each environment a callback through which it independently submits state.

With independent submissions, the service still has to wait for every environment and distinguish a slow response from a failure. Alex said Schola had previously used a similar approach and moved away from it because of the coordination complexity.

The recommendation was to avoid asynchronous coordination unless it provides a clear benefit. If parallel work becomes necessary, the service can assign work and collect the results centrally. The team planned to revisit this design in US4.

## 4. Episode Reset and Truncation

The training sequence shown in the diagrams required an explicit reset before the next episode. Alex explained why automatic reset is important for vectorized training: when one environment finishes, it should not require a separate reset exchange that disrupts the whole batch.

The discussion covered two reset approaches:

- Reset on the next step, with the incoming action disregarded for that reset.
- Reset on the same step while preserving the final observation in the information returned to the trainer.

The reset behaviour must match the Python framework's expectations. The team planned to update the implementation and diagram descriptions accordingly; the meeting did not confirm that this was already complete.

Alex also clarified that truncation limits belong to the environment developer. A fixed step limit would not suit every problem. Both termination and truncation end an episode and lead to a reset in the simulator, but training algorithms handle their implications differently, including how they estimate remaining return.

## 5. Inference and Batching

The team reviewed the inference path through `StepperRunner`, `SimpleStepper`, observations, policy evaluation, and actions. The existing design evaluates agents individually.

Alex considered the general approach reasonable. He recommended retaining a simple single-agent policy path and optionally adding batch support for policies that can use it. Batching can improve performance by reducing repeated transfers and execution overhead, but it should not be forced on every policy.

The team proposed adding batched inference later, after the remaining integration work. Alex agreed that deferring it was reasonable.

## 6. Implementation Progress and Timeline

Team members reported that US4 had recently been implemented, including running training episodes in Unity and exporting ONNX models. They also reported a recent change to clamp model actions to the configured bounds. These were progress reports, not a full verification of those features during the meeting.

Remaining work discussed included US6 setup integration, the architecture changes raised in the review, and general polish. The team estimated that most changes could be ready the following week, with the next week used to finish and polish them.

The team described the course completion deadline as roughly the second week of December. Alex initially suggested increasing the pace, while noting that he did not yet have the full implementation picture. The two-week completion estimate remained a team target rather than a confirmed delivery date.

## 7. Return to Base Demo

Mahi presented the Return to Base scene submitted for the previous course deliverable. It creates drone objects and a base, then uses a pretrained ONNX model to move the drones back toward the base.

The setup consists of adding the game script to a demo object and assigning the model in the Inspector. The scene demonstrates the existing inference components, including `SentisPolicy` and `SimpleStepper`.

The team clarified that this demo shows inference only; it does not demonstrate episode training or the new Unity–Python training integration. An updated demo/tutorial was planned to show the additional work.

## 8. Next Meeting and Document Review

Alex said he would review the diagrams and documentation when available and send further feedback in the team chat.

He would be away at a conference the following Tuesday and travelling on Thursday. He offered **Friday, November 13**, or **the morning of Monday, November 9**, as possible alternatives. The team noted that Fridays are often busy and agreed to check everyone's calendars and respond in the chat. No meeting time was finalized.

## 9. Action Items

| Owner | Action | Timing |
| --- | --- | --- |
| Student team | Review interface names and clarify how the training-state connection is configured; update the UML diagrams. | Follow-up after the meeting. |
| Student team / US4 contributors | Review service-led state collection and confirm support for multiple environments. | As part of the next implementation updates. |
| Student team | Add or verify automatic reset behaviour and document how termination and truncation are handled. | Targeted for the next round of changes. |
| Student team | Finish US6 integration and prepare an updated demo/tutorial covering the newer work. | Most work targeted for the following week, with polish in the week after. |
| Student team | Check availability for November 9 in the morning or November 13 and reply in the chat. | Before scheduling the next meeting. |
| Alex Cann | Review the diagrams and documentation and share additional feedback in the chat. | When available. |

## 10. Items Still to Be Confirmed

- Complete attendee list and individual ownership of the technical follow-ups.
- Final interface names and training-state configuration.
- Multi-environment support and the final state-collection design.
- Which automatic reset modes are implemented and verified with Python.
- Scope and timing of the updated demo and optional batched inference.
- Date and time of the next meeting.
