// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.
using UnityEngine;

namespace AMD.Schola.Samples.TrainingDemo
{
    /// <summary>Planar reach-target task with randomized episodes and a swept circular hazard.</summary>
    public sealed class DangerZoneTask : MonoBehaviour, IReward, IEpisodeStatus
    {
        [SerializeField]
        Transform m_Agent;

        [SerializeField]
        Transform m_Target;

        [SerializeField]
        Transform m_Danger;

        [SerializeField, Min(0.01f)]
        float m_DangerRadius = 0.8f;

        [SerializeField, Min(0.01f)]
        float m_ArrivalDistance = 0.3f;

        [SerializeField, Min(1)]
        int m_MaxSteps = 256;

        [SerializeField]
        int m_LayoutSeed = 7;
        System.Random m_Random;
        Vector3 m_PreviousPosition;
        float m_PreviousDistance;
        bool m_Terminated;
        bool m_Truncated;

        public Transform Agent => m_Agent;
        public Transform Target => m_Target;
        public Transform Danger => m_Danger;
        public int Episodes { get; private set; }
        public int Successes { get; private set; }
        public int Failures { get; private set; }
        public int Steps { get; private set; }
        public float EpisodeReward { get; private set; }
        public string LastResult { get; private set; } = "Waiting for a reset";
        public bool Finished => m_Terminated || m_Truncated;

        public int LayoutSeed
        {
            get => m_LayoutSeed;
            set
            {
                m_LayoutSeed = value;
                m_Random = null;
            }
        }

        public void Configure(Transform agent, Transform target, Transform danger)
        {
            m_Agent = agent;
            m_Target = target;
            m_Danger = danger;
        }

        public void OnEpisodeReset()
        {
            if (m_Random == null)
                m_Random = new System.Random(m_LayoutSeed);
            m_Agent.position = new Vector3(-2f, 0.25f, RandomLane());
            m_Target.position = new Vector3(2f, 0.25f, RandomLane());
            m_PreviousPosition = m_Agent.position;
            m_PreviousDistance = PlanarDistance(m_Agent.position, m_Target.position);
            Steps = 0;
            EpisodeReward = 0f;
            m_Terminated = false;
            m_Truncated = false;
            Episodes++;
        }

        // ScholaEnvironment notifies both interfaces. Randomize once, through IReward.
        void IEpisodeStatus.OnEpisodeReset() { }

        float RandomLane() => (float)(m_Random.NextDouble() * 3.2 - 1.6);

        public float ComputeReward()
        {
            if (Finished)
                return 0f;
            // TranslateActuator has three outputs. This task projects movement onto the arena.
            var position = m_Agent.position;
            position.y = 0.25f;
            m_Agent.position = position;
            Steps++;
            var distance = PlanarDistance(position, m_Target.position);
            var reward = m_PreviousDistance - distance - 0.005f;
            if (CrossesDanger(m_PreviousPosition, position))
            {
                reward -= 5f;
                Finish("Entered danger zone", false);
            }
            else if (Mathf.Abs(position.x) > 2.8f || Mathf.Abs(position.z) > 2.8f)
            {
                reward -= 5f;
                Finish("Left arena", false);
            }
            else if (distance <= m_ArrivalDistance)
            {
                reward += 5f;
                Finish("Target reached", true);
            }
            else if (Steps >= m_MaxSteps)
            {
                m_Truncated = true;
                Failures++;
                LastResult = "Step limit";
            }
            m_PreviousPosition = position;
            m_PreviousDistance = distance;
            EpisodeReward += reward;
            return reward;
        }

        void Finish(string result, bool success)
        {
            m_Terminated = true;
            LastResult = result;
            if (success)
                Successes++;
            else
                Failures++;
        }

        public void GetStatus(out bool terminated, out bool truncated)
        {
            terminated = m_Terminated;
            truncated = m_Truncated;
        }

        bool CrossesDanger(Vector3 start, Vector3 end)
        {
            var a = new Vector2(start.x, start.z);
            var delta = new Vector2(end.x - start.x, end.z - start.z);
            var center = new Vector2(m_Danger.position.x, m_Danger.position.z);
            var fraction =
                delta.sqrMagnitude > 0f
                    ? Mathf.Clamp01(Vector2.Dot(center - a, delta) / delta.sqrMagnitude)
                    : 0f;
            return Vector2.Distance(a + fraction * delta, center) <= m_DangerRadius;
        }

        static float PlanarDistance(Vector3 a, Vector3 b) =>
            Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        void OnGUI()
        {
            GUI.Box(
                new Rect(12, 12, 480, 100),
                $"Schola Danger Zone\nEpisodes started: {Episodes}   Successes: {Successes}   Failures: {Failures}\n"
                    + $"Step: {Steps}/{m_MaxSteps}   Reward: {EpisodeReward:F2}\nLast result: {LastResult}"
            );
        }
    }
}
