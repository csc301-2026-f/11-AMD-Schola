// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using UnityEngine;

namespace AMD.Schola.Tests.Agent
{
    /// <summary>Independent actuator with a discrete action that changes its object's rotation.</summary>
    public sealed class InterfaceTestActuator : MonoBehaviour, IActuator
    {
        [SerializeField]
        string m_ActionKey = "rotate";

        public string ActionKey
        {
            get => m_ActionKey;
            set => m_ActionKey = value;
        }

        public int InitializationCount { get; private set; }
        public int ActionCount { get; private set; }
        public Point LastAction { get; private set; }
        public int ChoiceCount = 4;

        public void InitActuator()
        {
            InitializationCount++;
        }

        public Space GetActionSpace()
        {
            return new DiscreteSpace(ChoiceCount);
        }

        public void TakeAction(Point action)
        {
            LastAction = action;
            ActionCount++;
            transform.localRotation = Quaternion.Euler(0f, ((DiscretePoint)action).Value * 90f, 0f);
        }
    }
}
