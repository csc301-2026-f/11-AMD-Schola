// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
using System.IO;
using AMD.Schola.Training;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AMD.Schola.Samples.TrainingDemo.Editor
{
    public static class DangerZoneSceneBuilder
    {
        [MenuItem("Tools/Schola/Create Danger Zone Training Scene")]
        public static void CreateTraining() => Create(false);

        [MenuItem("Tools/Schola/Create Danger Zone Inference Scene")]
        public static void CreateInference() => Create(true);

        static void Create(bool inference)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Danger Zone Scene",
                inference ? "ScholaDangerZoneInference" : "ScholaDangerZoneTraining",
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

        public static string SamplePath()
        {
            var script = AssetDatabase.FindAssets("DangerZoneSceneBuilder t:MonoScript")[0];
            return Path.GetDirectoryName(
                    Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(script))
                )
                .Replace('\\', '/');
        }

        /// <summary>Build into an empty active scene. Used by menu commands and sample tests.</summary>
        public static ScholaEnvironment Populate(bool inference)
        {
            var root = new GameObject("Schola Danger Zone Environment");
            var environment = root.AddComponent<ScholaEnvironment>();
            var agentObject = Primitive(
                "Agent",
                PrimitiveType.Cube,
                root.transform,
                new Vector3(-2f, 0.25f, -1f),
                Vector3.one * 0.4f,
                "DangerZoneAgent",
                new Color(0.1f, 0.45f, 0.9f)
            );
            var target = Primitive(
                "Target",
                PrimitiveType.Sphere,
                root.transform,
                new Vector3(2f, 0.25f, 1f),
                Vector3.one * 0.5f,
                "DangerZoneTarget",
                new Color(1f, 0.55f, 0.05f)
            );
            var danger = Primitive(
                "Danger Zone",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(1.6f, 0.02f, 1.6f),
                "DangerZoneHazard",
                new Color(0.85f, 0.05f, 0.08f)
            );
            Primitive(
                "Arena",
                PrimitiveType.Cube,
                root.transform,
                new Vector3(0f, -0.08f, 0f),
                new Vector3(5.6f, 0.1f, 5.6f),
                "DangerZoneGround",
                new Color(0.18f, 0.22f, 0.28f)
            );
            var position = agentObject.AddComponent<PositionSensor>();
            position.ObservationKey = "agent_position";
            var goal = target.AddComponent<PositionSensor>();
            goal.ObservationKey = "target_position";
            var hazard = danger.AddComponent<PositionSensor>();
            hazard.ObservationKey = "danger_position";
            var actuator = agentObject.AddComponent<TranslateActuator>();
            actuator.MaxDisplacement = 0.1f;
            var agent = agentObject.AddComponent<ScholaAgent>();
            agent.SetSensors(new[] { position, goal, hazard });
            agent.SetActuators(new[] { actuator });
            environment.SetAgent(agent);
            root.AddComponent<DangerZoneTask>()
                .Configure(agent.transform, target.transform, danger.transform);
            if (inference)
            {
                var model = AssetDatabase.LoadAssetAtPath<ModelAsset>(
                    SamplePath() + "/Models/danger_zone.onnx"
                );
                root.AddComponent<DangerZoneInference>().Configure(environment, agent, model);
            }
            else
            {
                var server = root.AddComponent<TrainingServer>();
                server.ScriptSettings.BuiltinSettings.Timesteps = 100000;
                server.ScriptSettings.BuiltinSettings.CheckpointPath = "ckpt/danger_zone";
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.gray * 0.6f;
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 8, -5);
            camera.transform.LookAt(Vector3.zero);
            camera.orthographic = true;
            camera.orthographicSize = 3.7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            Selection.activeGameObject = root;
            return environment;
        }

        static GameObject Primitive(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            string materialName,
            Color color
        )
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            var folder = SamplePath() + "/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(SamplePath(), "Materials");
            var path = folder + "/" + materialName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { color = color };
                AssetDatabase.CreateAsset(material, path);
            }
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        public static void GenerateScenes()
        {
            var folder = SamplePath() + "/Scenes";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(SamplePath(), "Scenes");
            foreach (var inference in new[] { false, true })
            {
                var scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single
                );
                Populate(inference);
                EditorSceneManager.SaveScene(
                    scene,
                    folder
                        + (
                            inference
                                ? "/ScholaDangerZoneInference.unity"
                                : "/ScholaDangerZoneTraining.unity"
                        )
                );
            }
            AssetDatabase.SaveAssets();
            Debug.Log("SCHOLA_DANGER_ZONE_SCENES_SAVED");
        }
    }
}
