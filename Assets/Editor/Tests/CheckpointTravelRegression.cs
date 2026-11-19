using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using World;
using Object = UnityEngine.Object;

/// <summary>Travel integration test. Restores the user's checkpoint PlayerPrefs on completion.</summary>
public static class CheckpointTravelRegression
{
    private static IEnumerator routine;
    private static int lastFrame;
    private static double deadline;
    private static readonly List<string> checks = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static string saved, unlocked;
    private static bool hadSaved, hadUnlocked;
    private static CheckpointMenu menu;
    private static readonly List<GameObject> mapEnemies=new List<GameObject>();

    [MenuItem("Tools/Diagnostics/Test Checkpoint Travel (Play Mode)")]
    public static void RunMenu()
    {
        if (!Application.isPlaying || Time.timeScale <= 0 || routine != null) throw new InvalidOperationException("Run once in unpaused Play Mode.");
        hadSaved = PlayerPrefs.HasKey(CheckpointService.SaveKey); hadUnlocked = PlayerPrefs.HasKey(CheckpointService.UnlockSaveKey);
        saved = PlayerPrefs.GetString(CheckpointService.SaveKey); unlocked = PlayerPrefs.GetString(CheckpointService.UnlockSaveKey);
        Directory.CreateDirectory("Library/CheckpointTests");
        File.WriteAllText("Library/CheckpointTests/travel-save-backup.json", JsonUtility.ToJson(new Backup { hadSaved=hadSaved, hadUnlocked=hadUnlocked, saved=saved, unlocked=unlocked }));
        checks.Clear(); errors.Clear(); lastFrame = -1;
        deadline = EditorApplication.timeSinceStartup + 100;
        routine = Run();
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Poll;
    }

    [Serializable] private sealed class Backup { public bool hadSaved, hadUnlocked; public string saved, unlocked; }
    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + trace);
    }
    private static void Poll()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Travel regression timed out.");
            if (!Application.isPlaying) throw new InvalidOperationException("Play Mode was stopped during travel tests.");
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            if (!routine.MoveNext()) { Check(errors.Count == 0,"No Unity errors: " + string.Join("\n",errors)); Finish(null); }
        }
        catch (Exception error) { Finish(error); }
    }
    private static void Finish(Exception error)
    {
        EditorApplication.update -= Poll;
        Application.logMessageReceived -= OnLog;
        if (menu != null) menu.Close();
        foreach(var enemy in mapEnemies)if(enemy!=null)enemy.SetActive(true);mapEnemies.Clear();
        Time.timeScale = 1;
        if (hadSaved) PlayerPrefs.SetString(CheckpointService.SaveKey,saved); else PlayerPrefs.DeleteKey(CheckpointService.SaveKey);
        if (hadUnlocked) PlayerPrefs.SetString(CheckpointService.UnlockSaveKey,unlocked); else PlayerPrefs.DeleteKey(CheckpointService.UnlockSaveKey);
        PlayerPrefs.Save();
        var result = (error == null ? "PASS" : "FAIL: " + error) + "\n" + string.Join("\n",checks);
        File.WriteAllText("Library/CheckpointTests/travel-results.txt",result);
        routine = null;
        Debug.Log(result);
    }
    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        checks.Add("PASS: " + label);
    }
    private static void IsolateFromMapCombat()
    {
        // Travel UI is tested separately from combat locks. Keep authored map actors out of this fixture.
        foreach(var enemy in Object.FindObjectsByType<EnemyClassification>(FindObjectsSortMode.None))
            if(enemy.gameObject.scene==SceneManager.GetActiveScene()&&enemy.gameObject.activeSelf)
            {mapEnemies.Add(enemy.gameObject);enemy.gameObject.SetActive(false);}
    }
    private static PlayerInteractor Actor()
    {
        var actor = Object.FindFirstObjectByType<PlayerInteractor>();
        actor.GetComponent<CharacterInvulnerability>()?.GrantInvulnerability(10000);
        actor.GetComponent<AnimationEventHandler>()?.UnlockAction();
        return actor;
    }
    private static CheckpointInteractable SpawnStatue(Vector3 position, string id, string title)
    {
        var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/CheckpointStatue.prefab"),position,Quaternion.identity);
        var statue = go.GetComponent<CheckpointInteractable>(); statue.checkpointId = id;
        go.GetComponent<CheckpointMenu>().checkpointName = title;
        return statue;
    }
    private static void Unlock(CheckpointInteractable statue)
    {
        CheckpointService.Save(statue.gameObject.scene.path,statue.checkpointId,statue.RespawnPosition,statue.GetComponent<CheckpointMenu>().checkpointName);
    }
    private static GameObject SpawnEnemy(string path,Vector3 position,string name)
    {
        var staging = new GameObject("Inactive travel enemy staging"); staging.SetActive(false);
        var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),staging.transform,false);
        enemy.SetActive(false); enemy.transform.SetParent(null,false); enemy.transform.position=position; enemy.name=name; enemy.SetActive(true);
        enemy.GetComponent<EnemyCurrencyReward>().rewardsEnabled=false;
        enemy.GetComponent<EnemyBehavior>().enabled=false;
        Object.Destroy(staging); return enemy;
    }
    private static IEnumerator Run()
    {
        Check(SceneManager.GetActiveScene().path == "Assets/Scenes/WastelandScene.unity","Start in the real Wasteland scene");
        IsolateFromMapCombat();
        var source = Object.FindFirstObjectByType<CheckpointInteractable>();
        string sourcePath = source.gameObject.scene.path, sourceId = source.checkpointId;
        var actor = Actor();
        actor.transform.position = new Vector3(10000,10000,0);
        CheckpointService.ClearSaved();
        Check(!CheckpointService.CanTravelTo(sourcePath,sourceId),"Locked statues reject travel");
        Check(!CheckpointService.CanTravelTo("Assets/Scenes/Missing.unity","none"),"Invalid scenes reject travel");
        // The old single-checkpoint save must still produce a usable unlocked destination.
        var legacy = new CheckpointService.Record { scenePath=sourcePath,checkpointId=sourceId,x=source.RespawnPosition.x,y=source.RespawnPosition.y };
        PlayerPrefs.SetString(CheckpointService.SaveKey,JsonUtility.ToJson(legacy));
        Check(CheckpointService.GetTravelDestinations("","").Any(d=>d.checkpointId==sourceId),"Legacy single-checkpoint save migrates into travel list");
        Unlock(source);
        menu = source.GetComponent<CheckpointMenu>();
        yield return null; yield return null;
        while (ScreenFader.Instance.IsTransitioning) yield return null;
        float readyDeadline=Time.realtimeSinceStartup+25;
        while((InGameCutscene.AnyPlaying||!actor.CanUseCheckpoint)&&Time.realtimeSinceStartup<readyDeadline)yield return null;
        Check(menu.Open(actor),"Checkpoint menu opens (canUse="+actor.CanUseCheckpoint+", cutscene="+InGameCutscene.AnyPlaying+", combat="+actor.IsInCombat+", scale="+Time.timeScale+")");
        menu.SelectSection(4); menu.OpenTravelList();
        Check(menu.IsChoosingDestination && !menu.TravelToSelected(),"Current statue stays visible but cannot be confirmed");
        Check(!menu.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Du hành  [E]").interactable,"Current-statue confirm is disabled");
        menu.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Quay lại  [Esc]").onClick.Invoke();
        Check(menu.IsOpen && !menu.IsChoosingDestination && Time.timeScale==0,"Back returns to checkpoint menu without unpausing");
        menu.Close();

        var statues = new List<CheckpointInteractable>();
        for (int i=0;i<7;i++)
        {
            var statue = SpawnStatue(new Vector3(10000+i*4,10000,0),"travel-test-"+i,"Tượng du hành "+(i+1));
            statues.Add(statue); Unlock(statue);
        }
        Unlock(statues[0]); Unlock(source);
        var destinations = CheckpointService.GetTravelDestinations(sourcePath,sourceId);
        Check(destinations.Count==7,"Unlocked list excludes current statue and deduplicates saves");
        Check(destinations.All(d=>d.displayName.StartsWith("Tượng du hành") && d.Area=="Wasteland"),"Destination names and areas are shown");
        Check(menu.Open(actor),"Menu reopens after cancel");
        menu.SelectSection(4); menu.OpenTravelList();
        menu.SelectDestination(6);
        Check(menu.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>()!=null && b.GetComponentInChildren<Text>().text.Contains("Tượng du hành 7")),"Destinations beyond first page are reachable");
        menu.SelectDestination(CheckpointService.GetTravelDestinations(sourcePath,sourceId,true).FindIndex(d=>d.checkpointId==statues[0].checkpointId));
        for (int i=0;i<20;i++) yield return null;
        ScreenCapture.CaptureScreenshot("Library/CheckpointTests/travel-list.png");
        yield return null;
        Vector3 enemyHome=new Vector3(30000,30000);
        var wounded=SpawnEnemy("Assets/Prefabs/Characters/Orc.prefab",enemyHome,"Travel wounded normal");
        var deadNormal=SpawnEnemy("Assets/Prefabs/Characters/Orc.prefab",enemyHome+Vector3.right*5,"Travel dead normal");
        var special=SpawnEnemy("Assets/Prefabs/Characters/EliteOrc.prefab",enemyHome+Vector3.right*10,"Travel special exception");
        wounded.transform.position+=Vector3.up*8;
        wounded.GetComponent<CharacterStats>().currentHP=17; wounded.GetComponent<EnemyBreak>().ReceiveHit(10000);
        deadNormal.GetComponent<CharacterHealth>().TakeDamageOverTime(100000);
        special.GetComponent<CharacterStats>().currentHP=33;
        Object.Destroy(deadNormal);
        var stats = actor.GetComponent<CharacterStats>(); stats.currentHP=12; stats.currentStamina=3;
        actor.GetComponent<PlayerHealthPotion>().RestoreTravelState(2,3);
        actor.GetComponent<CharacterDebuffs>().ApplyStress(20);
        actor.GetComponent<CharacterBuffs>().Apply(CharacterBuffs.Kind.Damage,20,25);
        foreach (var skill in actor.GetComponent<SkillDamageManager>().skills) skill.lastUsedTime=Time.time;
        Check(menu.TravelToSelected(),"Confirm starts same-scene travel");
        Check(CheckpointService.IsTraveling && Time.timeScale==0 && !menu.IsOpen,"Travel closes menu and freezes gameplay during fade");
        Check(!CheckpointService.TravelTo(sourcePath,sourceId,actor),"Duplicate travel requests are rejected");
        var pause = Object.FindFirstObjectByType<SettingsMenuToggle>();
        if (pause!=null) { pause.SetOpen(true); Check(!pause.IsOpen,"Pause cannot open during travel"); }
        while (CheckpointService.IsTraveling) yield return null;
        Check(actor.gameObject.scene.path==sourcePath && Vector3.Distance(actor.transform.position,statues[0].RespawnPosition)<.1f,"Same-scene travel arrives at destination RespawnPoint");
        Check(CheckpointService.ReadSaved().checkpointId==statues[0].checkpointId,"Arrival becomes the new respawn checkpoint");
        Check(Time.timeScale==1 && actor.GetComponent<PlayerController>().enabled && stats.currentHP==stats.maxHP && stats.currentStamina==stats.maxStamina,"Travel restores gameplay and refills HP and stamina");
        Check(actor.GetComponent<PlayerHealthPotion>().Charges==actor.GetComponent<PlayerHealthPotion>().capacity && actor.GetComponent<PlayerHealthPotion>().CooldownRemaining==0 && !actor.GetComponent<CharacterDebuffs>().IsStressed,"Same-scene travel refills potions, resets cooldown and clears debuffs");
        Check(!actor.GetComponent<CharacterBuffs>().Has(CharacterBuffs.Kind.Damage) && actor.GetComponent<SkillDamageManager>().skills.All(s=>Time.time+.001f>=s.lastUsedTime+s.cooldownTime),"Same-scene travel resets temporary buffs and skill cooldowns");
        var resetNormal=Object.FindObjectsByType<EnemyClassification>(FindObjectsSortMode.None).Single(e=>e.name=="Travel wounded normal");
        Check(resetNormal.Stats.currentHP==resetNormal.Stats.maxHP && !resetNormal.GetComponent<EnemyBreak>().IsBroken && Vector3.Distance(resetNormal.transform.position,enemyHome)<.2f,"Same-scene travel resets a damaged/broken normal enemy at its initial position");
        Check(Object.FindObjectsByType<EnemyClassification>(FindObjectsSortMode.None).Any(e=>e.name=="Travel dead normal" && !e.Health.isDead),"Same-scene travel revives a destroyed normal corpse");
        Check(special!=null && special.GetComponent<CharacterStats>().currentHP==33,"Same-scene travel keeps the existing Elite exception");
        Check(CheckpointService.IsUnlocked(sourcePath,sourceId),"Previous statue remains unlocked");

        // Test a real scene change into the existing, serialized Wasteland statue.
        var load = SceneManager.LoadSceneAsync("Assets/Scenes/CaveScene.unity");
        while (!load.isDone) yield return null;
        yield return null; yield return null;
        IsolateFromMapCombat();
        actor = Actor(); actor.transform.position = new Vector3(10000,10000,0);
        var caveStatue = SpawnStatue(actor.transform.position+Vector3.right,"travel-cave-test","Tượng hang thử nghiệm");
        Unlock(caveStatue);
        menu = caveStatue.GetComponent<CheckpointMenu>();
        Check(menu.Open(actor),"Checkpoint menu works in another scene");
        menu.SelectSection(4); menu.OpenTravelList();
        destinations = CheckpointService.GetTravelDestinations(caveStatue.gameObject.scene.path,caveStatue.checkpointId,true);
        int index = destinations.FindIndex(d=>d.scenePath==sourcePath && d.checkpointId==sourceId);
        Check(index>=0,"Previously unlocked statue remains listed after changing scene");
        menu.SelectDestination(index);
        actor.GetComponent<CharacterStats>().currentHP=23;
        actor.GetComponent<CharacterStats>().currentStamina=4;
        actor.GetComponent<PlayerHealthPotion>().RestoreTravelState(1,4);
        var status = actor.GetComponent<CharacterDebuffs>();
        status.ApplyBurn(20,.01f); status.ApplyPoison(20,.01f); status.ApplyStress(20);
        Check(menu.TravelToSelected(),"Confirm starts cross-scene travel");
        while (CheckpointService.IsTraveling) yield return null;
        actor = Actor();
        source = Object.FindObjectsByType<CheckpointInteractable>(FindObjectsSortMode.None).Single(s=>s.checkpointId==sourceId);
        Check(SceneManager.GetActiveScene().path==sourcePath && Vector3.Distance(actor.transform.position,source.RespawnPosition)<.1f,"Cross-scene travel resolves the real target statue and spawn position");
        Check(CheckpointService.ReadSaved().checkpointId==sourceId && CheckpointService.IsUnlocked("Assets/Scenes/CaveScene.unity","travel-cave-test"),"Cross-scene arrival updates respawn without losing unlocks");
        Check(Time.timeScale==1 && actor.GetComponent<PlayerController>().enabled && !CheckpointMenu.AnyOpen,"Cross-scene fade restores controls and clears all menu locks");
        Check(actor.GetComponent<CharacterStats>().currentHP==actor.GetComponent<CharacterStats>().maxHP && actor.GetComponent<CharacterStats>().currentStamina==actor.GetComponent<CharacterStats>().maxStamina,"Cross-scene travel refills HP and stamina");
        Check(actor.GetComponent<PlayerHealthPotion>().Charges==actor.GetComponent<PlayerHealthPotion>().capacity && actor.GetComponent<PlayerHealthPotion>().CooldownRemaining==0,"Cross-scene travel refills potions and resets cooldown");
        status = actor.GetComponent<CharacterDebuffs>();
        Check(!status.IsBurning && !status.IsPoisoned && !status.IsStressed,"Cross-scene travel clears all active debuffs");
    }
}
