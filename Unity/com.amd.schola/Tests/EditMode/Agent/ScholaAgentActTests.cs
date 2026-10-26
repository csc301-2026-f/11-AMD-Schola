// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Text.RegularExpressions;
using AMD.Schola;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AMD.Schola.Tests.Agent
{
    /// <summary>
    /// US2 agent action aggregation tests.
    /// Act fans out DictPoint entries by ActionKey; invalid top-level actions throw clearly.
    /// </summary>
    public sealed class ScholaAgentActTests
    {
        const float Tolerance = 1e-5f;
        const string TranslateKey = "translate";

        [Test]
        public void Act_ValidDictPoint_AppliesToActuator()
        {
            var agentGo = new GameObject("ScholaAgent_Act");
            var actuatorGo = new GameObject("TranslateActuator");
            try
            {
                actuatorGo.transform.SetParent(agentGo.transform);
                actuatorGo.transform.position = new Vector3(10f, 20f, 30f);

                var actuator = actuatorGo.AddComponent<TranslateActuator>();
                actuator.ActionKey = TranslateKey;

                var agent = agentGo.AddComponent<ScholaAgent>();
                agent.SetActuators(new[] { actuator });
                actuator.InitActuator();

                var action = new DictPoint
                {
                    Points = { [TranslateKey] = new BoxPoint { Values = { 1f, -2f, 0.5f } } },
                };

                agent.Act(action);

                var expected = new Vector3(11f, 18f, 30.5f);
                Assert.That(
                    actuatorGo.transform.position.x,
                    Is.EqualTo(expected.x).Within(Tolerance)
                );
                Assert.That(
                    actuatorGo.transform.position.y,
                    Is.EqualTo(expected.y).Within(Tolerance)
                );
                Assert.That(
                    actuatorGo.transform.position.z,
                    Is.EqualTo(expected.z).Within(Tolerance)
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(agentGo);
            }
        }

        [Test]
        public void Act_Null_Throws()
        {
            var agentGo = new GameObject("ScholaAgent_ActNull");
            var actuatorGo = new GameObject("TranslateActuator");
            try
            {
                actuatorGo.transform.SetParent(agentGo.transform);

                var actuator = actuatorGo.AddComponent<TranslateActuator>();
                actuator.ActionKey = TranslateKey;

                var agent = agentGo.AddComponent<ScholaAgent>();
                agent.SetActuators(new[] { actuator });
                actuator.InitActuator();

                Assert.Throws<ArgumentNullException>(() => agent.Act(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(agentGo);
            }
        }

        [Test]
        public void Act_NonDictPoint_ThrowsClearError()
        {
            var agentGo = new GameObject("ScholaAgent_ActNonDict");
            var actuatorGo = new GameObject("TranslateActuator");
            try
            {
                actuatorGo.transform.SetParent(agentGo.transform);

                var actuator = actuatorGo.AddComponent<TranslateActuator>();
                actuator.ActionKey = TranslateKey;

                var agent = agentGo.AddComponent<ScholaAgent>();
                agent.SetActuators(new[] { actuator });
                actuator.InitActuator();

                var wrong = new BoxPoint { Values = { 1f, 2f, 3f } };

                var ex = Assert.Throws<ArgumentException>(() => agent.Act(wrong));
                Assert.That(
                    ex.Message,
                    Does.Contain("DictPoint").IgnoreCase,
                    "Error must mention the expected DictPoint type"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(agentGo);
            }
        }

        [Test]
        public void Act_MissingKey_ThrowsClearError()
        {
            var agentGo = new GameObject("ScholaAgent_ActMissingKey");
            var actuatorGo = new GameObject("TranslateActuator");
            try
            {
                actuatorGo.transform.SetParent(agentGo.transform);

                var actuator = actuatorGo.AddComponent<TranslateActuator>();
                actuator.ActionKey = TranslateKey;

                var agent = agentGo.AddComponent<ScholaAgent>();
                agent.SetActuators(new[] { actuator });
                actuator.InitActuator();

                var action = new DictPoint
                {
                    Points = { ["other"] = new BoxPoint { Values = { 1f, 2f, 3f } } },
                };

                var ex = Assert.Throws<ArgumentException>(() => agent.Act(action));
                Assert.That(
                    ex.Message,
                    Does.Contain(TranslateKey),
                    "Error must mention the missing expected action key"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(agentGo);
            }
        }

        [Test]
        public void Act_MissingLaterKey_DoesNotApplyEarlierActuators()
        {
            var agentGo = new GameObject("ScholaAgent_ActMissingLaterKey");
            var actuatorAGo = new GameObject("TranslateActuatorA");
            var actuatorBGo = new GameObject("TranslateActuatorB");
            try
            {
                actuatorAGo.transform.SetParent(agentGo.transform);
                actuatorBGo.transform.SetParent(agentGo.transform);
                actuatorAGo.transform.position = new Vector3(10f, 20f, 30f);
                actuatorBGo.transform.position = new Vector3(1f, 2f, 3f);

                var actuatorA = actuatorAGo.AddComponent<TranslateActuator>();
                actuatorA.ActionKey = TranslateKey;
                var actuatorB = actuatorBGo.AddComponent<TranslateActuator>();
                actuatorB.ActionKey = "translate_b";

                var agent = agentGo.AddComponent<ScholaAgent>();
                agent.SetActuators(new[] { actuatorA, actuatorB });
                actuatorA.InitActuator();
                actuatorB.InitActuator();

                var startA = actuatorAGo.transform.position;
                var startB = actuatorBGo.transform.position;

                var action = new DictPoint
                {
                    Points = { [TranslateKey] = new BoxPoint { Values = { 1f, -2f, 0.5f } } },
                };

                var ex = Assert.Throws<ArgumentException>(() => agent.Act(action));
                Assert.That(
                    ex.Message,
                    Does.Contain("translate_b"),
                    "Error must mention the missing later action key"
                );
                Assert.That(
                    actuatorAGo.transform.position,
                    Is.EqualTo(startA),
                    "Earlier actuator must not move when a later action key is missing"
                );
                Assert.That(
                    actuatorBGo.transform.position,
                    Is.EqualTo(startB),
                    "Later actuator must not move when its action key is missing"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(agentGo);
            }
        }

        [Test]
        public void DefineActionSpace_StableKeys()
        {
            var agentGo = new GameObject("ScholaAgent_DefineActionSpace");
            var actuatorAGo = new GameObject("ActuatorA");
            var actuatorBGo = new GameObject("ActuatorB");
            try
            {
                actuatorAGo.transform.SetParent(agentGo.transform);
                actuatorBGo.transform.SetParent(agentGo.transform);

                var actuatorA = actuatorAGo.AddComponent<TranslateActuator>();
                actuatorA.ActionKey = TranslateKey;
                var actuatorB = actuatorBGo.AddComponent<TranslateActuator>();
                actuatorB.ActionKey = "translate_b";

                var agent = agentGo.AddComponent<ScholaAgent>();
                agent.SetActuators(new[] { actuatorA, actuatorB });
                actuatorA.InitActuator();
                actuatorB.InitActuator();

                var defined = agent.DefineActionSpace() as DictSpace;
                Assert.That(defined, Is.Not.Null, "DefineActionSpace must return a DictSpace");
                Assert.That(
                    defined.Spaces.Keys,
                    Is.EquivalentTo(new[] { TranslateKey, "translate_b" }),
                    "Action space keys must match actuator ActionKeys"
                );
                Assert.That(defined.Spaces[TranslateKey], Is.TypeOf<BoxSpace>());
                Assert.That(defined.Spaces["translate_b"], Is.TypeOf<BoxSpace>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(agentGo);
            }
        }

        [Test]
        public void DefineActionSpace_DuplicateActionKey_WarnsAndKeepsLast()
        {
            var agentGo = new GameObject("ScholaAgent_DuplicateActionKey");
            var actuatorAGo = new GameObject("ActuatorA");
            var actuatorBGo = new GameObject("ActuatorB");
            try
            {
                actuatorAGo.transform.SetParent(agentGo.transform);
                actuatorBGo.transform.SetParent(agentGo.transform);

                var actuatorA = actuatorAGo.AddComponent<TranslateActuator>();
                actuatorA.ActionKey = TranslateKey;
                var actuatorB = actuatorBGo.AddComponent<TranslateActuator>();
                actuatorB.ActionKey = TranslateKey;

                var agent = agentGo.AddComponent<ScholaAgent>();
                agent.SetActuators(new[] { actuatorA, actuatorB });
                actuatorA.InitActuator();
                actuatorB.InitActuator();

                LogAssert.Expect(
                    LogType.Warning,
                    new Regex($"Duplicate action key '{TranslateKey}'")
                );

                var defined = agent.DefineActionSpace() as DictSpace;
                Assert.That(defined, Is.Not.Null, "DefineActionSpace must return a DictSpace");
                Assert.That(
                    defined.Spaces.Keys,
                    Is.EquivalentTo(new[] { TranslateKey }),
                    "Duplicate ActionKeys still publish a single last-write-wins space entry"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(agentGo);
            }
        }
    }
}
