// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using System.Collections.Generic;
using Unity.InferenceEngine;
using UnityEngine;

namespace AMD.Schola
{
    /// <summary>
    /// Ready-made boilerplate for running a trained ONNX policy on one or more agents without
    /// Python. Assign the agents and the model in the Inspector and enter Play Mode.
    /// </summary>
    [RequireComponent(typeof(StepperRunner))]
    public sealed class PolicyRunner : MonoBehaviour
    {
        [SerializeField]
        List<ScholaAgent> m_Agents = new List<ScholaAgent>();

        [SerializeField]
        ModelAsset m_Model;

        [SerializeField]
        BackendType m_Backend = BackendType.CPU;

        SentisPolicy m_Policy;

        public IReadOnlyList<ScholaAgent> Agents => m_Agents;

        public ModelAsset Model
        {
            get { return m_Model; }
            set { m_Model = value; }
        }

        public BackendType Backend
        {
            get { return m_Backend; }
            set { m_Backend = value; }
        }

        public bool IsRunning
        {
            get
            {
                var runner = GetComponent<StepperRunner>();
                return m_Policy != null && runner != null && runner.IsRunning;
            }
        }

        public void SetAgents(IEnumerable<ScholaAgent> agents)
        {
            m_Agents = new List<ScholaAgent>(agents);
        }

        void Start()
        {
            StartRunning();
        }

        void OnDestroy()
        {
            StopRunning();
        }

        /// <summary>
        /// Loads the model and starts stepping the agents. Called automatically on Start.
        /// </summary>
        /// <returns>False, with the reason logged, if the agents or the model cannot be used.</returns>
        public bool StartRunning()
        {
            StopRunning();

            var agents = new List<IAgent>();
            foreach (var agent in m_Agents)
            {
                if (agent != null)
                {
                    agents.Add(agent);
                }
            }

            if (agents.Count == 0)
            {
                Debug.LogError($"{nameof(PolicyRunner)} on '{name}': No agents assigned.", this);
                return false;
            }

            if (m_Model == null)
            {
                Debug.LogError($"{nameof(PolicyRunner)} on '{name}': No model assigned.", this);
                return false;
            }

            var first = (ScholaAgent)agents[0];
            var definition = new InteractionDefinition(first.Define(), first.DefineActionSpace());

            var policy = new SentisPolicy(m_Model, m_Backend);
            if (!policy.Init(definition))
            {
                // SentisPolicy already logged why the model does not fit the agent.
                policy.Dispose();
                return false;
            }

            if (!GetComponent<StepperRunner>().Setup(new SimpleStepper(), agents, policy))
            {
                policy.Dispose();
                return false;
            }

            m_Policy = policy;
            return true;
        }

        /// <summary>
        /// Stops stepping and frees the model.
        /// </summary>
        public void StopRunning()
        {
            var runner = GetComponent<StepperRunner>();
            if (runner != null)
            {
                runner.StopRunning();
            }

            if (m_Policy != null)
            {
                m_Policy.Dispose();
                m_Policy = null;
            }
        }
    }
}
