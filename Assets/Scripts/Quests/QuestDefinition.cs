using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/Quests/Quest")]
public sealed class QuestDefinition : ScriptableObject
{
    public string questId;
    public string title;
    [TextArea(2,8)] public string description;
    public Sprite icon;
    public bool countPreviousKills=true;
    public List<QuestDefinition> prerequisites=new List<QuestDefinition>();
    [Tooltip("Phải hạ đủ những encounter này trước khi nhận nhiệm vụ. Dùng đúng Persistent Id trên boss.")]
    public List<string> requiredDefeatedEncounters=new List<string>();
    [Serializable] public sealed class BossObjective
    {
        [Tooltip("Nếu gán cổ vật: kiểm tra sở hữu cổ vật làm bằng chứng hạ boss, không tiêu hao. Thay cho Boss Id / Encounter Id.")]
        public GameItemDefinition relic;
        public string bossId;
        [Tooltip("Optional: exact EnemyClassification.DefeatId for one specific encounter.")] public string encounterId;
        public string description;
        [Min(1)] public int count=1;
    }
    [Serializable] public sealed class ConsumableReward { public ConsumableKind item;[Min(1)] public int count=1; }
    [Serializable] public sealed class ItemReward { public GameItemDefinition item;[Min(1)] public int count=1; }
    public List<BossObjective> objectives=new List<BossObjective>();
    [Min(0)] public int gold,experience,lifeSeeds;
    public List<ConsumableReward> consumables=new List<ConsumableReward>();
    public List<ItemReward> items=new List<ItemReward>();
    public static QuestDefinition[] All=>Resources.LoadAll<QuestDefinition>("Quests");
}
