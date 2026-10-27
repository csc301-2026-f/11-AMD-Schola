// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using UnityEngine;

namespace AMD.Schola.Samples.TrainingDemo
{
    /// <summary>Reward and episode hooks for a position-controlled cube reaching a target.</summary>
    public sealed class TrainingDemoTask : MonoBehaviour, IReward, IEpisodeStatus
    {
        [SerializeField]
        Transform m_Agent;

        [SerializeField]
        Transform m_Target;

        [SerializeField]
        int m_MaxSteps = 128;

        [SerializeField]
        float m_ArrivalDistance = 0.2f;
        int m_Steps;
        float m_PreviousDistance;

        public void Configure(Transform agent, Transform target)
        {
            m_Agent = agent;
            m_Target = target;
        }

        public void OnEpisodeReset()
        {
            m_Steps = 0;
            m_PreviousDistance = Vector3.Distance(m_Agent.position, m_Target.position);
        }

        public float ComputeReward()
        {
            m_Steps++;
            var distance = Vector3.Distance(m_Agent.position, m_Target.position);
            var reward = m_PreviousDistance - distance - 0.01f;
            if (distance <= m_ArrivalDistance)
                reward += 1f;
            m_PreviousDistance = distance;
            return reward;
        }

        public void GetStatus(out bool terminated, out bool truncated)
        {
            terminated = Vector3.Distance(m_Agent.position, m_Target.position) <= m_ArrivalDistance;
            truncated = !terminated && m_Steps >= m_MaxSteps;
        }
    }
}
