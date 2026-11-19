using UnityEditor;
using UnityEngine;
using World;

/// <summary>Edit the shared area's visible name without leaving its region in the scene.</summary>
[CustomEditor(typeof(AreaTrigger))]
public sealed class AreaTriggerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var trigger = (AreaTrigger)target;
        if (trigger.area == null)
        {
            EditorGUILayout.HelpBox("Gán Area để đặt tên cho khu vực.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tên thông báo khu vực", EditorStyles.boldLabel);
        var definition = new SerializedObject(trigger.area);
        definition.Update();
        EditorGUILayout.PropertyField(definition.FindProperty("displayName"), new GUIContent("Tên khu vực"));
        EditorGUILayout.PropertyField(definition.FindProperty("subtitle"), new GUIContent("Tên phụ"));
        if (definition.ApplyModifiedProperties()) SceneView.RepaintAll();
    }
}
