// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace AMD.Schola.Training
{
    /// <summary>Loopback-only transport spike. Definitions and gameplay steps are deliberately unimplemented.</summary>
    public sealed class GymStartupServer
    {
        readonly object m_Gate = new object();
        Server m_Server;
        int m_StartupCount;
        int m_LastResetMode = -1;

        public int StartupCount => Volatile.Read(ref m_StartupCount);
        public int LastResetMode => Volatile.Read(ref m_LastResetMode);
        public int Port { get; private set; }
        public bool IsRunning { get { lock (m_Gate) { return m_Server != null; } } }

        /// <summary>Starts on IPv4 loopback. Port zero selects an available port for tests.</summary>
        public void Start(int port = 50051)
        {
            if (port < 0 || port > 65535) { throw new ArgumentOutOfRangeException(nameof(port)); }
            lock (m_Gate)
            {
                if (m_Server != null) { throw new InvalidOperationException("Server is already running."); }
                var server = new Server();
                try
                {
                    server.Services.Add(global::Schola.GymService.BindService(new StartupService(this)));
                    server.Ports.Add(new ServerPort("127.0.0.1", port, ServerCredentials.Insecure));
                    server.Start();
                    foreach (var boundPort in server.Ports) { Port = boundPort.BoundPort; }
                    m_Server = server;
                }
                catch
                {
                    server.KillAsync().GetAwaiter().GetResult();
                    throw;
                }
            }
        }

        /// <summary>Cancels pending RPCs and releases the listener before returning.</summary>
        public void Stop()
        {
            lock (m_Gate)
            {
                if (m_Server == null) { return; }
                // Handlers never wait for Unity's main thread, so joining cannot deadlock on scene work.
                m_Server.KillAsync().GetAwaiter().GetResult();
                m_Server = null;
                Port = 0;
            }
        }

        sealed class StartupService : global::Schola.GymService.GymServiceBase
        {
            readonly GymStartupServer m_Owner;
            public StartupService(GymStartupServer owner) { m_Owner = owner; }

            public override Task<global::Schola.GymConnectorStartResponse> StartGymConnector(
                global::Schola.GymConnectorStartRequest request, ServerCallContext context)
            {
                var mode = (int)request.AutoresetType;
                if (mode < 0 || mode > 2)
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Unknown autoreset_type."));
                }
                Volatile.Write(ref m_Owner.m_LastResetMode, mode);
                Interlocked.Increment(ref m_Owner.m_StartupCount);
                return Task.FromResult(new global::Schola.GymConnectorStartResponse());
            }

            public override Task<global::Schola.State> UpdateState(
                global::Schola.StateUpdate request, ServerCallContext context)
            {
                if (request.UpdateCase == global::Schola.StateUpdate.UpdateOneofCase.None &&
                    ((int)request.Status == 1 || (int)request.Status == 2))
                {
                    return Task.FromResult(new global::Schola.State());
                }
                throw new RpcException(new Status(StatusCode.Unimplemented,
                    "Startup experiment only: environment steps and resets are not implemented."));
            }
        }
    }
}
