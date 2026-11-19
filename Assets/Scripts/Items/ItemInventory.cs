using System.Collections.Generic;
using UnityEngine;

/// <summary>Facade over the existing quest and boss saves; no duplicated legacy inventory.</summary>
public static class ItemInventory
{
    public static int Count(GameItemDefinition item)
    {
        if(item==null)return 0;
        if(item.itemId==LifeSeedItem.Id)return GameEconomy.LifeSeeds;
        long count=0;
        foreach(var owned in QuestService.OwnedItems())if(owned.id==item.itemId)count+=owned.count;
        foreach(var owned in BossItemInventory.OwnedItems())if(owned.Id==item.itemId)count+=owned.Quantity;
        return (int)System.Math.Min(int.MaxValue,count);
    }
    public static List<GameItemDefinition> Owned(ItemCategory category)
    {
        var result=new List<GameItemDefinition>();
        foreach(var item in GameItemDefinition.All)if(item.InventoryCategory==category&&Count(item)>0)result.Add(item);
        return result;
    }
    public static bool Remove(GameItemDefinition item,int amount)
    {
        if(item==null||amount<=0||Count(item)<amount)return false;
        if(item.itemId==LifeSeedItem.Id)return GameEconomy.RemoveLifeSeedItem(amount);
        int fromQuest=0;foreach(var owned in QuestService.OwnedItems())if(owned.id==item.itemId)fromQuest=System.Math.Min(amount,owned.count);
        if(fromQuest>0&&!QuestService.RemoveItem(item,fromQuest))return false;
        return amount==fromQuest||BossItemInventory.Consume(item.itemId,amount-fromQuest);
    }
}
