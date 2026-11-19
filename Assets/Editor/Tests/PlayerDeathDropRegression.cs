using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Wallet/grave persistence checks; does not simulate the full death animation or scene loading.</summary>
public static class PlayerDeathDropRegression
{
    [Serializable] private sealed class EmptyWalletFixture { public int version=1;public GameEconomy.PlayerDeathDrop deathDrop; }
    public static string Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
        bool existed=PlayerPrefs.HasKey(GameEconomy.SaveKey);
        string original=PlayerPrefs.GetString(GameEconomy.SaveKey, "");
        var checks=new List<string>();GameObject actor=null;
        try
        {
            actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"));
            actor.SetActive(false);actor.tag="Untagged";actor.transform.position=new Vector3(10000,10001,0);
            var health=actor.GetComponent<CharacterHealth>();health.isDead=true;
            PlayerPrefs.DeleteKey(GameEconomy.SaveKey);GameEconomy.ReloadFromSave();
            GameEconomy.AddRewards(101,201);
            Check(GameEconomy.RecordPlayerDeathDrop(health),"Dead player records a grave",checks);
            var first=GameEconomy.PendingDeathDrop;
            Check(GameEconomy.Gold==51&&GameEconomy.Experience==101&&first.gold==50&&first.experience==100,"Half of each currency drops with integer rounding",checks);
            Check(first.scenePath==actor.scene.path&&first.position==actor.transform.position,"Grave retains scene path and death position",checks);
            GameEconomy.ReloadFromSave();
            Check(GameEconomy.PendingDeathDrop.id==first.id&&GameEconomy.Gold==51,"Reload preserves grave and remaining wallet",checks);
            Check(!GameEconomy.RecoverPlayerDeathDrop("wrong-id")&&GameEconomy.Gold==51,"Unknown receipt cannot recover resources",checks);
            Check(GameEconomy.RecoverPlayerDeathDrop(first.id)&&GameEconomy.Gold==101&&GameEconomy.Experience==201,"Recovery restores both currencies",checks);
            Check(!GameEconomy.RecoverPlayerDeathDrop(first.id)&&GameEconomy.PendingDeathDrop==null,"Recovery cannot pay twice",checks);
            GameEconomy.ReloadFromSave();
            Check(GameEconomy.PendingDeathDrop==null&&GameEconomy.Gold==101,"Recovered state remains cleared after reload",checks);
            GameEconomy.RecordPlayerDeathDrop(health);first=GameEconomy.PendingDeathDrop;
            actor.transform.position+=Vector3.right*4;GameEconomy.RecordPlayerDeathDrop(health);
            var second=GameEconomy.PendingDeathDrop;
            Check(second.id!=first.id&&second.position==actor.transform.position&&second.gold==25&&second.experience==50,"Second death replaces the first grave",checks);
            Check(!GameEconomy.RecoverPlayerDeathDrop(first.id),"Superseded grave can no longer pay",checks);
            Check(GameEconomy.RecoverPlayerDeathDrop(second.id)&&GameEconomy.Gold==51&&GameEconomy.Experience==101,"Only the newest grave is recoverable",checks);
            GameEconomy.RecordPlayerDeathDrop(health);first=GameEconomy.PendingDeathDrop;
            PlayerPrefs.SetString(GameEconomy.SaveKey,JsonUtility.ToJson(new EmptyWalletFixture{deathDrop=first}));GameEconomy.ReloadFromSave();
            Check(GameEconomy.RecordPlayerDeathDrop(health)&&GameEconomy.PendingDeathDrop==null&&!GameEconomy.RecoverPlayerDeathDrop(first.id),"A death with an empty wallet also removes the old grave",checks);
            health.isDead=false;
            Check(!GameEconomy.RecordPlayerDeathDrop(health),"Living actors cannot create graves",checks);
            string result="PASS "+checks.Count+" death-drop service checks\n"+string.Join("\n",checks);
            Directory.CreateDirectory("Library/PlayerDeathDropValidation");File.WriteAllText("Library/PlayerDeathDropValidation/results.txt",result);return result;
        }
        finally
        {
            if(actor!=null)Object.Destroy(actor);
            if(existed)PlayerPrefs.SetString(GameEconomy.SaveKey,original);else PlayerPrefs.DeleteKey(GameEconomy.SaveKey);
            PlayerPrefs.Save();GameEconomy.ReloadFromSave();
        }
    }
    static void Check(bool condition,string label,List<string> checks)
    {if(!condition)throw new InvalidOperationException(label);checks.Add("PASS "+label);}
}
