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
            m_Sensors = ToComponents(sensors, nameof(sensors));
        }

        /// <summary>Assigns actuator components after validating the entire collection.</summary>
        public void SetActuators(IEnumerable<IActuator> actuators)
        {
            m_Actuators = ToComponents(actuators, nameof(actuators));
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
            var dictSpace = new DictSpace();
            foreach (var sensor in Sensors)
            {
                if (sensor == null)
                {
                    continue;
                }

                dictSpace.Spaces[sensor.ObservationKey] = sensor.GetObservationSpace();
            }

            return dictSpace;
        }

        /// <summary>
        /// Builds the published action space from attached actuators (DictSpace of actuator spaces).
        /// Duplicate <see cref="IActuator.ActionKey"/> values warn and last-write-wins.
        /// </summary>
        public Space DefineActionSpace()
        {
            var dictSpace = new DictSpace();
            foreach (var actuator in Actuators)
            {
                if (actuator == null)
                {
                    continue;
                }

                if (dictSpace.Spaces.ContainsKey(actuator.ActionKey))
                {
                    Debug.LogWarning(
                        $"Duplicate action key '{actuator.ActionKey}' on {nameof(ScholaAgent)} '{name}'; later actuator overwrites the earlier action space entry.",
                        this
                    );
                }

                dictSpace.Spaces[actuator.ActionKey] = actuator.GetActionSpace();
            }

            return dictSpace;
        }

        /// <summary>
        /// Collects current observations from attached sensors into a DictPoint.
        /// </summary>
        public Point Observe()
        {
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
