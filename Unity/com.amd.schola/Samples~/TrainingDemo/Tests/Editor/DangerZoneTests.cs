// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
using AMD.Schola.Samples.TrainingDemo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AMD.Schola.Samples.TrainingDemo.Tests
{
    public sealed class DangerZoneTests
    {
        ScholaEnvironment m_Environment;
        DangerZoneTask m_Task;

        [SetUp]
        public void Setup()
        {
            // UnityTest resumes setup after the Play Mode domain reload.
            if (Application.isPlaying)
                return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            m_Environment = DangerZoneSceneBuilder.Populate(false);
            m_Task = m_Environment.GetComponent<DangerZoneTask>();
            m_Environment.Initialize();
            m_Environment.Reset();
        }

        [UnityEngine.TestTools.UnityTearDown]
        public System.Collections.IEnumerator Cleanup()
        {
            if (Application.isPlaying)
                yield return new UnityEngine.TestTools.ExitPlayMode();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static Point Action(float x, float y, float z)
        {
            var value = new BoxPoint();
            value.Values.AddRange(new[] { x, y, z });
            var point = new DictPoint();
            point.Points["translate"] = value;
            return point;
        }

        [Test]
        public void Reset_RandomizesOncePerEpisodeAndPublishesMatchingObservations()
        {
            Assert.That(m_Task.Episodes, Is.EqualTo(1));
            var previous = m_Task.Target.position;
            var initial = m_Environment.Reset();
            Assert.That(m_Task.Episodes, Is.EqualTo(2));
            Assert.That(m_Task.Target.position, Is.Not.EqualTo(previous));
            Assert.That(m_Task.Agent.position.x, Is.EqualTo(-2f));
            Assert.That(m_Task.Target.position.x, Is.EqualTo(2f));
            Assert.That(((DictPoint)initial.Observations).Points.Count, Is.EqualTo(3));
        }

        [Test]
        public void SweptHazard_PreventsJumpingAcrossAndTerminatesWithPenalty()
        {
            m_Task.Agent.position = new Vector3(-0.9f, 0.25f, 0f);
            m_Task.ComputeReward(); // Establish a safe previous position.
            m_Task.Agent.position = new Vector3(0.9f, 0.25f, 0f);
            var state = m_Environment.Step(Action(0, 0, 0));
            Assert.That(state.Terminated, Is.True);
            Assert.That(m_Task.LastResult, Is.EqualTo("Entered danger zone"));
            Assert.That(state.Reward, Is.LessThan(0));
            Assert.That(m_Task.Failures, Is.EqualTo(1));
        }

        [Test]
        public void TargetSuccess_ProjectsHeightAndResetClearsTerminalState()
        {
            m_Task.Agent.position = new Vector3(-2f, 0.25f, 2.3f);
            m_Task.ComputeReward();
            m_Task.Agent.position = new Vector3(2f, 0.25f, 2.3f);
            m_Task.ComputeReward();
            m_Task.Agent.position = m_Task.Target.position + Vector3.back * 0.35f;
            m_Task.ComputeReward();
            var state = m_Environment.Step(Action(0, 0.1f, 0.1f));
            Assert.That(state.Terminated, Is.True);
            Assert.That(state.Reward, Is.GreaterThan(4f));
            Assert.That(m_Task.Agent.position.y, Is.EqualTo(0.25f));
            Assert.That(m_Task.Successes, Is.EqualTo(1));
            m_Environment.Reset();
            Assert.That(m_Task.Finished, Is.False);
            Assert.That(m_Task.Steps, Is.Zero);
        }

        [Test]
        public void StepLimit_TruncatesAndArenaExitTerminates()
        {
            AgentState state = null;
            for (var i = 0; i < 256; i++)
                state = m_Environment.Step(Action(0, 0, 0));
            Assert.That(state.Truncated, Is.True);
            Assert.That(state.Terminated, Is.False);
            m_Environment.Reset();
            m_Task.Agent.position = new Vector3(-2.85f, 0.25f, 0);
            state = m_Environment.Step(Action(0, 0, 0));
            Assert.That(state.Terminated, Is.True);
            Assert.That(m_Task.LastResult, Is.EqualTo("Left arena"));
        }

        [Test]
        public void ExportedPolicy_SucceedsOnAtLeastEightyOfOneHundredUnseenLayouts()
        {
            var model = UnityEditor.AssetDatabase.LoadAssetAtPath<Unity.InferenceEngine.ModelAsset>(
                DangerZoneSceneBuilder.SamplePath() + "/Models/danger_zone.onnx"
            );
            Assert.That(model, Is.Not.Null);
            m_Task.LayoutSeed = 123;
            using (var policy = new SentisPolicy(model))
            {
                Assert.That(policy.Init(m_Environment.Initialize()), Is.True);
                for (var episode = 0; episode < 100; episode++)
                {
                    m_Environment.Reset();
                    for (var step = 0; step < 256 && !m_Task.Finished; step++)
                    {
                        var agent = m_Task.Agent.GetComponent<ScholaAgent>();
                        Assert.That(policy.Think(agent.Observe(), out var action), Is.True);
                        m_Environment.Step(action);
                    }
                }
            }
            Debug.Log(
                $"DANGER_ZONE_EVALUATION: successes={m_Task.Successes}/100 failures={m_Task.Failures}"
            );
            Assert.That(m_Task.Successes, Is.GreaterThanOrEqualTo(80));
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator ShippedInference_RunsAnEpisodeWithoutTrainingServer()
        {
            EditorSceneManager.OpenScene(
                DangerZoneSceneBuilder.SamplePath() + "/Scenes/ScholaDangerZoneInference.unity"
            );
            var settings = new UnityEditor.SerializedObject(
                Object.FindFirstObjectByType<DangerZoneInference>()
            );
            settings.FindProperty("m_StepInterval").floatValue = 0.001f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            yield return new UnityEngine.TestTools.EnterPlayMode();
            var task = Object.FindFirstObjectByType<DangerZoneTask>();
            for (var frame = 0; frame < 600 && task.Successes + task.Failures == 0; frame++)
                yield return null;
            Assert.That(task.Successes + task.Failures, Is.GreaterThan(0));
            Assert.That(
                Object.FindFirstObjectByType<AMD.Schola.Training.TrainingServer>(),
                Is.Null
            );
            Assert.That(task.Agent.position.y, Is.EqualTo(0.25f));
            yield return new UnityEngine.TestTools.ExitPlayMode();
        }
    }
}
