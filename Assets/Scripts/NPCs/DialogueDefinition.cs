using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueAction { Continue,AcceptQuest,ReportQuest,OpenShop,End,ChallengeBoss }
[CreateAssetMenu(menuName="Game/Dialogue/Conversation")]
public sealed class DialogueDefinition : ScriptableObject
{
    [Tooltip("Tự nhận nhiệm vụ còn khả dụng khi mở cuộc hội thoại.")]
    public QuestDefinition autoAcceptQuest;
    [Tooltip("Nhánh dùng ngay sau lần tự nhận nếu chưa đủ mục tiêu. Nếu đã đủ mục tiêu, vẫn chọn theo Entry Rules.")]
    public string autoAcceptedStartNodeId;
    [Tooltip("Nhiệm vụ tự trả khi bắt đầu nói chuyện với NPC này. Các nhiệm vụ ở Tự nhận, lựa chọn AcceptQuest/ReportQuest và dấu hậu nhiệm vụ cũng được tự nhận diện.")]
    public List<QuestDefinition> autoTurnInQuests=new List<QuestDefinition>();
    public void TurnInReadyQuests()
    {
        var quests=new HashSet<QuestDefinition>(autoTurnInQuests??new List<QuestDefinition>());
        quests.Add(autoAcceptQuest);
        if(nodes!=null)foreach(var node in nodes)
        {
            if(node==null)continue;
            quests.Add(node.acknowledgeQuestCompletion);
            if(node.choices!=null)foreach(var choice in node.choices)
                if(choice!=null&&(choice.action==DialogueAction.AcceptQuest||choice.action==DialogueAction.ReportQuest))quests.Add(choice.quest);
        }
        foreach(var quest in quests)if(quest!=null&&QuestService.Status(quest)==QuestStatus.ReadyToReport)QuestService.TurnIn(quest);
    }
    [Serializable] public sealed class EntryRule
    {
        public string encounterId;
        public bool mustBeDefeated=true;
        public QuestDefinition quest;
        public QuestStatus requiredStatus;
        [Tooltip("Chỉ vào nhánh này khi chưa xem xong hội thoại hậu nhiệm vụ.")]
        public bool onlyBeforeCompletionDialogueSeen;
        public string startNodeId;
        public bool Matches => (string.IsNullOrEmpty(encounterId)||EnemyDefeatState.WasDefeated(encounterId)==mustBeDefeated)&&
            (quest==null||QuestService.Status(quest)==requiredStatus)&&
            (!onlyBeforeCompletionDialogueSeen||quest!=null&&!QuestService.HasSeenCompletionDialogue(quest));
    }
    [Tooltip("Kiểm tra từ trên xuống mỗi lần bắt đầu nói chuyện. Quy tắc khớp đầu tiên chọn nhánh; không có quy tắc khớp thì dùng Start Node Id.")]
    public List<EntryRule> entryRules=new List<EntryRule>();
    public string ResolveStartNodeId()
    {
        if(entryRules!=null)foreach(var rule in entryRules)
            if(rule!=null&&rule.Matches&&Find(rule.startNodeId)!=null)return rule.startNodeId;
        return startNodeId;
    }
    [Serializable] public sealed class Choice
    {
        public string text,nextNodeId;
        public DialogueAction action;
        public QuestDefinition quest;
        public QuestDefinition conditionQuest;
        public QuestStatus requiredStatus;
        public bool IsVisible=>conditionQuest==null||QuestService.Status(conditionQuest)==requiredStatus;
    }
    [Serializable] public sealed class Node
    {
        public string id,speaker;
        [TextArea(3,10)] public string text;
        public QuestDefinition textQuest;
        [Tooltip("Hoàn tất nhiệm vụ đủ điều kiện khi đến đoạn này, không trao phần thưởng.")]
        public QuestDefinition completeQuestWithoutReward;
        [Tooltip("Đánh dấu đã xem hết hội thoại hậu nhiệm vụ. Không quyết định hoàn thành hay phát thưởng.")]
        public QuestDefinition acknowledgeQuestCompletion;
        public string nextNodeId;
        public List<Choice> choices=new List<Choice>();
        public string ResolvedText=>textQuest==null?(text??""):(text??"").Replace("{title}",textQuest.title).Replace("{progress}",QuestService.ProgressText(textQuest)).Replace("{rewards}",QuestService.RewardText(textQuest)).Replace("{status}",QuestService.StatusText(QuestService.Status(textQuest)));
    }
    public string startNodeId="start";
    public List<Node> nodes=new List<Node>();
    public Node Find(string id)=>nodes.Find(n=>n!=null&&n.id==id);
}
