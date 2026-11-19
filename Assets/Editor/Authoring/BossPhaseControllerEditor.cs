using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BossPhaseController))]
public sealed class BossPhaseControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var controller=(BossPhaseController)target;
        var secondPhase=serializedObject.FindProperty("useSecondPhase");
        EditorGUILayout.PropertyField(secondPhase,new GUIContent("Use Second Phase","Tắt để boss chỉ dùng bộ chiêu Phase 1 trong cả trận."));
        if(secondPhase.boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("phase2HealthThreshold"),new GUIContent("Phase 2 HP Threshold","0.5 = chuyển khi còn 50% máu."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("transitionState"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idleState"));
        }
        if(Application.isPlaying)EditorGUILayout.LabelField("Current Phase",controller.CurrentPhase.ToString());
        var manager=controller.GetComponent<SkillDamageManager>();
        if(manager!=null)
        {
            DrawSkills(secondPhase.boolValue?"Phase 1 Skills":"Single Phase Skills",serializedObject.FindProperty("phase1Skills"),manager);
            if(secondPhase.boolValue)DrawSkills("Phase 2 Skills",serializedObject.FindProperty("phase2Skills"),manager);
        }
        EditorGUILayout.HelpBox(secondPhase.boolValue?"Boss phát animation nhận sát thương trong thời gian chuyển phase và miễn tác động, sau đó tiếp tục với bộ chiêu phase 2. Danh sách trống = không dùng chiêu nào.":"Boss chỉ có một phase: không chuyển ở nửa máu, không có khoảng gục xuống / miễn tác động do chuyển phase. Chọn bộ chiêu dùng trong cả trận bên trên.",MessageType.Info);
        serializedObject.ApplyModifiedProperties();
        if(GUILayout.Button("Default: first skill in Phase 1, all skills in Phase 2"))
        {
            Undo.RecordObject(controller,"Reset boss phase skill selection");controller.SetDefaultSkills();
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);EditorUtility.SetDirty(controller);
        }
    }
    static void DrawSkills(string label,SerializedProperty list,SkillDamageManager manager)
    {
        EditorGUILayout.Space();EditorGUILayout.LabelField(label,EditorStyles.boldLabel);
        var names=new HashSet<string>();
        foreach(var skill in manager.skills)
        {
            if(skill==null||string.IsNullOrEmpty(skill.skillName)||!names.Add(skill.skillName))continue;
            int index=-1;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).stringValue==skill.skillName){index=i;break;}
            bool selected=index>=0;bool next=EditorGUILayout.ToggleLeft(skill.skillName,selected);
            if(next&&!selected){int i=list.arraySize;list.InsertArrayElementAtIndex(i);list.GetArrayElementAtIndex(i).stringValue=skill.skillName;}
            else if(!next&&selected)list.DeleteArrayElementAtIndex(index);
        }
        if(list.arraySize==0)EditorGUILayout.HelpBox("Không có chiêu được chọn trong phase này.",MessageType.Warning);
        for(int i=0;i<list.arraySize;i++)if(!names.Contains(list.GetArrayElementAtIndex(i).stringValue))
            EditorGUILayout.HelpBox("Chiêu không còn trong SkillDamageManager: "+list.GetArrayElementAtIndex(i).stringValue,MessageType.Warning);
    }
}
