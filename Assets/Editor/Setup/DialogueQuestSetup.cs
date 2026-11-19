using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DialogueQuestSetup
{
    public const string QuestPath="Assets/Resources/Quests/DefeatTheWerebear.asset";
    public const string DialoguePath="Assets/Dialogue/PriestConversation.asset";
    [MenuItem("Tools/Dialogue and Quests/Create Missing Example")]
    public static void CreateMissing()
    {
        Directory.CreateDirectory("Assets/Resources/Quests");Directory.CreateDirectory("Assets/Resources/QuestItems");Directory.CreateDirectory("Assets/Dialogue");AssetDatabase.Refresh();
        foreach(var name in new[]{"Necromancer","OrcRider","Werebear"})
        {
            string path="Assets/Prefabs/Characters/"+name+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
            try{var boss=go.GetComponent<EnemyClassification>();if(string.IsNullOrEmpty(boss.questBossId)){boss.questBossId=name=="Necromancer"?"necromancer":name=="Werebear"?"werebear":"orc-rider";PrefabUtility.SaveAsPrefabAsset(go,path);}}
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        const string itemPath="Assets/Resources/QuestItems/PriestBlessing.asset";
        var item=AssetDatabase.LoadAssetAtPath<QuestItemDefinition>(itemPath);
        if(item==null)
        {
            item=ScriptableObject.CreateInstance<QuestItemDefinition>();item.itemId="priest-blessing";item.displayName="Ấn nguyện bình an";item.description="Mề đay tu sĩ trao để ghi nhận lời hứa đã hoàn thành. Vật phẩm kỷ niệm, chưa có hiệu ứng chỉ số.";
            foreach(var definition in Resources.LoadAll<BossItemDefinition>("BossRewards"))if(definition.icon!=null){item.icon=definition.icon;break;}
            if(item.icon==null&&ConsumableCatalog.Default!=null&&ConsumableCatalog.Default.items.Length>0)item.icon=ConsumableCatalog.Default.items[0].icon;
            AssetDatabase.CreateAsset(item,itemPath);
        }
        var quest=AssetDatabase.LoadAssetAtPath<QuestDefinition>(QuestPath);
        if(quest==null)
        {
            quest=ScriptableObject.CreateInstance<QuestDefinition>();quest.questId="priest-defeat-werebear";quest.title="Khuất phục Werebear";quest.description="Hạ Werebear rồi trở lại báo cáo với Tu sĩ. Chiến thắng trước khi nhận nhiệm vụ cũng được ghi nhận.";quest.icon=item.icon;quest.gold=150;quest.experience=300;quest.lifeSeeds=1;
            quest.objectives.Add(new QuestDefinition.BossObjective{bossId="werebear",encounterId="",description="Hạ Werebear",count=1});quest.items.Add(new QuestDefinition.ItemReward{item=item,count=1});
            AssetDatabase.CreateAsset(quest,QuestPath);
        }
        var conversation=AssetDatabase.LoadAssetAtPath<DialogueDefinition>(DialoguePath);
        if(conversation==null)
        {
            conversation=ScriptableObject.CreateInstance<DialogueDefinition>();conversation.startNodeId="start";
            var root=new DialogueDefinition.Node{id="start",text="Những linh hồn ngoài kia chưa được yên nghỉ. Ngươi có điều muốn hỏi ta?"};
            root.choices.Add(Choice("Nghe lời thỉnh cầu","offer",DialogueAction.Continue,quest,QuestStatus.Available));
            root.choices.Add(Choice("Hỏi về nhiệm vụ","progress",DialogueAction.Continue,quest,QuestStatus.Active));
            root.choices.Add(Choice("Báo cáo: Werebear đã bị hạ","report",DialogueAction.Continue,quest,QuestStatus.ReadyToReport));
            root.choices.Add(Choice("Nói về lời nguyện","after",DialogueAction.Continue,quest,QuestStatus.Completed));
            root.choices.Add(new DialogueDefinition.Choice{text="Mua bán vật phẩm",action=DialogueAction.OpenShop,conditionQuest=quest,requiredStatus=QuestStatus.Completed});root.choices.Add(new DialogueDefinition.Choice{text="Rời đi",action=DialogueAction.End});conversation.nodes.Add(root);
            conversation.nodes.Add(new DialogueDefinition.Node{id="offer",textQuest=quest,text="Hãy hạ Werebear rồi trở lại đây. Hoàn thành nhiệm vụ sẽ mở khóa cửa hàng của ta.\nPhần thưởng: {rewards}",choices=new List<DialogueDefinition.Choice>{new DialogueDefinition.Choice{text="Ta nhận lời",action=DialogueAction.AcceptQuest,quest=quest,nextNodeId="accepted"},new DialogueDefinition.Choice{text="Để khi khác",nextNodeId="start"}}});
            conversation.nodes.Add(new DialogueDefinition.Node{id="accepted",textQuest=quest,text="Ta sẽ chờ tin của ngươi.\n{progress}",nextNodeId="start"});
            conversation.nodes.Add(new DialogueDefinition.Node{id="progress",textQuest=quest,text="Lời thỉnh cầu vẫn còn đó. Hãy cẩn trọng khi đối đầu với hắn.\n{progress}",nextNodeId="start"});
            conversation.nodes.Add(new DialogueDefinition.Node{id="report",textQuest=quest,text="Ngươi đã trở về. Những linh hồn kia cuối cùng cũng được yên nghỉ.\nNhận thưởng: {rewards}",choices=new List<DialogueDefinition.Choice>{new DialogueDefinition.Choice{text="Hoàn tất báo cáo và nhận thưởng",action=DialogueAction.ReportQuest,quest=quest,nextNodeId="rewarded"},new DialogueDefinition.Choice{text="Báo cáo sau",nextNodeId="start"}}});
            conversation.nodes.Add(new DialogueDefinition.Node{id="rewarded",text="Hãy nhận lấy ấn nguyện này. Mong nó nhắc ngươi rằng lời hứa vẫn có trọng lượng trong cõi tàn phai.",nextNodeId="start"});
            conversation.nodes.Add(new DialogueDefinition.Node{id="after",text="Khu rừng đã yên lặng hơn. Ta không quên điều ngươi đã làm.",nextNodeId="start"});
            conversation.autoAcceptQuest=quest;conversation.autoAcceptedStartNodeId="offer";
            conversation.entryRules.Add(new DialogueDefinition.EntryRule{quest=quest,requiredStatus=QuestStatus.Active,startNodeId="progress"});
            conversation.entryRules.Add(new DialogueDefinition.EntryRule{quest=quest,requiredStatus=QuestStatus.Completed,startNodeId="report",onlyBeforeCompletionDialogueSeen=true});
            conversation.entryRules.Add(new DialogueDefinition.EntryRule{quest=quest,requiredStatus=QuestStatus.Completed,startNodeId="after"});
            conversation.Find("offer").choices.Clear();conversation.Find("offer").nextNodeId="accepted";
            conversation.Find("accepted").nextNodeId="";conversation.Find("progress").nextNodeId="";
            conversation.Find("rewarded").acknowledgeQuestCompletion=quest;
            foreach(var choice in conversation.Find("report").choices)if(choice.action==DialogueAction.ReportQuest)choice.action=DialogueAction.Continue;
            root.choices.RemoveAll(c=>c.action==DialogueAction.Continue);
            foreach(var choice in conversation.Find("report").choices)if(choice.action==DialogueAction.Continue&&choice.nextNodeId=="start"){choice.action=DialogueAction.End;choice.nextNodeId="";}
            AssetDatabase.CreateAsset(conversation,DialoguePath);
        }
        const string npcPath="Assets/Prefabs/NPCs/NPC_Priest.prefab";var npc=PrefabUtility.LoadPrefabContents(npcPath);
        try{var shop=npc.GetComponent<World.NpcShop>();if(shop!=null&&shop.requiredQuest==null)shop.requiredQuest=quest;var talk=npc.GetComponent<World.NpcInteractable>();if(talk.dialogue==null)talk.dialogue=conversation;PrefabUtility.SaveAsPrefabAsset(npc,npcPath);}
        finally{PrefabUtility.UnloadPrefabContents(npc);}
        AssetDatabase.SaveAssets();
    }
    static DialogueDefinition.Choice Choice(string text,string next,DialogueAction action,QuestDefinition quest,QuestStatus state)=>new DialogueDefinition.Choice{text=text,nextNodeId=next,action=action,conditionQuest=quest,requiredStatus=state};
}
