// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using AMD.Schola.Tests.Agent;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AMD.Schola.Tests.Environment
{
    public sealed class ScholaEnvironmentConfigurationTests
    {
        GameObject m_Root;
        ScholaEnvironment m_Environment;
        ScholaAgent m_Agent;
        ProbeSensor m_Sensor;
        ProbeActuator m_Actuator;
        ScholaEnvironmentTestFixture.InMemoryTrainingStateSink m_Sink;

        public sealed class ProbeSensor : MonoBehaviour, ISensor
        {
            public string Key = "sensor";
            public string ObservationKey => Key;
            public Space PublishedSpace = new DiscreteSpace(2);
            public Action OnInitialize;
            public int InitializationCount;
            public int ObservationCount;

            public void InitSensor()
            {
                InitializationCount++;
                OnInitialize?.Invoke();
            }

            public Space GetObservationSpace() => PublishedSpace;

            public Point CollectObservations()
            {
                ObservationCount++;
                return new DiscretePoint();
            }
        }

        public sealed class ProbeActuator : MonoBehaviour, IActuator
        {
            public string Key = "actuator";
            public string ActionKey => Key;
            public Space PublishedSpace = new DiscreteSpace(2);
            public Action OnInitialize;
            public int InitializationCount;
            public int ActionCount;

            public void InitActuator()
            {
                InitializationCount++;
                OnInitialize?.Invoke();
            }

            public Space GetActionSpace() => PublishedSpace;

            public void TakeAction(Point action)
            {
                ActionCount++;
                transform.localPosition += Vector3.right;
            }
        }

        [SetUp]
        public void SetUp()
        {
            // Add the environment before its agent so Unity's editor Reset callback
            // cannot initialize it as a side effect of AddComponent.
            m_Root = new GameObject("ConfigurationEnvironment");
            m_Environment = m_Root.AddComponent<ScholaEnvironment>();
            var agentObject = new GameObject("ConfigurationAgent");
            agentObject.transform.SetParent(m_Root.transform);
            m_Agent = agentObject.AddComponent<ScholaAgent>();
            m_Sensor = agentObject.AddComponent<ProbeSensor>();
            m_Actuator = agentObject.AddComponent<ProbeActuator>();
            m_Agent.SetSensors(new ISensor[] { m_Sensor });
            m_Agent.SetActuators(new IActuator[] { m_Actuator });
            m_Environment.SetAgent(m_Agent);
            m_Sink = new ScholaEnvironmentTestFixture.InMemoryTrainingStateSink();
            m_Environment.SetTrainingStateSink(m_Sink);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Root);
        }

        [TestCase(true, "wrong-interface")]
        [TestCase(false, "wrong-interface")]
        [TestCase(true, "blank-key")]
        [TestCase(false, "blank-key")]
        [TestCase(true, "duplicate-key")]
        [TestCase(false, "duplicate-key")]
        public void Initialize_InvalidBindingRejectsBeforeAnyInitializationCallback(
            bool sensor,
            string errorKind
        )
        {
            if (errorKind == "blank-key")
            {
                if (sensor)
                    m_Sensor.Key = " ";
                else
                    m_Actuator.Key = " ";
            }
            else
            {
                var valid = sensor ? (MonoBehaviour)m_Sensor : m_Actuator;
                var invalid =
                    errorKind == "duplicate-key" ? valid
                    : sensor ? (MonoBehaviour)m_Actuator
                    : m_Sensor;
                SetSerializedReferences(sensor ? "m_Sensors" : "m_Actuators", valid, invalid);
            }

            var error = Assert.Throws<InvalidOperationException>(() => m_Environment.Initialize());
            Assert.That(error.Message, Does.Contain(sensor ? "m_Sensors" : "m_Actuators"));
            Assert.That(m_Sensor.InitializationCount, Is.Zero);
            Assert.That(m_Actuator.InitializationCount, Is.Zero);
            Assert.That(m_Sink.InitialStates, Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Define_NullSpaceRejectsWithComponentDiagnostic(bool sensor)
        {
            SetPublishedSpace(sensor, null);
            var error = Assert.Throws<InvalidOperationException>(() =>
            {
                if (sensor)
                    m_Agent.Define();
                else
                    m_Agent.DefineActionSpace();
            });
            Assert.That(error.Message, Does.Contain("ConfigurationAgent"));
            Assert.That(error.Message, Does.Contain(sensor ? "ProbeSensor" : "ProbeActuator"));
            Assert.That(error.Message, Does.Contain(sensor ? "observation space" : "action space"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Initialize_NullSpaceDoesNotMarkInitializedAndResetRetries(bool sensor)
        {
            SetPublishedSpace(sensor, null);
            Assert.Throws<InvalidOperationException>(() => m_Environment.Initialize());
            Assert.Throws<InvalidOperationException>(() => m_Environment.Reset());
            Assert.That(m_Sensor.InitializationCount, Is.EqualTo(2));
            Assert.That(m_Actuator.InitializationCount, Is.EqualTo(2));
            Assert.That(m_Sensor.ObservationCount, Is.Zero);
            Assert.That(m_Sink.InitialStates, Is.Empty);

            SetPublishedSpace(sensor, new DiscreteSpace(2));
            m_Environment.Reset();
            Assert.That(m_Sensor.InitializationCount, Is.EqualTo(3));
            Assert.That(m_Actuator.InitializationCount, Is.EqualTo(3));
            Assert.That(m_Sink.InitialStates, Has.Count.EqualTo(1));
            m_Environment.Reset();
            Assert.That(m_Sensor.InitializationCount, Is.EqualTo(3));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Initialize_CallbackFailureLeavesEnvironmentRetryable(bool sensor)
        {
            Action fail = () => throw new InvalidOperationException("Custom init failed.");
            if (sensor)
                m_Sensor.OnInitialize = fail;
            else
                m_Actuator.OnInitialize = fail;
            var error = Assert.Throws<InvalidOperationException>(() => m_Environment.Initialize());
            Assert.That(error.Message, Does.Contain("Custom init failed"));
            m_Sensor.OnInitialize = null;
            m_Actuator.OnInitialize = null;
            m_Environment.Reset();
            Assert.That(m_Sensor.InitializationCount, Is.EqualTo(2));
            Assert.That(m_Actuator.InitializationCount, Is.EqualTo(sensor ? 1 : 2));
            Assert.That(m_Sink.InitialStates, Has.Count.EqualTo(1));
        }

        [Test]
        public void Initialize_CallbackInvalidatesKeyRejectsBeforePublishingAndResetRetries()
        {
            m_Actuator.OnInitialize = () => m_Sensor.Key = "";
            Assert.Throws<InvalidOperationException>(() => m_Environment.Initialize());
            m_Actuator.OnInitialize = null;
            m_Sensor.Key = "sensor";
            m_Environment.Reset();
            Assert.That(m_Sensor.InitializationCount, Is.EqualTo(2));
            Assert.That(m_Actuator.InitializationCount, Is.EqualTo(2));
            Assert.That(m_Sink.InitialStates, Has.Count.EqualTo(1));
        }

        [Test]
        public void Step_InitializesLazilyAndResetUsesPreActionSnapshot()
        {
            m_Agent.transform.localPosition = new Vector3(3f, 4f, 5f);
            var start = m_Agent.transform.localPosition;
            m_Environment.Step(ActionPoint());
            Assert.That(m_Sensor.InitializationCount, Is.EqualTo(1));
            Assert.That(m_Actuator.InitializationCount, Is.EqualTo(1));
            Assert.That(m_Agent.transform.localPosition, Is.EqualTo(start + Vector3.right));
            m_Environment.Reset();
            Assert.That(m_Agent.transform.localPosition, Is.EqualTo(start));
            Assert.That(m_Sensor.InitializationCount, Is.EqualTo(1));
        }

        [Test]
        public void Step_AfterFailedReinitializationRetriesBeforeAnyAction()
        {
            m_Environment.Initialize();
            m_Actuator.PublishedSpace = null;
            Assert.Throws<InvalidOperationException>(() => m_Environment.Initialize());
            Assert.Throws<InvalidOperationException>(() => m_Environment.Step(ActionPoint()));
            Assert.That(m_Actuator.ActionCount, Is.Zero);
            Assert.That(m_Sink.States, Is.Empty);
            m_Actuator.PublishedSpace = new DiscreteSpace(2);
            m_Environment.Step(ActionPoint());
            Assert.That(m_Sensor.InitializationCount, Is.EqualTo(4));
            Assert.That(m_Actuator.ActionCount, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StepAndReset_InvalidConfigurationDoesNotApplyActionsOrRestorePose(bool sensor)
        {
            m_Environment.Initialize();
            if (sensor)
                m_Sensor.Key = "";
            else
                m_Actuator.Key = "";
            m_Agent.transform.localPosition = Vector3.up;
            Assert.Throws<InvalidOperationException>(() => m_Environment.Step(ActionPoint()));
            Assert.Throws<InvalidOperationException>(() => m_Environment.Reset());
            Assert.That(m_Agent.transform.localPosition, Is.EqualTo(Vector3.up));
            Assert.That(m_Actuator.ActionCount, Is.Zero);
            Assert.That(m_Sensor.ObservationCount, Is.Zero);
            Assert.That(m_Sink.States, Is.Empty);
            Assert.That(m_Sink.InitialStates, Is.Empty);
        }

        [Test]
        public void Step_MissingLaterActionKeyDoesNotRunEarlierActuator()
        {
            var custom = m_Agent.gameObject.AddComponent<InterfaceTestActuator>();
            m_Agent.SetActuators(new IActuator[] { m_Actuator, custom });
            var error = Assert.Throws<ArgumentException>(() => m_Environment.Step(ActionPoint()));
            Assert.That(error.Message, Does.Contain("rotate"));
            Assert.That(m_Actuator.ActionCount, Is.Zero);
            Assert.That(custom.ActionCount, Is.Zero);
            Assert.That(m_Agent.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(m_Sink.States, Is.Empty);
        }

        [Test]
        public void SetAgent_ResetInitializesReplacementAndCapturesItsOwnPose()
        {
            m_Environment.Initialize();
            var replacement = new GameObject("ReplacementAgent");
            replacement.transform.SetParent(m_Root.transform);
            replacement.transform.localPosition = Vector3.up;
            var agent = replacement.AddComponent<ScholaAgent>();
            var sensor = replacement.AddComponent<ProbeSensor>();
            agent.SetSensors(new ISensor[] { sensor });
            m_Environment.SetAgent(agent);
            m_Environment.Reset();
            Assert.That(sensor.InitializationCount, Is.EqualTo(1));
            replacement.transform.localPosition = Vector3.down;
            m_Environment.Reset();
            Assert.That(replacement.transform.localPosition, Is.EqualTo(Vector3.up));
        }

        void SetPublishedSpace(bool sensor, Space space)
        {
            if (sensor)
                m_Sensor.PublishedSpace = space;
            else
                m_Actuator.PublishedSpace = space;
        }

        void SetSerializedReferences(string field, params MonoBehaviour[] components)
        {
            using (var serialized = new SerializedObject(m_Agent))
            {
                var property = serialized.FindProperty(field);
                property.arraySize = components.Length;
                for (var i = 0; i < components.Length; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = components[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static DictPoint ActionPoint() =>
            new DictPoint { Points = { ["actuator"] = new DiscretePoint { Value = 1 } } };
    }
}
