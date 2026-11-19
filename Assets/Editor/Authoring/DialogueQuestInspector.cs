using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueDefinition))]
public sealed class DialogueDefinitionInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Nodes: mỗi nút là một lượt nói. Choice: đáp án của người chơi. Next Node Id nối sang nút khác; Condition Quest + Required Status quyết định khi nào hiện đáp án. Text Quest cho phép dùng {title}, {progress}, {rewards}, {status} trong lời thoại.",MessageType.Info);
        DrawDefaultInspector();
        var graph=(DialogueDefinition)target;var ids=new HashSet<string>();
        foreach(var node in graph.nodes)if(node==null||string.IsNullOrWhiteSpace(node.id)||!ids.Add(node.id))EditorGUILayout.HelpBox("Node ID phải có giá trị và không trùng nhau.",MessageType.Error);
        if(graph.Find(graph.startNodeId)==null)EditorGUILayout.HelpBox("Start Node Id không tồn tại.",MessageType.Error);
        foreach(var node in graph.nodes)if(node!=null)
        {
            CheckLink(graph,node.nextNodeId);
            foreach(var choice in node.choices)if(choice!=null)
            {
                CheckLink(graph,choice.nextNodeId);
                if((choice.action==DialogueAction.AcceptQuest||choice.action==DialogueAction.ReportQuest)&&choice.quest==null)EditorGUILayout.HelpBox("Lựa chọn nhận/trả nhiệm vụ chưa gắn Quest ở node "+node.id,MessageType.Error);
            }
        }
    }
    static void CheckLink(DialogueDefinition graph,string id){if(!string.IsNullOrEmpty(id)&&graph.Find(id)==null)EditorGUILayout.HelpBox("Không tìm thấy Next Node Id: "+id,MessageType.Error);}
}
[CustomEditor(typeof(QuestDefinition))]
public sealed class QuestDefinitionInspector : Editor
{
    private EnemyClassification selectedBoss;
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Quest Id phải duy nhất và giữ ổn định sau khi phát hành save. Boss Id khớp Quest Boss Id trên EnemyClassification. Encounter Id để trống: chấp nhận boss cùng loại; có giá trị: chỉ đích danh một boss. Phần thưởng chỉ trao khi báo cáo qua hội thoại.",MessageType.Info);
        DrawDefaultInspector();var quest=(QuestDefinition)target;
        if(string.IsNullOrWhiteSpace(quest.questId)||quest.objectives.Count==0)EditorGUILayout.HelpBox("Cần Quest Id và ít nhất một mục tiêu.",MessageType.Error);
        foreach(var other in QuestDefinition.All)if(other!=quest&&other.questId==quest.questId)EditorGUILayout.HelpBox("Quest Id trùng với "+other.name,MessageType.Error);
        foreach(var objective in quest.objectives)if(objective==null||objective.count<1||string.IsNullOrWhiteSpace(objective.bossId)&&string.IsNullOrWhiteSpace(objective.encounterId))EditorGUILayout.HelpBox("Mục tiêu cần Boss Id hoặc Encounter Id và Count ít nhất 1.",MessageType.Error);
        EditorGUILayout.Space();selectedBoss=(EnemyClassification)EditorGUILayout.ObjectField("Lấy mục tiêu từ boss",selectedBoss,typeof(EnemyClassification),true);
        if(selectedBoss!=null&&GUILayout.Button("Thêm mục tiêu từ boss đã chọn"))
        {
            if(selectedBoss.enemyType!=EnemyClassification.EnemyType.Boss||string.IsNullOrWhiteSpace(selectedBoss.questBossId))Debug.LogWarning("Chọn Boss và đặt Quest Boss Id trên EnemyClassification trước.");
            else
            {
                Undo.RecordObject(quest,"Add boss quest objective");quest.objectives.Add(new QuestDefinition.BossObjective{bossId=selectedBoss.questBossId,encounterId=EditorUtility.IsPersistent(selectedBoss)?"":selectedBoss.DefeatId,description="Hạ "+selectedBoss.DisplayName,count=1});EditorUtility.SetDirty(quest);
            }
        }
        if(Application.isPlaying)EditorGUILayout.LabelField("Trạng thái hiện tại",QuestService.StatusText(QuestService.Status(quest)));
    }
}
