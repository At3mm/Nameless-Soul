using System;
using UnityEngine;

/// <summary>Single persisted wallet and stat purchases. Purchase count is a price input, not a character level.</summary>
public static class GameEconomy
{
    public enum Stat { Health, Stamina, Attack, Defense }
    public const string SaveKey = "Noname.Economy.v1";
    [Serializable] private sealed class Data
    {
        public int version = 1;
        public int gold, experience, health, stamina, attack, defense;
        public int spareHealthPotions;
        public int lifeSeeds, potionCapacityUpgrades;
        public System.Collections.Generic.List<string> lifeSeedItemReceipts=new System.Collections.Generic.List<string>();
        public string[] collectedLifeSeeds = new string[0];
        public System.Collections.Generic.List<EliteSeedDrop> eliteSeedDrops = new System.Collections.Generic.List<EliteSeedDrop>();
        public int[] consumables = new int[ConsumableCatalog.Count];
        public int selectedConsumable;
        public int[] quickItems;
        public string[] customQuickItems = new string[0];
        public string selectedCustomItem;
        public System.Collections.Generic.List<string> questRewardReceipts=new System.Collections.Generic.List<string>();
        public bool soulConsumed;
        public int soulConsumptionDeaths;
        public PlayerDeathDrop deathDrop;
    }
    private static Data data;
    private static EconomySettings settings;
    private static bool purchasing;
    public static event Action Changed;
    /// <summary>Positive currency actually credited, after a successful persisted transaction.</summary>
    public static event Action<int,int> CurrencyReceived;
    public static EconomySettings Settings
    {
        get
        {
            if (settings == null)
            {
                settings = Resources.Load<EconomySettings>("DefaultEconomy");
                if (settings == null) { settings = ScriptableObject.CreateInstance<EconomySettings>(); settings.hideFlags = HideFlags.HideAndDontSave; }
            }
            return settings;
        }
    }
    public static int Gold { get { EnsureLoaded(); return data.gold; } }
    public static int Experience { get { EnsureLoaded(); return data.experience; } }
    [Serializable] public sealed class PlayerDeathDrop
    {
        public string id,scenePath;
        public Vector3 position;
        public int gold,experience;
    }
    public static PlayerDeathDrop PendingDeathDrop
    {
        get{EnsureLoaded();var drop=data.deathDrop;return drop==null?null:new PlayerDeathDrop{id=drop.id,scenePath=drop.scenePath,position=drop.position,gold=drop.gold,experience=drop.experience};}
    }
    public static bool IsCurrentDeathDrop(string id){EnsureLoaded();return data.deathDrop!=null&&data.deathDrop.id==id;}
    public static bool RecordPlayerDeathDrop(CharacterHealth player)
    {
        if(!Application.isPlaying||player==null||!player.isDead||player.GetComponent<PlayerController>()==null||string.IsNullOrEmpty(player.gameObject.scene.path))return false;
        EnsureLoaded();if(purchasing)return false;
        int gold=data.gold/2,experience=data.experience/2;
        data.gold-=gold;data.experience-=experience;
        // Replace the previous grave even if this death has no currency to drop.
        data.deathDrop=gold>0||experience>0?new PlayerDeathDrop{id=Guid.NewGuid().ToString("N"),scenePath=player.gameObject.scene.path,position=player.transform.position,gold=gold,experience=experience}:null;
        Commit();return true;
    }
    public static bool RecoverPlayerDeathDrop(string id)
    {
        EnsureLoaded();var drop=data.deathDrop;
        if(purchasing||drop==null||drop.id!=id||(long)data.gold+drop.gold>int.MaxValue||(long)data.experience+drop.experience>int.MaxValue)return false;
        data.gold+=drop.gold;data.experience+=drop.experience;data.deathDrop=null;Commit(drop.gold,drop.experience);return true;
    }
    public static bool HasSoulConsumption { get { EnsureLoaded();return data.soulConsumed; } }
    public static int SoulConsumptionDeaths { get { EnsureLoaded();return data.soulConsumptionDeaths; } }
    public static bool RecordSoulConsumptionDeath(float roll)
    {
        EnsureLoaded();if(purchasing||float.IsNaN(roll)||float.IsInfinity(roll)||roll<0||roll>1)return false;
        float chance=SoulConsumption.NextChancePercent*.01f;
        bool newlyConsumed=!data.soulConsumed&&(chance>=1||roll<chance);
        if(newlyConsumed)data.soulConsumed=true;
        if(data.soulConsumptionDeaths<int.MaxValue)data.soulConsumptionDeaths++;
        Commit();return newlyConsumed;
    }
    public static bool PurifySoulConsumption()
    {
        EnsureLoaded();int item=(int)ConsumableKind.SoulPurification;
        if(purchasing||!World.CheckpointMenu.CanPurifySoulAtShrine||!data.soulConsumed||data.consumables[item]<=0)return false;
        data.consumables[item]--;data.soulConsumed=false;data.soulConsumptionDeaths=0;Commit();return true;
    }
    public static int SpareHealthPotions { get { EnsureLoaded(); return data.spareHealthPotions; } }
    public static int LifeSeeds { get { EnsureLoaded(); return data.lifeSeeds; } }
    public static int PotionCapacityUpgrades { get { EnsureLoaded(); return data.potionCapacityUpgrades; } }
    public static bool AddLifeSeeds(int amount)
    {
        EnsureLoaded();
        if (purchasing || amount <= 0 || (long)data.lifeSeeds + amount > int.MaxValue) return false;
        data.lifeSeeds += amount; Commit(); return true;
    }
    // Item grants use the same wallet as enemy rewards and shrine sacrifices.
    // The outer shop transaction persists both wallet and item when purchasing is true.
    public static bool HasLifeSeedItemReceipt(string receipt){EnsureLoaded();return data.lifeSeedItemReceipts.Contains(receipt);}
    public static bool GrantLifeSeedItem(int amount,string receipt=null)
    {
        EnsureLoaded();
        if(amount<=0||(long)data.lifeSeeds+amount>int.MaxValue||!string.IsNullOrEmpty(receipt)&&data.lifeSeedItemReceipts.Contains(receipt))return false;
        data.lifeSeeds+=amount;
        if(!string.IsNullOrEmpty(receipt))data.lifeSeedItemReceipts.Add(receipt);
        if(!purchasing)Commit();return true;
    }
    public static bool RemoveLifeSeedItem(int amount)
    {
        EnsureLoaded();if(amount<=0||data.lifeSeeds<amount)return false;
        data.lifeSeeds-=amount;if(!purchasing)Commit();return true;
    }
    public static int NextPotionSeedCost => Settings.PotionSeedCost(PotionCapacityUpgrades);
    public static bool SacrificeLifeSeed(PlayerHealthPotion potion)
    {
        EnsureLoaded();
        if (purchasing || potion == null || data.lifeSeeds < NextPotionSeedCost || NextPotionSeedCost<=0 || potion.capacity >= PlayerHealthPotion.MaximumCapacity || data.potionCapacityUpgrades >= PlayerHealthPotion.MaximumCapacity - PlayerHealthPotion.StartingCapacity) return false;
        var health = potion.GetComponent<CharacterHealth>();
        if (health == null || health.isDead) return false;
        data.lifeSeeds-=NextPotionSeedCost; data.potionCapacityUpgrades++;
        Commit(); return true;
    }
    public static bool HasCollectedLifeSeed(string id) { EnsureLoaded(); return Array.IndexOf(data.collectedLifeSeeds, id) >= 0; }
    [Serializable] public sealed class EliteSeedDrop
    {
        public string scenePath, pickupId;
        public Vector3 position;
    }
    public static EliteSeedDrop[] EliteSeedDrops { get { EnsureLoaded(); return data.eliteSeedDrops.ToArray(); } }
    public static EliteSeedDrop RecordEliteSeedDrop(EnemyClassification enemy)
    {
        EnsureLoaded();
        if (purchasing || enemy == null || (enemy.enemyType != EnemyClassification.EnemyType.Elite && enemy.enemyType != EnemyClassification.EnemyType.Boss) ||
            enemy.Health == null || !enemy.Health.isDead || EnemyDefeatState.WasDefeated(enemy.DefeatId) || data.lifeSeeds==int.MaxValue) return null;
        string id = "elite-seed:" + enemy.DefeatId;
        if (data.eliteSeedDrops.Exists(drop => drop.pickupId == id)) return null;
        var record = new EliteSeedDrop { scenePath = enemy.gameObject.scene.path, pickupId = id, position = enemy.transform.position };
        // Encounter receipt and seed are committed together: no pickup or duplicate on reload.
        data.eliteSeedDrops.Add(record);
        var collected=new System.Collections.Generic.List<string>(data.collectedLifeSeeds);
        collected.Add(record.scenePath+"."+record.pickupId);data.collectedLifeSeeds=collected.ToArray();
        data.lifeSeeds++;Commit();return record;
    }
    public static bool CollectLifeSeed(string id, int amount)
    {
        EnsureLoaded();
        if (purchasing || string.IsNullOrEmpty(id) || HasCollectedLifeSeed(id) || amount <= 0 || (long)data.lifeSeeds + amount > int.MaxValue) return false;
        var ids = new System.Collections.Generic.List<string>(data.collectedLifeSeeds); ids.Add(id);
        data.collectedLifeSeeds = ids.ToArray(); data.lifeSeeds += amount; Commit(); return true;
    }
    public static ConsumableKind SelectedConsumable { get { EnsureLoaded();return (ConsumableKind)data.selectedConsumable; } }
    public const int QuickItemLimit = 5;
    public static int QuickItemCount { get { EnsureLoaded(); return data.quickItems.Length+data.customQuickItems.Length; } }
    public static int LegacyQuickItemCount { get { EnsureLoaded(); return data.quickItems.Length; } }
    public static string SelectedCustomItem { get { EnsureLoaded(); return data.selectedCustomItem; } }
    public static string CustomQuickItemAt(int index){EnsureLoaded();return index>=0&&index<data.customQuickItems.Length?data.customQuickItems[index]:null;}
    public static bool IsCustomEquipped(string id){EnsureLoaded();return Array.IndexOf(data.customQuickItems,id)>=0;}
    public static bool ToggleCustomQuickItem(GameItemDefinition item)
    {
        EnsureLoaded();if(purchasing||item==null||!item.CanUse)return false;
        var ids=new System.Collections.Generic.List<string>(data.customQuickItems);
        if(ids.Contains(item.itemId))ids.Remove(item.itemId);
        else {if(QuickItemCount>=QuickItemLimit||ItemInventory.Count(item)<=0)return false;ids.Add(item.itemId);}
        data.customQuickItems=ids.ToArray();
        if(!ids.Contains(data.selectedCustomItem))data.selectedCustomItem=null;
        if(data.selectedConsumable<0&&string.IsNullOrEmpty(data.selectedCustomItem)&&ids.Count>0)data.selectedCustomItem=ids[0];
        Commit();return true;
    }
    public static void SelectCustomItem(string id)
    {EnsureLoaded();if(purchasing||!IsCustomEquipped(id)||data.selectedCustomItem==id)return;data.selectedCustomItem=id;Commit();}
    public static ConsumableKind QuickItemAt(int index) { EnsureLoaded(); return index>=0 && index<data.quickItems.Length ? (ConsumableKind)data.quickItems[index] : (ConsumableKind)(-1); }
    public static int QuickItemIndex(ConsumableKind kind) { EnsureLoaded(); return Array.IndexOf(data.quickItems,(int)kind); }
    public static bool ToggleQuickItem(ConsumableKind kind)
    {
        EnsureLoaded(); if(purchasing || !ConsumableCatalog.CanEquip(kind))return false;
        int index=Array.IndexOf(data.quickItems,(int)kind);
        var items=new System.Collections.Generic.List<int>(data.quickItems);
        if(index>=0)items.RemoveAt(index);
        else { if(QuickItemCount>=QuickItemLimit || ConsumableCount(kind)<=0)return false; items.Add((int)kind); }
        data.quickItems=items.ToArray();
        if(Array.IndexOf(data.quickItems,data.selectedConsumable)<0)data.selectedConsumable=items.Count>0?items[0]:-1;
        Commit();return true;
    }
    public static int ConsumableCount(ConsumableKind kind)
    {
        EnsureLoaded();
        if(kind==ConsumableKind.ShrineReturn)return data.consumables[(int)kind]>0 || QuestService.OwnedItems().Exists(i=>i.id==ConsumableCatalog.ShrineReturnItemId && i.count>0)?1:0;
        return ConsumableCatalog.Valid(kind)?data.consumables[(int)kind]:0;
    }
    public static int TotalPurchases { get { EnsureLoaded(); return data.health+data.stamina+data.attack+data.defense; } }
    public static int NextUpgradeCost => Settings.UpgradeCost(TotalPurchases);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { data = null; settings = null; purchasing = false; Changed = null; CurrencyReceived=null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        CharacterHealth.AnyDied -= OnDied;
        CharacterHealth.AnyDied += OnDied;
    }
    private static void OnDied(CharacterHealth health)
    {
        var reward = health.GetComponent<EnemyCurrencyReward>();
        if (reward != null) reward.TryClaim();
    }
    private static void EnsureLoaded()
    {
        if (data != null) return;
        try { data = JsonUtility.FromJson<Data>(PlayerPrefs.GetString(SaveKey,"")); }
        catch (ArgumentException) { data = null; }
        if (data == null || data.version != 1 || data.gold < 0 || data.experience < 0 || data.health < 0 || data.stamina < 0 || data.attack < 0 || data.defense < 0 || data.spareHealthPotions < 0 ||
            (long)data.health+data.stamina+data.attack+data.defense > int.MaxValue) data = new Data();
        if(data.consumables==null)data.consumables=new int[ConsumableCatalog.Count];
        if(data.lifeSeedItemReceipts==null)data.lifeSeedItemReceipts=new System.Collections.Generic.List<string>();
        if(data.questRewardReceipts==null)data.questRewardReceipts=new System.Collections.Generic.List<string>();
        data.soulConsumptionDeaths=Mathf.Max(0,data.soulConsumptionDeaths);
        if(data.deathDrop!=null&&(string.IsNullOrWhiteSpace(data.deathDrop.id)||string.IsNullOrWhiteSpace(data.deathDrop.scenePath)||data.deathDrop.gold<0||data.deathDrop.experience<0||
            data.deathDrop.gold==0&&data.deathDrop.experience==0||float.IsNaN(data.deathDrop.position.x)||float.IsInfinity(data.deathDrop.position.x)||float.IsNaN(data.deathDrop.position.y)||float.IsInfinity(data.deathDrop.position.y)||float.IsNaN(data.deathDrop.position.z)||float.IsInfinity(data.deathDrop.position.z)))data.deathDrop=null;
        data.lifeSeeds = Mathf.Max(0, data.lifeSeeds);
        if (data.collectedLifeSeeds == null) data.collectedLifeSeeds = new string[0];
        data.potionCapacityUpgrades = Mathf.Clamp(data.potionCapacityUpgrades, 0, PlayerHealthPotion.MaximumCapacity - PlayerHealthPotion.StartingCapacity);
        if (data.eliteSeedDrops == null) data.eliteSeedDrops = new System.Collections.Generic.List<EliteSeedDrop>();
        data.eliteSeedDrops.RemoveAll(drop => drop == null || string.IsNullOrEmpty(drop.pickupId));
        if(data.consumables.Length!=ConsumableCatalog.Count)Array.Resize(ref data.consumables,ConsumableCatalog.Count);
        for(int i=0;i<data.consumables.Length;i++)data.consumables[i]=Mathf.Max(0,data.consumables[i]);
        if(data.quickItems==null) data.quickItems=ConsumableCatalog.Valid((ConsumableKind)data.selectedConsumable) && data.consumables[data.selectedConsumable]>0 ? new[]{data.selectedConsumable} : new int[0];
        var valid=new System.Collections.Generic.List<int>();
        foreach(int kind in data.quickItems)if(ConsumableCatalog.CanEquip((ConsumableKind)kind) && !valid.Contains(kind) && valid.Count<QuickItemLimit)valid.Add(kind);
        data.quickItems=valid.ToArray();
        var custom=new System.Collections.Generic.List<string>();
        if(data.customQuickItems!=null)foreach(var id in data.customQuickItems)
            if(!string.IsNullOrEmpty(id)&&!custom.Contains(id)&&GameItemDefinition.FindItem(id)?.CanUse==true&&custom.Count+valid.Count<QuickItemLimit)custom.Add(id);
        data.customQuickItems=custom.ToArray();
        if(!custom.Contains(data.selectedCustomItem))data.selectedCustomItem=null;
        if(!valid.Contains(data.selectedConsumable))data.selectedConsumable=valid.Count>0?valid[0]:-1;
    }
    public static void ReloadFromSave()
    {
        if (purchasing) return;
        data = null; EnsureLoaded(); Changed?.Invoke();
    }
    public static void SaveProgress() { EnsureLoaded(); Commit(); SaveGameNotice.Show(); }
    private static void Commit(int receivedGold=0,int receivedExperience=0)
    {
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data)); PlayerPrefs.Save(); Changed?.Invoke();
        if(receivedGold>0||receivedExperience>0)CurrencyReceived?.Invoke(receivedGold,receivedExperience);
    }
    public static bool AddGold(int amount) { return AddRewards(amount,0); }
    public static bool TryGrantQuestReward(string receipt,int gold,int experience,int seeds,int[] quantities)
    {
        EnsureLoaded();if(purchasing||string.IsNullOrEmpty(receipt))return false;
        if(data.questRewardReceipts.Contains(receipt))return true;
        if(gold<0||experience<0||seeds<0||quantities==null||quantities.Length!=ConsumableCatalog.Count||
            (long)data.gold+gold>int.MaxValue||(long)data.experience+experience>int.MaxValue||(long)data.lifeSeeds+seeds>int.MaxValue)return false;
        for(int i=0;i<quantities.Length;i++)if(quantities[i]<0||(long)data.consumables[i]+quantities[i]>int.MaxValue)return false;
        data.gold+=gold;data.experience+=experience;data.lifeSeeds+=seeds;
        for(int i=0;i<quantities.Length;i++)data.consumables[i]+=quantities[i];
        data.questRewardReceipts.Add(receipt);Commit(gold,experience);return true;
    }
#if UNITY_EDITOR
    // Editor testing only; validate the entire grant before changing the persisted wallet.
    public static bool GrantForTesting(int gold, int experience, int[] quantities)
    {
        EnsureLoaded();
        if (purchasing || gold < 0 || experience < 0 || quantities == null || quantities.Length != ConsumableCatalog.Count ||
            (long)data.gold + gold > int.MaxValue || (long)data.experience + experience > int.MaxValue) return false;
        for (int i = 0; i < quantities.Length; i++)
            if (quantities[i] < 0 || (long)data.consumables[i] + quantities[i] > int.MaxValue) return false;
        
        bool changed = gold > 0 || experience > 0;
        data.gold += gold;
        data.experience += experience;
        for (int i = 0; i < quantities.Length; i++)
        {
            
            data.consumables[i] += quantities[i];
            changed |= quantities[i] > 0;
        }
        if (changed) Commit(gold,experience);
        return true;
    }
#endif
    public static bool AddExperience(int amount) { return AddRewards(0,amount); }
    public static bool AddRewards(int gold,int experience)
    {
        EnsureLoaded();
        if (purchasing || gold < 0 || experience < 0 || (long)data.gold+gold > int.MaxValue || (long)data.experience+experience > int.MaxValue) return false;
        if (gold == 0 && experience == 0) return true;
        data.gold += gold; data.experience += experience; Commit(gold,experience); return true;
    }
    public static bool TrySpendGold(int amount)
    {
        EnsureLoaded();
        if (purchasing || amount < 0 || data.gold < amount) return false;
        if (amount == 0) return true;
        data.gold -= amount; Commit(); return true;
    }
    public static bool BuySpareHealthPotion(int price, int quantity = 1)
    {
        EnsureLoaded();
        long total = (long)price * quantity;
        if (purchasing || price <= 0 || quantity <= 0 || total > data.gold || (long)data.spareHealthPotions + quantity > int.MaxValue) return false;
        data.gold -= (int)total; data.spareHealthPotions += quantity; Commit(); return true;
    }
    public static bool SellSpareHealthPotion(int price, int quantity = 1)
    {
        EnsureLoaded();
        long total = (long)price * quantity;
        if (purchasing || price <= 0 || quantity <= 0 || data.spareHealthPotions < quantity || (long)data.gold + total > int.MaxValue) return false;
        data.spareHealthPotions -= quantity; data.gold += (int)total; Commit((int)total,0); return true;
    }
    public static bool LoadSpareHealthPotion(PlayerHealthPotion potion)
    {
        EnsureLoaded();
        if (purchasing || potion == null || data.spareHealthPotions <= 0 || potion.Charges >= potion.capacity) return false;
        var health = potion.GetComponent<CharacterHealth>();
        if (health == null || health.isDead || potion.AddCharges(1) != 1) return false;
        data.spareHealthPotions--; Commit(); return true;
    }
    public static void SelectConsumable(ConsumableKind kind)
    {
        EnsureLoaded();if(purchasing||!ConsumableCatalog.Valid(kind)||Array.IndexOf(data.quickItems,(int)kind)<0||data.selectedConsumable==(int)kind&&string.IsNullOrEmpty(data.selectedCustomItem))return;
        data.selectedConsumable=(int)kind;data.selectedCustomItem=null;Commit();
    }
    public static bool BuyConsumable(ConsumableKind kind,int price,int quantity=1)
    {
        if(kind==ConsumableKind.ShrineReturn)return false;
        EnsureLoaded();long total=(long)price*quantity;
        if(purchasing||!ConsumableCatalog.Valid(kind)||price<=0||quantity<=0||total>data.gold||(long)data.consumables[(int)kind]+quantity>int.MaxValue)return false;
        
        data.gold-=(int)total;data.consumables[(int)kind]+=quantity;Commit();return true;
    }
    public static bool SellConsumable(ConsumableKind kind,int price,int quantity=1)
    {
        if(kind==ConsumableKind.ShrineReturn)return false;
        EnsureLoaded();long total=(long)price*quantity;
        if(purchasing||!ConsumableCatalog.Valid(kind)||price<=0||quantity<=0||data.consumables[(int)kind]<quantity||(long)data.gold+total>int.MaxValue)return false;
        data.consumables[(int)kind]-=quantity;data.gold+=(int)total;Commit((int)total,0);return true;
    }
    public static bool TryUseConsumable(ConsumableKind kind,Func<bool> apply)
    {
        if(kind==ConsumableKind.ShrineReturn)return false;
        EnsureLoaded();if(purchasing||!ConsumableCatalog.Valid(kind)||data.consumables[(int)kind]<=0||apply==null)return false;
        bool applied;purchasing=true;try{applied=apply();}finally{purchasing=false;}
        if(!applied)return false;data.consumables[(int)kind]--;Commit();return true;
    }
    /// <summary>Shop hook: deliver returns true only if its item grant succeeded; failed grants cost nothing.</summary>
    public static bool TryPurchase(int goldPrice,Func<bool> deliver)
    {
        EnsureLoaded();
        if (purchasing || goldPrice < 0 || deliver == null || data.gold < goldPrice) return false;
        bool delivered;
        purchasing = true;
        try { delivered = deliver(); }
        finally { purchasing = false; }
        if (!delivered) return false;
        data.gold -= goldPrice; Commit(); return true;
    }
    public static bool TrySellItem(int goldPrice,Func<bool> remove)
    {
        EnsureLoaded();if(purchasing||goldPrice<0||remove==null||(long)data.gold+goldPrice>int.MaxValue)return false;
        bool removed;purchasing=true;try{removed=remove();}finally{purchasing=false;}
        if(!removed)return false;data.gold+=goldPrice;Commit(goldPrice,0);return true;
    }
    public static int Purchases(Stat stat)
    {
        EnsureLoaded();
        switch (stat)
        {
            case Stat.Health: return data.health;
            case Stat.Stamina: return data.stamina;
            case Stat.Attack: return data.attack;
            case Stat.Defense: return data.defense;
            default: return 0;
        }
    }
    public static bool TryUpgrade(Stat stat)
    {
        EnsureLoaded();
        float gain = Settings.Gain(stat);
        int cost = NextUpgradeCost;
        if (purchasing || (int)stat < 0 || (int)stat > 3 || cost <= 0 || data.experience < cost ||
            gain <= 0 || float.IsNaN(gain) || float.IsInfinity(gain)) return false;
        switch (stat)
        {
            case Stat.Health: data.health++; break;
            case Stat.Stamina: data.stamina++; break;
            case Stat.Attack: data.attack++; break;
            case Stat.Defense: data.defense++; break;
        }
        data.experience -= cost; Commit(); return true;
    }
}
