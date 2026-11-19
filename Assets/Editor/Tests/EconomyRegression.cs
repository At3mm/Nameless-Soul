using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using World;
using Object = UnityEngine.Object;

public static class EconomyRegression
{
    private static string previewSave;
    private static bool previewHadSave, previewActive;
    private static CheckpointMenu previewMenu;

    [MenuItem("Tools/Diagnostics/Test Gold EXP Economy (Play Mode)")]
    public static void RunMenu() { Debug.Log(Run()); }
    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale <= 0) throw new InvalidOperationException("Run in unpaused Play Mode.");
        bool hadSave = PlayerPrefs.HasKey(GameEconomy.SaveKey);
        string previous = PlayerPrefs.GetString(GameEconomy.SaveKey);
        Directory.CreateDirectory("Library/EconomyTests");
        File.WriteAllText("Library/EconomyTests/save-backup.json",previous);
        var checks = new List<string>();
        var scene = SceneManager.CreateScene("Economy regression");
        CheckpointMenu menu = null;
        try
        {
            PlayerPrefs.DeleteKey(GameEconomy.SaveKey); GameEconomy.ReloadFromSave();
            Check(GameEconomy.Gold==0 && GameEconomy.Experience==0 && GameEconomy.TotalPurchases==0,"New wallet starts empty without a character level",checks);
            Check(GameEconomy.NextUpgradeCost==100 && GameEconomy.Settings.UpgradeCost(1)==160 && GameEconomy.Settings.UpgradeCost(2)==240,"Configured global EXP price curve",checks);
            Check(GameEconomy.AddRewards(100,1000),"Rewards grant both currencies",checks);
            Check(!GameEconomy.AddGold(-1) && !GameEconomy.AddExperience(-1) && !GameEconomy.TrySpendGold(-1),"Negative amounts rejected",checks);
            int coins = GameEconomy.Gold, exp = GameEconomy.Experience;
            Check(!GameEconomy.AddRewards(1,-1) && GameEconomy.Gold==coins,"Invalid reward pair does not partially credit Gold",checks);
            Check(!GameEconomy.AddGold(int.MaxValue) && GameEconomy.Gold==coins,"Integer overflow rejected without changing balance",checks);
            var player = Spawn("Assets/Prefabs/Characters/Player.prefab",scene,new Vector3(10000,10000)); player.tag = "Untagged";
            var stats = player.GetComponent<CharacterStats>();
            float baseHP = stats.maxHP, baseStamina = stats.maxStamina, baseAttack = stats.ATK, baseDefense = stats.DEF;
            Check(GameEconomy.TryUpgrade(GameEconomy.Stat.Health) && GameEconomy.Experience==exp-100 && stats.maxHP==baseHP+10,"Health upgrade consumes EXP and raises maximum HP",checks);
            Check(GameEconomy.Gold==coins && GameEconomy.NextUpgradeCost==160,"Upgrade never spends Gold and raises the shared next price",checks);
            Check(GameEconomy.TryUpgrade(GameEconomy.Stat.Attack) && GameEconomy.Experience==exp-260 && stats.ATK==baseAttack+1 && GameEconomy.NextUpgradeCost==240,"Choosing a different stat uses the global price rather than its own curve",checks);
            Check(!GameEconomy.TryUpgrade((GameEconomy.Stat)999),"Invalid upgrade kind rejected",checks);
            Check(GameEconomy.TrySpendGold(30) && GameEconomy.Gold==70 && GameEconomy.Experience==exp-260,"Gold spending does not consume EXP",checks);
            int delivered = 0;
            Check(!GameEconomy.TryPurchase(999,()=>{delivered++;return true;}) && delivered==0,"Insufficient Gold never grants an item",checks);
            Check(!GameEconomy.TryPurchase(20,()=>false) && GameEconomy.Gold==70,"Failed item delivery never charges Gold",checks);
            bool threw = false;
            try { GameEconomy.TryPurchase(20,()=>throw new InvalidOperationException("Test delivery failure")); }
            catch (InvalidOperationException) { threw = true; }
            Check(threw && GameEconomy.Gold==70,"Delivery exception preserves wallet and releases transaction lock",checks);
            bool nested = true;
            Check(GameEconomy.TryPurchase(20,()=>{ nested=GameEconomy.TrySpendGold(1); delivered++; return true; }) && !nested && delivered==1 && GameEconomy.Gold==50,"Successful purchase charges exactly once and rejects nested wallet mutations",checks);
            GameEconomy.ReloadFromSave();
            Check(GameEconomy.Gold==50 && GameEconomy.Experience==exp-260 && GameEconomy.TotalPurchases==2,"Wallet and stat purchases survive reloading save data",checks);
            Check(stats.maxHP==baseHP+10 && stats.ATK==baseAttack+1,"Reload does not stack upgrade bonuses",checks);
            player.SetActive(false); player.SetActive(true);
            Check(stats.maxHP==baseHP+10 && stats.ATK==baseAttack+1,"Re-enabling player does not duplicate bonuses",checks);
            player.GetComponent<CharacterHealth>().TakeDamageOverTime(100000);
            Check(GameEconomy.Gold==50 && GameEconomy.TotalPurchases==2,"Player death retains currency and purchases",checks);
            player = Spawn("Assets/Prefabs/Characters/Player.prefab",scene,new Vector3(10000,10000)); player.tag = "Untagged";
            stats = player.GetComponent<CharacterStats>();
            Check(stats.maxHP==baseHP+10 && stats.currentHP==stats.maxHP && stats.ATK==baseAttack+1 && stats.maxStamina==baseStamina,"New player instance restores saved stats and full resources",checks);

            foreach (EnemyClassification.EnemyType type in Enum.GetValues(typeof(EnemyClassification.EnemyType)))
            {
                var enemy = Spawn(type==EnemyClassification.EnemyType.Normal ? "Assets/Prefabs/Characters/Orc.prefab" : "Assets/Prefabs/Characters/EliteOrc.prefab",scene,new Vector3(-10000,10000));
                enemy.GetComponent<EnemyClassification>().enemyType = type;
                var reward = enemy.GetComponent<EnemyCurrencyReward>();
                Check(!reward.TryClaim(),type+": living enemy cannot grant death reward",checks);
                coins=GameEconomy.Gold; exp=GameEconomy.Experience;
                GameEconomy.Settings.Rewards(type,out int goldReward,out int expReward);
                enemy.GetComponent<CharacterHealth>().TakeDamageOverTime(100000);
                Check(GameEconomy.Gold==coins+goldReward && GameEconomy.Experience==exp+expReward,type+": actual death grants configured Gold/EXP once",checks);
                Check(!reward.TryClaim() && GameEconomy.Gold==coins+goldReward,type+": duplicate death reward rejected",checks);
            }
            coins=GameEconomy.Gold; exp=GameEconomy.Experience;
            Check(NormalEnemyRespawns.ResetNormals(scene)==1,"Prayer restores only the normal reward-bearing enemy",checks);
            var revived = scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<EnemyClassification>())
                .Single(e=>e.gameObject.activeInHierarchy && e.enemyType==EnemyClassification.EnemyType.Normal);
            revived.Health.TakeDamageOverTime(100000);
            Check(GameEconomy.Gold==coins+GameEconomy.Settings.normalGold && GameEconomy.Experience==exp+GameEconomy.Settings.normalExperience,"Revived normal enemy grants a fresh reward on its next death",checks);

            GameEconomy.AddExperience(1000);
            var actor = player.GetComponent<PlayerInteractor>() ?? player.AddComponent<PlayerInteractor>();
            var statue = Spawn("Assets/Prefabs/World/CheckpointStatue.prefab",scene,player.transform.position);
            menu = statue.GetComponent<CheckpointMenu>();
            Check(menu.Open(actor),"Statue opens outside combat",checks);
            menu.OpenUpgradeList();
            Check(menu.IsChoosingUpgrade,"Stat-upgrade modal opens",checks);
            coins=GameEconomy.Gold; exp=GameEconomy.Experience;
            menu.SelectUpgrade(3);
            Check(GameEconomy.Experience==exp,"Selecting a stat shows preview without spending EXP",checks);
            Check(menu.UpgradeSelected() && GameEconomy.Experience==exp-240 && GameEconomy.Gold==coins && stats.DEF==baseDefense+.5f,"Confirming at the statue upgrades exactly the selected stat at the current global price",checks);
            Check(GameEconomy.NextUpgradeCost==340 && menu.IsOpen && Time.timeScale==0,"Menu stays paused and next EXP price updates immediately",checks);
            var snapshot = JsonUtility.FromJson<Snapshot>(PlayerPrefs.GetString(GameEconomy.SaveKey)); snapshot.experience = 0;
            PlayerPrefs.SetString(GameEconomy.SaveKey,JsonUtility.ToJson(snapshot)); GameEconomy.ReloadFromSave();
            Check(!menu.UpgradeSelected() && menu.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Nâng chỉ số  [E]").interactable&&GameEconomy.Experience==0,"Insufficient EXP prevents upgrading while keeping feedback button available",checks);
            menu.Close();
            Check(Time.timeScale==1 && player.GetComponent<PlayerController>().enabled,"Closing upgrade menu restores gameplay",checks);
            string result = "PASS\n"+string.Join("\n",checks);
            File.WriteAllText("Library/EconomyTests/results.txt",result); return result;
        }
        finally
        {
            if (menu!=null) menu.Close();
            foreach (var root in scene.GetRootGameObjects()) root.SetActive(false);
            SceneManager.UnloadSceneAsync(scene);
            if (hadSave) PlayerPrefs.SetString(GameEconomy.SaveKey,previous); else PlayerPrefs.DeleteKey(GameEconomy.SaveKey);
            PlayerPrefs.Save(); GameEconomy.ReloadFromSave(); Time.timeScale = 1;
        }
    }
    [Serializable] private sealed class Snapshot { public int version,gold,experience,health,stamina,attack,defense; }
    private static GameObject Spawn(string path,Scene scene,Vector3 position)
    {
        var staging = new GameObject("Inactive economy staging"); staging.SetActive(false); SceneManager.MoveGameObjectToScene(staging,scene);
        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),staging.transform,false);
        instance.SetActive(false); instance.transform.SetParent(null,false); instance.transform.position = position;
        var enemy=instance.GetComponent<EnemyClassification>();if(enemy!=null)enemy.persistentId="economy-test:"+Guid.NewGuid().ToString("N");
        instance.SetActive(true);
        Object.Destroy(staging); return instance;
    }
    private static void Check(bool condition,string message,List<string> checks)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks.Add("PASS: "+message);
    }

    public static void OpenPreview()
    {
        if (previewActive || !Application.isPlaying) return;
        previewHadSave = PlayerPrefs.HasKey(GameEconomy.SaveKey); previewSave = PlayerPrefs.GetString(GameEconomy.SaveKey); previewActive = true;
        GameEconomy.AddRewards(120,1200);
        var actor = Object.FindFirstObjectByType<PlayerInteractor>();
        var checkpoint = Object.FindFirstObjectByType<CheckpointInteractable>();
        actor.transform.position = new Vector3(10000,10000); actor.GetComponent<AnimationEventHandler>().UnlockAction();
        previewMenu = checkpoint.GetComponent<CheckpointMenu>(); previewMenu.Open(actor); previewMenu.OpenUpgradeList();
    }
    public static void ClosePreview()
    {
        if (!previewActive) return;
        if (previewMenu!=null) previewMenu.Close();
        if (previewHadSave) PlayerPrefs.SetString(GameEconomy.SaveKey,previewSave); else PlayerPrefs.DeleteKey(GameEconomy.SaveKey);
        PlayerPrefs.Save(); GameEconomy.ReloadFromSave(); previewActive = false;
    }
}
