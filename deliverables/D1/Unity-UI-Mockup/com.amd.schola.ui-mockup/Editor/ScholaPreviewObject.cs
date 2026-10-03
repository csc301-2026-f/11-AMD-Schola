// SPDX-License-Identifier: MIT
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Schola.UI.Preview
{
    // Editor-only and HideAndDontSave. Never an asset, scene component or runtime contract.
    public sealed class ScholaPreviewObject : ScriptableObject
    {
        [NonSerialized] internal string page;
        [NonSerialized] internal string item;
    }

    [CustomEditor(typeof(ScholaPreviewObject))]
    internal sealed class ScholaPreviewInspector : Editor
    {
        private PreviewState state;
        public override VisualElement CreateInspectorGUI()
        {
            var data = (ScholaPreviewObject)target;
            state = new PreviewState { page = data.page ?? "Agents" };
            if (state.page == "Environments") state.environment = data.item ?? "Tag arena · multi-agent";
            else state.agent = data.item ?? "Runner";
            var root = new VisualElement(); PreviewUI.Style(root); root.AddToClassList("inspector-preview");
            root.Add(PreviewUI.Header());
            var content = new VisualElement(); content.AddToClassList("inspector-content"); root.Add(content);
            void Rebuild()
            {
                var foldouts = PreviewUI.FoldoutState(content);
                content.Clear(); PreviewUI.Render(content, state, Rebuild, null, true);
                PreviewUI.RestoreFoldouts(content, foldouts);
            }
            Rebuild(); return root;
        }
    }
}
