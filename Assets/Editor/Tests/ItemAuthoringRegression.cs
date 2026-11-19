using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ItemAuthoringRegression
{
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static bool LegacyFieldsPreserved(GameItemDefinition item)
    {
        string yaml=File.ReadAllText(AssetDatabase.GetAssetPath(item));
        return !string.IsNullOrEmpty(item.itemId)&&yaml.Contains("itemId: "+item.itemId)&&
            (yaml.Contains("icon: {fileID: 0}")||item.icon!=null);
    }
    [MenuItem("Tools/Items/Test Item Manager %#F7")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi test vật phẩm.");
        var keys=new[]{GameEconomy.SaveKey,QuestService.SaveKey,BossItemInventory.SaveKey};
        var saved=keys.ToDictionary(k=>k,k=>PlayerPrefs.HasKey(k)?PlayerPrefs.GetString(k):null);
        var created=new List<UnityEngine.Object>();checks=0;
        string result="";
        try
        {
            foreach(var key in keys)PlayerPrefs.DeleteKey(key);
            GameEconomy.ReloadFromSave();BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();
            var item=ItemAuthoring.CreateItem(ItemCategory.Support);created.Add(item);
            item.displayName="Regression item";item.maxStack=6;item.buyable=true;item.sellable=true;item.buyPrice=10;item.sellPrice=5;
            item.effects=new[]{new GameItemDefinition.Effect{kind=ItemEffectKind.HealHealth,percent=25,duration=1}};
            item.roles=ItemRole.Quest|ItemRole.Key|ItemRole.BossEvidence;
            ItemAuthoring.Save(item);Check(ItemAuthoring.Validate(item).Count==0,"Valid item rejected");
            string path=AssetDatabase.GetAssetPath(item);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            Check(item.effects.Length==1&&item.roles.HasFlag(ItemRole.Key)&&item.maxStack==6,"Shared fields did not serialize");
            var duplicate=ItemAuthoring.CreateItem(ItemCategory.Quest,item);created.Add(duplicate);
            Check(duplicate.itemId!=item.itemId&&duplicate.icon==item.icon&&duplicate.effects.Length==1,"Duplicate did not preserve data / generate ID");
            bool rejected=false;try{ItemAuthoring.ValidateId(duplicate,item.itemId);}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"Duplicate ID accepted");
            rejected=false;try{ItemAuthoring.ValidateId(item," ");}catch(InvalidOperationException){rejected=true;}Check(rejected,"Empty ID accepted");
            Check(QuestService.GrantItem(item,2,"regression-pickup"),"Grant failed");
            Check(!QuestService.GrantItem(item,2,"regression-pickup")&&ItemInventory.Count(item)==2,"Pickup paid twice");
            Check(!QuestService.GrantItem(item,5)&&ItemInventory.Count(item)==2,"Stack overflow changed inventory");
            QuestService.ReloadFromSave();Check(ItemInventory.Count(item)==2,"Count did not survive reload");
            Check(GameEconomy.GrantForTesting(100,0,new int[ConsumableCatalog.Count]),"Wallet setup failed");
            Check(GameEconomy.TryPurchase(20,()=>QuestService.GrantItem(item,2))&&GameEconomy.Gold==80&&ItemInventory.Count(item)==4,"Purchase failed");
            Check(!GameEconomy.TryPurchase(10,()=>QuestService.GrantItem(item,3))&&GameEconomy.Gold==80,"Failed grant charged gold");
            Check(GameEconomy.TrySellItem(5,()=>ItemInventory.Remove(item,1))&&GameEconomy.Gold==85&&ItemInventory.Count(item)==3,"Sell failed");
            Check(GameEconomy.ToggleCustomQuickItem(item)&&GameEconomy.IsCustomEquipped(item.itemId),"Custom equip failed");
            GameEconomy.SelectCustomItem(item.itemId);GameEconomy.ReloadFromSave();
            Check(GameEconomy.SelectedCustomItem==item.itemId&&GameEconomy.QuickItemCount==1,"Quick selection did not persist");
            var legacy=new int[ConsumableCatalog.Count];for(int i=0;i<5;i++)legacy[i]=1;
            GameEconomy.GrantForTesting(0,0,legacy);
            for(int i=0;i<4;i++)Check(GameEconomy.ToggleQuickItem((ConsumableKind)i),"Mixed equip failed");
            Check(!GameEconomy.ToggleQuickItem((ConsumableKind)4)&&GameEconomy.QuickItemCount==5,"Mixed quick slots exceed limit");
            var quest=ScriptableObject.CreateInstance<QuestDefinition>();quest.questId="item-regression-quest";quest.title="Proof";
            quest.objectives.Add(new QuestDefinition.BossObjective{relic=item,count=3});created.Add(quest);
            Check(QuestService.Progress(quest,quest.objectives[0])==3,"Generic item evidence not counted");
            string questSave=PlayerPrefs.GetString(QuestService.SaveKey);
            PlayerPrefs.SetString(QuestService.SaveKey,questSave.Replace("\"quests\":[]","\"quests\":[{\"id\":\"item-regression-quest\",\"completed\":false,\"previousKills\":[]}]"));
            QuestService.ReloadFromSave();Check(QuestService.Status(quest)==QuestStatus.ReadyToReport,"Owned evidence not ready to report");
            Check(ItemInventory.Remove(item,1)&&QuestService.Status(quest)==QuestStatus.Active,"Missing evidence still ready");
            var next=ScriptableObject.CreateInstance<QuestDefinition>();created.Add(next);next.questId="item-regression-next";
            next.objectives.Add(new QuestDefinition.BossObjective{relic=item,count=1});next.prerequisites.Add(quest);
            Check(QuestService.Status(next)==QuestStatus.Locked,"Chain opened before report");
            string active=PlayerPrefs.GetString(QuestService.SaveKey);
            PlayerPrefs.SetString(QuestService.SaveKey,active.Replace("\"completed\":false","\"completed\":true"));QuestService.ReloadFromSave();
            Check(QuestService.Status(next)==QuestStatus.Available,"Chain did not open after report");
            Check(ItemInventory.Count(item)==2,"Evidence check consumed items");
            foreach(var old in Resources.LoadAll<QuestItemDefinition>("QuestItems"))Check(LegacyFieldsPreserved(old),"Legacy quest fields lost: "+old.name);
            foreach(var old in Resources.LoadAll<BossItemDefinition>("BossRewards"))Check(LegacyFieldsPreserved(old),"Legacy relic fields lost: "+old.name);
            result="PASS "+checks+" checks: serialization, IDs, duplicate, stack cap, pickup receipts, persistence, transactions, mixed quick slots, relic evidence and quest chain, legacy assets.";
        }
        catch(Exception e){result="FAIL after "+checks+" checks: "+e;Debug.LogError(result);}
        finally
        {
            foreach(var pair in saved){if(pair.Value==null)PlayerPrefs.DeleteKey(pair.Key);else PlayerPrefs.SetString(pair.Key,pair.Value);}PlayerPrefs.Save();
            GameEconomy.ReloadFromSave();BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();
            foreach(var obj in created)if(obj!=null){Undo.ClearUndo(obj);string path=AssetDatabase.GetAssetPath(obj);if(!string.IsNullOrEmpty(path))AssetDatabase.DeleteAsset(path);else UnityEngine.Object.DestroyImmediate(obj);}
            Directory.CreateDirectory("Library/ItemAuthoringValidation");File.WriteAllText("Library/ItemAuthoringValidation/results.txt",result+"\nOriginal save restored.");
        }
        Debug.Log(result);
    }
}
