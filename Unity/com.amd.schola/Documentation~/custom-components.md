# Custom sensors and actuators

`ScholaAgent` works with any `MonoBehaviour` implementing `ISensor` or `IActuator`. You do not need to change the agent or inherit from `PositionSensor` or `TranslateActuator`.

The examples below observe an object's local height and rotate it around its local Y axis. Save each public component in a file with the matching name under your project's `Assets/` directory. If your scripts use an assembly definition, reference `AMD.Schola.Runtime`.

## 1. Add a sensor

Save as **`HeightSensor.cs`**:

```csharp
// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using AMD.Schola;
using UnityEngine;
using Space = AMD.Schola.Space;

public sealed class HeightSensor : MonoBehaviour, ISensor
{
    [SerializeField]
    string m_ObservationKey = "height";

    public string ObservationKey => m_ObservationKey;

    public void InitSensor() { }

    public Space GetObservationSpace() =>
        new BoxSpace { Dimensions = { new BoxSpaceDimension() } };

    public Point CollectObservations() =>
        new BoxPoint { Values = { transform.localPosition.y } };
}
```

The space publishes one continuous, unbounded dimension; every observation returns one float in that dimension. `InitSensor` is empty because this sensor needs no setup. Use it to resolve or validate dependencies when your implementation needs them.

## 2. Add an actuator

Save as **`YawActuator.cs`**:

```csharp
// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using AMD.Schola;
using UnityEngine;
using Space = AMD.Schola.Space;

public sealed class YawActuator : MonoBehaviour, IActuator
{
    [SerializeField]
    string m_ActionKey = "yaw";

    public string ActionKey => m_ActionKey;

    public void InitActuator() { }

    public Space GetActionSpace() =>
        new BoxSpace { Dimensions = { new BoxSpaceDimension(-1f, 1f) } };

    public void TakeAction(Point action)
    {
        if (!(action is BoxPoint box) || box.Values == null || box.Values.Count != 1)
            throw new ArgumentException("Yaw requires a BoxPoint with one value.", nameof(action));

        float value = box.Values[0];
        if (float.IsNaN(value) || float.IsInfinity(value) || value < -1f || value > 1f)
            throw new ArgumentException("Yaw must be finite and within [-1, 1].", nameof(action));

        transform.Rotate(0f, value * 10f, 0f, UnityEngine.Space.Self);
    }
}
```

Each action rotates by at most ten degrees. This uses direct Transform updates, matching the current environment's synchronous stepping. It does not advance Unity physics. The actuator checks its point type, shape, and bounds before changing the object; custom actuators are responsible for those checks.

## 3. Assign the components in the Inspector

1. Add `ScholaAgent`, `HeightSensor`, and `YawActuator` to a GameObject.
2. On **Schola Agent**, set **Sensors** to one element and drag the **Height Sensor component header** into it. Set **Actuators** to one element and drag the **Yaw Actuator component header** into it. Dragging the component selects the exact binding when an object has several components.
3. Add `ScholaEnvironment` to a parent object and assign the agent to its **Agent** field.
4. Check the Schola Agent Inspector for configuration errors. For example, assigning a `YawActuator` to **Sensors** reports that the component must implement `ISensor`.

The Inspector stores component references as `MonoBehaviour` lists. The public `Sensors` and `Actuators` properties expose read-only interface views; use the setters below to replace a list in code. Existing scenes retain assignments through the unchanged `m_Sensors` and `m_Actuators` serialized field names.

## 4. Configure and exercise the agent in code

This optional smoke example replaces the Inspector lists, initializes the environment, observes, applies one yaw action, and resets the pose. Save it as **`CustomAgentExample.cs`**, attach it to the agent object, and assign its **Environment** field before entering Play Mode. Use it on its own, with training and policy runners stopped.

```csharp
// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using AMD.Schola;
using UnityEngine;

[RequireComponent(typeof(ScholaAgent), typeof(HeightSensor), typeof(YawActuator))]
public sealed class CustomAgentExample : MonoBehaviour
{
    [SerializeField]
    ScholaEnvironment m_Environment;

    void Start()
    {
        if (m_Environment == null)
            throw new InvalidOperationException("Assign the ScholaEnvironment first.");

        var agent = GetComponent<ScholaAgent>();
        var sensor = GetComponent<HeightSensor>();
        var actuator = GetComponent<YawActuator>();
        agent.SetSensors(new ISensor[] { sensor });
        agent.SetActuators(new IActuator[] { actuator });
        m_Environment.SetAgent(agent);

        m_Environment.Initialize();
        var initial = m_Environment.Reset();
        var observations = (DictPoint)initial.Observations;
        var height = (BoxPoint)observations.Points[sensor.ObservationKey];
        Debug.Log($"Initial height: {height.Values[0]}", this);

        m_Environment.Step(
            new DictPoint
            {
                Points = { [actuator.ActionKey] = new BoxPoint { Values = { 0.5f } } },
            }
        );
        m_Environment.Reset();
    }
}
```

## Binding and lifecycle rules

- Keys must be nonblank and unique within each list, using ordinal, case-sensitive comparison. A sensor and an actuator may share a key because observation and action dictionaries are separate. Keep keys and space shapes stable while a policy or training session is using them.
- Null slots and destroyed Unity components are allowed and skipped. A non-null component with the wrong interface is an error. Plain C# objects implementing an interface cannot be assigned to an agent; bindings must be `MonoBehaviour` components.
- Setters validate a whole replacement list before accepting it. An invalid replacement leaves the previous list intact. Passing a null collection is an error; use an empty array to clear a list.
- `Initialize()` validates both lists before calling `InitSensor()` or `InitActuator()`. It then builds the observation and action dictionaries, rejects null spaces, and snapshots poses before marking initialization successful. An initialization failure leaves the environment uninitialized; fix the configuration before trying again. Callbacks already invoked before a failure are not rolled back, so initialization callbacks should tolerate retries.
- `Reset()` initializes when needed, restores the snapshot, invokes the configured reward and episode-status reset hooks, and returns fresh observations. It restores the agent hierarchy and assigned actuator components' transforms. It does not automatically restore arbitrary custom fields, rigidbody state, or other objects modified by an actuator. Arrange any additional episode reset work in your game's reset flow.
- `Step()` also initializes when needed and validates both binding lists before applying actions. Calling `SetAgent()` invalidates the environment's initialization, so its next reset or step builds a new snapshot for the assigned agent.
- Direct `Define()`, `DefineActionSpace()`, `Observe()`, and `Act()` calls do not call initialization callbacks. Use `ScholaEnvironment.Initialize()` first for components that need setup. After replacing components or changing the published contract, stop the consumer and initialize again; reset alone does not rebuild a successful initialization's snapshot.
- `Act()` checks that every required action key has a non-null point before invoking any actuator. This prevents a missing key from applying only part of an action. It does not make actuator execution transactional: if a later actuator throws, earlier actuator side effects are not undone.

See the [training guide](training.md) to connect the configured environment to Python, and the [interface tests](../Tests/EditMode/Agent/ScholaAgentInterfaceTests.cs) for more examples of custom components, validation, and destroyed-reference handling.
