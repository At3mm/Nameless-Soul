using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using World;
using Object = UnityEngine.Object;

/// <summary>Integration checks with real character/checkpoint prefabs in disposable scenes.</summary>
[InitializeOnLoad]
public static class CheckpointPrayerRegression
{
    private const string Pending = "CheckpointPrayerRegression.Pending";
    private static IEnumerator routine;
    private static readonly List<string> checks = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static double deadline;
    private static int lastFrame = -1;
    private static Scene scene, otherScene;
    private static CheckpointMenu menu;
    private static Scene menuScene;
    private static HashSet<int> menuExistingRoots;

    static CheckpointPrayerRegression()
    {
        if (SessionState.GetBool(Pending, false)) BeginPolling();
    }

    // Unity -batchmode -projectPath ... -executeMethod CheckpointPrayerRegression.RunBatch
    public static void RunBatch()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true);
        BeginPolling();
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Tools/Diagnostics/Test Checkpoint Prayer (Play Mode)")]
    public static void RunMenu()
    {
        if (!Application.isPlaying || Time.timeScale <= 0) throw new InvalidOperationException("Run in unpaused Play Mode.");
        SessionState.SetBool(Pending, true);
        BeginPolling();
    }

    private static void BeginPolling()
    {
        deadline = EditorApplication.timeSinceStartup + 180;
        lastFrame = -1;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= Poll;
        EditorApplication.update += Poll;
    }

    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message + "\n" + trace);
    }

    private static void Poll()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Prayer regression timed out.");
            if (!Application.isPlaying || EditorApplication.isCompiling) return;
            if (Time.frameCount == lastFrame) return;
            lastFrame = Time.frameCount;
            if (routine == null) routine = Run();
            if (!routine.MoveNext())
            {
                Check(errors.Count == 0, "No Unity errors during integration checks: " + string.Join("\n", errors));
                Finish(null);
            }
        }
        catch (Exception error) { Finish(error); }
    }

    private static void Finish(Exception error)
    {
        EditorApplication.update -= Poll;
        Application.logMessageReceived -= OnLog;
        SessionState.SetBool(Pending, false);
        if (menu != null) menu.Close();
        Time.timeScale = 1;
        if(menuExistingRoots!=null&&menuScene.IsValid()&&menuScene.isLoaded)
            foreach(var root in menuScene.GetRootGameObjects())if(!menuExistingRoots.Contains(root.GetInstanceID())){root.SetActive(false);Object.Destroy(root);}
        menuExistingRoots=null;
        if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
        if (otherScene.IsValid() && otherScene.isLoaded) SceneManager.UnloadSceneAsync(otherScene);
        string result = (error == null ? "PASS" : "FAIL: " + error) + "\n" + string.Join("\n", checks);
        Directory.CreateDirectory("Library/CheckpointTests");
        File.WriteAllText("Library/CheckpointTests/prayer-results.txt", result);
        routine = null; checks.Clear(); errors.Clear();
        Debug.Log(result);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks.Add("PASS: " + message);
    }

    private static GameObject Spawn(string path, Scene target, Vector3 position, Action<GameObject> configure = null)
    {
        var staging = new GameObject("Prayer test staging"); staging.SetActive(false);
        SceneManager.MoveGameObjectToScene(staging, target);
        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), staging.transform, false);
        instance.SetActive(false);
        instance.transform.SetParent(null, true);
        instance.transform.position = position;
        configure?.Invoke(instance);
        instance.SetActive(true);
        var reward = instance.GetComponent<EnemyCurrencyReward>(); if (reward != null) reward.rewardsEnabled = false;
        Object.Destroy(staging);
        return instance;
    }

    private static EnemyClassification[] Normals(Scene target)
    {
        return target.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyClassification>(true))
            .Where(e => e.gameObject.activeInHierarchy && e.enemyType == EnemyClassification.EnemyType.Normal).ToArray();
    }

    private static IEnumerator Run()
    {
        scene = SceneManager.CreateScene("Prayer regression");
        otherScene = SceneManager.CreateScene("Prayer other area");
        const string orc = "Assets/Prefabs/Characters/Orc.prefab";
        const string elitePath = "Assets/Prefabs/Characters/EliteOrc.prefab";
        Vector3 origin = new Vector3(10000, 10000, 0);
        var living = Spawn(orc, scene, origin, go => go.GetComponent<CharacterStats>().maxHP = 137);
        var dead = Spawn(orc, scene, origin + Vector3.right * 5);
        var elite = Spawn(elitePath, scene, origin + Vector3.right * 10);
        var boss = Spawn(elitePath, scene, origin + Vector3.right * 15, go => go.GetComponent<EnemyClassification>().enemyType = EnemyClassification.EnemyType.Boss);
        var deadElite = Spawn(elitePath, scene, origin + Vector3.right * 20);
        var deadBoss = Spawn(elitePath, scene, origin + Vector3.right * 25, go => go.GetComponent<EnemyClassification>().enemyType = EnemyClassification.EnemyType.Boss);
        var outside = Spawn(orc, otherScene, origin);
        yield return null; yield return null;

        living.transform.position += Vector3.up * 12;
        living.GetComponent<CharacterStats>().currentHP = 23;
        living.GetComponent<EnemyBreak>().ReceiveHit(10000);
        living.GetComponent<EnemyBehavior>().currentState = EnemyBehavior.OrcState.Attack;
        var debuffs = living.GetComponent<CharacterDebuffs>() ?? living.AddComponent<CharacterDebuffs>();
        debuffs.ApplyStress(10);
        elite.GetComponent<CharacterStats>().currentHP = 41;
        elite.GetComponent<EnemyBreak>().ReceiveHit(10000);
        boss.GetComponent<CharacterStats>().currentHP = 52;
        outside.GetComponent<CharacterStats>().currentHP = 29;
        dead.GetComponent<CharacterHealth>().TakeDamageOverTime(10000);
        deadElite.GetComponent<CharacterHealth>().TakeDamageOverTime(10000);
        deadBoss.GetComponent<CharacterHealth>().TakeDamageOverTime(10000);
        Check(dead.GetComponent<CharacterHealth>().isDead, "Normal enemy enters real death state");
        Object.Destroy(dead); Object.Destroy(deadElite); Object.Destroy(deadBoss);
        yield return null; yield return null;
        Check(dead == null && deadElite == null && deadBoss == null, "Corpses are destroyed before prayer");

        Time.timeScale = 0;
        int restored = NormalEnemyRespawns.ResetNormals(scene);
        Check(restored == 2, "Exactly two normal spawns restored while paused");
        var normals = Normals(scene);
        Check(normals.Length == 2, "Inactive snapshots and replaced enemies are excluded from active population");
        var fresh = normals.Single(e => e.name == living.name && (e.transform.position - origin).sqrMagnitude < .01f);
        Check(fresh.gameObject != living && fresh.Stats.currentHP == 137 && fresh.Stats.maxHP == 137, "Initial spawn position and scene-specific HP override restored");
        Check(!fresh.Health.isDead && fresh.GetComponent<Collider2D>().enabled && fresh.GetComponent<Rigidbody2D>().bodyType == RigidbodyType2D.Dynamic, "Respawned enemy is alive with working collider and dynamic body");
        Check(!fresh.GetComponent<EnemyBreak>().IsBroken && fresh.Stats.currentStamina == fresh.Stats.maxStamina && fresh.AI.currentState == EnemyBehavior.OrcState.Patrol, "Stamina, break and AI reset");
        Check(fresh.GetComponent<CharacterDebuffs>() == null || !fresh.GetComponent<CharacterDebuffs>().IsStressed, "Temporary debuffs cleared");
        Check(elite.GetComponent<CharacterStats>().currentHP == 41 && elite.GetComponent<EnemyBreak>().IsBroken && boss.GetComponent<CharacterStats>().currentHP == 52, "Living Elite and Boss health/break state untouched");
        Check(!scene.GetRootGameObjects().Any(go => go.GetComponent<EnemyClassification>() != null && go.GetComponent<EnemyClassification>().enemyType != EnemyClassification.EnemyType.Normal && go != elite && go != boss), "Destroyed Elite and Boss are not revived");
        Check(outside.GetComponent<CharacterStats>().currentHP == 29, "Other scene untouched");
        Check(NormalEnemyRespawns.ResetNormals(scene) == 2 && Normals(scene).Length == 2, "Repeated prayer replaces normals without duplicates");
        yield return null; yield return null;
        Check(Normals(scene).Length == 2, "New instances do not register extra spawn snapshots");

        // Checkpoint saving requires a real built scene. Keep low-level respawn fixtures separate.
        menuScene=SceneManager.GetActiveScene();menuExistingRoots=new HashSet<int>(menuScene.GetRootGameObjects().Select(r=>r.GetInstanceID()));
        var prayerNormal=Spawn(orc,menuScene,origin+Vector3.right*200,go=>go.name="Prayer integration normal");
        prayerNormal.GetComponent<EnemyBehavior>().enabled=false;prayerNormal.GetComponent<CharacterStats>().currentHP=17;
        var player = Spawn("Assets/Prefabs/Characters/Player.prefab", menuScene, origin - Vector3.right * 20);
        var actor = player.GetComponent<PlayerInteractor>() ?? player.AddComponent<PlayerInteractor>();
        var statue = Spawn("Assets/Prefabs/World/CheckpointStatue.prefab", menuScene, player.transform.position + Vector3.right);
        menu = statue.GetComponent<CheckpointMenu>();
        Time.timeScale = 1;
        yield return null; yield return null;
        Check(menu.Open(actor) && Time.timeScale == 0, "Real checkpoint menu opens and pauses");
        Check(!menu.Pray(), "Prayer cannot execute from another section");
        var before = Normals(scene).Select(e => e.GetInstanceID()).ToArray();
        menu.SelectSection(3);
        Check(before.SequenceEqual(Normals(scene).Select(e => e.GetInstanceID())), "Selecting/hovering Prayer does not reset enemies");
        Check(menu.sectionIcons.Length >= 4 && menu.sectionIcons[3] != null, "Prayer uses a native UI icon");
        var playerStats = player.GetComponent<CharacterStats>(); playerStats.currentHP = 10; playerStats.currentStamina = 2;
        var playerDebuffs = player.GetComponent<CharacterDebuffs>(); playerDebuffs.ApplyStress(10);
        var button = menu.GetComponentsInChildren<Button>(true).Single(b => b.name == "PerformPrayer");
        Check(button.gameObject.activeInHierarchy && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == button.gameObject, "Explicit prayer button is available to mouse and keyboard submit");
        button.onClick.Invoke();
        Check(menu.IsPraying && playerStats.currentHP == 10, "Prayer begins fading before restoring player");
        Check(!menu.Pray(), "Repeated prayer is blocked during fade");
        // Leave headroom for cold asset/shader loading during Editor integration runs.
        float prayerDeadline = Time.realtimeSinceStartup + 15;
        bool sawBlack = false;
        while (menu.IsPraying && Time.realtimeSinceStartup < prayerDeadline)
        {
            var overlay = menu.transform.Find("Checkpoint Menu/Prayer screen fade");
            if (overlay != null && overlay.GetComponent<Image>().color.a >= .999f)
            {
                sawBlack = true;
                Check(overlay.GetComponentInParent<CanvasGroup>().alpha == 1, "Prayer overlay reaches full screen opacity");
            }
            yield return null;
        }
        yield return null;
        Check(sawBlack, "Prayer reaches black before fading back in");
        Check(!menu.IsPraying && menu.transform.Find("Checkpoint Menu/Prayer screen fade") == null, "Prayer fade finishes and removes input blocker while paused");
        Check(playerStats.currentHP == playerStats.maxHP && playerStats.currentStamina == playerStats.maxStamina && !playerDebuffs.IsStressed, "Prayer restores player HP/stamina and clears debuffs");
        Check(menu.IsOpen && Time.timeScale == 0 && Normals(menuScene).Any(e=>e.name=="Prayer integration normal"&&e.Stats.currentHP==e.Stats.maxHP), "Confirming prayer resets normals and keeps checkpoint menu open");
        menu.Close();
        Check(Time.timeScale == 1 && player.GetComponent<PlayerController>().enabled && !menu.IsOpen, "Closing restores gameplay and player controls");
        Check(CheckpointMenu.BlocksPauseMenu, "Closing frame blocks accidental pause menu");
        yield return null; yield return null; yield return null;
        Check(!CheckpointMenu.BlocksPauseMenu, "Pause guard releases after closing");
        Check(Normals(scene).Length == 2, "Population stable after AI and runtime UI initialize");
    }
}
