// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

using UnityEditor;
using UnityEngine;

namespace AMD.Schola.Editor
{
    [CustomEditor(typeof(ScholaAgent)), CanEditMultipleObjects]
    public sealed class ScholaAgentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Unity's default serialized Inspector preserves Undo, prefab overrides,
            // multi-object editing, and the existing component reference fields.
            DrawDefaultInspector();
            EditorGUILayout.HelpBox(
                "Assign MonoBehaviour components implementing ISensor or IActuator. Keys must be nonblank and unique within each list. Empty or destroyed references are allowed and skipped.",
                MessageType.Info
            );

            foreach (var selected in targets)
            {
                var agent = (ScholaAgent)selected;
                if (!agent.TryValidateConfiguration(out var error))
                    EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }

        // Key properties can be edited on another component/Inspector.
        public override bool RequiresConstantRepaint() => true;
    }
}
