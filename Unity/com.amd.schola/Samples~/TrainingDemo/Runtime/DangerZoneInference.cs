// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
using Unity.InferenceEngine;
using UnityEngine;

namespace AMD.Schola.Samples.TrainingDemo
{
    /// <summary>Runs exported actions through the same environment steps and resets as training.</summary>
    public sealed class DangerZoneInference : MonoBehaviour
    {
        [SerializeField]
        ScholaEnvironment m_Environment;

        [SerializeField]
        ScholaAgent m_Agent;

        [SerializeField]
        ModelAsset m_Model;

        [SerializeField, Min(0.01f)]
        float m_StepInterval = 0.05f;
        SentisPolicy m_Policy;
        float m_NextStep;
        bool m_ResetPending;
        public ModelAsset Model
        {
            get => m_Model;
            set => m_Model = value;
        }

        public void Configure(ScholaEnvironment environment, ScholaAgent agent, ModelAsset model)
        {
            m_Environment = environment;
            m_Agent = agent;
            m_Model = model;
        }

        void Start()
        {
            if (m_Environment == null || m_Agent == null || m_Model == null)
            {
                Debug.LogError(
                    "Danger Zone: assign Environment, Agent and your exported danger-zone ONNX Model to Danger Zone Inference."
                );
                enabled = false;
                return;
            }
            var definition = m_Environment.Initialize();
            m_Policy = new SentisPolicy(m_Model, BackendType.CPU);
            if (!m_Policy.Init(definition))
            {
                m_Policy.Dispose();
                m_Policy = null;
                enabled = false;
                return;
            }
            m_Environment.Reset();
        }

        void Update()
        {
            if (m_Policy == null || Time.time < m_NextStep)
                return;
            if (m_ResetPending)
            {
                m_Environment.Reset();
                m_ResetPending = false;
            }
            if (!m_Policy.Think(m_Agent.Observe(), out var action))
            {
                enabled = false;
                return;
            }
            var state = m_Environment.Step(action);
            m_ResetPending = state.Terminated || state.Truncated;
            m_NextStep = Time.time + (m_ResetPending ? 1f : m_StepInterval);
        }

        void OnDisable()
        {
            m_Policy?.Dispose();
            m_Policy = null;
        }
    }
}
