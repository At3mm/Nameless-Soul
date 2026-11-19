using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using World;
using Object = UnityEngine.Object;

public static class CheckpointRestRegression
{
    [MenuItem("Tools/Diagnostics/Test Checkpoint Prayer Only Rest (Play Mode)")]
    public static void RunMenu() { Debug.Log(Run()); }
    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale<=0) throw new InvalidOperationException("Run in unpaused Play Mode.");
        string saved = PlayerPrefs.GetString(CheckpointService.SaveKey), unlocked = PlayerPrefs.GetString(CheckpointService.UnlockSaveKey);
        bool hadSaved = PlayerPrefs.HasKey(CheckpointService.SaveKey), hadUnlocked = PlayerPrefs.HasKey(CheckpointService.UnlockSaveKey);
        var checks = new List<string>();
        var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"),new Vector3(10000,10000),Quaternion.identity);
        player.tag = "Untagged";
        var statue = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/CheckpointStatue.prefab"),player.transform.position,Quaternion.identity);
        var checkpoint = statue.GetComponent<CheckpointInteractable>(); checkpoint.checkpointId="prayer-only-rest-regression";
        statue.GetComponent<InGameCutscene>().enabled=false;
        var menu = statue.GetComponent<CheckpointMenu>();
        try
        {
            var actor = player.GetComponent<PlayerInteractor>() ?? player.AddComponent<PlayerInteractor>();
            var stats = player.GetComponent<CharacterStats>();
            var potion = player.GetComponent<PlayerHealthPotion>();
            var debuffs = player.GetComponent<CharacterDebuffs>();
            stats.currentHP=42; stats.currentStamina=17;
            potion.RestoreTravelState(1,2);
            debuffs.ApplyBurn(20,1); debuffs.ApplyPoison(20,1); debuffs.ApplyStress(20);
            int healed = 0; player.GetComponent<CharacterHealth>().Healed += amount=>healed++;
            Check(checkpoint.TryInteract(actor),"First statue activation still succeeds",checks);
            Check(CheckpointService.IsUnlocked(statue.scene.path,checkpoint.checkpointId),"First activation unlocks the statue",checks);
            Check(stats.currentHP==42 && stats.currentStamina==17 && potion.Charges==1,"First activation does not refill HP, stamina or potions",checks);
            Check(debuffs.IsBurning && debuffs.IsPoisoned && debuffs.IsStressed && healed==0,"First activation does not cleanse or trigger a healing animation",checks);
            Check(checkpoint.TryInteract(actor) && menu.IsOpen && Time.timeScale==0,"Revisiting still opens the menu",checks);
            Check(stats.currentHP==42 && stats.currentStamina==17 && potion.Charges==1 && healed==0,"Opening the menu does not heal or refill resources",checks);
            Check(debuffs.IsBurning && debuffs.IsPoisoned && debuffs.IsStressed,"Opening the menu preserves all debuffs",checks);
            menu.SelectSection(3);
            Check(stats.currentHP==42 && potion.Charges==1,"Selecting Prayer alone does not restore resources",checks);
            Check(menu.Pray(),"Confirming Prayer succeeds",checks);
            Check(menu.IsPraying&&stats.currentHP==42&&stats.currentStamina==17&&potion.Charges==1,"Prayer waits for fade before restoring resources",checks);
            Check(debuffs.IsBurning&&debuffs.IsPoisoned&&debuffs.IsStressed&&healed==0,"Prayer does not clear debuffs before reaching black; full restoration checked by CheckpointPrayerRegression",checks);
            menu.Close();
            Check(Time.timeScale==1 && player.GetComponent<PlayerController>().enabled,"Closing returns control normally",checks);
            Directory.CreateDirectory("Library/CheckpointTests");
            string result="PASS\n"+string.Join("\n",checks); File.WriteAllText("Library/CheckpointTests/rest-results.txt",result); return result;
        }
        finally
        {
            menu.Close(); player.SetActive(false); statue.SetActive(false); Object.Destroy(player); Object.Destroy(statue);
            if(hadSaved) PlayerPrefs.SetString(CheckpointService.SaveKey,saved); else PlayerPrefs.DeleteKey(CheckpointService.SaveKey);
            if(hadUnlocked) PlayerPrefs.SetString(CheckpointService.UnlockSaveKey,unlocked); else PlayerPrefs.DeleteKey(CheckpointService.UnlockSaveKey);
            PlayerPrefs.Save(); Time.timeScale=1;
        }
    }
    private static void Check(bool condition,string message,List<string> checks)
    {
        if(!condition) throw new InvalidOperationException(message);
        checks.Add("PASS: "+message);
    }
}
