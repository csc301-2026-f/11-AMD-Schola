// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AMD.Schola
{
    /// <summary>
    /// Agent that defines observation/action spaces from sensors and actuators,
    /// aggregates Observe into a DictPoint, and fans out Act to actuators.
    /// Assigned ISensor/IActuator implementations must be MonoBehaviour components.
    /// Keys are nonblank and unique within each collection (ordinal, case-sensitive).
    /// </summary>
    public sealed class ScholaAgent : MonoBehaviour, IAgent
    {
        // Unity serializes component references, not interface fields. Keep these names
        // so existing scenes and prefabs retain their sensor and actuator assignments.
        [SerializeField, Tooltip("Assign MonoBehaviour components implementing ISensor.")]
        List<MonoBehaviour> m_Sensors = new List<MonoBehaviour>();

        [SerializeField, Tooltip("Assign MonoBehaviour components implementing IActuator.")]
        List<MonoBehaviour> m_Actuators = new List<MonoBehaviour>();

        [NonSerialized]
        ComponentView<ISensor> m_SensorView;

        [NonSerialized]
        ComponentView<IActuator> m_ActuatorView;

        /// <summary>Live component view; empty or destroyed references are returned as null.</summary>
        public IReadOnlyList<ISensor> Sensors =>
            m_SensorView ??= new ComponentView<ISensor>(() => m_Sensors, nameof(m_Sensors));

        /// <summary>Live component view; empty or destroyed references are returned as null.</summary>
        public IReadOnlyList<IActuator> Actuators =>
            m_ActuatorView ??= new ComponentView<IActuator>(() => m_Actuators, nameof(m_Actuators));

        /// <summary>Assigns sensor components after validating the entire collection.</summary>
        public void SetSensors(IEnumerable<ISensor> sensors)
        {
            var components = ToComponents(sensors, nameof(sensors));
            var error = FindBindingError<ISensor>(
                components,
                sensor => sensor.ObservationKey,
                nameof(m_Sensors),
                nameof(ISensor.ObservationKey)
            );
            if (error != null)
                throw new ArgumentException(error, nameof(sensors));
            m_Sensors = components;
        }

        /// <summary>Assigns actuator components after validating the entire collection.</summary>
        public void SetActuators(IEnumerable<IActuator> actuators)
        {
            var components = ToComponents(actuators, nameof(actuators));
            var error = FindBindingError<IActuator>(
                components,
                actuator => actuator.ActionKey,
                nameof(m_Actuators),
                nameof(IActuator.ActionKey)
            );
            if (error != null)
                throw new ArgumentException(error, nameof(actuators));
            m_Actuators = components;
        }

        /// <summary>
        /// Checks component types and keys without invoking initialization, observation, or action callbacks.
        /// Empty/destroyed references are allowed and skipped. Reports the first configuration error.
        /// </summary>
        public bool TryValidateConfiguration(out string error)
        {
            error = SensorBindingError() ?? ActuatorBindingError();
            return error == null;
        }

        string SensorBindingError() =>
            FindBindingError<ISensor>(
                m_Sensors,
                sensor => sensor.ObservationKey,
                nameof(m_Sensors),
                nameof(ISensor.ObservationKey)
            );

        string ActuatorBindingError() =>
            FindBindingError<IActuator>(
                m_Actuators,
                actuator => actuator.ActionKey,
                nameof(m_Actuators),
                nameof(IActuator.ActionKey)
            );

        string FindBindingError<T>(
            IReadOnlyList<MonoBehaviour> components,
            Func<T, string> keyOf,
            string field,
            string keyName
        )
            where T : class
        {
            var keys = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < components.Count; i++)
            {
                var component = components[i];
                // Test Unity's destroyed-object null before converting to an interface.
                if (component == null)
                    continue;

                var location =
                    $"{nameof(ScholaAgent)} '{name}'.{field}[{i}] ('{component.name}', {component.GetType().Name})";
                if (!(component is T binding))
                    return $"{location} must implement {typeof(T).Name}.";

                var key = keyOf(binding);
                if (string.IsNullOrWhiteSpace(key))
                    return $"{location} has a null, empty, or whitespace-only {keyName}; assign a nonblank key.";

                if (keys.TryGetValue(key, out var previous))
                    return $"{location} has duplicate {keyName} '{key}', already used by {field}[{previous}] ('{components[previous].name}', {components[previous].GetType().Name}); keys must be unique within this collection.";

                keys.Add(key, i);
            }

            return null;
        }

        static void RequireValidBindings(string error)
        {
            if (error != null)
                throw new InvalidOperationException(error);
        }

        static List<MonoBehaviour> ToComponents<T>(IEnumerable<T> values, string parameter)
            where T : class
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameter);
            }

            var components = new List<MonoBehaviour>();
            foreach (var value in values)
            {
                if (value == null)
                {
                    components.Add(null);
                }
                else if (value is MonoBehaviour component)
                {
                    components.Add(component == null ? null : component);
                }
                else
                {
                    throw new ArgumentException(
                        $"Entry {components.Count} must be a MonoBehaviour implementing {typeof(T).Name}, got {value.GetType().Name}.",
                        parameter
                    );
                }
            }

            return components;
        }

        // Resolve the current serialized list so retained views also reflect setter calls
        // and Inspector edits. Check Unity's destroyed-object null before casting to T.
        sealed class ComponentView<T> : IReadOnlyList<T>
            where T : class
        {
            readonly Func<List<MonoBehaviour>> m_Source;
            readonly string m_FieldName;

            public ComponentView(Func<List<MonoBehaviour>> source, string fieldName)
            {
                m_Source = source;
                m_FieldName = fieldName;
            }

            public int Count => m_Source().Count;

            public T this[int index]
            {
                get
                {
                    var component = m_Source()[index];
                    if (component == null)
                    {
                        return null;
                    }

                    if (component is T value)
                    {
                        return value;
                    }

                    throw new InvalidOperationException(
                        $"{nameof(ScholaAgent)}.{m_FieldName}[{index}] references '{component.name}' ({component.GetType().Name}), which must implement {typeof(T).Name}."
                    );
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < Count; i++)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// Builds the published observation space from attached sensors (DictSpace of sensor spaces).
        /// </summary>
        public Space Define()
        {
            RequireValidBindings(SensorBindingError());
            var dictSpace = new DictSpace();
            foreach (var sensor in Sensors)
            {
                if (sensor == null)
                {
                    continue;
                }

                var space = sensor.GetObservationSpace();
                if (space == null)
                    throw new InvalidOperationException(
                        $"{nameof(ScholaAgent)} '{name}' sensor '{sensor.ObservationKey}' ({sensor.GetType().Name}) returned a null observation space."
                    );
                dictSpace.Spaces.Add(sensor.ObservationKey, space);
            }

            return dictSpace;
        }

        /// <summary>
        /// Builds the published action space from attached actuators (DictSpace of actuator spaces).
        /// Invalid component bindings, blank keys, and duplicate keys are rejected.
        /// </summary>
        public Space DefineActionSpace()
        {
            RequireValidBindings(ActuatorBindingError());
            var dictSpace = new DictSpace();
            foreach (var actuator in Actuators)
            {
                if (actuator == null)
                {
                    continue;
                }

                var space = actuator.GetActionSpace();
                if (space == null)
                    throw new InvalidOperationException(
                        $"{nameof(ScholaAgent)} '{name}' actuator '{actuator.ActionKey}' ({actuator.GetType().Name}) returned a null action space."
                    );
                dictSpace.Spaces.Add(actuator.ActionKey, space);
            }

            return dictSpace;
        }

        /// <summary>
        /// Collects current observations from attached sensors into a DictPoint.
        /// </summary>
        public Point Observe()
        {
            RequireValidBindings(SensorBindingError());
            var dictPoint = new DictPoint();
            foreach (var sensor in Sensors)
            {
                if (sensor == null)
                {
                    continue;
                }

                dictPoint.Points[sensor.ObservationKey] = sensor.CollectObservations();
            }

            return dictPoint;
        }

        /// <summary>
        /// Applies a DictPoint action to attached actuators keyed by <see cref="IActuator.ActionKey"/>.
        /// All keys are checked before any actuator is applied so a missing key leaves the scene unchanged.
        /// </summary>
        public void Act(Point action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (!(action is DictPoint dictPoint))
            {
                throw new ArgumentException(
                    $"Expected {nameof(DictPoint)}, got {action.GetType().Name}.",
                    nameof(action)
                );
            }

            RequireValidBindings(ActuatorBindingError());

            foreach (var actuator in Actuators)
            {
                if (actuator == null)
                {
                    continue;
                }

                if (
                    !dictPoint.Points.TryGetValue(actuator.ActionKey, out var nested)
                    || nested == null
                )
                {
                    throw new ArgumentException(
                        $"Missing action key '{actuator.ActionKey}'; expected keys: {FormatExpectedActionKeys()}.",
                        nameof(action)
                    );
                }
            }

            foreach (var actuator in Actuators)
            {
                if (actuator == null)
                {
                    continue;
                }

                actuator.TakeAction(dictPoint.Points[actuator.ActionKey]);
            }
        }

        string FormatExpectedActionKeys()
        {
            var builder = new StringBuilder();
            foreach (var actuator in Actuators)
            {
                if (actuator == null)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(actuator.ActionKey);
            }

            return builder.Length > 0 ? builder.ToString() : "(none)";
        }
    }
}
