// SPDX-License-Identifier: MIT
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Schola.UI.Preview
{
    [Serializable] internal sealed class CatalogField
    {
        public string key, label, kind, value, cppType, tooltip, condition, category, sourceDefault, translation;
        public string[] options;
        public bool hidden;
        public int line;
    }
    [Serializable] internal sealed class CatalogSection
    {
        public string id, source;
        public CatalogField[] fields;
    }
    [Serializable] internal sealed class PreviewCatalog
    {
        public const string PackagePath = "Packages/com.amd.schola.ui-mockup";
        public CatalogSection[] sections;
        private static PreviewCatalog cached;
        public static PreviewCatalog Load()
        {
            if (cached != null) return cached;
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PackagePath + "/Editor/Data/catalog.json");
            if (asset == null) throw new InvalidOperationException("Schola preview catalog is missing. Reinstall the package through Package Manager.");
            cached = JsonUtility.FromJson<PreviewCatalog>(asset.text);
            return cached;
        }
        public CatalogSection Find(string id) => sections.First(s => s.id == id);
    }

    internal sealed class PreviewState
    {
        public string page = "Training", connector = "gRPC", script = "Python", python = "System PATH";
        public string framework = "RLlib", algorithm = "PPO", environment = "Tag arena · multi-agent";
        public string agent = "Runner", sensor = "Raycast", actuator = "Movement Input", space = "Box", stepper = "Simple";
        public string backend = "CPU (proposed)";
    }
}
