# US1–US4 Unity architecture (UML)
US1 (Observations), US2 (actions), US3(episodes) are implemented. US4 (train through Schola) will be merged soon.

## Legend

Every class, component, and dashed edge uses one of these markers.

| Marker | Mermaid cue | Meaning |
| --- | --- | --- |
| implemented | Solid box, no extra stereotype | Type on `main` with a Runtime or Inference class |
| placeholder | `<<placeholder>>` | Contract on `main`, no Runtime implementation (`IReward`, `IEpisodeStatus`, `ITrainingStateSink`) |
| adjacent | `<<US5>>` | Inference loop. |
| Unreal | `<<Unreal>>` | Comparison type. Not a Unity type |
| not on main | `<<notOnMain>>` | `GymService` host, reverted JSON bridge, `origin/US4` gRPC spike |

Dashed associations mean optional, missing, or not implemented on `main`.

---

## 1. System context

Unity Runtime on `main` can observe, act, and run a local episode. Inference (US5) drives the same `IAgent` without rewards or reset. The Unity gRPC **server** is the missing US4 piece.

```mermaid
flowchart LR
  subgraph unityRuntime ["Unity Runtime — implemented on main"]
    sensors["ISensor / PositionSensor"]
    agent["ScholaAgent"]
    actuators["IActuator / TranslateActuator"]
    env["ScholaEnvironment"]
    reward["IReward\n<<placeholder>>"]
    status["IEpisodeStatus\n<<placeholder>>"]
    sink["ITrainingStateSink\n<<placeholder>>"]
    sensors --> agent
    agent --> actuators
    env --> agent
    env -.-> reward
    env -.-> status
    env -.-> sink
  end

  subgraph inferencePath ["Inference — <<US5>>"]
    runner["StepperRunner"]
    stepper["SimpleStepper"]
    policy["IPolicy / SentisPolicy"]
    runner --> stepper
    stepper --> agent
    stepper --> policy
  end

  subgraph pythonSide ["Python — implemented on main"]
    grpcClient["GrpcProtocol"]
    gymEnv["GymEnv / VecEnv / RayEnv"]
    grpcClient --> gymEnv
    gymEnv --> sb3["Stable-Baselines3"]
    gymEnv --> rllib["RLlib"]
  end

  subgraph missing ["<<notOnMain>>"]
    gymService["GymService gRPC server"]
  end

  proto["Proto/ GymService contract"]
  sink -.-> gymService
  gymService -.-> grpcClient
  gymService --- proto
  grpcClient --- proto
```

Training path expected by existing Python: Unity hosts `GymService`; Python is the client. That server is not on `main`. `ITrainingStateSink` is the US4 hook on the environment; nothing on `main` serializes it to gRPC.

Sources: `Unity/com.amd.schola/Runtime/`, `Unity/com.amd.schola/Inference/`, `Unity/com.amd.schola/Training/README.md`, `Proto/GymConnector.proto`, `Resources/python/schola/`.

---

## 2. Agent composition (US1–US2)

`ScholaAgent` implements `IAgent` and stores **concrete** `List<PositionSensor>` / `List<TranslateActuator>` (exposed as `IReadOnlyList`), not `ISensor` / `IActuator`. A new sensor or actuator type needs a code change on the agent.

```mermaid
classDiagram
  class ISensor {
    <<interface>>
    +InitSensor()
    +GetObservationSpace() Space
    +CollectObservations() Point
  }

  class PositionSensor {
    +ObservationKey string
    +InitSensor()
    +GetObservationSpace() Space
    +CollectObservations() Point
  }

  class IActuator {
    <<interface>>
    +InitActuator()
    +GetActionSpace() Space
    +TakeAction(Point)
  }

  class TranslateActuator {
    +ActionKey string
    +InitActuator()
    +GetActionSpace() Space
    +TakeAction(Point)
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

  class Point {
    <<abstract>>
  }
  class BoxPoint {
    +Values List~float~
    +Shape List~int~
  }
  class DictPoint {
    +Points Dict~string Point~
  }
  class DiscretePoint {
    +Value int
  }

  class Space {
    <<abstract>>
  }
  class BoxSpace {
    +Dimensions List~BoxSpaceDimension~
    +Shape List~int~
  }
  class BoxSpaceDimension {
    +Low floatNullable
    +High floatNullable
  }
  class DictSpace {
    +Spaces Dict~string Space~
  }
  class DiscreteSpace {
    +High int
  }

  PositionSensor ..|> ISensor
  TranslateActuator ..|> IActuator
  ScholaAgent ..|> IAgent
  ScholaAgent "1" *-- "*" PositionSensor : Sensors
  ScholaAgent "1" *-- "*" TranslateActuator : Actuators
  Point <|-- BoxPoint
  Point <|-- DictPoint
  Point <|-- DiscretePoint
  Space <|-- BoxSpace
  Space <|-- DictSpace
  Space <|-- DiscreteSpace
  BoxSpace *-- BoxSpaceDimension
  DictPoint o-- Point
  DictSpace o-- Space
  PositionSensor ..> BoxPoint : CollectObservations
  PositionSensor ..> BoxSpace : GetObservationSpace
  TranslateActuator ..> BoxPoint : TakeAction
  TranslateActuator ..> BoxSpace : GetActionSpace
  ScholaAgent ..> DictSpace : Define / DefineActionSpace
  ScholaAgent ..> DictPoint : Observe / Act
```

Notes, from the types on `main`:

- `Define()` builds a `DictSpace` keyed by `PositionSensor.ObservationKey` (default `"position"`).
- `Observe()` builds a matching `DictPoint` of 3-float world `BoxPoint`s.
- `DefineActionSpace()` builds a `DictSpace` keyed by `TranslateActuator.ActionKey` (default `"translate"`). Duplicate keys warn and last-write-wins.
- `Act(Point)` requires a `DictPoint`, checks **every** actuator key before any `TakeAction`, then applies 3-float `BoxPoint` deltas to `transform.position`. Wrong type or arity throws.
- Unity Point/Space omit proto `MultiDiscrete`, `MultiBinary`, and `Text`. There is no `Space.Contains` / bound enforcement on `main`.
- Unity `IAgent` is Observe/Act only. It has no `Define` / `GetStatus` / `SetStatus` (those exist on Unreal `IAgent`; see diagram 6). `ScholaAgent.Define` is a concrete method, not part of `IAgent`.

Sources: `Runtime/Sensors/`, `Runtime/Actuators/`, `Runtime/Agent/`, `Runtime/Points/`, `Runtime/Spaces/`.

---

## 3. Episode environment (US3)

`ScholaEnvironment` is one env, one `ScholaAgent`. `IEnvironment.Step` takes one `Point` and returns one `AgentState` with no agent-id field. `IReward`, `IEpisodeStatus`, and `ITrainingStateSink` are contracts on `main` with no Runtime class; tests supply fakes.

```mermaid
classDiagram
  class IEnvironment {
    <<interface>>
    +Initialize() InteractionDefinition
    +Seed(int)
    +SetOptions(IReadOnlyDictionary)
    +Reset() InitialAgentState
    +Step(Point) AgentState
  }

  class ScholaEnvironment {
    +DefaultAgentId SingleAgent
    +SetAgent(ScholaAgent)
    +SetReward(IReward)
    +SetEpisodeStatus(IEpisodeStatus)
    +SetTrainingStateSink(ITrainingStateSink)
    +Initialize() InteractionDefinition
    +Seed(int)
    +SetOptions(IReadOnlyDictionary)
    +Reset() InitialAgentState
    +Step(Point) AgentState
  }

  class InteractionDefinition {
    +ObservationSpace Space
    +ActionSpace Space
  }

  class AgentState {
    +Observations Point
    +Reward float
    +Terminated bool
    +Truncated bool
    +Info Dict~string string~
  }

  class InitialAgentState {
    +Observations Point
    +Info Dict~string string~
  }

  class IReward {
    <<interface>>
    <<placeholder>>
    +OnEpisodeReset()
    +ComputeReward() float
  }

  class IEpisodeStatus {
    <<interface>>
    <<placeholder>>
    +OnEpisodeReset()
    +GetStatus(out terminated out truncated)
  }

  class ITrainingStateSink {
    <<interface>>
    <<placeholder>>
    +SubmitState(AgentState)
    +SubmitInitialState(InitialAgentState)
  }

  class ScholaAgent

  ScholaEnvironment ..|> IEnvironment
  ScholaEnvironment "1" *-- "1" ScholaAgent
  ScholaEnvironment ..> InteractionDefinition : Initialize
  ScholaEnvironment ..> AgentState : Step
  ScholaEnvironment ..> InitialAgentState : Reset
  ScholaEnvironment ..> IReward : optional
  ScholaEnvironment ..> IEpisodeStatus : optional
  ScholaEnvironment ..> ITrainingStateSink : optional US4 hook
```

`IEnvironment` mirrors Unreal `ISingleAgentScholaEnvironment` with C# return values instead of out-params.

Behavior on `main`:

- `Initialize()` inits sensors/actuators, snapshots local poses of the agent hierarchy plus configured actuator transforms, and returns `InteractionDefinition(agent.Define(), agent.DefineActionSpace())`.
- `Step(action)` does `Act` → `ComputeReward` (or `0`) → `GetStatus` (or both flags false) → `Observe` → optional `SubmitState`. It does **not** auto-reset; the caller must `Reset()`.
- `Reset()` calls `Initialize()` if not yet initialized, then restores the initialize snapshot, calls `OnEpisodeReset` on reward/status, observes, and returns `InitialAgentState` (obs + info only). No reward or done flags on reset.
- `Seed` and `SetOptions` are no-ops. `AgentState.Info` is allocated empty and never populated.
- `DefaultAgentId = "SingleAgent"` is declared and unused. Reserved for a future Gym connector wrap that keys proto maps the way Unreal's single-agent adapter does.
- Reward and status may be assigned in the inspector (`GetComponentInChildren`) or via setters. The sink is **not** auto-resolved; it must be `SetTrainingStateSink`.

Sources: `Runtime/Environment/`, `Runtime/TrainingDataTypes/`, `Runtime/Common/InteractionDefinition.cs`.

---

## 4. Dual-loop sequences (US3 vs US5)

Both loops share `IAgent.Observe` / `IAgent.Act`. Only the environment loop has reward, episode flags, reset, and the training-state sink. Inference has none of those.

### Training / episode loop — `ScholaEnvironment.Step`

```mermaid
sequenceDiagram
  actor Caller
  participant Env as ScholaEnvironment
  participant Agent as ScholaAgent
  participant Actuator as TranslateActuator
  participant Reward as IReward placeholder
  participant Status as IEpisodeStatus placeholder
  participant Sensor as PositionSensor
  participant Sink as ITrainingStateSink placeholder

  Caller ->> Env: Step(action DictPoint)
  Env ->> Agent: Act(action)
  Agent ->> Agent: require every ActionKey
  Agent ->> Actuator: TakeAction(BoxPoint)
  Actuator ->> Actuator: transform.position += delta

  alt IReward bound
    Env ->> Reward: ComputeReward()
  else unbound
    Env ->> Env: reward = 0
  end

  alt IEpisodeStatus bound
    Env ->> Status: GetStatus(terminated, truncated)
  else unbound
    Env ->> Env: both flags false
  end

  Env ->> Agent: Observe()
  Agent ->> Sensor: CollectObservations()
  Sensor -->> Agent: BoxPoint x,y,z
  Agent -->> Env: DictPoint

  opt ITrainingStateSink bound
    Env ->> Sink: SubmitState(AgentState)
  end

  Env -->> Caller: AgentState
  Note over Caller,Env: No auto-reset. Caller must Reset() to start the next episode.
```

### Reset — `ScholaEnvironment.Reset`

```mermaid
sequenceDiagram
  actor Caller
  participant Env as ScholaEnvironment
  participant Reward as IReward placeholder
  participant Status as IEpisodeStatus placeholder
  participant Agent as ScholaAgent
  participant Sink as ITrainingStateSink placeholder

  Caller ->> Env: Reset()
  Env ->> Env: RestorePoses()
  opt IReward bound
    Env ->> Reward: OnEpisodeReset()
  end
  opt IEpisodeStatus bound
    Env ->> Status: OnEpisodeReset()
  end
  Env ->> Agent: Observe()
  opt ITrainingStateSink bound
    Env ->> Sink: SubmitInitialState(InitialAgentState)
  end
  Env -->> Caller: InitialAgentState
```

### Inference loop — `SimpleStepper.Step` (`<<US5>>`)

```mermaid
sequenceDiagram
  participant Runner as StepperRunner US5
  participant Stepper as SimpleStepper US5
  participant Agent as IAgent
  participant Policy as IPolicy SentisPolicy US5

  Runner ->> Stepper: Step()
  loop each living IAgent by list index
    Stepper ->> Agent: Observe()
  end
  loop each observation
    Stepper ->> Policy: Think(obs, out action)
  end
  loop each agent
    Stepper ->> Agent: Act(action)
  end
  Stepper -->> Runner: success / fail
  Note over Stepper,Policy: No reward, no terminated/truncated, no Reset, no ITrainingStateSink. Agents identified by list index, not agent-id.
```

`SimpleStepper` can drive `IReadOnlyList<IAgent>` with one shared `IPolicy`. That is inference-only multi-agent by index, not a training multi-agent environment.

Sources: `Runtime/Environment/ScholaEnvironment.cs`, `Runtime/Steppers/SimpleStepper.cs`, `Runtime/Steppers/StepperRunner.cs`, `Runtime/Policies/IPolicy.cs`, `Inference/SentisPolicy.cs`.

---

## 5. Protocol nesting vs Unity cardinality (US3–US4)

Proto and Unreal nest `env_id` → `agent_id` → payload. Unity `IEnvironment` is one env, one agent. Python `GymEnv` also assumes 1 env, 1 agent. `GymVectorEnv` / SB3 flatten `(env, agent)` slots; `RayEnv` uses true multi-agent dicts. Unity has no `IMultiAgentEnvironment` and no `map<string, AgentState>`.

```mermaid
classDiagram
  class ProtoTrainingState {
    <<protocol>>
    +environment_states EnvironmentState[]
  }
  class ProtoEnvironmentState {
    <<protocol>>
    +agent_states map~string AgentState~
  }
  class ProtoAgentState {
    <<protocol>>
    +observations Point
    +reward float
    +terminated bool
    +truncated bool
    +info map~string string~
  }
  class ProtoTrainingDefinition {
    <<protocol>>
    +environment_definitions EnvironmentDefinition[]
  }
  class ProtoEnvironmentDefinition {
    <<protocol>>
    +agent_definitions map~string AgentDefinition~
  }
  class ProtoStateUpdate {
    <<protocol>>
    +reset Reset
    +step Step
  }
  class ProtoStep {
    <<protocol>>
    +environments EnvironmentStep[]
  }
  class ProtoEnvironmentStep {
    <<protocol>>
    +updates map~string Point~
  }

  ProtoTrainingState *-- ProtoEnvironmentState : env index
  ProtoEnvironmentState *-- ProtoAgentState : agent_id
  ProtoTrainingDefinition *-- ProtoEnvironmentDefinition : env index
  ProtoStateUpdate *-- ProtoStep
  ProtoStep *-- ProtoEnvironmentStep : env index
  ProtoEnvironmentStep --> ProtoAgentState : agent_id keys actions

  class UnityIEnvironment {
    +Step(Point) AgentState
    +Reset() InitialAgentState
  }
  class UnityAgentState {
    +Observations Point
    +Reward float
    +Terminated bool
    +Truncated bool
    +Info Dict
  }
  class UnityScholaEnvironment {
    +DefaultAgentId SingleAgent
  }

  UnityScholaEnvironment ..|> UnityIEnvironment
  UnityIEnvironment ..> UnityAgentState : one state, no agent-id

  note for UnityScholaEnvironment "DefaultAgentId unused on main.\nUnreal single-agent adapter injects key SingleAgent.\nGym connector would wrap here for US4."
```

| Layer | Environments | Agents per env | How identity is keyed |
| --- | --- | --- | --- |
| Proto `TrainingState` / `TrainingDefinition` | Many (`repeated`) | Many (`map<string, …>`) | env index, then `agent_id` |
| Unreal `IScholaEnvironment` | Connector holds `TArray` of envs | Many (`TMap<FString, …>`) | `agent_id` string |
| Unreal `ISingleAgentScholaEnvironment` | One | One, adapter injects `"SingleAgent"` | injected key |
| Unity `ScholaEnvironment` | One | One | none; `DefaultAgentId` unused |
| Python `GymEnv` | One | One | 1:1 wrap |
| Python `GymVectorEnv` / `sb3.VecEnv` | Many slots | Flattened `(env, agent)`; agents in one env finish together | slot index |
| Python `RayEnv` | Many | True multi-agent dicts | `agent_id` |
| Unity `SimpleStepper` (`<<US5>>`) | N/A | Many `IAgent`s, one `IPolicy` | list index, no episode |

No competitive/cooperative configs exist in Unity.

Sources: `Proto/State.proto`, `Proto/Definitions.proto`, `Proto/StateUpdates.proto`, `Runtime/Environment/IEnvironment.cs`, `Resources/python/schola/gym/env.py`.

---

## 6. US4 target and Unreal delta

### Target training connection (`GymService`)

Python already calls these three RPCs. Unity must host the server. `Training/README.md` specifies generate C# from `Proto/`, host `GymService`, parse `-ScholaPort=`. That host is not on `main`. No generated C# under `Training/Generated/`.

```mermaid
flowchart TB
  subgraph pythonClient ["Python client — implemented"]
    cli["schola sb3/rllib train … external"]
    protoClient["GrpcProtocol"]
    cli --> protoClient
  end

  subgraph contract ["Proto contract — implemented"]
    startRpc["StartGymConnector(GymConnectorStartRequest)"]
    defRpc["RequestTrainingDefinition()"]
    updateRpc["UpdateState(StateUpdate) returns State"]
  end

  subgraph unityHost ["Unity host — <<notOnMain>>"]
    gymService["GymService server"]
    port["parse -ScholaPort="]
    sink["ITrainingStateSink <<placeholder>>"]
    env["ScholaEnvironment"]
    gymService --- port
    gymService -.-> sink
    sink -.-> env
  end

  protoClient -->|"StartGymConnector"| startRpc
  protoClient -->|"RequestTrainingDefinition"| defRpc
  protoClient -->|"UpdateState reset or step"| updateRpc
  startRpc -.-> gymService
  defRpc -.-> gymService
  updateRpc -.-> gymService
```

`UpdateState` carries either `Reset` (`map<env_id, EnvironmentSettings>`) or `Step` (`repeated EnvironmentStep`, each `map<agent_id, Point>`). The reply is `State` with either `TrainingState` or `InitialState`. Auto-reset (`AutoResetType`: SAME_STEP / NEXT_STEP / DISABLED) is a connector concern; `ScholaEnvironment` does not auto-reset.

### Unreal comparison (`<<Unreal>>`)

```mermaid
classDiagram
  class UnityIAgent {
    +Observe() Point
    +Act(Point)
  }
  class UnrealIAgent {
    <<Unreal>>
    +Define()
    +GetStatus() EAgentStatus
    +SetStatus(EAgentStatus)
    +Observe()
    +Act()
  }

  class UnityIEnvironment {
    +Step(Point) AgentState
  }
  class UnrealISingleAgent {
    <<Unreal>>
    +InitializeEnvironment()
    +Step()
    +Reset()
  }
  class UnrealIScholaEnvironment {
    <<Unreal>>
    +Step() AgentStateMap
  }
  class UnrealIMultiAgent {
    <<Unreal>>
    +Step() AgentStateMap
  }

  class UnityIReward {
    <<placeholder>>
  }
  class UnityIEpisodeStatus {
    <<placeholder>>
  }
  class UnityITrainingStateSink {
    <<placeholder>>
  }
  class URPCGymConnector {
    <<Unreal>>
  }
  class UnityGymService {
    <<notOnMain>>
  }

  UnrealISingleAgent ..|> UnrealIScholaEnvironment : adapter key SingleAgent
  UnrealIMultiAgent ..|> UnrealIScholaEnvironment
  URPCGymConnector ..> UnrealIScholaEnvironment : TArray of envs
  UnityITrainingStateSink ..> UnityGymService : intended US4
  UnityIEnvironment ..> UnityIReward
  UnityIEnvironment ..> UnityIEpisodeStatus

  note for UnrealISingleAgent "Unreal has no IReward or IEpisodeStatus. Reward and done flags are filled inside Step."
  note for UnityIAgent "Unity IAgent is Observe/Act only. Define is a ScholaAgent method not on the interface."
```

Unreal differences that matter for a Unity Gym connector:

- Unreal `IAgent` also has `Define`, `GetStatus`, `SetStatus`. Unity `IAgent` is Observe/Act only; `ScholaAgent.Define` is concrete.
- Unreal environments fill `FAgentState.Reward` / `bTerminated` / `bTruncated` inside `Step`. They have no `IReward` / `IEpisodeStatus`.
- Unreal `IMultiAgentScholaEnvironment` has **no Unity equivalent**.
- Unreal hosts `GymService` via `URPCGymConnector` / `UCommunicationManager`. Unity does not.

### Reverted and spike US4

These are historical. They are not types on `main`.

1. **`origin/US4`** — native gRPC spike: generated proto C# plus `GymStartupServer` implementing `StartGymConnector` only. Step/reset unimplemented.
2. **PR #17 / `8fe1ddb` (reverted by PR #18 / `547a9f4`)** — custom loopback TCP + JSON (`TrainingServer`, `TrainingSession`, `TrainingWire`) plus Python `schola.unity.bridge` that adapted JSON to `GymService`. It did **not** implement Unity-side `GymService`, called `IEnvironment` directly, and did **not** implement `ITrainingStateSink`.

Which of native gRPC, a restored JSON bridge, or a third design the team will ship is unresolved. README and `Training/` point at native `GymService`.

Sources: `Proto/GymConnector.proto`, `Unity/com.amd.schola/Training/README.md`, `Source/Schola/Public/Agent/AgentInterface.h`, `Source/ScholaTraining/Public/Environment/`, `Source/ScholaProtobuf/Public/GymConnectors/gRPC/gRPCGymConnector.h`.

---

## Type inventory

| Type | Story | Status | Notes |
| --- | --- | --- | --- |
| `ISensor` | US1 | implemented | `InitSensor`, `GetObservationSpace`, `CollectObservations` |
| `PositionSensor` | US1 | implemented | 3-float world `BoxPoint`, key `"position"` |
| `IActuator` | US2 | implemented | `InitActuator`, `GetActionSpace`, `TakeAction` |
| `TranslateActuator` | US2 | implemented | 3-float `BoxPoint` delta to `transform.position` |
| `IAgent` | US1–US2 | implemented | Observe / Act only |
| `ScholaAgent` | US1–US2 | implemented | Concrete sensor/actuator lists (public `IReadOnlyList`) |
| `Point` / `BoxPoint` / `DictPoint` / `DiscretePoint` | US1–US2 | implemented | No MultiDiscrete, MultiBinary, Text |
| `Space` / `BoxSpace` / `DictSpace` / `DiscreteSpace` | US1–US2 | implemented | No `Contains` / bound enforcement |
| `IEnvironment` | US3 | implemented | Mirrors Unreal `ISingleAgentScholaEnvironment` |
| `ScholaEnvironment` | US3 | implemented | One `ScholaAgent`; `Seed` / `SetOptions` no-ops |
| `IReward` | US3 | placeholder | No Runtime class; test fakes only |
| `IEpisodeStatus` | US3 | placeholder | Independent terminated / truncated; test fakes only |
| `AgentState` / `InitialAgentState` | US3 | implemented | Hand-written proto mirrors; no agent-id field |
| `InteractionDefinition` | US3 | implemented | ObservationSpace + ActionSpace |
| `ITrainingStateSink` | US4 | placeholder | `SubmitState` / `SubmitInitialState`; US4 hook |
| `IPolicy` / `SentisPolicy` | US5 | adjacent | Local ONNX; one agent per `Think` |
| `IStepper` / `SimpleStepper` / `StepperRunner` | US5 | adjacent | Observe-think-act; no reward or reset |
| `GymService` | US4 | not on main | `StartGymConnector`, `RequestTrainingDefinition`, `UpdateState` |
| TrainingServer JSON bridge | US4 | not on main | PR #17 reverted; do not treat as implemented |
| `IMultiAgentScholaEnvironment` | Unreal | Unreal | No Unity equivalent |
