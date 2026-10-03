// SPDX-License-Identifier: MIT
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Schola.UI.Preview
{
    public sealed class ScholaWorkspace : EditorWindow
    {
        [NonSerialized] private PreviewState state;
        [NonSerialized] private VisualElement details;
        [NonSerialized] private ScrollView scroll;
        [NonSerialized] private ScholaPreviewObject selected;
        internal PreviewState State => state ?? (state = new PreviewState());

        [MenuItem("Window/AMD Schola/UI Mockup")]
        public static void Open()
        {
            var window = GetWindow<ScholaWorkspace>();
            window.titleContent = new GUIContent("Schola UI Mockup", EditorGUIUtility.IconContent("Settings").image);
            window.minSize = new Vector2(620, 420);
            window.Show();
        }
        private void OnEnable()
        {
            state = new PreviewState();
            AssemblyReloadEvents.beforeAssemblyReload += ReleasePreview;
            EditorApplication.quitting += ReleasePreview;
        }
        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= ReleasePreview;
            EditorApplication.quitting -= ReleasePreview;
            ReleasePreview();
        }
        private void ReleasePreview()
        {
            if (selected == null) return;
            if (Selection.activeObject == selected) Selection.activeObject = null;
            DestroyImmediate(selected); selected = null;
        }
        public void CreateGUI()
        {
            var root = rootVisualElement; root.Clear(); PreviewUI.Style(root);
            root.Add(PreviewUI.Header());
            var body = new VisualElement(); body.AddToClassList("workspace-body"); root.Add(body);
            var nav = new VisualElement(); nav.AddToClassList("navigation"); body.Add(nav);
            PreviewUI.Text(nav, "WORKSPACE", "nav-caption");
            for (int i = 0; i < PreviewUI.Pages.Length; i++)
            {
                var page = PreviewUI.Pages[i];
                var button = new Button(() => Navigate(page)) { name = "nav-" + page };
                button.AddToClassList("nav-button");
                var icon = new Image { image = EditorGUIUtility.IconContent(PreviewUI.Icons[i]).image }; icon.AddToClassList("nav-icon"); button.Add(icon);
                button.Add(new Label(page)); nav.Add(button);
            }
            var spacer = new VisualElement(); spacer.style.flexGrow = 1; nav.Add(spacer);
            PreviewUI.Text(nav, "Schola 2.1\nUnity UI mockup", "nav-footer");
            scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("details-scroll");
            details = new VisualElement(); details.AddToClassList("details"); scroll.Add(details); body.Add(scroll);
            var footer = new VisualElement(); footer.AddToClassList("statusbar");
            PreviewUI.Text(footer, "UI mockup · No backend integration", "status");
            root.Add(footer); Rebuild();
        }
        internal void Navigate(string page)
        {
            State.page = page; Rebuild(); if (scroll != null) scroll.scrollOffset = Vector2.zero;
        }
        internal void Rebuild()
        {
            if (details == null) return;
            var foldouts = PreviewUI.FoldoutState(details);
            details.Clear();
            foreach (var page in PreviewUI.Pages) rootVisualElement.Q<Button>("nav-" + page)?.EnableInClassList("selected", State.page == page);
            try { PreviewUI.Render(details, State, Rebuild, Inspect); PreviewUI.RestoreFoldouts(details, foldouts); }
            catch (Exception e) { details.Add(new HelpBox(e.Message, HelpBoxMessageType.Error)); Debug.LogException(e); }
        }
        private void Inspect(string page, string item)
        {
            ReleasePreview();
            selected = CreateInstance<ScholaPreviewObject>();
            selected.hideFlags = HideFlags.HideAndDontSave;
            selected.name = item + " · Schola Preview";
            selected.page = page; selected.item = item;
            Selection.activeObject = selected;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }
    }
}
