using System;
using UnityEngine;

public enum ConsumableKind { Antidote, BurnRemedy, CalmingMedicine, Power, Haste, Resistance, Endurance, Protection, SoulPurification, ShrineReturn }

[CreateAssetMenu(menuName="Game/Consumable Catalog")]
public sealed class ConsumableCatalog : ScriptableObject
{
    [Serializable] public sealed class Item
    {
        public ConsumableKind kind;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        [Min(1)] public int buyPrice = 30;
        [Min(1)] public int sellPrice = 10;
        [Min(.1f)] public float duration = 30;
        [Range(1,100)] public float percent = 20;
    }
    public Item[] items = new Item[0];
    public const int Count = 10;
    public const int ShopCount = 9; // ShrineReturn is a reusable quest item; its stable ID remains for quick-slot compatibility.
    public const string ShrineReturnItemId = "shrine-return-charm";
    private static ConsumableCatalog cached;
    public static ConsumableCatalog Default
    {
        get { if(cached==null)cached=Resources.Load<ConsumableCatalog>("DefaultConsumables");return cached; }
    }
    public Item Find(ConsumableKind kind) => items!=null?Array.Find(items,i=>i!=null && i.kind==kind):null;
    public static bool Valid(ConsumableKind kind) => (int)kind>=0 && (int)kind<Count;
    public static bool CanEquip(ConsumableKind kind)=>Valid(kind)&&kind!=ConsumableKind.SoulPurification;
}
