// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AMD.Schola.Training;
using NUnit.Framework;
using UnityEngine;

namespace AMD.Schola.Tests.Training
{
    public sealed class TrainingServerTests
    {
        static void SendJson(NetworkStream stream, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length));
            stream.Write(length, 0, 4);
            stream.Write(bytes, 0, bytes.Length);
        }

        static string ReceiveJson(NetworkStream stream)
        {
            var header = Read(stream, 4);
            var size = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0));
            return Encoding.UTF8.GetString(Read(stream, size));
        }

        static void SendStart(TcpClient client) =>
            SendJson(
                client.GetStream(),
                "{\"version\":" + TrainingWire.Version + ",\"command\":\"start\"}"
            );

        static string Exchange(TrainingServer server, int port, params string[] requests)
        {
            var reply = Task.Run(() =>
            {
                using (var client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, port);
                    client.ReceiveTimeout = 5000;
                    var stream = client.GetStream();
                    string last = null;
                    foreach (var request in requests)
                    {
                        SendJson(stream, request);
                        last = ReceiveJson(stream);
                    }
                    return last;
                }
            });
            var timer = Stopwatch.StartNew();
            while (!reply.IsCompleted && timer.Elapsed.TotalSeconds < 5)
            {
                server.PumpRequests();
                Thread.Sleep(1);
            }
            Assert.That(reply.IsCompleted, Is.True, "Transport did not reply");
            return reply.GetAwaiter().GetResult();
        }

        static ScholaAgent AddTranslateAgent(GameObject parent, string name, string observationKey)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            var sensor = go.AddComponent<PositionSensor>();
            sensor.ObservationKey = observationKey;
            var actuator = go.AddComponent<TranslateActuator>();
            actuator.MaxDisplacement = 1;
            var agent = go.AddComponent<ScholaAgent>();
            agent.SetSensors(new[] { sensor });
            agent.SetActuators(new[] { actuator });
            return agent;
        }

        static void SetSerializedField(object target, string fieldName, object value)
        {
            var field = target
                .GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        [Test]
        public void Registration_SnapshotsInspectorOrderAndRejectsInvalidChanges()
        {
            var root = new GameObject("TrainingRegistration");
            var first = new GameObject("First");
            var second = new GameObject("Second");
            first.transform.SetParent(root.transform);
            second.transform.SetParent(root.transform);
            var a = first.AddComponent<ScholaEnvironment>();
            var b = second.AddComponent<ScholaMultiAgentEnvironment>();
            var server = root.AddComponent<TrainingServer>();
            var assigned = new MonoBehaviour[] { a, b };
            SetSerializedField(server, "m_Environments", assigned);
            try
            {
                server.RegisterConfiguredEnvironments();
                var snapshot = server.RegisteredEnvironments;
                Assert.That(snapshot, Has.Count.EqualTo(2));
                Assert.That(snapshot[0], Is.TypeOf<SingleAgentEnvironmentAdapter>());
                Assert.That(snapshot[1], Is.SameAs(b));
                assigned[0] = b;
                Assert.Throws<InvalidOperationException>(() =>
                    server.RegisterConfiguredEnvironments()
                );
                Assert.That(server.RegisteredEnvironments, Is.SameAs(snapshot));
                assigned[0] = null;
                Assert.Throws<InvalidOperationException>(() =>
                    server.RegisterConfiguredEnvironments()
                );
                Assert.That(server.RegisteredEnvironments, Is.SameAs(snapshot));
                assigned[0] = server;
                Assert.Throws<InvalidOperationException>(() =>
                    server.RegisterConfiguredEnvironments()
                );
                assigned[0] = a;
                SetSerializedField(server, "m_Environment", a);
                Assert.Throws<InvalidOperationException>(() =>
                    server.RegisterConfiguredEnvironments()
                );
                Assert.That(server.RegisteredEnvironments, Is.SameAs(snapshot));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Registration_ProgrammaticListKeepsStableIdsAndRejectsInvalidEntries()
        {
            var root = new GameObject("TrainingRegistrationApi");
            var a = root.AddComponent<ScholaMultiAgentEnvironment>();
            var child = new GameObject("SecondEnvironment");
            child.transform.SetParent(root.transform);
            var b = child.AddComponent<ScholaMultiAgentEnvironment>();
            var server = root.AddComponent<TrainingServer>();
            var assigned = new IMultiAgentEnvironment[] { a, b };
            try
            {
                server.RegisterEnvironments(assigned);
                var snapshot = server.RegisteredEnvironments;
                assigned[0] = b;
                Assert.That(snapshot[0], Is.SameAs(a));
                Assert.That(snapshot[1], Is.SameAs(b));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<IMultiAgentEnvironment>)snapshot)[0] = b
                );
                Assert.Throws<ArgumentNullException>(() => server.RegisterEnvironments(null));
                Assert.Throws<ArgumentException>(() =>
                    server.RegisterEnvironments(Array.Empty<IMultiAgentEnvironment>())
                );
                Assert.Throws<ArgumentException>(() =>
                    server.RegisterEnvironments(new IMultiAgentEnvironment[] { a, null })
                );
                Assert.Throws<ArgumentException>(() => server.RegisterEnvironments(assigned));
                Assert.That(server.RegisteredEnvironments, Is.SameAs(snapshot));
                // Registration is available before the batch transport is implemented.
                Assert.Throws<InvalidOperationException>(() => server.StartRegisteredServer(0));
                Assert.That(server.IsRunning, Is.False);
                Assert.That(server.BoundPort, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Registration_CannotChangeWhileServerRuns_AndCanChangeAfterStop()
        {
            var go = new GameObject("TrainingRegistrationLifecycle");
            var agent = AddTranslateAgent(go, "Agent", "position");
            var environment = go.AddComponent<ScholaEnvironment>();
            environment.SetAgent(agent);
            var server = go.AddComponent<TrainingServer>();
            try
            {
                server.StartServer(environment, 0);
                var snapshot = server.RegisteredEnvironments;
                Assert.That(snapshot, Has.Count.EqualTo(1));
                Assert.Throws<InvalidOperationException>(() =>
                    server.RegisterEnvironments(snapshot)
                );
                var response = JsonUtility.FromJson<TrainingResponse>(
                    Exchange(
                        server,
                        server.BoundPort,
                        "{\"version\":" + TrainingWire.Version + ",\"command\":\"start\"}",
                        "{\"version\":" + TrainingWire.Version + ",\"command\":\"definition\"}"
                    )
                );
                Assert.That(response.ok, Is.True, response.error);
                Assert.That(response.agents[0].id, Is.EqualTo("SingleAgent"));
                Assert.That(server.RegisteredEnvironments, Is.SameAs(snapshot));
                server.StopServer();
                server.RegisterEnvironments(snapshot);
                Assert.That(server.RegisteredEnvironments[0], Is.SameAs(snapshot[0]));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Stop_WithRequestWaitingForMainThread_ReleasesConnectionAndPort()
        {
            var go = new GameObject("TrainingShutdownTest");
            var agent = go.AddComponent<ScholaAgent>();
            var environment = go.AddComponent<ScholaEnvironment>();
            environment.SetAgent(agent);
            var server = go.AddComponent<TrainingServer>();
            try
            {
                server.StartServer(environment, 0);
                var port = server.BoundPort;
                using (var client = new TcpClient())
                {
                    client.Connect(IPAddress.Loopback, port);
                    SendStart(client);
                    // Deliberately do not pump Update: shutdown must cancel waiting work.
                    var timer = Stopwatch.StartNew();
                    server.StopServer();
                    Assert.That(timer.Elapsed.TotalSeconds, Is.LessThan(2));
                    Assert.That(server.IsRunning, Is.False);
                }
                var replacement = new TcpListener(IPAddress.Loopback, port);
                replacement.Start();
                replacement.Stop();
                // A stopped listener can be restarted without leaking its worker.
                server.StartServer(environment, port);
                Assert.That(server.IsRunning, Is.True);
                server.StopServer();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void NetworkRequest_ExecutesOnlyWhenMainThreadPumps()
        {
            var go = new GameObject("TrainingPumpTest");
            var agent = AddTranslateAgent(go, "Agent", "position");
            var environment = go.AddComponent<ScholaEnvironment>();
            environment.SetAgent(agent);
            var server = go.AddComponent<TrainingServer>();
            try
            {
                server.StartServer(environment, 0);
                var response = JsonUtility.FromJson<TrainingResponse>(
                    Exchange(
                        server,
                        server.BoundPort,
                        "{\"version\":" + TrainingWire.Version + ",\"command\":\"start\"}"
                    )
                );
                Assert.That(response.ok, Is.True);
                Assert.That(response.version, Is.EqualTo(TrainingWire.Version));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void StartServer_ServesMultiAgentEnvironmentOverTheWire()
        {
            var go = new GameObject("TrainingMultiAgentDefinitionTest");
            var agentA = AddTranslateAgent(go, "A", "position");
            var agentB = AddTranslateAgent(go, "B", "position");
            var environment = go.AddComponent<ScholaMultiAgentEnvironment>();
            environment.SetAgents(
                new[]
                {
                    new KeyValuePair<string, ScholaAgent>("agent_0", agentA),
                    new KeyValuePair<string, ScholaAgent>("agent_1", agentB),
                }
            );
            var server = go.AddComponent<TrainingServer>();
            try
            {
                server.StartServer(environment, 0);
                var response = JsonUtility.FromJson<TrainingResponse>(
                    Exchange(
                        server,
                        server.BoundPort,
                        "{\"version\":" + TrainingWire.Version + ",\"command\":\"start\"}",
                        "{\"version\":" + TrainingWire.Version + ",\"command\":\"definition\"}"
                    )
                );
                Assert.That(response.ok, Is.True);
                Assert.That(response.agents, Has.Length.EqualTo(2));
                Assert.That(
                    new[] { response.agents[0].id, response.agents[1].id },
                    Is.EquivalentTo(new[] { "agent_0", "agent_1" })
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void StartServer_UsesAssignedMultiAgentFieldBeforeSingleAgentComponent()
        {
            var go = new GameObject("TrainingResolveMultiAgent");
            var singleAgent = AddTranslateAgent(go, "Single", "position");
            var multiAgent = AddTranslateAgent(go, "Multi", "position");
            var single = go.AddComponent<ScholaEnvironment>();
            single.SetAgent(singleAgent);
            var multi = go.AddComponent<ScholaMultiAgentEnvironment>();
            multi.SetAgents(
                new[] { new KeyValuePair<string, ScholaAgent>("team_agent", multiAgent) }
            );
            var server = go.AddComponent<TrainingServer>();
            SetSerializedField(server, "m_MultiAgentEnvironment", multi);
            try
            {
                server.StartServer();
                var response = JsonUtility.FromJson<TrainingResponse>(
                    Exchange(
                        server,
                        server.BoundPort,
                        "{\"version\":" + TrainingWire.Version + ",\"command\":\"start\"}",
                        "{\"version\":" + TrainingWire.Version + ",\"command\":\"definition\"}"
                    )
                );
                Assert.That(response.ok, Is.True);
                Assert.That(response.agents, Has.Length.EqualTo(1));
                Assert.That(response.agents[0].id, Is.EqualTo("team_agent"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void StartServer_RejectsBothEnvironmentFieldsAssigned()
        {
            var go = new GameObject("TrainingBothEnvironments");
            var singleAgent = go.AddComponent<ScholaAgent>();
            var multiAgent = AddTranslateAgent(go, "Multi", "position");
            var single = go.AddComponent<ScholaEnvironment>();
            single.SetAgent(singleAgent);
            var multi = go.AddComponent<ScholaMultiAgentEnvironment>();
            multi.SetAgents(
                new[] { new KeyValuePair<string, ScholaAgent>("team_agent", multiAgent) }
            );
            var server = go.AddComponent<TrainingServer>();
            SetSerializedField(server, "m_Environment", single);
            SetSerializedField(server, "m_MultiAgentEnvironment", multi);
            try
            {
                Assert.Throws<InvalidOperationException>(() => server.StartServer());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static byte[] Read(Stream stream, int count)
        {
            var data = new byte[count];
            var offset = 0;
            while (offset < count)
            {
                var n = stream.Read(data, offset, count - offset);
                if (n == 0)
                    throw new EndOfStreamException();
                offset += n;
            }
            return data;
        }
    }
}
