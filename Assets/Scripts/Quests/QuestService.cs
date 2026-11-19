using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum QuestStatus { Locked,Available,Active,ReadyToReport,Completed }

public static class QuestService
{
    public const string SaveKey="Noname.Quests.v1";
    [Serializable] private sealed class Record { public string id;public bool completed,rewardsClaimed,completionDialogueSeen;public List<string> previousKills=new List<string>(); }
    [Serializable] private sealed class Kill { public string bossId,encounterId; }
    [Serializable] public sealed class OwnedItem { public string id;public int count;public GameItemDefinition Definition=>GameItemDefinition.FindItem(id); }
    [Serializable] private sealed class Data
    {
        public int version;
        public List<Record> quests=new List<Record>();public List<Kill> kills=new List<Kill>();public List<OwnedItem> items=new List<OwnedItem>();
        public List<string> itemReceipts=new List<string>();
    }
    private static Data data;
    private static bool paying,processing;
    public static event Action Changed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){data=null;paying=false;processing=false;Changed=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        CharacterHealth.AnyDied-=OnDied;CharacterHealth.AnyDied+=OnDied;
        BossItemInventory.Changed-=OnExternalInventoryChanged;BossItemInventory.Changed+=OnExternalInventoryChanged;
        GameEconomy.Changed-=OnExternalInventoryChanged;GameEconomy.Changed+=OnExternalInventoryChanged;
        if(ProcessAutomaticCompletion())Changed?.Invoke();
    }
    private static void OnExternalInventoryChanged(){if(ProcessAutomaticCompletion())Changed?.Invoke();}
    private static void Load()
    {
        if(data!=null)return;
        try{data=JsonUtility.FromJson<Data>(PlayerPrefs.GetString(SaveKey,""));}catch(ArgumentException){data=null;}
        bool fresh=data==null;
        if(fresh)
        {
            data=new Data();
            data.items.Add(new OwnedItem{id=ConsumableCatalog.ShrineReturnItemId,count=1});
        }
        if(data.itemReceipts==null)data.itemReceipts=new List<string>();
        if(data.quests==null)data.quests=new List<Record>();if(data.kills==null)data.kills=new List<Kill>();if(data.items==null)data.items=new List<OwnedItem>();
        data.quests.RemoveAll(r=>r==null||string.IsNullOrEmpty(r.id));data.kills.RemoveAll(k=>k==null||string.IsNullOrEmpty(k.encounterId));data.items.RemoveAll(i=>i==null||string.IsNullOrEmpty(i.id)||i.count<=0);
        foreach(var record in data.quests)if(record.previousKills==null)record.previousKills=new List<string>();
        bool upgraded=data.version<2;
        if(upgraded){foreach(var record in data.quests)if(record.completed)record.rewardsClaimed=true;data.version=2;}
        // Exact encounter objectives can recover victories from saves made before quests existed.
        bool migrated=false;
        foreach(var quest in QuestDefinition.All)foreach(var objective in quest.objectives)
            if(objective!=null&&objective.relic==null&&!string.IsNullOrEmpty(objective.encounterId)&&EnemyDefeatState.WasDefeated(objective.encounterId))migrated|=AddKill(objective.bossId,objective.encounterId);
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyClassification>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(enemy.enemyType!=EnemyClassification.EnemyType.Normal&&!string.IsNullOrEmpty(enemy.questBossId)&&EnemyDefeatState.WasDefeated(enemy.DefeatId))migrated|=AddKill(enemy.questBossId,enemy.DefeatId);
        if((fresh||migrated||upgraded)&&Application.isPlaying)Save(false);
    }
    private static void Save(bool notify=true){PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));PlayerPrefs.Save();if(notify){ProcessAutomaticCompletion();Changed?.Invoke();}}
    public static void ReloadFromSave(){data=null;Load();ProcessAutomaticCompletion();Changed?.Invoke();}
    private static bool Valid(QuestDefinition q)=>q!=null&&!string.IsNullOrWhiteSpace(q.questId)&&q.objectives!=null&&q.objectives.Count>0&&q.objectives.TrueForAll(o=>o!=null&&o.count>0&&(o.relic!=null?!string.IsNullOrWhiteSpace(o.relic.itemId):(!string.IsNullOrWhiteSpace(o.bossId)||!string.IsNullOrWhiteSpace(o.encounterId))));
    public static QuestStatus Status(QuestDefinition quest)
    {
        Load();if(!Valid(quest))return QuestStatus.Locked;
        var record=data.quests.Find(r=>r.id==quest.questId);
        if(record!=null){if(record.completed)return QuestStatus.Completed;return quest.objectives.TrueForAll(o=>Progress(quest,o)>=o.count)?QuestStatus.ReadyToReport:QuestStatus.Active;}
        if(quest.requiredDefeatedEncounters!=null&&quest.requiredDefeatedEncounters.Exists(id=>string.IsNullOrWhiteSpace(id)||!EnemyDefeatState.WasDefeated(id)))return QuestStatus.Locked;
        if(quest.prerequisites!=null&&quest.prerequisites.Exists(q=>q==null||!data.quests.Exists(r=>r.id==q.questId&&r.completed)))return QuestStatus.Locked;
        return QuestStatus.Available;
    }
    public static int Progress(QuestDefinition quest,QuestDefinition.BossObjective objective)
    {
        if(objective.relic!=null)
        {
            return Mathf.Min(ItemInventory.Count(objective.relic),objective.count);
        }
        Load();var record=data.quests.Find(r=>r.id==quest.questId);
        int count=data.kills.Count(k=>(string.IsNullOrEmpty(objective.bossId)||k.bossId==objective.bossId)&&
            (string.IsNullOrEmpty(objective.encounterId)||k.encounterId==objective.encounterId)&&
            (quest.countPreviousKills||record!=null&&!record.previousKills.Contains(k.encounterId)));
        return Mathf.Min(count,objective.count);
    }
    public static bool Accept(QuestDefinition quest)
    {
        if(!Application.isPlaying||paying)return false;
        var status=Status(quest);
        if(status!=QuestStatus.Available)
        {Notice(status==QuestStatus.Locked?"Chưa đủ điều kiện nhận nhiệm vụ.":status==QuestStatus.Completed?"Nhiệm vụ này đã hoàn thành.":"Bạn đã nhận nhiệm vụ này.");return false;}
        data.quests.Add(new Record{id=quest.questId,previousKills=data.kills.Select(k=>k.encounterId).ToList()});Notice("Đã nhận nhiệm vụ: "+quest.title);Save();return true;
    }
    // Compatibility for old callers; rewards are now delivered automatically.
    public static bool TurnIn(QuestDefinition quest)
    {
        if(!Application.isPlaying||paying)return false;
        if(Status(quest)!=QuestStatus.ReadyToReport)return false;
        var record=data.quests.Find(r=>r.id==quest.questId);
        record.completed=true;Save(false);
        TryDeliverRewards(quest,record);Changed?.Invoke();
        Notice("Đã hoàn thành: "+quest.title);return true;
    }

    private static bool TryDeliverRewards(QuestDefinition quest,Record record)
    {
        if(record.rewardsClaimed)return true;
        var quantities=new int[ConsumableCatalog.Count];
        foreach(var reward in quest.consumables)
        {
            if(reward==null||!ConsumableCatalog.Valid(reward.item)||reward.count<=0||(long)quantities[(int)reward.item]+reward.count>int.MaxValue)return false;
            quantities[(int)reward.item]+=reward.count;
        }
        var itemTotals=new Dictionary<string,int>();
        foreach(var reward in quest.items)
        {
            if(reward==null||reward.item==null||string.IsNullOrWhiteSpace(reward.item.itemId)||reward.count<=0)return false;
            string id=reward.item.itemId;int existing=itemTotals.ContainsKey(id)?itemTotals[id]:0;
            if((long)existing+reward.count>int.MaxValue)return false;itemTotals[id]=existing+reward.count;
        }
        foreach(var reward in itemTotals){var definition=GameItemDefinition.FindItem(reward.Key);if(definition==null||(long)ItemInventory.Count(definition)+reward.Value>definition.maxStack){return false;}}
        int seedItems=itemTotals.ContainsKey(LifeSeedItem.Id)?itemTotals[LifeSeedItem.Id]:0;
        if((long)quest.lifeSeeds+seedItems>int.MaxValue)return false;
        itemTotals.Remove(LifeSeedItem.Id);
        paying=true;
        try
        {
            // Wallet + receipt share one save. A retry after interruption cannot pay twice.
            if(!GameEconomy.TryGrantQuestReward("quest:"+quest.questId,quest.gold,quest.experience,quest.lifeSeeds+seedItems,quantities))return false;
            foreach(var reward in itemTotals){var owned=data.items.Find(i=>i.id==reward.Key);if(owned==null)data.items.Add(new OwnedItem{id=reward.Key,count=reward.Value});else owned.count+=reward.Value;}
            record.rewardsClaimed=true;Save(false);return true;
        }
        finally{paying=false;}
    }
    public static bool ProcessAutomaticCompletion()
    {
        if(!Application.isPlaying||paying||processing)return false;
        Load();processing=true;bool changed=false;
        try
        {
            var all=QuestDefinition.All;bool delivered;int passes=0;
            do
            {
                delivered=false;
                foreach(var quest in all)
                {
                    if(!Valid(quest))continue;
                    var record=data.quests.Find(r=>r.id==quest.questId);if(record==null)continue;
                    if(record.completed&&!record.rewardsClaimed&&TryDeliverRewards(quest,record)){changed=true;delivered=true;}
                }
            }while(delivered&&++passes<=all.Length);
        }
        finally{processing=false;}
        return changed;
    }
    // Legacy node field no longer suppresses configured rewards.
    public static bool CompleteWithoutReward(QuestDefinition quest)
    {return Status(quest)==QuestStatus.Completed||TurnIn(quest);}
    public static bool HasSeenCompletionDialogue(QuestDefinition quest)
    {Load();return quest!=null&&data.quests.Exists(r=>r.id==quest.questId&&r.completionDialogueSeen);}
    public static void AcknowledgeCompletionDialogue(QuestDefinition quest)
    {
        if(!Application.isPlaying||quest==null||Status(quest)!=QuestStatus.Completed)return;
        var record=data.quests.Find(r=>r.id==quest.questId);if(record.completionDialogueSeen)return;
        record.completionDialogueSeen=true;Save();
    }
    public static bool HasPendingRewards(QuestDefinition quest)
    {Load();return quest!=null&&data.quests.Exists(r=>r.id==quest.questId&&r.completed&&!r.rewardsClaimed);}
    private static bool AddKill(string bossId,string encounterId)
    {
        if(string.IsNullOrEmpty(encounterId)||data.kills.Exists(k=>k.encounterId==encounterId))return false;
        data.kills.Add(new Kill{bossId=bossId,encounterId=encounterId});return true;
    }
    private static void OnDied(CharacterHealth health)
    {
        var boss=health!=null?health.GetComponent<EnemyClassification>():null;
        if(boss==null||boss.enemyType==EnemyClassification.EnemyType.Normal)return;
        Load();
        if(!AddKill(boss.questBossId,boss.DefeatId))return;Save();
    }
    private static void Notice(string message){var player=UnityEngine.Object.FindFirstObjectByType<World.PlayerInteractor>();if(player!=null)player.ShowNotice(message);}
    public static List<OwnedItem> OwnedItems(){Load();return data.items.Select(i=>new OwnedItem{id=i.id,count=i.count}).ToList();}
    public static bool HasItemReceipt(string receipt){Load();return data.itemReceipts.Contains(receipt)||GameEconomy.HasLifeSeedItemReceipt(receipt);}
    public static bool GrantItem(GameItemDefinition item,int amount,string receipt=null)
    {
        Load();if(item==null||string.IsNullOrWhiteSpace(item.itemId)||amount<=0||paying)return false;
        if(item.itemId==LifeSeedItem.Id)return GameEconomy.GrantLifeSeedItem(amount,receipt);
        if(!string.IsNullOrEmpty(receipt)&&data.itemReceipts.Contains(receipt))return false;
        if((long)ItemInventory.Count(item)+amount>item.maxStack)return false;
        var owned=data.items.Find(i=>i.id==item.itemId);
        if(owned==null)data.items.Add(new OwnedItem{id=item.itemId,count=amount});else owned.count+=amount;
        if(!string.IsNullOrEmpty(receipt))data.itemReceipts.Add(receipt);Save();return true;
    }
    public static bool RemoveItem(GameItemDefinition item,int amount)
    {
        Load();if(item==null||amount<=0||paying)return false;
        if(item.itemId==LifeSeedItem.Id)return GameEconomy.RemoveLifeSeedItem(amount);
        var owned=data.items.Find(i=>i.id==item.itemId);if(owned==null||owned.count<amount)return false;
        owned.count-=amount;if(owned.count==0)data.items.Remove(owned);Save();return true;
    }
    public static QuestDefinition[] Accepted(){Load();return QuestDefinition.All.Where(q=>data.quests.Exists(r=>r.id==q.questId)).ToArray();}
    public static string StatusText(QuestStatus status)=>status==QuestStatus.Completed?"Đã hoàn thành":status==QuestStatus.ReadyToReport?"Đủ mục tiêu · Quay lại NPC":status==QuestStatus.Active?"Đang thực hiện":status==QuestStatus.Available?"Chưa nhận":"Chưa mở";
    public static string ProgressText(QuestDefinition quest)=>string.Join("\n",quest.objectives.Select(o=>o.description+"  "+Progress(quest,o)+" / "+o.count));
    public static string RewardText(QuestDefinition quest)
    {
        var parts=new List<string>();if(quest.gold>0)parts.Add(quest.gold+" vàng");if(quest.experience>0)parts.Add(quest.experience+" EXP");if(quest.lifeSeeds>0)parts.Add(quest.lifeSeeds+" Hạt sinh mệnh");
        foreach(var r in quest.consumables)if(r!=null)parts.Add((ConsumableCatalog.Default?.Find(r.item)?.displayName??r.item.ToString())+" x"+r.count);
        foreach(var r in quest.items)if(r!=null&&r.item!=null)parts.Add(r.item.displayName+" x"+r.count);return string.Join(", ",parts);
    }
}
