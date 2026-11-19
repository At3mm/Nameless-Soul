using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One persisted claim per encounter; item and receipt are saved together.</summary>
public static class BossItemInventory
{
    public const string SaveKey = "Noname.BossItems.v1";
    [Serializable] private sealed class Claim
    {
        public string encounterId, itemId, displayName, description;
        public bool consumed;
    }
    [Serializable] private sealed class SaveData
    {
        public int version = 1;
        public List<Claim> claims = new List<Claim>();
    }
    public sealed class Item
    {
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public int Quantity { get; private set; }
        public Sprite Icon => GameItemDefinition.FindItem(Id)?.icon;
        public bool IsQuestItem => GameItemDefinition.FindItem(Id)?.InventoryCategory == ItemCategory.Quest;
        internal Item(string id, string title, string description, int quantity)
        { Id = id; DisplayName = title; Description = description; Quantity = quantity; }
    }

    private static SaveData data;
    public static event Action Changed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { data = null; Changed = null; }
    private static void Load()
    {
        if (data != null) return;
        try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey, "")); }
        catch (ArgumentException) { data = null; }
        if (data == null || data.version != 1) data = new SaveData();
        if (data.claims == null) data.claims = new List<Claim>();
        var ids = new HashSet<string>();
        data.claims.RemoveAll(c => c == null || string.IsNullOrWhiteSpace(c.encounterId) ||
            string.IsNullOrWhiteSpace(c.itemId) || !ids.Add(c.encounterId));
    }
    public static void ReloadFromSave() { data = null; Load(); Changed?.Invoke(); }
    public static List<Item> OwnedItems()
    {
        Load();
        var result = new List<Item>();
        foreach (var claim in data.claims)
        {
            if(claim.consumed)continue;
            int index = result.FindIndex(i => i.Id == claim.itemId);
            var definition = GameItemDefinition.FindItem(claim.itemId);
            var item = new Item(claim.itemId, definition != null ? definition.displayName : claim.displayName,
                definition != null ? definition.description : claim.description, index >= 0 ? result[index].Quantity + 1 : 1);
            if (index >= 0) result[index] = item; else result.Add(item);
        }
        return result;
    }
    public static bool Consume(string id,int amount)
    {
        Load();var owned=data.claims.FindAll(c=>!c.consumed&&c.itemId==id);
        if(amount<=0||owned.Count<amount)return false;
        for(int i=0;i<amount;i++)owned[i].consumed=true;
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data));PlayerPrefs.Save();Changed?.Invoke();return true;
    }
    public static bool TryGrant(EnemyClassification boss, GameItemDefinition definition = null)
    {
        if (!Application.isPlaying || boss == null || boss.enemyType != EnemyClassification.EnemyType.Boss) return false;
        var health = boss.GetComponent<CharacterHealth>();
        if (health == null || !health.isDead || string.IsNullOrWhiteSpace(boss.DefeatId) ||
            EnemyDefeatState.WasDefeated(boss.DefeatId)) return false;
        if (definition != null && string.IsNullOrWhiteSpace(definition.itemId)) return false;
        if (definition != null && ItemInventory.Count(definition) >= definition.maxStack) return false;
        if(definition!=null&&definition.itemId==LifeSeedItem.Id)
        {
            bool granted=GameEconomy.GrantLifeSeedItem(1,"boss-item:"+boss.DefeatId);
            if(granted)BossRewardNotice.Show(new Item(definition.itemId,definition.displayName,definition.description,1),boss.DisplayName,boss.DefeatId);
            return granted;
        }
        Load();
        if (data.claims.Exists(c => c.encounterId == boss.DefeatId)) return false;
        var claim = new Claim
        {
            encounterId = boss.DefeatId,
            itemId = definition != null ? definition.itemId : "boss-memory:" + boss.DefeatId,
            displayName = definition != null ? definition.displayName : "Ký ức: " + boss.DisplayName,
            description = definition != null ? definition.description : "Kỷ vật ghi dấu chiến thắng trước " + boss.DisplayName + "."
        };
        data.claims.Add(claim);
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Changed?.Invoke();
        BossRewardNotice.Show(new Item(claim.itemId, claim.displayName, claim.description, 1), boss.DisplayName, boss.DefeatId);
        return true;
    }
}
