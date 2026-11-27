// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;
using AMD.Schola.Training;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AMD.Schola.Samples.TrainingDemo.Editor
{
    public static class TrainingDemoSceneBuilder
    {
        [MenuItem("Tools/Schola/Create Training Demo Scene")]
        public static void CreateScene()
        {
            Create(false);
        }

        [MenuItem("Tools/Schola/Create Inference Demo Scene")]
        public static void CreateInferenceScene() => Create(true);

        static void Create(bool inference)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before creating a Schola scene.");
                return;
            }
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Schola Scene",
                inference ? "ScholaInferenceDemo" : "ScholaTrainingDemo",
                "unity",
                "Save under Assets."
            );
            if (
                string.IsNullOrEmpty(path)
                || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()
            )
                return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Populate(inference);
            EditorSceneManager.SaveScene(scene, path);
        }

        public static ScholaAgent Populate(
            bool inference,
            Unity.InferenceEngine.ModelAsset model = null
        )
        {
            var root = new GameObject("Schola Training Environment");
            var environment = root.AddComponent<ScholaEnvironment>();
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Agent";
            cube.GetComponent<Renderer>().sharedMaterial = Material(
                "Agent",
                new Color(0.1f, 0.45f, 0.9f)
            );
            cube.transform.SetParent(root.transform);
            cube.transform.position = new Vector3(-1, 0.5f, 0);
            // This example uses Transform displacement, not physics simulation.
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.name = "Target";
            target.GetComponent<Renderer>().sharedMaterial = Material(
                "Target",
                new Color(1f, 0.5f, 0.05f)
            );
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.gray * 0.6f;
            target.transform.SetParent(root.transform);
            target.transform.position = new Vector3(1, 0.5f, 0);
            target.transform.localScale = Vector3.one * 0.3f;
            Object.DestroyImmediate(target.GetComponent<Collider>());
            var position = cube.AddComponent<PositionSensor>();
            position.ObservationKey = "agent_position";
            var goal = target.AddComponent<PositionSensor>();
            goal.ObservationKey = "target_position";
            var actuator = cube.AddComponent<TranslateActuator>();
            actuator.MaxDisplacement = 0.1f;
            var agent = cube.AddComponent<ScholaAgent>();
            agent.SetSensors(new[] { position, goal });
            agent.SetActuators(new[] { actuator });
            environment.SetAgent(agent);
            root.AddComponent<TrainingDemoTask>().Configure(cube.transform, target.transform);
            if (inference)
            {
                var runner = root.AddComponent<PolicyRunner>();
                runner.SetAgents(new[] { agent });
                if (model == null)
                {
                    model = AssetDatabase.LoadAssetAtPath<Unity.InferenceEngine.ModelAsset>(
                        SamplePath() + "/Models/reach_target.onnx"
                    );
                }
                runner.Model = model;
                root.AddComponent<InferenceDemoStatus>()
                    .Configure(agent.transform, target.transform, runner);
            }
            else
                root.AddComponent<TrainingServer>();
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.22f);
            camera.transform.position = new Vector3(0, 4, -7);
            camera.transform.LookAt(new Vector3(0, 0.5f, 0));
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            return agent;
        }

        /// <summary>
        /// Two cubes, each reaching its own target, under one
        /// <see cref="ScholaMultiAgentEnvironment"/>. Both agents have the same spaces and
        /// finish on the same step, so <c>schola unity train</c> trains them as one policy.
        /// </summary>
        [MenuItem("Tools/Schola/Create Multi-Agent Training Demo Scene")]
        public static void CreateMultiAgentScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Schola Multi-Agent Training Environment");
            var environment = root.AddComponent<ScholaMultiAgentEnvironment>();
            var lanes = new[] { -1.5f, 1.5f };
            var agents = new List<KeyValuePair<string, ScholaAgent>>();
            var targets = new Transform[lanes.Length];
            var team = new MultiAgentTrainingDemoTask[lanes.Length];
            for (var i = 0; i < lanes.Length; i++)
            {
                var agent = CreateReachingAgent(
                    root.transform,
                    $"Agent {i}",
                    $"Target {i}",
                    lanes[i],
                    out targets[i]
                );
                // On the agent's GameObject, where the environment looks for its hooks.
                team[i] = agent.gameObject.AddComponent<MultiAgentTrainingDemoTask>();
                agents.Add(new KeyValuePair<string, ScholaAgent>($"agent_{i}", agent));
            }
            for (var i = 0; i < team.Length; i++)
                team[i].Configure(agents[i].Value.transform, targets[i], team);
            environment.SetAgents(agents);
            // With no environment assigned, the server uses the one on its GameObject.
            root.AddComponent<TrainingServer>();
            AddCameraAndLight(new Vector3(0, 6, -8));
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log(
                "Schola Multi-Agent Training Demo ready. Save the scene, enter Play Mode, then run schola unity train."
            );
        }

        /// <summary>
        /// A cube with a bounded translate actuator that observes its own position and
        /// its target's position. The cube starts at x = -1 and the target sits at x = 1.
        /// </summary>
        static ScholaAgent CreateReachingAgent(
            Transform root,
            string agentName,
            string targetName,
            float z,
            out Transform target
        )
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = agentName;
            cube.transform.SetParent(root);
            cube.transform.position = new Vector3(-1, 0.5f, z);
            // This example uses Transform displacement, not physics simulation.
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = targetName;
            sphere.transform.SetParent(root);
            sphere.transform.position = new Vector3(1, 0.5f, z);
            sphere.transform.localScale = Vector3.one * 0.3f;
            Object.DestroyImmediate(sphere.GetComponent<Collider>());
            var position = cube.AddComponent<PositionSensor>();
            position.ObservationKey = "agent_position";
            var goal = sphere.AddComponent<PositionSensor>();
            goal.ObservationKey = "target_position";
            var actuator = cube.AddComponent<TranslateActuator>();
            actuator.MaxDisplacement = 0.1f;
            var agent = cube.AddComponent<ScholaAgent>();
            agent.SetSensors(new[] { position, goal });
            agent.SetActuators(new[] { actuator });
            target = sphere.transform;
            return agent;
        }

        static void AddCameraAndLight(Vector3 cameraPosition)
        {
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = cameraPosition;
            camera.transform.LookAt(new Vector3(0, 0.5f, 0));
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
        }

        static string SamplePath()
        {
            var script = AssetDatabase.FindAssets("TrainingDemoSceneBuilder t:MonoScript")[0];
            return System
                .IO.Path.GetDirectoryName(
                    System.IO.Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(script))
                )
                .Replace('\\', '/');
        }

        static Material Material(string name, Color color)
        {
            var sample = SamplePath();
            var folder = sample + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(sample, "Materials");
            var path = folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { color = color };
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
    }
}
