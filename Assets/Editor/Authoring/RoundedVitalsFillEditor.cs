using UnityEditor;

[CustomEditor(typeof(RoundedVitalsFill)), CanEditMultipleObjects]
public class RoundedVitalsFillEditor : UnityEditor.UI.ImageEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rounded fill details", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cornerRadius"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pixelSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("topHighlight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bottomBrightness"));
        serializedObject.ApplyModifiedProperties();
    }
}
