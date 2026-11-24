# AMD Schola Unity — architecture overview (UML)

Simplified view of US1–US5 on `main`: Runtime agents and environments, the Python↔Unity training bridge, and local inference. For exhaustive diagrams, sequences, and type inventory, see [`us1-us4-uml.md`](us1-us4-uml.md) (filename is historical; that file already covers US5).

**Shipped training path:** Python hosts `GymService` (gRPC, default ~8000); Unity hosts `TrainingServer` (TCP/JSON, default 8001, wire VERSION 2). States return on the wire. `ITrainingStateSink` exists on environments but is unused by the training connector. Optional Unity-hosted gRPC under `Training/Generated/` is tooling only.

---

## 1. Layered architecture

**Takeaway:** Runtime owns Observe/Act and episodes. Training reaches Runtime through Python’s gRPC adapter and Unity’s TCP session. Inference reuses the same agent loop without rewards, reset, or Python.

```mermaid
classDiagram
  direction TB

  %% —— Python Integration ——
  class VecEnv {
    <<Python SB3>>
    one slot per agent
    env index 0
  }
  class GrpcProtocol {
    <<Python>>
    GymServiceStub
  }
  class UnityGymService {
    <<Python>>
    GymServiceServicer
  }
  class UnityConnection {
    <<Python>>
    TCP JSON VERSION 2
    default :8001
  }
  class UnityBridgeServer {
    <<Python>>
    gRPC loopback ~:8000
  }

  %% —— Unity Training US4 ——
  class TrainingServer {
    +StartServer()
    +PumpRequests()
    +StopServer()
    +RunScriptOnPlay bool
  }
  class TrainingSession {
    +Handle(TrainingRequest) TrainingResponse
  }
  class TrainingWire {
    <<static>>
    +Version 2
    +EncodeSpace / EncodePoint
    +DecodePoint / Validate
  }
  class TrainingScriptSettings {
    +GetLaunchableScript(port, timeout)
  }
  class LaunchableScript {
    +LaunchScript()
    +KillScript()
  }

  %% —— Unity Runtime US1–US3 ——
  class IMultiAgentEnvironment {
    <<interface>>
    +Initialize() map~string InteractionDefinition~
    +Reset() map~string InitialAgentState~
    +Step(map) map~string AgentState~
  }
  class IEnvironment {
    <<interface>>
    +Initialize() InteractionDefinition
    +Reset() InitialAgentState
    +Step(Point) AgentState
  }
  class ScholaMultiAgentEnvironment {
    +SetAgents(map)
  }
  class ScholaEnvironment {
    +DefaultAgentId SingleAgent
    +SetAgent(ScholaAgent)
  }
  class SingleAgentEnvironmentAdapter {
    wraps under SingleAgent
  }
  class IAgent {
    <<interface>>
    +Observe() Point
    +Act(Point)
  }
  class ScholaAgent {
    +Sensors IReadOnlyList~PositionSensor~
    +Actuators IReadOnlyList~TranslateActuator~
    +Define() Space
    +DefineActionSpace() Space
    +Observe() Point
    +Act(Point)
  }
  class ISensor {
    <<interface>>
    +GetObservationSpace() Space
    +CollectObservations() Point
  }
  class PositionSensor {
    +ObservationKey string
  }
  class IActuator {
    <<interface>>
    +GetActionSpace() Space
    +TakeAction(Point)
  }
  class TranslateActuator {
    +ActionKey string
  }
  class IReward {
    <<interface>>
    <<placeholder>>
    +ComputeReward() float
  }
  class IEpisodeStatus {
    <<interface>>
    <<placeholder>>
    +GetStatus(out terminated out truncated)
  }
  class AgentState {
    +Observations Point
    +Reward float
    +Terminated bool
    +Truncated bool
  }
  class IPolicy {
    <<interface>>
    +Init(InteractionDefinition) bool
    +Think(Point observations, out Point actions) bool
  }
  class IStepper {
    <<interface>>
    +Init(agents, policy) bool
    +Step() bool
  }
  class SimpleStepper
  class StepperRunner {
    +Setup(stepper, agents, policy)
  }

  %% —— Unity Inference US5 ——
  class PolicyRunner {
    +StartRunning()
    +StopRunning()
  }
  class SentisPolicy {
    +Init(InteractionDefinition) bool
    +Think(observations, out actions) bool
  }

  %% Relationships
  VecEnv --> GrpcProtocol
  GrpcProtocol ..> UnityGymService : gRPC
  UnityBridgeServer *-- UnityGymService
  UnityBridgeServer *-- UnityConnection
  UnityGymService ..> UnityConnection
  UnityConnection ..> TrainingServer : loopback TCP

  TrainingServer --> TrainingSession
  TrainingServer ..> TrainingScriptSettings : Run Script On Play
  TrainingScriptSettings ..> LaunchableScript
  TrainingSession ..> TrainingWire
  TrainingSession --> IMultiAgentEnvironment

  ScholaMultiAgentEnvironment ..|> IMultiAgentEnvironment
  SingleAgentEnvironmentAdapter ..|> IMultiAgentEnvironment
  SingleAgentEnvironmentAdapter --> IEnvironment
  ScholaEnvironment ..|> IEnvironment
  ScholaEnvironment "1" *-- "1" ScholaAgent
  ScholaMultiAgentEnvironment "1" *-- "*" ScholaAgent : by agent id
  ScholaEnvironment ..> IReward : optional
  ScholaEnvironment ..> IEpisodeStatus : optional
  ScholaMultiAgentEnvironment ..> IReward : optional
  ScholaMultiAgentEnvironment ..> IEpisodeStatus : optional
  ScholaEnvironment ..> AgentState : Step
  ScholaMultiAgentEnvironment ..> AgentState : Step

  ScholaAgent ..|> IAgent
  PositionSensor ..|> ISensor
  TranslateActuator ..|> IActuator
  ScholaAgent "1" *-- "*" PositionSensor
  ScholaAgent "1" *-- "*" TranslateActuator

  SimpleStepper ..|> IStepper
  SentisPolicy ..|> IPolicy
  PolicyRunner --> SentisPolicy
  PolicyRunner --> StepperRunner
  StepperRunner --> SimpleStepper
  SimpleStepper --> IPolicy
  SimpleStepper --> IAgent
```

**Cardinality:** One Unity environment on the wire (index 0). Agents are keyed by id; the adapter injects `"SingleAgent"`. Python `GymEnv` stays one agent per env; `schola unity train` uses SB3 `VecEnv` and can train many agents on that single env (shared policy).

**Sensor/actuator lists:** `ScholaAgent` stores concrete `PositionSensor` / `TranslateActuator` lists, not `ISensor` / `IActuator`. New sensor or actuator types require an agent code change.

**Shared loop:** Training and inference both call `IAgent.Observe` / `IAgent.Act`. Only training uses environment reward, terminated/truncated, reset, and the wire protocol.

Sources: `Runtime/`, `Training/`, `Inference/`, `Resources/python/schola/unity/bridge.py`, `Resources/python/schola/scripts/unity.py`.

---

## 2. Space / Point model

**Takeaway:** Spaces describe legal shapes; points carry values. Default agents are Box leaves under Dict. Discrete and MultiBinary exist for nesting and the training wire; `SentisPolicy` has no MultiBinary.

```mermaid
classDiagram
  direction TB

  class Space {
    <<abstract>>
  }
  class BoxSpace {
    +Dimensions
    +Shape
  }
  class DictSpace {
    +Spaces Dict~string Space~
  }
  class DiscreteSpace {
    +High int
  }
  class MultiBinarySpace {
    +Shape int
  }

  class Point {
    <<abstract>>
  }
  class BoxPoint {
    +Values List~float~
  }
  class DictPoint {
    +Points Dict~string Point~
  }
  class DiscretePoint {
    +Value int
  }
  class MultiBinaryPoint {
    +Values List~bool~
  }

  Space <|-- BoxSpace
  Space <|-- DictSpace
  Space <|-- DiscreteSpace
  Space <|-- MultiBinarySpace
  DictSpace o-- Space

  Point <|-- BoxPoint
  Point <|-- DictPoint
  Point <|-- DiscretePoint
  Point <|-- MultiBinaryPoint
  DictPoint o-- Point
```

Wire kinds: `"dict"` | `"box"` | `"discrete"` | `"multi_binary"`. Proto `MultiDiscrete` / `Text` are unimplemented in Unity.

Sources: `Runtime/Spaces/`, `Runtime/Points/`, `Training/TrainingWire.cs`.

---

## 3. One training step (behavioral anchor)

```mermaid
sequenceDiagram
  participant Py as UnityGymService / bridge
  participant Srv as TrainingServer
  participant Ses as TrainingSession
  participant Env as IMultiAgentEnvironment
  participant Ag as ScholaAgent

  Py->>Srv: TCP JSON step (VERSION 2)
  Srv->>Ses: Handle(step)
  Ses->>Env: Step(actions by agent id)
  Env->>Ag: Act then Observe
  Env-->>Ses: AgentState map
  Ses-->>Srv: TrainingResponse
  Srv-->>Py: length-prefixed JSON
```

`TrainingSession` may autoreset when every agent is done. Environments do not auto-reset inside `Step`.

---

## 4. Unity vs Unreal (parity notes)

Short comparison for partner discussions (no full Unreal UML here):

1. **Environments** — Unity `IEnvironment` / `IMultiAgentEnvironment` mirror Unreal single- and multi-agent contracts; Unity wraps single-agent under `"SingleAgent"` via `SingleAgentEnvironmentAdapter`.
2. **Agent surface** — Unreal `IAgent` includes `Define` / status; Unity `IAgent` is Observe/Act only (`Define` is concrete on `ScholaAgent`).
3. **Reward / done** — Unreal fills reward and flags inside env `Step`; Unity splits them into optional `IReward` / `IEpisodeStatus` (no Runtime framework class; samples implement them).
4. **Connector** — Unreal hosts `GymService` in-process (`URPCGymConnector`); Unity ships TCP/JSON and lets Python `schola.unity.bridge` host `GymService`.
5. **Run Script On Play** — Same idea on both sides (`URPCGymConnector` ↔ `TrainingServer` + `LaunchableScript`): launch the trainer on Play, stop when the session ends.

---

## Legend

| Marker | Meaning |
| --- | --- |
| Solid types | On `main` in Runtime, Training, Inference, or Python bridge |
| `<<placeholder>>` | Contract with no Runtime framework class (`IReward`, `IEpisodeStatus`) |
| Dashed links | Optional binding or cross-process dependency |

`ITrainingStateSink` is omitted from the primary diagram; it remains an optional environment hook unused by the shipped connector.
