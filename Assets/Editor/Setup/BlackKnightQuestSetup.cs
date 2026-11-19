using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using World;

public static class BlackKnightQuestSetup
{
    public const string QuestPath="Assets/Resources/Quests/BlackKnightChallenge.asset";
    public const string DialoguePath="Assets/Dialogue/BlackKnightConversation.asset";
    [MenuItem("Tools/Dialogue and Quests/Set Up Black Knight Encounter")]
    public static void CreateMissing()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        Directory.CreateDirectory("Assets/Resources/Quests");Directory.CreateDirectory("Assets/Dialogue");AssetDatabase.Refresh();
        var quest=AssetDatabase.LoadAssetAtPath<QuestDefinition>(QuestPath);
        if(quest==null)
        {
            quest=ScriptableObject.CreateInstance<QuestDefinition>();quest.questId="black-knight-challenge";quest.title="Lời hẹn hiệp sĩ đen";
            quest.description="Hạ Necromancer ở Rừng hoang, rồi quay lại báo cáo với Black Knight. Sau khi báo cáo và kết thúc đối thoại, hiệp sĩ sẽ thách đấu ngươi.";
            quest.countPreviousKills=true;quest.gold=100;quest.experience=200;
            quest.objectives.Add(new QuestDefinition.BossObjective{bossId="necromancer",encounterId="boss:necromancer:wild-forest:01",description="Hạ Necromancer ở Rừng hoang",count=1});
            AssetDatabase.CreateAsset(quest,QuestPath);
        }
        var dialogue=AssetDatabase.LoadAssetAtPath<DialogueDefinition>(DialoguePath);
        if(dialogue==null)
        {
            dialogue=ScriptableObject.CreateInstance<DialogueDefinition>();
            var start=new DialogueDefinition.Node{id="start",speaker="Black Knight",text="Ta đang chờ một kẻ đủ bản lĩnh. Ngươi có muốn nhận thử thách của ta?"};
            start.choices.Add(Choice("Nghe thử thách","offer",quest,QuestStatus.Available));start.choices.Add(Choice("Hỏi về thử thách","progress",quest,QuestStatus.Active));
            start.choices.Add(Choice("Báo cáo: Necromancer ở Rừng hoang đã bị hạ","report",quest,QuestStatus.ReadyToReport));
            start.choices.Add(Choice("Ta đã sẵn sàng cho cuộc đấu","duel",quest,QuestStatus.Completed));start.choices.Add(new DialogueDefinition.Choice{text="Rời đi",action=DialogueAction.End});dialogue.nodes.Add(start);
            dialogue.nodes.Add(new DialogueDefinition.Node{id="offer",textQuest=quest,text="Hãy hạ Necromancer ở Rừng hoang rồi quay về đây. Khi ấy, ta sẽ tự mình thử tài ngươi.\nPhần thưởng trước cuộc đấu: {rewards}",choices=new List<DialogueDefinition.Choice>{new DialogueDefinition.Choice{text="Ta nhận lời",action=DialogueAction.AcceptQuest,quest=quest,nextNodeId="accepted"},new DialogueDefinition.Choice{text="Để khi khác",nextNodeId="start"}}});
            dialogue.nodes.Add(new DialogueDefinition.Node{id="accepted",text="Ta sẽ chờ ngươi ở đây. Hãy trở về khi đã hoàn thành lời hẹn.",nextNodeId="start"});
            dialogue.nodes.Add(new DialogueDefinition.Node{id="progress",textQuest=quest,text="Kẻ gọi hồn ở Rừng hoang là mục tiêu của ngươi. Hãy quay lại báo cáo khi đã hạ được hắn.\n{progress}",nextNodeId="start"});
            dialogue.nodes.Add(new DialogueDefinition.Node{id="report",textQuest=quest,text="Ngươi đã giữ lời. Hãy nhận phần thưởng, rồi chuẩn bị cho cuộc đấu của chúng ta.\n{rewards}",choices=new List<DialogueDefinition.Choice>{new DialogueDefinition.Choice{text="Báo cáo và nhận thưởng",action=DialogueAction.ReportQuest,quest=quest,nextNodeId="duel"},new DialogueDefinition.Choice{text="Để ta chuẩn bị thêm",nextNodeId="start"}}});
            dialogue.nodes.Add(new DialogueDefinition.Node{id="duel",text="Giờ đến lượt ta. Hãy rút vũ khí — cuộc thử thách thực sự bắt đầu.",choices=new List<DialogueDefinition.Choice>{new DialogueDefinition.Choice{text="Kết thúc đối thoại và chuẩn bị chiến đấu",action=DialogueAction.End}}});
            AssetDatabase.CreateAsset(dialogue,DialoguePath);
        }
        var root=PrefabUtility.LoadPrefabContents(BlackKnightSetup.PrefabPath);
        try
        {
            var gate=root.GetComponent<QuestBossEncounter>();if(gate==null)gate=root.AddComponent<QuestBossEncounter>();
            if(gate.requiredQuest==null)gate.requiredQuest=quest;
            if(gate.conversation==null)
            {
                var go=new GameObject("FriendlyConversation");go.transform.SetParent(root.transform,false);
                var npc=go.AddComponent<NpcInteractable>();npc.displayName="Black Knight";npc.interactionLabel="Nói chuyện với Black Knight";npc.interactionRadius=2.5f;npc.dialogue=dialogue;npc.dialogueFont=GameFont.Current;
                go.AddComponent<NpcStationary>();gate.conversation=npc;
            }
            var priest=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Priest.prefab");
            var style=priest!=null?priest.GetComponent<NpcInteractable>():null;
            if(style!=null)
            {
                if(gate.conversation.panelSprite==null)gate.conversation.panelSprite=style.panelSprite;
                if(gate.conversation.advanceArrow==null)gate.conversation.advanceArrow=style.advanceArrow;
                if(gate.conversation.dialogueFont==null)gate.conversation.dialogueFont=style.dialogueFont;
            }
            PrefabUtility.SaveAsPrefabAsset(root,BlackKnightSetup.PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("Black Knight starts friendly, accepts/reports its own quest, then becomes a boss after dialogue closes.");
    }
    static DialogueDefinition.Choice Choice(string text,string next,QuestDefinition quest,QuestStatus status)=>new DialogueDefinition.Choice{text=text,nextNodeId=next,conditionQuest=quest,requiredStatus=status};
}
