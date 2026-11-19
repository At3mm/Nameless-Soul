using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class QuestAuthoringRegression
{
    public static string LastResult { get; private set; }
    private static int passed;
    private static void Check(bool value,string reason){if(!value)throw new Exception(reason);passed++;}
    [MenuItem("Tools/Dialogue and Quests/Test Quest Manager")]
    public static void MenuRun(){Debug.Log(Run());}
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi test Quest Manager.");
        passed=0;string token=Guid.NewGuid().ToString("N");string qroot="Assets/Resources/Quests/__QuestToolTest_"+token;string droot="Assets/__QuestToolTest_"+token;
        AssetDatabase.CreateFolder("Assets/Resources/Quests","__QuestToolTest_"+token);AssetDatabase.CreateFolder("Assets","__QuestToolTest_"+token);
        var preview=EditorSceneManager.NewPreviewScene();var created=new List<UnityEngine.Object>();
        try
        {
            var a=QuestAuthoring.Create(qroot+"/A.asset");var b=QuestAuthoring.Create(qroot+"/B.asset",null,a);var c=QuestAuthoring.Create(qroot+"/C.asset",null,b);created.AddRange(new UnityEngine.Object[]{a,b,c});
            Check(a.questId!=b.questId&&a.questId!=c.questId&&b.questId!=c.questId,"New IDs collide");
            Check(b.prerequisites.SequenceEqual(new[]{a})&&c.prerequisites.SequenceEqual(new[]{b}),"Create-next did not link A -> B -> C");
            Check(QuestAuthoring.WouldCycle(a,c)&&!QuestAuthoring.WouldCycle(c,a),"Cycle detection failed");
            var boss=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/OrcRider.prefab").GetComponent<EnemyClassification>();
            foreach(var q in new[]{a,b,c})Check(QuestAuthoring.AddBossObjective(q,boss,true),"Prefab boss objective failed");
            Check(a.objectives[0].encounterId==""&&a.objectives[0].bossId==boss.questBossId,"Prefab must target boss type, not one encounter");
            Check(!QuestAuthoring.AddBossObjective(a,boss,true),"Duplicate goal was added");
            Check(QuestAuthoring.Validate(a,new[]{a,b,c}).Count==0,"Valid quest reported invalid");
            string bid=b.questId;b.questId=a.questId;
            Check(QuestAuthoring.Validate(b,new[]{a,b,c}).Any(e=>e.Contains("trùng")),"Duplicate quest ID not detected");b.questId=bid;
            a.prerequisites.Add(c);Check(QuestAuthoring.Validate(a,new[]{a,b,c}).Any(e=>e.Contains("vòng lặp")),"Loop not validated");a.prerequisites.Clear();
            var duplicate=QuestAuthoring.Create(qroot+"/Copy.asset",a);created.Add(duplicate);
            Check(duplicate.questId!=a.questId&&duplicate.objectives.Count==a.objectives.Count,"Copy did not get a new ID / copy goals");
            string text=a.objectives[0].description;duplicate.objectives[0].description="Copy only";Check(a.objectives[0].description==text,"Copy shares mutable goals");
            var go=new GameObject("Quest tool test boss");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,preview);
            var sceneBoss=go.AddComponent<EnemyClassification>();sceneBoss.enemyType=EnemyClassification.EnemyType.Boss;sceneBoss.questBossId="quest-tool-test-boss";sceneBoss.persistentId="quest-tool-test:"+token;
            Check(QuestAuthoring.AddBossObjective(a,sceneBoss,true)&&a.objectives.Last().encounterId==sceneBoss.persistentId,"Exact encounter picker failed");
            var dialogue=QuestAuthoring.CreateDialogue(droot+"/Dialogue.asset","Test NPC");created.Add(dialogue);dialogue.nodes[0].text="Keep custom introduction";dialogue.nodes.Add(new DialogueDefinition.Node{id="custom",text="Keep custom story"});
            Check(QuestAuthoring.AddDialogueLinks(a,dialogue)==2,"Missing accept/report branches");int count=dialogue.nodes.Count;
            Check(QuestAuthoring.AddDialogueLinks(a,dialogue)==0&&dialogue.nodes.Count==count,"Repeated generation duplicated nodes");
            Check(dialogue.nodes[0].text=="Keep custom introduction"&&dialogue.Find("custom").text=="Keep custom story","Existing dialogue was overwritten");
            Check(QuestAuthoring.AddDialogueLinks(b,dialogue)==2,"Second quest dialogue generation failed");
            foreach(var node in dialogue.nodes)
            {
                Check(string.IsNullOrEmpty(node.nextNodeId)||dialogue.Find(node.nextNodeId)!=null,"Dangling node link");
                foreach(var choice in node.choices)Check(string.IsNullOrEmpty(choice.nextNodeId)||dialogue.Find(choice.nextNodeId)!=null,"Dangling choice link");
            }
            Check(dialogue.nodes[0].choices.Last().action==DialogueAction.End,"Exit choice should remain last");
            Check(dialogue.nodes[0].choices.Any(ch=>ch.conditionQuest==b&&ch.requiredStatus==QuestStatus.Available)&&dialogue.nodes[0].choices.Any(ch=>ch.conditionQuest==b&&ch.requiredStatus==QuestStatus.Completed),"Wrong availability/report conditions");
            var npcGo=new GameObject("Quest tool test NPC");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(npcGo,preview);var npc=npcGo.AddComponent<World.NpcInteractable>();QuestAuthoring.BindDialogue(npc,dialogue);Check(npc.dialogue==dialogue,"NPC assignment failed");
            Undo.IncrementCurrentGroup();string title=a.title;Undo.RecordObject(a,"Quest tool title undo test");a.title="Undo test";EditorUtility.SetDirty(a);Undo.FlushUndoRecordObjects();Undo.PerformUndo();Check(a.title==title,"Quest edit Undo failed");
            foreach(var q in new[]{a,b,c}){EditorUtility.SetDirty(q);AssetDatabase.SaveAssetIfDirty(q);}
            AssetDatabase.ImportAsset(qroot+"/B.asset",ImportAssetOptions.ForceUpdate);
            Check(AssetDatabase.LoadAssetAtPath<QuestDefinition>(qroot+"/B.asset").prerequisites[0]==a,"Prerequisite reference did not persist");
            LastResult="Quest Manager: passed "+passed+" checks (create/copy/chain, goals, validation, dialogue preservation/idempotence, NPC binding, Undo, persistence).";return LastResult;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);foreach(var obj in created)if(obj!=null)Undo.ClearUndo(obj);
            AssetDatabase.DeleteAsset(qroot);AssetDatabase.DeleteAsset(droot);AssetDatabase.Refresh();
        }
    }
}
