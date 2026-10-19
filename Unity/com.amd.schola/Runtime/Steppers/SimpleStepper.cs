// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;

namespace AMD.Schola {
    /// <summary>
    /// Synchronous stepper. Each <see cref="Step"/> runs the whole observe, think, act cycle
    /// on the calling thread, so the caller waits while the policy runs. Suitable for small models.
    /// </summary>
    public sealed class SimpleStepper : IStepper {
        IReadOnlyList<IAgent> m_Agents;
        IPolicy m_Policy;

        public bool Init(IReadOnlyList<IAgent> agents, IPolicy policy) {
            m_Agents = null;
            m_Policy = null;

            if (agents == null || agents.Count == 0) {
                Debug.LogError($"{nameof(SimpleStepper)}.Init(): No agents provided.");
                return false;
            }

            for (var i = 0; i < agents.Count; i++) {
                if (agents[i] == null) {
                    Debug.LogError($"{nameof(SimpleStepper)}.Init(): Agent {i} is null.");
                    return false;
                }
            }

            if (policy == null) {
                Debug.LogError($"{nameof(SimpleStepper)}.Init(): No policy provided.");
                return false;
            }

            m_Agents = agents;
            m_Policy = policy;
            return true;
        }

        public bool Step() {
            if (m_Agents == null || m_Policy == null) {
                Debug.LogError($"{nameof(SimpleStepper)}.Step(): Init must succeed before Step can run.");
                return false;
            }

            // 1. Every agent looks at the world before anyone acts, so they all see the same state.
            var observations = new List<Point>(m_Agents.Count);
            foreach (var agent in m_Agents) {
                observations.Add(agent.Observe());
            }

            // 2. The policy decides an action for each observation. If any decision fails,
            //    nobody acts this step.
            var actions = new List<Point>(m_Agents.Count);
            for (var i = 0; i < m_Agents.Count; i++) {
                if (!m_Policy.Think(observations[i], out var action)) {
                    Debug.LogError($"{nameof(SimpleStepper)}.Step(): Policy failed to think for agent {i}.");
                    return false;
                }
                actions.Add(action);
            }

            // 3. Hand each agent the action that was chosen for it.
            for (var i = 0; i < m_Agents.Count; i++) {
                m_Agents[i].Act(actions[i]);
            }

            return true;
        }
    }
}