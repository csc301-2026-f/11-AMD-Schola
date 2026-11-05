// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace AMD.Schola.Tests.Policies
{
    /// <summary>
    /// Tests the PolicyRunner boilerplate.
    /// </summary>
    public sealed class PolicyRunnerTests
    {
        const string ReachTargetPath =
            "Packages/com.amd.schola/Tests/EditMode/Policies/TestModels/reach_target.onnx";
        const string CartPolePath =
            "Packages/com.amd.schola/Tests/EditMode/Policies/TestModels/cartpole.onnx";

        GameObject m_AgentObject;
        GameObject m_TargetObject;
        GameObject m_RunnerObject;
        ScholaAgent m_Agent;
        PolicyRunner m_Runner;

        [SetUp]
        public void SetUp()
        {
            m_AgentObject = new GameObject("Agent");
            m_TargetObject = new GameObject("Target");

            var agentSensor = m_AgentObject.AddComponent<PositionSensor>();
            agentSensor.ObservationKey = "agent_position";
            var targetSensor = m_TargetObject.AddComponent<PositionSensor>();
            targetSensor.ObservationKey = "target_position";

            var actuator = m_AgentObject.AddComponent<TranslateActuator>();
            actuator.MaxDisplacement = 0.1f;

            m_Agent = m_AgentObject.AddComponent<ScholaAgent>();
            m_Agent.SetSensors(new[] { agentSensor, targetSensor });
            m_Agent.SetActuators(new[] { actuator });

            m_RunnerObject = new GameObject("Policy Runner");
            m_Runner = m_RunnerObject.AddComponent<PolicyRunner>();
            m_Runner.SetAgents(new[] { m_Agent });
        }

        [TearDown]
        public void TearDown()
        {
            // OnDestroy does not run for components destroyed in Edit Mode, so free the model here.
            m_Runner.StopRunning();
            Object.DestroyImmediate(m_RunnerObject);
            Object.DestroyImmediate(m_TargetObject);
            Object.DestroyImmediate(m_AgentObject);
        }

        [Test]
        public void AddComponent_AlsoAddsStepperRunner()
        {
            Assert.That(m_RunnerObject.GetComponent<StepperRunner>(), Is.Not.Null);
        }

        [Test]
        public void StartRunning_WithMatchingModel_IsRunning()
        {
            m_Runner.Model = LoadModel(ReachTargetPath);

            Assert.That(m_Runner.StartRunning(), Is.True);
            Assert.That(m_Runner.IsRunning, Is.True);
        }

        [Test]
        public void StopRunning_StopsTheRunner()
        {
            m_Runner.Model = LoadModel(ReachTargetPath);
            Assert.That(m_Runner.StartRunning(), Is.True);

            m_Runner.StopRunning();

            Assert.That(m_Runner.IsRunning, Is.False);
        }

        [Test]
        public void StartRunning_ModelForDifferentAgent_FailsWithClearError()
        {
            m_Runner.Model = LoadModel(CartPolePath);

            LogAssert.Expect(LogType.Error, new Regex("Model has no input named"));
            Assert.That(m_Runner.StartRunning(), Is.False);
            Assert.That(m_Runner.IsRunning, Is.False);
        }

        [Test]
        public void StartRunning_NoModel_Fails()
        {
            LogAssert.Expect(LogType.Error, new Regex("No model assigned"));
            Assert.That(m_Runner.StartRunning(), Is.False);
        }

        [Test]
        public void StartRunning_NoAgents_Fails()
        {
            m_Runner.Model = LoadModel(ReachTargetPath);
            m_Runner.SetAgents(new ScholaAgent[0]);

            LogAssert.Expect(LogType.Error, new Regex("No agents assigned"));
            Assert.That(m_Runner.StartRunning(), Is.False);
        }

        // StepperRunner.Update does not run in Edit Mode, so this drives the same
        // stepper/policy/agent loop directly. Without action clamping, the first step would
        // throw because TranslateActuator rejects the model's raw output (about +/-2).
        [Test]
        public void TrainedModel_MovesAgentToTarget_WithinActuatorBounds()
        {
            m_AgentObject.transform.position = new Vector3(2f, 0f, 0f);
            m_TargetObject.transform.position = Vector3.zero;

            var policy = new SentisPolicy(LoadModel(ReachTargetPath));
            try
            {
                var definition = new InteractionDefinition(
                    m_Agent.Define(),
                    m_Agent.DefineActionSpace()
                );
                Assert.That(policy.Init(definition), Is.True);

                var stepper = new SimpleStepper();
                Assert.That(stepper.Init(new List<IAgent> { m_Agent }, policy), Is.True);

                for (var i = 0; i < 60; i++)
                {
                    Assert.That(stepper.Step(), Is.True, $"Step {i} failed");
                }
            }
            finally
            {
                policy.Dispose();
            }

            Assert.That(
                Vector3.Distance(m_AgentObject.transform.position, Vector3.zero),
                Is.LessThan(0.5f)
            );
        }

        static ModelAsset LoadModel(string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<ModelAsset>(path);
            Assert.That(model, Is.Not.Null, $"Test model missing at {path}");
            return model;
        }
    }
}
