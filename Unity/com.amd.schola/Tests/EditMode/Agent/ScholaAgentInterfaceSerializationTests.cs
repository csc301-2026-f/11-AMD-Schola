// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AMD.Schola.Tests.Agent
{
    public sealed class ScholaAgentInterfaceSerializationTests
    {
        GameObject m_Root;
        string m_AssetFolder;

        [SetUp]
        public void SetUp()
        {
            var folderName = "ScholaInterfaceBindingTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folderName);
            m_AssetFolder = "Assets/" + folderName;
            m_Root = new GameObject("SerializedInterfaceAgent");
            var agent = m_Root.AddComponent<ScholaAgent>();
            var position = m_Root.AddComponent<PositionSensor>();
            var translate = m_Root.AddComponent<TranslateActuator>();
            var customObject = new GameObject("CustomBindings");
            customObject.transform.SetParent(m_Root.transform);
            var sensor = customObject.AddComponent<InterfaceTestSensor>();
            var actuator = customObject.AddComponent<InterfaceTestActuator>();
            Assert.That(sensor, Is.Not.Null, "The test sensor must be an attachable component.");
            Assert.That(
                actuator,
                Is.Not.Null,
                "The test actuator must be an attachable component."
            );
            sensor.ObservationKey = "saved_category";
            actuator.ActionKey = "saved_rotation";
            agent.SetSensors(new ISensor[] { position, null, sensor });
            agent.SetActuators(new IActuator[] { translate, null, actuator });
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Root != null)
                Object.DestroyImmediate(m_Root);
            if (m_AssetFolder != null)
                AssetDatabase.DeleteAsset(m_AssetFolder);
        }

        [Test]
        public void SceneSaveAndReload_PreservesMixedComponentReferencesAndNullSlots()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scenePath = m_AssetFolder + "/InterfaceBindings.unity";
            // Opening a saved scene additively preserves any existing Untitled editor scene.
            Assert.That(
                AssetDatabase.CopyAsset(
                    "Packages/com.amd.schola/Tests/EditMode/Agent/EmptyInterfaceTestScene.unity",
                    scenePath
                ),
                Is.True
            );
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.MoveGameObjectToScene(m_Root, scene);
                Assert.That(EditorSceneManager.SaveScene(scene, scenePath), Is.True);
                Assert.That(EditorSceneManager.CloseScene(scene, true), Is.True);
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                var roots = scene.GetRootGameObjects();
                Assert.That(roots, Has.Length.EqualTo(1));
                m_Root = roots[0];
                AssertRestoredBindings(m_Root);
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid() && originalScene.isLoaded)
                    SceneManager.SetActiveScene(originalScene);
            }
        }

        [Test]
        public void PrefabSaveAndReload_PreservesMixedComponentReferencesAndNullSlots()
        {
            var path = m_AssetFolder + "/InterfaceBindings.prefab";
            Assert.That(PrefabUtility.SaveAsPrefabAsset(m_Root, path), Is.Not.Null);
            Object.DestroyImmediate(m_Root);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null);
            m_Root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            AssertRestoredBindings(m_Root);
        }

        static void AssertRestoredBindings(GameObject root)
        {
            var agent = root.GetComponent<ScholaAgent>();
            var sensor = root.GetComponentInChildren<InterfaceTestSensor>();
            var actuator = root.GetComponentInChildren<InterfaceTestActuator>();
            Assert.That(agent.Sensors, Has.Count.EqualTo(3));
            Assert.That(agent.Actuators, Has.Count.EqualTo(3));
            Assert.That(agent.Sensors[0], Is.SameAs(root.GetComponent<PositionSensor>()));
            Assert.That(agent.Actuators[0], Is.SameAs(root.GetComponent<TranslateActuator>()));
            Assert.That(agent.Sensors[1], Is.Null);
            Assert.That(agent.Actuators[1], Is.Null);
            Assert.That(agent.Sensors[2], Is.SameAs(sensor));
            Assert.That(agent.Actuators[2], Is.SameAs(actuator));
            Assert.That(
                ((DictSpace)agent.Define()).Spaces.Keys,
                Is.EquivalentTo(new[] { "position", "saved_category" })
            );
            Assert.That(
                ((DictSpace)agent.DefineActionSpace()).Spaces.Keys,
                Is.EquivalentTo(new[] { "translate", "saved_rotation" })
            );

            var rotation = new DiscretePoint { Value = 2 };
            agent.Act(
                new DictPoint
                {
                    Points =
                    {
                        ["translate"] = new BoxPoint { Values = { 0f, 0f, 0f } },
                        ["saved_rotation"] = rotation,
                    },
                }
            );
            Assert.That(actuator.LastAction, Is.SameAs(rotation));
            Assert.That(
                ((DictPoint)agent.Observe()).Points["saved_category"],
                Is.TypeOf<DiscretePoint>()
            );
        }
    }
}
