using System;
using UnityEngine;

public enum ItemCategory { Support, Quest, BossRelic }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
[Flags] public enum ItemRole { None=0, Quest=1, Key=2, BossEvidence=4 }
public enum ItemEffectKind { HealHealth, RestoreStamina, Power, Haste, Resistance, Endurance, Protection, CurePoison, CureBurn, CureStress, ReturnToShrine }

/// <summary>Shared asset data. Existing quest/relic assets keep their GUIDs and serialized field names.</summary>
public abstract class GameItemDefinition : ScriptableObject
{
    public string itemId, displayName;
    [TextArea(2,6)] public string description;
    public Sprite icon;
    public ItemCategory category = ItemCategory.Quest;
    public ItemRarity rarity;
    public ItemRole roles;
    [Min(1)] public int maxStack = int.MaxValue;
    [Tooltip("Tiêu hao sau khi dùng hiệu ứng trực tiếp/ô nhanh thành công. Không điều khiển hiến tế Hạt sinh mệnh tại tượng thần.")]
    public bool consumable = true;
    [Min(0)] public float useCooldown = 1f;
    public bool buyable, sellable;
    [Min(0)] public int buyPrice = 30, sellPrice = 10;
    [Serializable] public sealed class Effect
    {
        public ItemEffectKind kind;
        [Range(0,100)] public float percent = 20f;
        [Min(.1f)] public float duration = 30f;
    }
    public Effect[] effects = new Effect[0];
    public bool CanUse => effects != null && effects.Length > 0;
    public ItemCategory InventoryCategory => this is BossItemDefinition boss ? (boss.showAsQuestItem ? ItemCategory.Quest : ItemCategory.BossRelic) : category;
    public static GameItemDefinition FindItem(string id)
    {
        if(string.IsNullOrEmpty(id))return null;
        foreach(var item in All)if(item.itemId==id)return item;
        return null;
    }
    private static GameItemDefinition[] cached;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache(){cached=null;}
    public static GameItemDefinition[] All => Application.isPlaying ? cached ?? (cached=Resources.LoadAll<GameItemDefinition>("")) : Resources.LoadAll<GameItemDefinition>("");
}
