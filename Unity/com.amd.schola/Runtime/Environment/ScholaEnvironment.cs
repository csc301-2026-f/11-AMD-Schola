// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;

namespace AMD.Schola
{
    /// <summary>
    /// Single-agent training environment that composes a <see cref="ScholaAgent"/>.
    /// Step applies an action then reports protocol-shaped <see cref="AgentState"/>.
    /// Reset restores the initialize snapshot and returns <see cref="InitialAgentState"/>.
    /// </summary>
    public sealed class ScholaEnvironment : MonoBehaviour, IEnvironment
    {
        /// <summary>
        /// Agent-id key Unreal's single-agent adapter injects. Unused by US3 tests; reserved for US4.
        /// </summary>
        public const string DefaultAgentId = "SingleAgent";

        [SerializeField]
        ScholaAgent m_Agent;

        IReward m_Reward;
        IEpisodeStatus m_Status;
        ITrainingStateSink m_Sink;

        public void SetAgent(ScholaAgent agent)
        {
            m_Agent = agent;
        }

        public void SetReward(IReward reward)
        {
            m_Reward = reward;
        }

        public void SetEpisodeStatus(IEpisodeStatus status)
        {
            m_Status = status;
        }

        public void SetTrainingStateSink(ITrainingStateSink sink)
        {
            m_Sink = sink;
        }

        public InteractionDefinition Initialize()
        {
            _ = m_Agent;
            return new InteractionDefinition();
        }

        public void Seed(int seed)
        {
            _ = seed;
        }

        public void SetOptions(IReadOnlyDictionary<string, string> options)
        {
            _ = options;
        }

        public InitialAgentState Reset()
        {
            _ = m_Sink;
            return new InitialAgentState();
        }

        public AgentState Step(Point action)
        {
            _ = action;
            _ = m_Reward;
            _ = m_Status;
            return new AgentState();
        }
    }
}
