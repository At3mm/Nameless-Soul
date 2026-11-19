using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using World;
using Object = UnityEngine.Object;

public static class CheckpointCombatRegression
{
    [MenuItem("Tools/Diagnostics/Test Checkpoint Combat Lock (Play Mode)")]
    public static void RunMenu() { Debug.Log(Run()); }

    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale <= 0) throw new InvalidOperationException("Run in unpaused Play Mode.");
        var checks = new List<string>();
        var scene = SceneManager.CreateScene("Checkpoint combat regression");
        CheckpointMenu menu = null;
        string save = PlayerPrefs.GetString(CheckpointService.SaveKey), unlocks = PlayerPrefs.GetString(CheckpointService.UnlockSaveKey);
        bool hadDefeats = PlayerPrefs.HasKey(EnemyDefeatState.SaveKey);
        string defeats = PlayerPrefs.GetString(EnemyDefeatState.SaveKey);
        try
        {
            var player = Spawn("Assets/Prefabs/Characters/Player.prefab",scene,new Vector3(10000,10000));
            player.tag = "Untagged";
            var actor = player.GetComponent<PlayerInteractor>() ?? player.AddComponent<PlayerInteractor>();
            var statue = Spawn("Assets/Prefabs/World/CheckpointStatue.prefab",scene,player.transform.position);
            var checkpoint = statue.GetComponent<CheckpointInteractable>(); menu = statue.GetComponent<CheckpointMenu>();
            var door = new GameObject("Combat door"); SceneManager.MoveGameObjectToScene(door,scene);
            door.transform.position = player.transform.position;
            var interactable = door.AddComponent<EventInteractable>();
            int uses = 0; interactable.onInteract.AddListener(()=>uses++);
            foreach (EnemyClassification.EnemyType type in Enum.GetValues(typeof(EnemyClassification.EnemyType)))
            {
                var enemy = Spawn(type == EnemyClassification.EnemyType.Normal ? "Assets/Prefabs/Characters/Orc.prefab" : "Assets/Prefabs/Characters/EliteOrc.prefab",scene,player.transform.position+Vector3.right*10);
                enemy.GetComponent<EnemyClassification>().enemyType = type;
                var ai = enemy.GetComponent<EnemyBehavior>();
                var breaker = enemy.GetComponent<EnemyBreak>();
                Check(!actor.IsInCombat,type+": distant patrol permits checkpoint",checks);
                enemy.transform.position = player.transform.position+Vector3.right;
                Check(actor.IsInCombat && !actor.CanUseCheckpoint,type+": instant aggro blocks before first AI update",checks);
                enemy.transform.position = player.transform.position+Vector3.right*10;
                typeof(EnemyBehavior).GetField("player",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ai,player.transform);
                foreach (var state in new[] { EnemyBehavior.OrcState.Alert,EnemyBehavior.OrcState.Chase,EnemyBehavior.OrcState.Attack,EnemyBehavior.OrcState.Strafe,EnemyBehavior.OrcState.Retreat })
                {
                    ai.currentState = state;
                    Check(actor.IsInCombat && actor.CanInteract && !actor.CanUseCheckpoint,type+": "+state+" blocks checkpoint only",checks);
                    Check(!checkpoint.TryInteract(actor) && !menu.Open(actor),type+": "+state+" cannot save, heal, trigger cutscene or open menu",checks);
                }
                Check(interactable.TryInteract(actor) && uses>0,type+": ordinary world interactions still work",checks);
                ai.currentState = EnemyBehavior.OrcState.Return;
                Check(!actor.IsInCombat && actor.CanUseCheckpoint,type+": disengaged enemy returning home releases checkpoint",checks);
                Check(menu.Open(actor),type+": menu opens after combat ends",checks);
                ai.currentState = EnemyBehavior.OrcState.Attack;
                menu.SelectSection(3);
                float hp = player.GetComponent<CharacterStats>().currentHP;
                Check(!menu.Pray() && player.GetComponent<CharacterStats>().currentHP==hp,type+": direct prayer call cannot bypass combat lock",checks);
                menu.SelectSection(4); menu.OpenTravelList();
                Check(!menu.IsChoosingDestination,type+": direct travel-list call cannot bypass combat lock",checks);
                menu.Close();
                ai.currentState = EnemyBehavior.OrcState.Return;
                breaker.ReceiveHit(10000);
                Check(actor.IsInCombat && !menu.Open(actor),type+": broken enemy still counts as combat",checks);
                SceneManager.MoveGameObjectToScene(enemy,SceneManager.GetActiveScene());
                Check(!actor.IsInCombat,type+": enemy in another scene does not block",checks);
                SceneManager.MoveGameObjectToScene(enemy,scene);
                enemy.GetComponent<CharacterHealth>().TakeDamageOverTime(10000);
                Check(!actor.IsInCombat && actor.CanUseCheckpoint,type+": dead enemy releases checkpoint immediately",checks);
                enemy.SetActive(false); Object.Destroy(enemy);
            }
            Check(save==PlayerPrefs.GetString(CheckpointService.SaveKey) && unlocks==PlayerPrefs.GetString(CheckpointService.UnlockSaveKey),"Combat interaction attempts never change checkpoint saves or unlocks",checks);
            var result = "PASS\n"+string.Join("\n",checks);
            Directory.CreateDirectory("Library/CheckpointTests"); File.WriteAllText("Library/CheckpointTests/combat-results.txt",result);
            return result;
        }
        finally
        {
            if (menu!=null) menu.Close();
            Time.timeScale = 1;
            foreach (var root in scene.GetRootGameObjects()) root.SetActive(false);
            SceneManager.UnloadSceneAsync(scene);
            if (hadDefeats) PlayerPrefs.SetString(EnemyDefeatState.SaveKey, defeats);
            else PlayerPrefs.DeleteKey(EnemyDefeatState.SaveKey);
            PlayerPrefs.Save();
            EnemyDefeatState.ReloadFromSave();
        }
    }
    private static void Check(bool condition,string message,List<string> checks)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks.Add("PASS: "+message);
    }
    private static GameObject Spawn(string path,Scene scene,Vector3 position)
    {
        var storage = new GameObject("Inactive test staging"); storage.SetActive(false);
        SceneManager.MoveGameObjectToScene(storage,scene);
        var copy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),storage.transform,false);
        copy.SetActive(false); copy.transform.SetParent(null,false); copy.transform.position = position;
        var classification = copy.GetComponent<EnemyClassification>();
        if (classification != null) classification.persistentId = "checkpoint-combat-regression-" + Guid.NewGuid().ToString("N");
        copy.SetActive(true);
        var reward = copy.GetComponent<EnemyCurrencyReward>(); if (reward != null) reward.rewardsEnabled = false;
        Object.Destroy(storage); return copy;
    }
}
