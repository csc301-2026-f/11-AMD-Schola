// Copyright (c) 2026 Advanced Micro Devices, Inc. All Rights Reserved.

// Run only in an isolated project containing the pre-refactor Runtime sources.
using System;
using System.Collections.Generic;
using System.Reflection;
using AMD.Schola;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GenerateLegacyFixtures
{
    public static void Generate()
    {
        AssertFieldType("m_Sensors", typeof(List<PositionSensor>));
        AssertFieldType("m_Actuators", typeof(List<TranslateActuator>));
        EditorSettings.serializationMode = SerializationMode.ForceText;
        AssetDatabase.CreateFolder("Assets", "GeneratedLegacyFixtures");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("LegacyConcreteAgent");
        root.transform.position = new Vector3(1f, 2f, 3f);
        var agent = root.AddComponent<ScholaAgent>();
        var sensor = root.AddComponent<PositionSensor>();
        sensor.ObservationKey = "legacy_position";
        var actuator = root.AddComponent<TranslateActuator>();
        actuator.ActionKey = "legacy_translate";
        var child = new GameObject("ChildBindings");
        child.transform.SetParent(root.transform, false);
        child.transform.localPosition = new Vector3(4f, 5f, 6f);
        var childSensor = child.AddComponent<PositionSensor>();
        childSensor.ObservationKey = "child_position";
        var childActuator = child.AddComponent<TranslateActuator>();
        childActuator.ActionKey = "child_translate";
        agent.SetSensors(new PositionSensor[] { sensor, null, childSensor });
        agent.SetActuators(new TranslateActuator[] { actuator, null, childActuator });
        var prefab = PrefabUtility.SaveAsPrefabAsset(
            root,
            "Assets/GeneratedLegacyFixtures/LegacyConcreteBindings.prefab"
        );
        if (prefab == null)
            throw new InvalidOperationException("Could not save legacy prefab.");
        if (
            !EditorSceneManager.SaveScene(
                scene,
                "Assets/GeneratedLegacyFixtures/LegacyConcreteBindings.unity"
            )
        )
            throw new InvalidOperationException("Could not save legacy scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("Saved legacy fixtures using concrete sensor and actuator lists.");
    }

    static void AssertFieldType(string name, Type expected)
    {
        var field = typeof(ScholaAgent).GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (field == null || field.FieldType != expected)
            throw new InvalidOperationException(
                "Fixture generation requires legacy concrete field " + name
            );
    }
}
