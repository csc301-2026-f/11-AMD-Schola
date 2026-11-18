// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AMD.Schola.Tests.Agent
{
    public sealed class ScholaAgentInterfaceTests
    {
        GameObject m_Root;
        ScholaAgent m_Agent;
        InterfaceTestSensor m_Sensor;
        InterfaceTestActuator m_Actuator;

        [SetUp]
        public void SetUp()
        {
            m_Root = new GameObject("InterfaceCompositionTests");
            m_Agent = m_Root.AddComponent<ScholaAgent>();
            m_Sensor = m_Root.AddComponent<InterfaceTestSensor>();
            m_Actuator = m_Root.AddComponent<InterfaceTestActuator>();
            Assert.That(m_Sensor, Is.Not.Null, "The test sensor must be an attachable component.");
            Assert.That(
                m_Actuator,
                Is.Not.Null,
                "The test actuator must be an attachable component."
            );
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Root);
        }

        [Test]
        public void CustomAndBuiltInComponents_PublishAndConsumeTheirOwnPointTypes()
        {
            var position = m_Root.AddComponent<PositionSensor>();
            var translate = m_Root.AddComponent<TranslateActuator>();
            m_Agent.SetSensors(new ISensor[] { position, m_Sensor });
            m_Agent.SetActuators(new IActuator[] { translate, m_Actuator });

            var observations = (DictSpace)m_Agent.Define();
            var actions = (DictSpace)m_Agent.DefineActionSpace();
            Assert.That(
                observations.Spaces.Keys,
                Is.EquivalentTo(new[] { "position", "category" })
            );
            Assert.That(observations.Spaces["position"], Is.TypeOf<BoxSpace>());
            Assert.That(observations.Spaces["category"], Is.TypeOf<DiscreteSpace>());
            Assert.That(actions.Spaces["translate"], Is.TypeOf<BoxSpace>());
            Assert.That(actions.Spaces["rotate"], Is.TypeOf<DiscreteSpace>());

            var rotation = new DiscretePoint { Value = 1 };
            m_Agent.Act(
                new DictPoint
                {
                    Points =
                    {
                        ["translate"] = new BoxPoint { Values = { 1f, 2f, 3f } },
                        ["rotate"] = rotation,
                    },
                }
            );
            Assert.That(m_Root.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(m_Actuator.LastAction, Is.SameAs(rotation));
            Assert.That(
                Quaternion.Angle(m_Root.transform.rotation, Quaternion.Euler(0f, 90f, 0f)),
                Is.LessThan(0.001f)
            );

            m_Sensor.Value = 6;
            var observed = (DictPoint)m_Agent.Observe();
            Assert.That(((DiscretePoint)observed.Points["category"]).Value, Is.EqualTo(6));
            Assert.That(
                ((BoxPoint)observed.Points["position"]).Values,
                Is.EqualTo(new[] { 1f, 2f, 3f })
            );
        }

        [Test]
        public void PreviouslyObtainedViews_ReflectSetterReplacementAndInspectorChanges()
        {
            IReadOnlyList<ISensor> sensors = m_Agent.Sensors;
            IReadOnlyList<IActuator> actuators = m_Agent.Actuators;
            var position = m_Root.AddComponent<PositionSensor>();
            var translate = m_Root.AddComponent<TranslateActuator>();

            m_Agent.SetSensors(new ISensor[] { m_Sensor });
            m_Agent.SetActuators(new IActuator[] { m_Actuator });
            Assert.That(sensors[0], Is.SameAs(m_Sensor));
            Assert.That(actuators[0], Is.SameAs(m_Actuator));

            SetSerializedReference("m_Sensors", position);
            SetSerializedReference("m_Actuators", translate);
            Assert.That(sensors[0], Is.SameAs(position));
            Assert.That(actuators[0], Is.SameAs(translate));
            Assert.That(
                ((DictSpace)m_Agent.Define()).Spaces.Keys,
                Is.EquivalentTo(new[] { "position" })
            );
            Assert.That(
                ((DictSpace)m_Agent.DefineActionSpace()).Spaces.Keys,
                Is.EquivalentTo(new[] { "translate" })
            );

            m_Agent.SetSensors(Array.Empty<ISensor>());
            m_Agent.SetActuators(Array.Empty<IActuator>());
            Assert.That(sensors, Is.Empty);
            Assert.That(actuators, Is.Empty);
        }

        [Test]
        public void Setters_NullEnumerableThrowsWithoutReplacingExistingBindings()
        {
            BindCustomComponents();
            Assert.Throws<ArgumentNullException>(() => m_Agent.SetSensors(null));
            Assert.Throws<ArgumentNullException>(() => m_Agent.SetActuators(null));
            Assert.That(m_Agent.Sensors[0], Is.SameAs(m_Sensor));
            Assert.That(m_Agent.Actuators[0], Is.SameAs(m_Actuator));
        }

        [Test]
        public void Setters_NonComponentImplementationThrowsBeforeChangingBindings()
        {
            BindCustomComponents();
            var position = m_Root.AddComponent<PositionSensor>();
            var translate = m_Root.AddComponent<TranslateActuator>();

            Assert.Throws<ArgumentException>(() =>
                m_Agent.SetSensors(new ISensor[] { position, new PlainSensor() })
            );
            Assert.Throws<ArgumentException>(() =>
                m_Agent.SetActuators(new IActuator[] { translate, new PlainActuator() })
            );
            Assert.That(m_Agent.Sensors, Has.Count.EqualTo(1));
            Assert.That(m_Agent.Sensors[0], Is.SameAs(m_Sensor));
            Assert.That(m_Agent.Actuators, Has.Count.EqualTo(1));
            Assert.That(m_Agent.Actuators[0], Is.SameAs(m_Actuator));
        }

        [Test]
        public void Setters_EnumerationFailureLeavesPreviousBindingsIntact()
        {
            BindCustomComponents();
            Assert.Throws<InvalidOperationException>(() =>
                m_Agent.SetSensors(FailAfter<ISensor>(m_Root.AddComponent<PositionSensor>()))
            );
            Assert.Throws<InvalidOperationException>(() =>
                m_Agent.SetActuators(FailAfter<IActuator>(m_Root.AddComponent<TranslateActuator>()))
            );
            Assert.That(m_Agent.Sensors[0], Is.SameAs(m_Sensor));
            Assert.That(m_Agent.Actuators[0], Is.SameAs(m_Actuator));
        }

        [Test]
        public void NullAndDestroyedBindings_AreSkippedByAgentAndEnvironment()
        {
            var destroyedSensor = m_Root.AddComponent<PositionSensor>();
            var destroyedActuator = m_Root.AddComponent<TranslateActuator>();
            m_Agent.SetSensors(new ISensor[] { null, destroyedSensor, m_Sensor });
            m_Agent.SetActuators(new IActuator[] { null, destroyedActuator, m_Actuator });
            Object.DestroyImmediate(destroyedSensor);
            Object.DestroyImmediate(destroyedActuator);

            Assert.That(m_Agent.Sensors[0], Is.Null);
            Assert.That(m_Agent.Sensors[1], Is.Null);
            Assert.That(m_Agent.Actuators[0], Is.Null);
            Assert.That(m_Agent.Actuators[1], Is.Null);
            Assert.That(m_Agent.TryValidateConfiguration(out var error), Is.True, error);

            var environment = m_Root.AddComponent<ScholaEnvironment>();
            environment.SetAgent(m_Agent);
            Assert.DoesNotThrow(() => environment.Initialize());
            Assert.That(m_Sensor.InitializationCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(m_Actuator.InitializationCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                ((DictSpace)m_Agent.Define()).Spaces.Keys,
                Is.EquivalentTo(new[] { "category" })
            );
            Assert.That(
                ((DictSpace)m_Agent.DefineActionSpace()).Spaces.Keys,
                Is.EquivalentTo(new[] { "rotate" })
            );
            Assert.That(
                ((DictPoint)m_Agent.Observe()).Points.Keys,
                Is.EquivalentTo(new[] { "category" })
            );
            Assert.DoesNotThrow(() => environment.Step(RotationAction()));
            Assert.That(m_Actuator.ActionCount, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InspectorReferenceWithWrongInterface_ThrowsDescriptiveError(bool sensor)
        {
            var field = sensor ? "m_Sensors" : "m_Actuators";
            SetSerializedReference(field, sensor ? (MonoBehaviour)m_Actuator : m_Sensor);

            var error = Assert.Throws<InvalidOperationException>(() =>
            {
                if (sensor)
                    _ = m_Agent.Sensors[0];
                else
                    _ = m_Agent.Actuators[0];
            });
            Assert.That(error.Message, Does.Contain(sensor ? "ISensor" : "IActuator"));
            Assert.That(error.Message, Does.Contain("0"));
            Assert.That(m_Agent.TryValidateConfiguration(out var diagnostic), Is.False);
            Assert.That(diagnostic, Does.Contain(sensor ? "ISensor" : "IActuator"));
            if (sensor)
            {
                Assert.Throws<InvalidOperationException>(() => m_Agent.Define());
                Assert.Throws<InvalidOperationException>(() => m_Agent.Observe());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => m_Agent.DefineActionSpace());
                Assert.Throws<InvalidOperationException>(() => m_Agent.Act(RotationAction()));
            }
            AssertNoCallbacks();
        }

        [Test]
        public void Act_MissingCustomKeyDoesNotApplyEarlierBuiltInActuator()
        {
            var translate = m_Root.AddComponent<TranslateActuator>();
            m_Agent.SetActuators(new IActuator[] { translate, m_Actuator });
            var action = new DictPoint
            {
                Points = { ["translate"] = new BoxPoint { Values = { 1f, 0f, 0f } } },
            };

            var error = Assert.Throws<ArgumentException>(() => m_Agent.Act(action));
            Assert.That(error.Message, Does.Contain("rotate"));
            Assert.That(m_Root.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(m_Actuator.ActionCount, Is.Zero);
        }

        [Test]
        public void DuplicateSensorKey_RejectsReplacementAndKeepsExistingBindings()
        {
            BindCustomComponents();
            var position = m_Root.AddComponent<PositionSensor>();
            position.ObservationKey = m_Sensor.ObservationKey;
            var error = Assert.Throws<ArgumentException>(() =>
                m_Agent.SetSensors(new ISensor[] { position, m_Sensor })
            );
            Assert.That(error.Message, Does.Contain("duplicate ObservationKey"));
            Assert.That(error.Message, Does.Contain("m_Sensors[0]"));
            Assert.That(error.Message, Does.Contain("m_Sensors[1]"));
            Assert.That(m_Agent.Sensors, Has.Count.EqualTo(1));
            Assert.That(m_Agent.Sensors[0], Is.SameAs(m_Sensor));
        }

        [Test]
        public void DuplicateActuatorKey_RejectsReplacementAndKeepsExistingBindings()
        {
            BindCustomComponents();
            var second = m_Root.AddComponent<InterfaceTestActuator>();
            var error = Assert.Throws<ArgumentException>(() =>
                m_Agent.SetActuators(new IActuator[] { second, m_Actuator })
            );
            Assert.That(error.Message, Does.Contain("duplicate ActionKey"));
            Assert.That(error.Message, Does.Contain("m_Actuators[0]"));
            Assert.That(error.Message, Does.Contain("m_Actuators[1]"));
            Assert.That(m_Agent.Actuators, Has.Count.EqualTo(1));
            Assert.That(m_Agent.Actuators[0], Is.SameAs(m_Actuator));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t\n")]
        public void BlankKeys_RejectReplacementWithoutChangingEitherCollection(string key)
        {
            BindCustomComponents();
            var sensor = m_Root.AddComponent<InterfaceTestSensor>();
            var actuator = m_Root.AddComponent<InterfaceTestActuator>();
            sensor.ObservationKey = key;
            actuator.ActionKey = key;
            Assert.Throws<ArgumentException>(() => m_Agent.SetSensors(new ISensor[] { sensor }));
            Assert.Throws<ArgumentException>(() =>
                m_Agent.SetActuators(new IActuator[] { actuator })
            );
            Assert.That(m_Agent.Sensors[0], Is.SameAs(m_Sensor));
            Assert.That(m_Agent.Actuators[0], Is.SameAs(m_Actuator));
            Assert.That(m_Agent.TryValidateConfiguration(out _), Is.True);
        }

        [TestCase(true, null)]
        [TestCase(true, "")]
        [TestCase(true, " \t\n")]
        [TestCase(false, null)]
        [TestCase(false, "")]
        [TestCase(false, " \t\n")]
        public void KeyChangedAfterBinding_IsRejectedByInspectorValidationAndRuntime(
            bool sensor,
            string key
        )
        {
            BindCustomComponents();
            if (sensor)
                m_Sensor.ObservationKey = key;
            else
                m_Actuator.ActionKey = key;
            Assert.That(m_Agent.TryValidateConfiguration(out var error), Is.False);
            Assert.That(error, Does.Contain(sensor ? "ObservationKey" : "ActionKey"));
            Assert.That(error, Does.Contain("InterfaceCompositionTests"));
            Assert.That(error, Does.Contain("[0]"));
            if (sensor)
            {
                Assert.Throws<InvalidOperationException>(() => m_Agent.Define());
                Assert.Throws<InvalidOperationException>(() => m_Agent.Observe());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => m_Agent.DefineActionSpace());
                Assert.Throws<InvalidOperationException>(() => m_Agent.Act(RotationAction()));
            }
            AssertNoCallbacks();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InspectorDuplicateKeys_AreRejectedBeforeComponentCallbacks(bool sensor)
        {
            BindCustomComponents();
            if (sensor)
            {
                var second = m_Root.AddComponent<InterfaceTestSensor>();
                SetSerializedReference("m_Sensors", m_Sensor, second);
                Assert.Throws<InvalidOperationException>(() => m_Agent.Define());
                Assert.Throws<InvalidOperationException>(() => m_Agent.Observe());
                Assert.That(second.ObservationCount, Is.Zero);
                Assert.That(second.SpaceRequestCount, Is.Zero);
            }
            else
            {
                var second = m_Root.AddComponent<InterfaceTestActuator>();
                SetSerializedReference("m_Actuators", m_Actuator, second);
                Assert.Throws<InvalidOperationException>(() => m_Agent.DefineActionSpace());
                Assert.Throws<InvalidOperationException>(() => m_Agent.Act(RotationAction()));
                Assert.That(second.ActionCount, Is.Zero);
                Assert.That(second.SpaceRequestCount, Is.Zero);
            }
            Assert.That(m_Agent.TryValidateConfiguration(out var error), Is.False);
            Assert.That(error, Does.Contain("duplicate"));
            AssertNoCallbacks();
        }

        [Test]
        public void KeyUniqueness_IsOrdinalAndIndependentAcrossCollections()
        {
            var second = m_Root.AddComponent<InterfaceTestSensor>();
            second.ObservationKey = "Category";
            m_Actuator.ActionKey = m_Sensor.ObservationKey;
            m_Agent.SetSensors(new ISensor[] { m_Sensor, second });
            m_Agent.SetActuators(new IActuator[] { m_Actuator });
            Assert.That(m_Agent.TryValidateConfiguration(out var error), Is.True, error);
            Assert.That(
                ((DictSpace)m_Agent.Define()).Spaces.Keys,
                Is.EquivalentTo(new[] { "category", "Category" })
            );
            Assert.That(
                ((DictSpace)m_Agent.DefineActionSpace()).Spaces.Keys,
                Is.EquivalentTo(new[] { "category" })
            );
        }

        [Test]
        public void RepeatedComponentReference_IsRejectedAsDuplicateKey()
        {
            BindCustomComponents();
            Assert.Throws<ArgumentException>(() =>
                m_Agent.SetSensors(new ISensor[] { m_Sensor, m_Sensor })
            );
            Assert.Throws<ArgumentException>(() =>
                m_Agent.SetActuators(new IActuator[] { m_Actuator, m_Actuator })
            );
            Assert.That(m_Agent.Sensors, Has.Count.EqualTo(1));
            Assert.That(m_Agent.Actuators, Has.Count.EqualTo(1));
        }

        [Test]
        public void InspectorUsesAgentEditor_AndValidationDoesNotInvokeCallbacks()
        {
            BindCustomComponents();
            var editor = UnityEditor.Editor.CreateEditor(m_Agent);
            try
            {
                Assert.That(
                    editor.GetType().FullName,
                    Is.EqualTo("AMD.Schola.Editor.ScholaAgentEditor")
                );
                Assert.That(m_Agent.TryValidateConfiguration(out var error), Is.True, error);
                AssertNoCallbacks();
            }
            finally
            {
                Object.DestroyImmediate(editor);
            }
        }

        void AssertNoCallbacks()
        {
            Assert.That(m_Sensor.InitializationCount, Is.Zero);
            Assert.That(m_Actuator.InitializationCount, Is.Zero);
            Assert.That(m_Sensor.SpaceRequestCount, Is.Zero);
            Assert.That(m_Actuator.SpaceRequestCount, Is.Zero);
            Assert.That(m_Sensor.ObservationCount, Is.Zero);
            Assert.That(m_Actuator.ActionCount, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Environment_InitializesAndResetsCustomActuatorOutsideAgentHierarchy(
            bool onParent
        )
        {
            Object.DestroyImmediate(m_Actuator);
            var external = new GameObject("ExternalCustomActuator");
            external.transform.SetParent(m_Root.transform);
            var agentObject = new GameObject("ChildAgent");
            agentObject.transform.SetParent(onParent ? external.transform : m_Root.transform);
            var agent = agentObject.AddComponent<ScholaAgent>();
            var sensor = agentObject.AddComponent<InterfaceTestSensor>();
            var actuator = external.AddComponent<InterfaceTestActuator>();
            var startRotation = Quaternion.Euler(10f, 20f, 30f);
            external.transform.localRotation = startRotation;
            agent.SetSensors(new ISensor[] { sensor });
            agent.SetActuators(new IActuator[] { actuator });
            var environmentObject = new GameObject("Environment");
            environmentObject.transform.SetParent(m_Root.transform);
            var environment = environmentObject.AddComponent<ScholaEnvironment>();
            environment.SetAgent(agent);

            environment.Initialize();
            Assert.That(sensor.InitializationCount, Is.EqualTo(1));
            Assert.That(actuator.InitializationCount, Is.EqualTo(1));
            environment.Step(RotationAction());
            Assert.That(
                Quaternion.Angle(external.transform.localRotation, startRotation),
                Is.GreaterThan(1f)
            );
            environment.Reset();
            Assert.That(
                Quaternion.Angle(external.transform.localRotation, startRotation),
                Is.LessThan(0.001f)
            );
        }

        void BindCustomComponents()
        {
            m_Agent.SetSensors(new ISensor[] { m_Sensor });
            m_Agent.SetActuators(new IActuator[] { m_Actuator });
        }

        void SetSerializedReference(string field, params MonoBehaviour[] components)
        {
            using (var serialized = new SerializedObject(m_Agent))
            {
                var property = serialized.FindProperty(field);
                Assert.That(property, Is.Not.Null);
                property.arraySize = components.Length;
                for (var i = 0; i < components.Length; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = components[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static DictPoint RotationAction()
        {
            return new DictPoint { Points = { ["rotate"] = new DiscretePoint { Value = 1 } } };
        }

        static IEnumerable<T> FailAfter<T>(T first)
        {
            yield return first;
            throw new InvalidOperationException("Input enumeration failed.");
        }

        sealed class PlainSensor : ISensor
        {
            public string ObservationKey => "plain";

            public void InitSensor() { }

            public Space GetObservationSpace() => new DiscreteSpace(1);

            public Point CollectObservations() => new DiscretePoint();
        }

        sealed class PlainActuator : IActuator
        {
            public string ActionKey => "plain";

            public void InitActuator() { }

            public Space GetActionSpace() => new DiscreteSpace(1);

            public void TakeAction(Point action) { }
        }
    }
}
