// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using AMD.Schola.Samples.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace AMD.Schola.Samples.Tests
{
    public sealed class SetupDemoTests
    {
        Scene m_Scene;
        SetupDemoController m_Controller;

        [SetUp]
        public void SetUp()
        {
            m_Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            m_Controller = SetupDemoSceneBuilder.Populate(m_Scene);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.CloseScene(m_Scene, true);
        }

        [Test]
        public void Steps_ReadRealObservations_ReachTargetWithoutOvershoot_AndFollowMovedTarget()
        {
            Assert.That(m_Controller.ValidateConfiguration(), Is.True);
            var agent = m_Controller.GetComponent<ScholaAgent>();
            var target = agent.Sensors[1];
            Assert.That(
                ((DictSpace)agent.Define()).Spaces.Keys,
                Is.EquivalentTo(
                    new[]
                    {
                        SetupDemoController.AgentPositionKey,
                        SetupDemoController.TargetPositionKey,
                    }
                )
            );

            m_Controller.Step(0.5f);
            Assert.That(agent.transform.position.x, Is.EqualTo(-2f).Within(1e-5f));
            var observed = (BoxPoint)
                m_Controller.LastObservation.Points[SetupDemoController.AgentPositionKey];
            Assert.That(observed.Values[0], Is.EqualTo(-2f).Within(1e-5f));
            m_Controller.Step(100f);
            Assert.That(m_Controller.HasArrived, Is.True);
            Assert.That(agent.transform.position, Is.EqualTo(target.transform.position));

            target.transform.position = new Vector3(0f, 0.5f, 2f);
            m_Controller.Step(0.5f);
            Assert.That(m_Controller.HasArrived, Is.False);
            m_Controller.Step(100f);
            Assert.That(agent.transform.position, Is.EqualTo(target.transform.position));
        }

        [Test]
        public void DuplicateObservationKeys_ReportConfigurationError()
        {
            var agent = m_Controller.GetComponent<ScholaAgent>();
            agent.Sensors[1].ObservationKey = SetupDemoController.AgentPositionKey;
            LogAssert.Expect(
                LogType.Error,
                "Schola Setup Demo: Sensors must be [cube agent_position, target target_position]. See the setup guide."
            );
            Assert.That(m_Controller.ValidateConfiguration(), Is.False);
        }

        [Test]
        public void SavedScene_ReloadsComponentReferencesAndSensorKeys()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/ScholaSetupDemoTest.unity");
            try
            {
                Assert.That(EditorSceneManager.SaveScene(m_Scene, path), Is.True);
                EditorSceneManager.CloseScene(m_Scene, true);
                m_Scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                SetupDemoController loaded = null;
                foreach (var root in m_Scene.GetRootGameObjects())
                {
                    if (root.TryGetComponent<SetupDemoController>(out var controller))
                    {
                        loaded = controller;
                    }
                }

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.ValidateConfiguration(), Is.True);
                loaded.Step(0.5f);
                Assert.That(loaded.LastObservation.Points.Count, Is.EqualTo(2));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
