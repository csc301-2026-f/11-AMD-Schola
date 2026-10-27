// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
using System;
using UnityEngine;

namespace AMD.Schola.Training
{
    /// <summary>Attach to an empty GameObject to test the Windows Editor gRPC listener.</summary>
    [AddComponentMenu("AMD Schola/Training/Gym Startup Probe")]
    public sealed class GymStartupProbe : MonoBehaviour
    {
        [SerializeField, Range(1024, 65535)]
        int m_Port = 50051;
        GymStartupServer m_Server;
        int m_LoggedStartupCount;

        void OnEnable()
        {
            if (!Application.isPlaying) { return; }
            m_Server = new GymStartupServer();
            try
            {
                m_Server.Start(m_Port);
                Debug.Log($"Schola startup probe listening on 127.0.0.1:{m_Server.Port}.", this);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                m_Server.Stop();
                m_Server = null;
                enabled = false;
            }
        }

        void Update()
        {
            // gRPC handlers only update thread-safe counters; Unity APIs stay on this thread.
            if (m_Server != null && m_Server.StartupCount != m_LoggedStartupCount)
            {
                m_LoggedStartupCount = m_Server.StartupCount;
                Debug.Log($"Schola startup acknowledged: count={m_LoggedStartupCount}, resetMode={m_Server.LastResetMode}.", this);
            }
        }

        void OnDisable()
        {
            if (m_Server == null) { return; }
            m_Server.Stop();
            m_Server = null;
            m_LoggedStartupCount = 0;
        }

        void OnApplicationQuit() { OnDisable(); }
        void OnDestroy() { OnDisable(); }
    }
}
