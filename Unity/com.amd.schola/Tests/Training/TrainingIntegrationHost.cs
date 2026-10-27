// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using AMD.Schola.Training;
using UnityEditor;
using UnityEngine;

namespace AMD.Schola.Tests.Training
{
    /// <summary>
    /// Batch-mode host for Test/unity/test_unity_live.py. Uses real US1/2/3 runtime
    /// components and Editor updates to pump the same transport used in Play Mode.
    /// </summary>
    public static class TrainingIntegrationHost
    {
        static TrainingServer s_Server;

        [InitializeOnLoadMethod]
        static void RestoreAfterReload()
        {
            // Unity can reload once more after executing a batch entry point when
            // test-assembly defines change. Restore only this explicitly requested host.
            if (
                Application.isBatchMode
                && Array.IndexOf(
                    System.Environment.GetCommandLineArgs(),
                    "AMD.Schola.Tests.Training.TrainingIntegrationHost.Run"
                ) >= 0
            )
                EditorApplication.delayCall += Run;
        }

        sealed class Episode : MonoBehaviour, IReward, IEpisodeStatus
        {
            public Transform Agent;
            int m_Steps;

            public void OnEpisodeReset() => m_Steps = 0;

            public float ComputeReward()
            {
                m_Steps++;
                return Agent.position.x;
            }

            public void GetStatus(out bool terminated, out bool truncated)
            {
                terminated = Agent.position.x >= 0.2f;
                truncated = !terminated && m_Steps >= 4;
            }
        }

        public static void Run()
        {
            if (s_Server != null && s_Server.IsRunning)
                return;
            var previous = GameObject.Find("US4 integration environment");
            if (previous != null)
                UnityEngine.Object.DestroyImmediate(previous);
            var root = new GameObject("US4 integration environment");
            var agentObject = new GameObject("Agent");
            agentObject.transform.SetParent(root.transform);
            var sensor = agentObject.AddComponent<PositionSensor>();
            sensor.ObservationKey = "position";
            var actuator = agentObject.AddComponent<TranslateActuator>();
            actuator.MaxDisplacement = 0.1f;
            var agent = agentObject.AddComponent<ScholaAgent>();
            agent.SetSensors(new[] { sensor });
            agent.SetActuators(new[] { actuator });
            var episode = root.AddComponent<Episode>();
            episode.Agent = agentObject.transform;
            var environment = root.AddComponent<ScholaEnvironment>();
            environment.SetAgent(agent);
            environment.SetReward(episode);
            environment.SetEpisodeStatus(episode);
            var server = root.AddComponent<TrainingServer>();
            var port = int.Parse(
                System.Environment.GetEnvironmentVariable("SCHOLA_TEST_UNITY_PORT") ?? "8001"
            );
            server.StartServer(environment, port);
            s_Server = server;
            EditorApplication.update += server.PumpRequests;
            // The test process owns and terminates this dedicated batch Editor.
            Debug.Log($"SCHOLA_US4_READY:{server.BoundPort}");
        }
    }
}
