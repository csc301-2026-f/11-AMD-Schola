// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.IO;
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

        [Test]
        public void LegacyConcreteListScene_LoadsBindingsWithoutResaving()
        {
            Object.DestroyImmediate(m_Root);
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(
                CopyLegacyFixture("unity"),
                OpenSceneMode.Additive
            );
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots, Has.Length.EqualTo(1));
                m_Root = roots[0];
                AssertLegacyBindings(m_Root);
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
        public void LegacyConcreteListPrefab_LoadsBindingsWithoutResaving()
        {
            Object.DestroyImmediate(m_Root);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CopyLegacyFixture("prefab"));
            Assert.That(prefab, Is.Not.Null);
            m_Root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            AssertLegacyBindings(m_Root);
        }

        string CopyLegacyFixture(string extension)
        {
            var fileName = "LegacyConcreteBindings." + extension;
            var source = "Packages/com.amd.schola/Tests/EditMode/Agent/LegacyFixtures/" + fileName;
            // Resolve the physical package location; Packages/ can be a virtual UPM path.
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(source);
            Assert.That(package, Is.Not.Null);
            var sourcePath = Path.Combine(
                package.resolvedPath,
                "Tests/EditMode/Agent/LegacyFixtures/" + fileName
            );
            var destination = m_AssetFolder + "/" + fileName;
            // Copy saved legacy bytes, never create/resave these fixtures with the new agent.
            File.Copy(sourcePath, destination);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            return destination;
        }

        static void AssertLegacyBindings(GameObject root)
        {
            var agent = root.GetComponent<ScholaAgent>();
            var child = root.transform.Find("ChildBindings");
            Assert.That(agent, Is.Not.Null);
            Assert.That(child, Is.Not.Null);
            Assert.That(agent.Sensors, Has.Count.EqualTo(3));
            Assert.That(agent.Actuators, Has.Count.EqualTo(3));
            Assert.That(agent.Sensors[0], Is.SameAs(root.GetComponent<PositionSensor>()));
            Assert.That(agent.Actuators[0], Is.SameAs(root.GetComponent<TranslateActuator>()));
            Assert.That(agent.Sensors[1], Is.Null);
            Assert.That(agent.Actuators[1], Is.Null);
            Assert.That(agent.Sensors[2], Is.SameAs(child.GetComponent<PositionSensor>()));
            Assert.That(agent.Actuators[2], Is.SameAs(child.GetComponent<TranslateActuator>()));
            Assert.That(agent.TryValidateConfiguration(out var error), Is.True, error);
            Assert.That(
                ((DictSpace)agent.Define()).Spaces.Keys,
                Is.EquivalentTo(new[] { "legacy_position", "child_position" })
            );
            Assert.That(
                ((DictSpace)agent.DefineActionSpace()).Spaces.Keys,
                Is.EquivalentTo(new[] { "legacy_translate", "child_translate" })
            );
            Assert.That(root.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(child.localPosition, Is.EqualTo(new Vector3(4f, 5f, 6f)));
            var before = (DictPoint)agent.Observe();
            Assert.That(
                ((BoxPoint)before.Points["legacy_position"]).Values,
                Is.EqualTo(new[] { 1f, 2f, 3f })
            );
            Assert.That(
                ((BoxPoint)before.Points["child_position"]).Values,
                Is.EqualTo(new[] { 5f, 7f, 9f })
            );
            agent.Act(
                new DictPoint
                {
                    Points =
                    {
                        ["legacy_translate"] = new BoxPoint { Values = { 0.5f, 1f, -1f } },
                        ["child_translate"] = new BoxPoint { Values = { -2f, 0f, 3f } },
                    },
                }
            );
            var after = (DictPoint)agent.Observe();
            Assert.That(
                ((BoxPoint)after.Points["legacy_position"]).Values,
                Is.EqualTo(new[] { 1.5f, 3f, 2f })
            );
            Assert.That(
                ((BoxPoint)after.Points["child_position"]).Values,
                Is.EqualTo(new[] { 3.5f, 8f, 11f })
            );
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
