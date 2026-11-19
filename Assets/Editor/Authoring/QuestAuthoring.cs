using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Editor-only authoring operations; never changes player progress.</summary>
public static class QuestAuthoring
{
    public static bool IsRuntimePath(QuestDefinition quest)=>AssetDatabase.GetAssetPath(quest).Contains("/Resources/Quests/");
    public static QuestDefinition Create(string path,QuestDefinition copy=null,QuestDefinition prerequisite=null)
    {
        if(!path.StartsWith("Assets/",StringComparison.Ordinal)||!path.Contains("/Resources/Quests/")||!path.EndsWith(".asset",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Lưu nhiệm vụ trong thư mục Resources/Quests để game tìm thấy.");
        if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new InvalidOperationException("Đường dẫn đã có asset; hãy chọn tên khác.");
        var quest=ScriptableObject.CreateInstance<QuestDefinition>();
        if(copy!=null)EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(copy),quest);
        quest.questId="quest-"+Guid.NewGuid().ToString("N").Substring(0,12);
        quest.title=copy!=null?copy.title+" (bản sao)":"Nhiệm vụ mới";
        if(prerequisite!=null)quest.prerequisites=new List<QuestDefinition>{prerequisite};
        Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();AssetDatabase.CreateAsset(quest,path);Undo.RegisterCreatedObjectUndo(quest,"Tạo nhiệm vụ");AssetDatabase.SaveAssetIfDirty(quest);return quest;
    }
    public static bool WouldCycle(QuestDefinition quest,QuestDefinition prerequisite)
    {
        var visited=new HashSet<QuestDefinition>();var pending=new Stack<QuestDefinition>();if(prerequisite!=null)pending.Push(prerequisite);
        while(pending.Count>0){var item=pending.Pop();if(item==quest)return true;if(!visited.Add(item)||item.prerequisites==null)continue;foreach(var p in item.prerequisites)if(p!=null)pending.Push(p);}return false;
    }
    public static List<string> Validate(QuestDefinition quest,IEnumerable<QuestDefinition> all)
    {
        var errors=new List<string>();if(quest==null)return errors;
        if(!IsRuntimePath(quest))errors.Add("Asset nằm ngoài Resources/Quests: game sẽ không tìm thấy nhiệm vụ này.");
        if(string.IsNullOrWhiteSpace(quest.questId))errors.Add("Quest Id đang trống.");
        else if(all.Any(q=>q!=null&&q!=quest&&q.questId==quest.questId))errors.Add("Quest Id trùng với nhiệm vụ khác.");
        if(string.IsNullOrWhiteSpace(quest.title))errors.Add("Tên nhiệm vụ đang trống.");
        if(quest.objectives==null||quest.objectives.Count==0)errors.Add("Cần ít nhất một mục tiêu hạ boss.");
        else foreach(var o in quest.objectives)
        {
            if(o==null||o.count<1){errors.Add("Mục tiêu cần số lượng ít nhất 1.");continue;}
            if(o.relic!=null)
            {
                if(string.IsNullOrWhiteSpace(o.relic.itemId)||!AssetDatabase.GetAssetPath(o.relic).Contains("/Resources/"))
                    errors.Add("Vật phẩm xác nhận cần ID và asset trong Resources.");
            }
            else if(string.IsNullOrWhiteSpace(o.bossId)&&string.IsNullOrWhiteSpace(o.encounterId))errors.Add("Mục tiêu cần cổ vật xác nhận, Boss Id hoặc Encounter Id.");
        }
        var prerequisites=new HashSet<QuestDefinition>();
        if(quest.prerequisites!=null)foreach(var p in quest.prerequisites)
        {
            if(p==null){errors.Add("Có nhiệm vụ điều kiện chưa được gán.");continue;}
            if(!prerequisites.Add(p))errors.Add("Nhiệm vụ điều kiện bị lặp: "+p.title);
            if(WouldCycle(quest,p))errors.Add("Chuỗi nhiệm vụ có vòng lặp qua: "+p.title);
            if(!IsRuntimePath(p))errors.Add("Nhiệm vụ điều kiện nằm ngoài Resources/Quests: "+p.title);
        }
        if(quest.gold<0||quest.experience<0||quest.lifeSeeds<0)errors.Add("Phần thưởng không được âm.");
        if(quest.consumables!=null)foreach(var r in quest.consumables)if(r==null||r.count<1||!ConsumableCatalog.Valid(r.item))errors.Add("Vật phẩm hỗ trợ chưa hợp lệ.");
        if(quest.items!=null)foreach(var r in quest.items)if(r==null||r.item==null||r.count<1||string.IsNullOrWhiteSpace(r.item.itemId)||!AssetDatabase.GetAssetPath(r.item).Contains("/Resources/"))errors.Add("Vật phẩm thưởng cần ID, số lượng và asset trong Resources.");
        return errors.Distinct().ToList();
    }
    public static bool AddBossObjective(QuestDefinition quest,EnemyClassification boss,bool exact)
    {
        if(quest==null||boss==null||boss.enemyType==EnemyClassification.EnemyType.Normal||string.IsNullOrWhiteSpace(boss.questBossId))throw new InvalidOperationException("Chọn boss / elite có Quest Boss Id trong Enemy Classification.");
        string encounter="";
        if(exact&&!EditorUtility.IsPersistent(boss))
        {
            if(string.IsNullOrWhiteSpace(boss.persistentId)&&string.IsNullOrEmpty(boss.gameObject.scene.path))throw new InvalidOperationException("Lưu scene hoặc gán Persistent Id trước khi chọn đúng một lần gặp.");
            encounter=boss.DefeatId;
        }
        if(quest.objectives!=null&&quest.objectives.Any(o=>o!=null&&o.relic==null&&o.bossId==boss.questBossId&&o.encounterId==encounter))return false;
        Undo.RecordObject(quest,"Thêm mục tiêu boss");if(quest.objectives==null)quest.objectives=new List<QuestDefinition.BossObjective>();
        quest.objectives.Add(new QuestDefinition.BossObjective{bossId=boss.questBossId,encounterId=encounter,count=1,description="Hạ "+boss.DisplayName});EditorUtility.SetDirty(quest);return true;
    }
    public static DialogueDefinition CreateDialogue(string path,string speaker)
    {
        if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new InvalidOperationException("Đường dẫn hội thoại đã có asset.");
        var dialogue=ScriptableObject.CreateInstance<DialogueDefinition>();dialogue.nodes.Add(new DialogueDefinition.Node{id="start",speaker=speaker,text="Ngươi có điều muốn hỏi?",choices=new List<DialogueDefinition.Choice>{new DialogueDefinition.Choice{text="Rời đi",action=DialogueAction.End}}});
        Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();AssetDatabase.CreateAsset(dialogue,path);Undo.RegisterCreatedObjectUndo(dialogue,"Tạo hội thoại");return dialogue;
    }
    public static int AddDialogueLinks(QuestDefinition quest,DialogueDefinition dialogue)
    {
        if(quest==null||dialogue==null)throw new InvalidOperationException("Chọn nhiệm vụ và hội thoại.");
        var errors=Validate(quest,AssetDatabase.FindAssets("t:QuestDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<QuestDefinition>(AssetDatabase.GUIDToAssetPath(g))));
        if(errors.Count>0)throw new InvalidOperationException("Sửa cấu hình nhiệm vụ trước: "+errors[0]);
        var start=dialogue.Find(dialogue.startNodeId);if(start==null)throw new InvalidOperationException("Start Node Id của hội thoại không tồn tại.");
        bool accept=dialogue.autoAcceptQuest==quest||dialogue.nodes.Any(n=>n!=null&&n.choices!=null&&n.choices.Any(c=>c!=null&&c.quest==quest&&c.action==DialogueAction.AcceptQuest));
        bool report=dialogue.nodes.Any(n=>n!=null&&(n.acknowledgeQuestCompletion==quest||n.choices!=null&&n.choices.Any(c=>c!=null&&c.quest==quest&&c.action==DialogueAction.ReportQuest)));
        if(accept&&report)return 0;
        Undo.RecordObject(dialogue,"Thêm lựa chọn nhận / trả nhiệm vụ");int added=0;
        if(!accept)
        {
            string offer=NodeId(dialogue,"offer"),accepted=NodeId(dialogue,"accepted");
            dialogue.nodes.Add(new DialogueDefinition.Node{id=offer,textQuest=quest,text="{title}\n{progress}\nPhần thưởng: {rewards}",choices=new List<DialogueDefinition.Choice>{new DialogueDefinition.Choice{text="Nhận nhiệm vụ",action=DialogueAction.AcceptQuest,quest=quest,nextNodeId=accepted},new DialogueDefinition.Choice{text="Để khi khác",nextNodeId=start.id}}});
            dialogue.nodes.Add(new DialogueDefinition.Node{id=accepted,textQuest=quest,text="Đã nhận: {title}. Đủ mục tiêu thì quay lại nói chuyện để tự trả nhiệm vụ và nhận thưởng.\n{progress}",nextNodeId=start.id});
            InsertBeforeEnd(start,new DialogueDefinition.Choice{text="Nhận: "+quest.title,nextNodeId=offer,conditionQuest=quest,requiredStatus=QuestStatus.Available});added++;
        }
        if(!report)
        {
            string reportId=NodeId(dialogue,"report"),rewarded=NodeId(dialogue,"rewarded");
            dialogue.nodes.Add(new DialogueDefinition.Node{id=reportId,textQuest=quest,text="Đã hoàn thành: {title}\n{progress}\nPhần thưởng: {rewards}",choices=new List<DialogueDefinition.Choice>{new DialogueDefinition.Choice{text="Tiếp tục",action=DialogueAction.Continue,quest=quest,nextNodeId=rewarded},new DialogueDefinition.Choice{text="Báo cáo sau",nextNodeId=start.id}}});
            dialogue.nodes.Add(new DialogueDefinition.Node{id=rewarded,textQuest=quest,text="Đã hoàn thành: {title}.\nPhần thưởng: {rewards}",acknowledgeQuestCompletion=quest,nextNodeId=start.id});
            InsertBeforeEnd(start,new DialogueDefinition.Choice{text="Kết quả: "+quest.title,nextNodeId=reportId,conditionQuest=quest,requiredStatus=QuestStatus.Completed});added++;
        }
        EditorUtility.SetDirty(dialogue);return added;
    }
    private static string NodeId(DialogueDefinition dialogue,string kind)
    {string stem="quest_"+kind,id=stem;int i=1;while(dialogue.Find(id)!=null)id=stem+"_"+i++;return id;}
    private static void InsertBeforeEnd(DialogueDefinition.Node node,DialogueDefinition.Choice choice)
    {if(node.choices==null)node.choices=new List<DialogueDefinition.Choice>();int i=node.choices.FindIndex(c=>c!=null&&c.action==DialogueAction.End);if(i<0)node.choices.Add(choice);else node.choices.Insert(i,choice);}
    public static void BindDialogue(World.NpcInteractable npc,DialogueDefinition dialogue)
    {if(npc==null||dialogue==null)throw new InvalidOperationException("Chọn NPC và hội thoại.");Undo.RecordObject(npc,"Gán hội thoại NPC");npc.dialogue=dialogue;PersistComponent(npc);}
    public static void BindGate(World.QuestBossEncounter gate,QuestDefinition quest)
    {if(gate==null||quest==null)throw new InvalidOperationException("Chọn boss chuyển trạng thái và nhiệm vụ.");Undo.RecordObject(gate,"Gán nhiệm vụ mở boss");gate.requiredQuest=quest;PersistComponent(gate);}
    private static void PersistComponent(Component component)
    {
        EditorUtility.SetDirty(component);
        if(EditorUtility.IsPersistent(component))PrefabUtility.SavePrefabAsset(component.transform.root.gameObject);
        else {PrefabUtility.RecordPrefabInstancePropertyModifications(component);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(component.gameObject.scene);}
    }
}
