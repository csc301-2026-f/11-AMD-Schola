// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Schola.UI.Preview
{
    internal static class PreviewUI
    {
        internal static readonly string[] Pages = { "Training", "Environments", "Agents", "Sensors", "Actuators", "Spaces", "Inference" };
        internal static readonly string[] Icons = { "Settings", "SceneAsset Icon", "GameObject Icon", "Camera Icon", "MoveTool", "Grid.BoxTool", "TextAsset Icon" };
        internal static readonly string[] Descriptions = {
            "Configure how your Unity environments would train with Schola.",
            "Organize environments, agent membership and episode resets.",
            "Define what an agent observes, how it acts and which policy it uses.",
            "Describe observations collected from the Unity scene.",
            "Map policy actions to movement, rotation and position.",
            "Inspect the observation and action space definitions.",
            "Bring a trained policy into the observation–action loop."
        };

        internal static void Style(VisualElement root)
        {
            var firstSetup = !root.ClassListContains("schola");
            root.AddToClassList("schola");
            root.EnableInClassList("light", !EditorGUIUtility.isProSkin);
            if (firstSetup)
            {
                root.schedule.Execute(() => root.EnableInClassList("light", !EditorGUIUtility.isProSkin)).Every(500);
                root.RegisterCallback<GeometryChangedEvent>(e => root.EnableInClassList("narrow", e.newRect.width < 520));
            }
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PreviewCatalog.PackagePath + "/Editor/Schola.uss");
            if (sheet != null && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
        }
        internal static Label Text(VisualElement parent, string text, string css = null)
        {
            var label = new Label(text); if (css != null) label.AddToClassList(css); parent.Add(label); return label;
        }
        internal static VisualElement Header()
        {
            var header = new VisualElement(); header.AddToClassList("brand-header");
            Text(header, "AMD", "amd"); Text(header, "SCHOLA", "wordmark");
            var spacer = new VisualElement(); spacer.style.flexGrow = 1; header.Add(spacer);
            Text(header, "UNITY", "platform"); Text(header, "UI MOCKUP", "badge"); return header;
        }
        internal static Foldout Group(VisualElement parent, string title, bool open = true)
        {
            var f = new Foldout { text = title, value = open }; f.AddToClassList("section"); parent.Add(f); return f;
        }
        internal static void Note(VisualElement parent, string text) => Text(parent, text, "note");
        internal static Dictionary<string, bool> FoldoutState(VisualElement root)
        {
            var result = new Dictionary<string, bool>();
            foreach (var foldout in root.Query<Foldout>().ToList()) result[FoldoutPath(foldout)] = foldout.value;
            return result;
        }
        internal static void RestoreFoldouts(VisualElement root, Dictionary<string, bool> state)
        {
            foreach (var foldout in root.Query<Foldout>().ToList())
                if (state.TryGetValue(FoldoutPath(foldout), out var value)) foldout.SetValueWithoutNotify(value);
        }
        private static string FoldoutPath(Foldout foldout)
        {
            var path = foldout.text;
            for (var parent = foldout.parent; parent != null; parent = parent.parent)
                if (parent is Foldout ancestor) path = ancestor.text + "/" + path;
            return path;
        }
        internal static void Static(VisualElement parent, string label, string value, string tip = "Presentation example; no configuration is saved.")
        {
            var input = new TextField(label) { value = value, isReadOnly = true, tooltip = tip };
            input.AddToClassList("property"); input.AddToClassList("readonly"); parent.Add(input);
        }
        internal static void Selector(VisualElement parent, string label, string value, IEnumerable<string> options, Action<string> change)
        {
            var input = new PopupField<string>(label, options.ToList(), value);
            input.AddToClassList("property"); input.AddToClassList("selector");
            input.tooltip = "Preview selector — changes only the panel being shown.";
            input.RegisterValueChangedCallback(e => change(e.newValue)); parent.Add(input);
        }
        internal static void ActionButton(VisualElement parent, string label, string tip)
        {
            var button = new Button { text = label, tooltip = tip }; button.SetEnabled(false); button.AddToClassList("operation"); parent.Add(button);
        }
        private static float Number(string v) => float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : 0;

        internal static void Field(VisualElement parent, CatalogField f, string source)
        {
            var tip = f.tooltip + "\n\n" + source + ":" + f.line;
            if (!string.IsNullOrEmpty(f.condition)) tip += "\nAvailable when: " + ObjectNames.NicifyVariableName(f.condition);
            VisualElement input;
            switch (f.kind)
            {
                case "bool":
                    var toggle = new Toggle(f.label) { value = f.value == "true" }; toggle.SetEnabled(false); input = toggle; break;
                case "int":
                    input = new IntegerField(f.label) { value = (int)Number(f.value), isReadOnly = true }; break;
                case "float":
                    input = new FloatField(f.label) { value = Number(f.value), isReadOnly = true }; break;
                case "enum":
                    var choices = f.options?.Length > 0 ? f.options.ToList() : new List<string> { f.value };
                    if (!choices.Contains(f.value)) choices.Insert(0, f.value);
                    var popup = new PopupField<string>(f.label, choices, f.value); popup.SetEnabled(false); input = popup; break;
                case "vector":
                    var vector = new Vector3Field(f.label) { value = Vector3.zero }; vector.SetEnabled(false); input = vector; break;
                case "color":
                    var color = new ColorField(f.label) { value = f.value == "Green" ? Color.green : Color.red }; color.SetEnabled(false); input = color; break;
                case "bounds":
                    var bounds = new Vector2Field(f.label) { value = new Vector2(-180, 180) }; bounds.SetEnabled(false); input = bounds; break;
                case "array":
                    var values = string.IsNullOrWhiteSpace(f.value) ? Array.Empty<string>() : f.value.Split(',');
                    var array = Group(parent, f.label + "   ·   " + values.Length + " elements", values.Length > 0);
                    array.tooltip = tip;
                    if (values.Length == 0) Note(array, "Empty");
                    for (var i = 0; i < values.Length; i++) Static(array, "Element " + i, values[i].Trim(), tip);
                    return;
                default:
                    input = new TextField(f.label) { value = f.value, isReadOnly = true }; break;
            }
            input.name = f.key; input.tooltip = tip; input.AddToClassList("property"); input.AddToClassList("readonly"); parent.Add(input);
        }

        internal static void Section(VisualElement parent, string id, params string[] skip)
        {
            var section = PreviewCatalog.Load().Find(id);
            Foldout conditional = null;
            foreach (var f in section.fields)
            {
                if (skip.Contains(f.key)) continue;
                if (f.kind == "section")
                {
                    var nested = Group(parent, f.label, false); Section(nested, f.value); continue;
                }
                var target = parent;
                if (!string.IsNullOrEmpty(f.condition))
                {
                    if (conditional == null)
                    {
                        conditional = Group(parent, "Conditional fields", false);
                        Note(conditional, "Options available when their parent setting is enabled. Hover a field for its condition.");
                    }
                    target = conditional;
                }
                Field(target, f, section.source);
            }
        }

        internal static void Render(VisualElement parent, PreviewState state, Action rebuild, Action<string, string> inspect = null, bool inspector = false)
        {
            var index = Array.IndexOf(Pages, state.page);
            Text(parent, state.page, "page-title");
            Text(parent, Descriptions[Math.Max(0, index)], "description");
            if (!inspector) Text(parent, "Browse the proposed interface. Configuration fields are read-only.", "preview-note");
            switch (state.page)
            {
                case "Training": Training(parent, state, rebuild); break;
                case "Environments": Environments(parent, state, rebuild, inspect); break;
                case "Agents": Agents(parent, state, rebuild, inspect); break;
                case "Sensors": Sensors(parent, state, rebuild); break;
                case "Actuators": Actuators(parent, state, rebuild); break;
                case "Spaces": Spaces(parent, state, rebuild); break;
                case "Inference": Inference(parent, state, rebuild); break;
            }
        }

        private static void Training(VisualElement parent, PreviewState s, Action rebuild)
        {
            var connector = Group(parent, "Connector", false);
            Selector(connector, "Connector preview", s.connector, new[] { "gRPC", "Manual" }, v => { s.connector = v; rebuild(); });
            if (s.connector == "gRPC")
            {
                Section(connector, "FRPCServerSettings");
                var advanced = Group(connector, "Connection options", false);
                Section(advanced, "FExternalGymConnectorSettings");
                Section(advanced, "UAbstractGymConnector", "Status", "bFirstStep");
            }
            else
            {
                Section(connector, "UManualGymConnector");
                Note(connector, "Manual stepping is represented here as a Unity-side workflow.");
            }
            var scripts = Group(parent, "Script Settings");
            Section(scripts, "URPCGymConnector", "ServerSettings", "ScriptSettings");
            Selector(scripts, "Script preview", s.script, new[] { "Python", "Other" }, v => { s.script = v; rebuild(); });
            if (s.script == "Python")
            {
                Selector(scripts, "Python environment", s.python, new[] { "System PATH", "Conda", "Custom Executable" }, v => { s.python = v; rebuild(); });
                if (s.python == "Conda") Static(scripts, "Conda Env Name", "");
                if (s.python == "Custom Executable") Static(scripts, "Custom Python Path", "");
                Selector(scripts, "Training script", s.framework, new[] { "RLlib", "Stable Baselines 3", "Custom" }, v => { s.framework = v; s.algorithm = "PPO"; rebuild(); });
            }
            if (s.script == "Other" || s.framework == "Custom")
            {
                Section(Group(parent, "Custom Script Settings"), "FCustomTrainingSettings");
            }
            else
            {
                var rllib = s.framework == "RLlib";
                var settings = Group(parent, rllib ? "Builtin RLlib Settings" : "Builtin SB3 Settings");
                var prefix = rllib ? "FRLlib" : "FSB3";
                // Scalars first, matching the Unreal details-panel hierarchy.
                Section(settings, prefix + "TrainingSettings", "LoggingSettings", "CheckpointSettings", "ResumeSettings", "NetworkArchitectureSettings", "ResourceSettings", "Algorithm", "PPOSettings", "SACSettings", "APPOSettings", "IMPALASettings");
                var algorithms = Group(settings, "Algorithm Settings", false);
                Selector(algorithms, "Algorithm preview", s.algorithm, rllib ? new[] { "PPO", "APPO", "IMPALA", "SAC" } : new[] { "PPO", "SAC" }, v => { s.algorithm = v; rebuild(); });
                Section(algorithms, prefix + s.algorithm + "Settings");
                Section(Group(settings, "Logging Settings", false), prefix + "LoggingSettings");
                Section(Group(settings, "Checkpoint Settings", false), prefix + "CheckpointSettings");
                Section(Group(settings, "Resume Settings", false), prefix + "ResumeSettings");
                Section(Group(settings, "Network Architecture Settings", false), prefix + "NetworkArchSettings");
                if (rllib) Section(Group(settings, "Resource Settings", false), "FRLlibResourceSettings");
            }
            ActionButton(parent, "Start Training", "Training is not connected in this UI preview.");
        }
        private static void InspectButton(VisualElement parent, string page, string item, Action<string, string> inspect)
        {
            if (inspect == null) return;
            var b = new Button(() => inspect(page, item)) { text = "Show in Inspector", tooltip = "Select a temporary preview object. No scene or asset is created." };
            b.AddToClassList("inspect-button"); parent.Add(b);
        }
        private static void Environments(VisualElement parent, PreviewState s, Action rebuild, Action<string, string> inspect)
        {
            Selector(parent, "Example environment", s.environment, new[] { "Tag arena · multi-agent", "Navigation · single-agent" }, v => { s.environment = v; rebuild(); });
            var multi = s.environment.StartsWith("Tag");
            var env = Group(parent, "Environment");
            Static(env, "GameObject", multi ? "Schola / Tag Arena" : "Schola / Navigation");
            Static(env, "Environment Type", multi ? "Multi Agent" : "Single Agent");
            Static(env, "Environment ID", multi ? "tag_arena_0" : "navigation_0");
            var members = Group(parent, multi ? "Agents   ·   2 members" : "Agents   ·   1 member");
            Static(members, "Runner", "Navigation policy · Raycast + Movement");
            if (multi) Static(members, "Tagger", "Pursuit policy · Raycast + Movement");
            var reset = Group(parent, "Episode Reset");
            Static(reset, "Auto Reset", "Same Step", "SameStep source reset mode.");
            Static(reset, "Use Seed", "False"); Static(reset, "Seed", "0");
            Note(Group(reset, "Environment options   ·   0 entries", false), "No custom options.");
            InspectButton(parent, "Environments", s.environment, inspect);
        }
        private static void Agents(VisualElement parent, PreviewState s, Action rebuild, Action<string, string> inspect)
        {
            Selector(parent, "Example agent", s.agent, new[] { "Runner", "Tagger" }, v => { s.agent = v; rebuild(); });
            var agent = Group(parent, "Agent Definition");
            Static(agent, "GameObject", "Tag Arena / " + s.agent);
            Static(agent, "Agent ID", s.agent.ToLowerInvariant() + "_0");
            Static(agent, "Agent Type", s.agent == "Runner" ? "navigation" : "pursuit");
            Static(agent, "Policy", s.agent == "Runner" ? "Navigation policy" : "Pursuit policy");
            Static(agent, "Status", "Preview");
            var observation = Group(parent, "Observation Definition");
            Static(observation, "Space", "Box · float32 [4]");
            Static(observation, "Bounds", "0 … 1");
            Note(observation, "Two rays × (hit flag + normalized distance); no tracked tags.");
            var action = Group(parent, "Action Definition");
            Static(action, "Space", "Box · float32 [3]"); Static(action, "Bounds", "0 … 1");
            Static(action, "Axes", "X right · Y up · Z forward");
            var sensors = Group(parent, "Sensors   ·   1 component"); Static(sensors, "Element 0", "Raycast sensor");
            var actuators = Group(parent, "Actuators   ·   1 component"); Static(actuators, "Element 0", "Movement Input actuator");
            InspectButton(parent, "Agents", s.agent, inspect);
        }
        private static void Sensors(VisualElement parent, PreviewState s, Action rebuild)
        {
            Selector(parent, "Sensor preview", s.sensor, new[] { "Raycast", "Camera", "Fake Camera" }, v => { s.sensor = v; rebuild(); });
            var id = s.sensor == "Raycast" ? "URayCastSensor" : s.sensor == "Camera" ? "UCameraSensor" : "UFakeCameraSensor";
            Section(Group(parent, s.sensor + " Sensor"), id);
            if (s.sensor == "Raycast")
            {
                Note(parent, "Spatial values use Unity axes and meters. Two rays over a 90° arc.");
                parent.Add(new RayDiagram());
            }
            else if (s.sensor == "Camera")
            {
                var capture = Group(parent, "Unity Capture Preview");
                Static(capture, "Camera", "None (Camera)"); Static(capture, "Render Texture", "None (RenderTexture)");
                Static(capture, "Capture Source", "Color · proposed Unity mapping");
                Note(capture, "Camera and render texture are presentation fields. No image is captured.");
            }
            else Note(parent, "A source-defined test sensor. The custom image array starts empty.");
        }
        private static void Actuators(VisualElement parent, PreviewState s, Action rebuild)
        {
            Selector(parent, "Actuator preview", s.actuator, new[] { "Movement Input", "Rotation", "Teleport" }, v => { s.actuator = v; rebuild(); });
            var id = s.actuator == "Movement Input" ? "UMovementInputActuator" : s.actuator == "Rotation" ? "URotationActuator" : "UTeleportActuator";
            Section(Group(parent, s.actuator + " Actuator"), id);
            Note(parent, "Unity axes: X right, Y up, Z forward. Distances are in meters; angles are in degrees.");
            if (s.actuator != "Movement Input") Note(parent, "Teleport Physics and Sweep retain Unreal terminology for review; their Unity implementation is not defined.");
        }
        private static void Spaces(VisualElement parent, PreviewState s, Action rebuild)
        {
            Selector(parent, "Space preview", s.space, new[] { "Box", "Discrete", "MultiDiscrete", "MultiBinary", "Dict", "Text" }, v => { s.space = v; rebuild(); });
            Section(Group(parent, s.space + " Space"), "F" + s.space + "Space");
            if (s.space == "Box") Section(Group(parent, "Box Dimension Defaults", false), "FBoxSpaceDimension");
            Note(parent, "These are source defaults. Populated agent examples are available under Agents.");
        }
        private static void Inference(VisualElement parent, PreviewState s, Action rebuild)
        {
            var policy = Group(parent, "Policy");
            Section(policy, "UNNEPolicy", "PolicyDefinition", "RuntimeName", "bNetworkLoaded", "ActionBuffer", "ObservationBuffer", "StateBuffer");
            Selector(policy, "Backend preview", s.backend, new[] { "CPU (proposed)", "GPU (proposed)" }, v => { s.backend = v; rebuild(); });
            Static(policy, "Network Loaded", "False");
            var definition = Group(parent, "Policy Definition");
            Static(definition, "Agent Type", "navigation"); Static(definition, "Observation Space", "Box · float32 [4]"); Static(definition, "Action Space", "Box · float32 [3]");
            var stepper = Group(parent, "Stepper");
            Selector(stepper, "Stepper preview", s.stepper, new[] { "Simple", "Pipelined" }, v => { s.stepper = v; rebuild(); });
            Static(stepper, "Agents", "Runner"); Static(stepper, "Policy", "Navigation policy");
            Note(stepper, s.stepper == "Simple" ? "Observe → infer → act, in one step." : "Observe → dispatch inference → apply actions on a later frame.");
            var buffers = Group(parent, "Policy Data", false);
            Static(buffers, "Observation Buffer", "Empty · model not loaded"); Static(buffers, "Action Buffer", "Empty · model not loaded"); Static(buffers, "State Buffers", "0 elements");
            Note(parent, "Backend choices describe the proposed interface. No ONNX runtime is installed or invoked.");
            ActionButton(parent, "Run Inference", "Inference is not connected in this UI preview.");
        }
    }

    // Small schematic of the source-default ray fan, not a scene or sensor simulation.
    internal sealed class RayDiagram : VisualElement
    {
        public RayDiagram()
        {
            AddToClassList("ray-diagram");
            generateVisualContent += ctx =>
            {
                var p = ctx.painter2D;
                float w = contentRect.width, h = contentRect.height;
                var origin = new Vector2(w / 2, h - 22);
                p.lineWidth = 1; p.strokeColor = new Color(.5f, .5f, .5f, .25f);
                for (float x = 12; x < w; x += 24) { p.BeginPath(); p.MoveTo(new Vector2(x, 0)); p.LineTo(new Vector2(x, h)); p.Stroke(); }
                for (float y = 0; y < h; y += 24) { p.BeginPath(); p.MoveTo(new Vector2(0, y)); p.LineTo(new Vector2(w, y)); p.Stroke(); }
                p.lineWidth = 2; p.strokeColor = new Color(.3f, .65f, .95f);
                float length = Math.Min(h - 34, w / 2 - 12);
                foreach (var sign in new[] { -1, 1 }) { p.BeginPath(); p.MoveTo(origin); p.LineTo(origin + new Vector2(sign * length, -length)); p.Stroke(); }
                p.fillColor = p.strokeColor; p.BeginPath(); p.Arc(origin, 4, 0, 360); p.Fill();
            };
        }
    }
}
