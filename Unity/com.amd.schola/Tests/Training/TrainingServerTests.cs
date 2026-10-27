// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
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
        static void SendStart(TcpClient client)
        {
            var bytes = Encoding.UTF8.GetBytes("{\"version\":1,\"command\":\"start\"}");
            var length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length));
            client.GetStream().Write(length, 0, 4);
            client.GetStream().Write(bytes, 0, bytes.Length);
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
            var agent = go.AddComponent<ScholaAgent>();
            var sensor = go.AddComponent<PositionSensor>();
            sensor.ObservationKey = "position";
            agent.SetSensors(new[] { sensor });
            var actuator = go.AddComponent<TranslateActuator>();
            actuator.MaxDisplacement = 1;
            agent.SetActuators(new[] { actuator });
            var environment = go.AddComponent<ScholaEnvironment>();
            environment.SetAgent(agent);
            var server = go.AddComponent<TrainingServer>();
            try
            {
                server.StartServer(environment, 0);
                var port = server.BoundPort;
                var reply = Task.Run(() =>
                {
                    using (var client = new TcpClient())
                    {
                        client.Connect(IPAddress.Loopback, port);
                        client.ReceiveTimeout = 5000;
                        SendStart(client);
                        var stream = client.GetStream();
                        var header = Read(stream, 4);
                        var size = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0));
                        return Encoding.UTF8.GetString(Read(stream, size));
                    }
                });
                var timer = Stopwatch.StartNew();
                while (!reply.IsCompleted && timer.Elapsed.TotalSeconds < 5)
                {
                    server.PumpRequests();
                    Thread.Sleep(1);
                }
                Assert.That(reply.IsCompleted, Is.True, "Transport did not reply");
                Assert.That(
                    JsonUtility.FromJson<TrainingResponse>(reply.GetAwaiter().GetResult()).ok,
                    Is.True
                );
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
