using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Isolated broad gameplay checks, with save restoration on every exit.</summary>
[InitializeOnLoad]
public static class ProjectPlayRegression
{
    const string Pending = "ProjectPlayRegression.Pending";
    const string Profiles = "Assets/Effects/Audio/Enemies/Profiles/";
    static readonly string[] Actors = { "Orc", "EliteOrc", "OrcRider", "Slime", "Werewolf", "Werebear", "Necromancer", "NecromancerBat", "Skeleton", "ArmorSkeleton", "GreatswordSkeleton", "BlackKnight", "BlackKnightBoss" };
    static readonly string[] Keys = { World.CheckpointService.SaveKey, World.CheckpointService.UnlockSaveKey, GameEconomy.SaveKey, EnemyDefeatState.SaveKey, BossItemInventory.SaveKey, QuestService.SaveKey, GameplayTutorial.SaveKey, "Noname.PreviousRun.v1" };
    static readonly List<string> checks = new List<string>();
    static readonly List<string> errors = new List<string>();
    static IEnumerator routine;
    static double deadline;
    static int frame = -1;
    static readonly List<GameObject> extras=new List<GameObject>();
    static readonly List<string> failures=new List<string>();
    static bool finishing;
    static GameObject actor;

    static ProjectPlayRegression()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (SessionState.GetBool(Pending, false)) StartPolling();
    }

    [MenuItem("Tools/Diagnostics/Test Project Gameplay and Lava")]
    public static void Run()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before running creature checks.");
        foreach (string key in Keys)
        {
            SessionState.SetBool(Pending + ".exists." + key, PlayerPrefs.HasKey(key));
            SessionState.SetString(Pending + ".value." + key, PlayerPrefs.GetString(key, ""));
        }
        var preview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/CaveScene.unity");
        try
        {
            var lava=preview.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LavaZone>(true)).First();
            var map=lava.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            var tile=map.GetTilesBlock(map.cellBounds).First(t=>t!=null);
            SessionState.SetString(Pending+".lavaTile",AssetDatabase.GetAssetPath(tile));
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
        SessionState.SetString(Pending + ".start", EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuController.ScenePath);
        SessionState.SetBool(Pending, true);
        Directory.CreateDirectory("Library/ProjectPlayValidation");
        File.WriteAllText("Library/ProjectPlayValidation/status.txt", "RUNNING");
        StartPolling();
        EditorApplication.EnterPlaymode();
    }
    static void StartPolling()
    {
        deadline = EditorApplication.timeSinceStartup + 240;
        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
        EditorApplication.update -= Poll; EditorApplication.update += Poll;
    }
    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
    }
    static void Poll()
    {
        if (finishing) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Cave creature check timed out.");
            if (!Application.isPlaying || EditorApplication.isCompiling || (routine==null&&Object.FindFirstObjectByType<MainMenuController>()==null) || frame == Time.frameCount) return;
            frame = Time.frameCount;
            if (routine == null) routine = Checks();
            if (!routine.MoveNext()) Finish(null);
        }
        catch (Exception e) { Finish(e); }
    }
    static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
        checks.Add("PASS " + message);
    }
    static GameObject player, priest, target;
    static void ResetTestSave()
    {
        foreach(var key in Keys)PlayerPrefs.DeleteKey(key);
        GameEconomy.ReloadFromSave();EnemyDefeatState.ReloadFromSave();BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();
    }
    static void Case(string name,Func<string> run)
    {
        try{ResetTestSave();Time.timeScale=1;string result=run();checks.Add("PASS "+name+" | "+result.Split('\n')[0]);}
        catch(Exception error){failures.Add(name+": "+error.Message);checks.Add("FAIL "+name+" | "+error);}
        finally{Time.timeScale=1;}
    }
    static string CheckBossMusic()
    {
        var music=GameMusicController.Instance;
        var player=Object.FindFirstObjectByType<PlayerController>();
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/FlameGolem.prefab");
        if(music==null||player==null||prefab==null)throw new InvalidOperationException("Music controller, player, or Flame Golem prefab is missing.");
        var boss=Object.Instantiate(prefab,player.transform.position+Vector3.right*2,Quaternion.identity);
        var health=boss.GetComponent<CharacterHealth>();
        try
        {
            var ai=boss.GetComponent<EnemyBehavior>();
            typeof(EnemyBehavior).GetField("player",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(ai,player.transform);
            ai.currentState=EnemyBehavior.OrcState.Attack;
            music.Evaluate();
            Check(music.CurrentContext==GameMusicController.Context.Boss&&music.CurrentTrack==music.Settings.boss,"Engaged boss selects the boss track");
            health.isDead=true;
            music.Evaluate();
            Check(music.CurrentContext==GameMusicController.Context.Exploration&&music.CurrentTrack==music.Settings.exploration,"Defeated boss returns to exploration music");
            return "Boss and exploration tracks switch with combat state";
        }
        finally
        {
            if(health!=null)health.isDead=true;
            Object.Destroy(boss);
            music.Evaluate();
        }
    }
    static IEnumerator Checks()
    {
        foreach(var key in Keys)PlayerPrefs.DeleteKey(key);
        GameEconomy.ReloadFromSave();QuestService.ReloadFromSave();BossItemInventory.ReloadFromSave();EnemyDefeatState.ReloadFromSave();Time.timeScale=1;
        var grid=new GameObject("Lava wading fixture",typeof(Grid));grid.SetActive(false);grid.transform.position=new Vector3(10000,10000,0);extras.Add(grid);
        var surface=new GameObject("Lava",typeof(UnityEngine.Tilemaps.Tilemap),typeof(UnityEngine.Tilemaps.TilemapRenderer));surface.transform.SetParent(grid.transform,false);
        var map=surface.GetComponent<UnityEngine.Tilemaps.Tilemap>();var tile=AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>(SessionState.GetString(Pending+".lavaTile",""));
        Check(tile!=null,"Real CaveScene lava tile found");
        for(int x=-4;x<=7;x++)for(int y=-3;y<=2;y++)map.SetTile(new Vector3Int(x,y,0),tile);
        var rigid=surface.AddComponent<Rigidbody2D>();rigid.bodyType=RigidbodyType2D.Static;
        var composite=surface.AddComponent<CompositeCollider2D>();composite.geometryType=CompositeCollider2D.GeometryType.Polygons;composite.isTrigger=true;
        var collider=surface.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>();collider.compositeOperation=Collider2D.CompositeOperation.Merge;
        var zone=surface.AddComponent<LavaZone>();surface.AddComponent<WaterSlowZone>();
        var renderer=surface.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>();renderer.sortingLayerName="Lava";
        var unlit=new Material(Shader.Find("Sprites/Default"));renderer.sharedMaterial=unlit;
        grid.SetActive(true);
        var actors=new List<GameObject>();var originals=new List<Material>();
        int index=0;
        foreach(string name in new[]{"Player","LavaSlime","FlameGolem"})
        {
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/"+name+".prefab"));extras.Add(actor);actors.Add(actor);
            actor.transform.position=grid.transform.position+Vector3.right*(index++*2);
            var ai=actor.GetComponent<EnemyBehavior>();if(ai!=null)ai.enabled=false;
            actor.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
            originals.Add(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/"+name+".prefab").GetComponent<SpriteRenderer>().sharedMaterial);
            actor.GetComponent<CharacterStats>().maxHP=actor.GetComponent<CharacterStats>().currentHP=1000;
        }
        var cameraGo=new GameObject("Lava test camera",typeof(Camera));extras.Add(cameraGo);
        var camera=cameraGo.GetComponent<Camera>();camera.transform.position=grid.transform.position+new Vector3(2,0,-10);camera.orthographic=true;camera.orthographicSize=2.5f;camera.depth=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.07f,.05f);
        Physics2D.SyncTransforms();
        float wait=Time.time+.35f;while(Time.time<wait)yield return null;
        foreach(var actor in actors)
        {
            var water=actor.GetComponent<WaterPresence>();var effect=actor.GetComponent<PlayerWaterRipples>();
            Check(effect!=null&&effect.IsInLava&&effect.Immersion>.9f,actor.name+" is visibly immersed in lava");
            Check(actor.GetComponent<SpriteRenderer>().sharedMaterial==WaterEffectDefaults.Current.submergedMaterial,actor.name+" uses feet clipping material");
            Check(water.EnemySpeedMultiplier<1,actor.name+" retains water-style movement slow");
            var ring=actor.GetComponentsInChildren<LineRenderer>().FirstOrDefault(r=>r.enabled);
            Check(ring!=null&&ring.sortingLayerName=="Lava"&&ring.startColor.r>ring.startColor.g,actor.name+" has orange lava ripples");
        }
        Check(actors[0].GetComponent<PlayerController>().WaterSpeedMultiplier<1,"Player already has liquid slow");
        var canvases=Object.FindObjectsByType<UnityEngine.Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();foreach(var canvas in canvases)canvas.enabled=false;
        Directory.CreateDirectory("Library/ProjectPlayValidation");ScreenCapture.CaptureScreenshot("Library/ProjectPlayValidation/LavaWading.png");yield return null;yield return null;
        foreach(var canvas in canvases)if(canvas!=null)canvas.enabled=true;
        wait=Time.time+1.2f;while(Time.time<wait)yield return null;
        Check(actors[0].GetComponent<CharacterDebuffs>()!=null&&actors[0].GetComponent<CharacterDebuffs>().IsBurning,"Player still receives lava burn");
        Check(actors.Skip(1).All(a=>!zone.IsExposing(a.GetComponent<CharacterHealth>())&&(a.GetComponent<CharacterDebuffs>()==null||!a.GetComponent<CharacterDebuffs>().IsBurning)),"Lava-native monsters are protected from environmental lava burn");
        var player=actors[0];var feet=player.GetComponent<Collider2D>();var visual=player.GetComponent<PlayerWaterRipples>();float footDelta=feet.bounds.min.y-player.transform.position.y;
        player.transform.position=new Vector3(grid.transform.position.x,grid.transform.position.y+3-footDelta-visual.footOffset.y-.04f,0);Physics2D.SyncTransforms();
        wait=Time.time+.15f;while(Time.time<wait)yield return null;
        Check(visual.IsInLava&&zone.IsExposing(player.GetComponent<CharacterHealth>()),"Lava classification and burn both follow feet at shoreline");
        for(int i=0;i<actors.Count;i++)actors[i].transform.position=grid.transform.position+new Vector3(15+i*2,0,0);
        Physics2D.SyncTransforms();wait=Time.time+.5f;while(Time.time<wait)yield return null;
        for(int i=0;i<actors.Count;i++)Check(actors[i].GetComponent<PlayerWaterRipples>().Immersion==0&&actors[i].GetComponent<SpriteRenderer>().sharedMaterial==originals[i],actors[i].name+" restores normal rendering after leaving lava");
        foreach(var go in extras)if(go!=null){go.SetActive(false);Object.Destroy(go);}extras.Clear();Object.Destroy(unlit);yield return null;yield return null;
        Case("Stamina",StaminaRegression.Run);yield return null;yield return null;
        var sourcePlayer=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"));sourcePlayer.transform.position=new Vector3(30000,30000,0);yield return null;
        Case("Player action locks",PlayerActionLockRegression.Run);sourcePlayer.SetActive(false);Object.Destroy(sourcePlayer);yield return null;yield return null;
        Case("Player exhaustion stun",PlayerStunRegression.Run);yield return null;yield return null;
        Case("Enemy break",EnemyBreakRegression.Run);yield return null;yield return null;
        Case("Obstructed enemy vision",EnemyVisionRegression.Run);yield return null;yield return null;
        Case("Werewolf teleport",WerewolfTeleportRegression.Run);yield return null;yield return null;
        Case("Checkpoint combat lock",CheckpointCombatRegression.Run);yield return null;yield return null;
        Case("Checkpoint rest",CheckpointRestRegression.Run);yield return null;yield return null;
        Case("Economy and rewards",EconomyRegression.Run);yield return null;yield return null;
        Case("Death drop service and persistence",PlayerDeathDropRegression.Run);yield return null;yield return null;
        ResetTestSave();CheckpointPrayerRegression.RunMenu();
        wait=Time.realtimeSinceStartup+120;
        while(SessionState.GetBool("CheckpointPrayerRegression.Pending",false)&&Time.realtimeSinceStartup<wait)yield return null;
        Check(!SessionState.GetBool("CheckpointPrayerRegression.Pending",false)&&File.ReadAllText("Library/CheckpointTests/prayer-results.txt").StartsWith("PASS"),"Prayer fade, healing and normal respawn integration");yield return null;yield return null;
        ResetTestSave();var load=UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/Scenes/WastelandScene.unity");while(!load.isDone)yield return null;yield return null;yield return null;
        Directory.CreateDirectory("Library/CheckpointTests");File.WriteAllText("Library/CheckpointTests/travel-results.txt","RUNNING");CheckpointTravelRegression.RunMenu();
        wait=Time.realtimeSinceStartup+120;
        while(File.ReadAllText("Library/CheckpointTests/travel-results.txt").StartsWith("RUNNING")&&Time.realtimeSinceStartup<wait)yield return null;
        Check(File.ReadAllText("Library/CheckpointTests/travel-results.txt").StartsWith("PASS"),"Same-scene and cross-scene checkpoint travel");
        wait=Time.realtimeSinceStartup+.5f;while(Time.realtimeSinceStartup<wait)yield return null;
        foreach(var enemy in Object.FindObjectsByType<EnemyClassification>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
        Case("Boss music selection",CheckBossMusic);yield return null;yield return null;
        Case("Pause and action recovery in gameplay",PlayerActionLockRegression.Run);yield return null;yield return null;
        var inventory=Object.FindObjectsByType<InventoryConsumablesView>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
        Check(inventory!=null,"Gameplay inventory exists");GameEconomy.GrantLifeSeedItem(1);inventory.SelectCategory(InventoryConsumablesView.Category.Support);
        Check(inventory.transform.Find("Inventory category tabs").childCount==2,"Inventory exposes exactly two categories");
        inventory.SelectQuestEntry("item:"+LifeSeedItem.Id);Check(!inventory.ActivateInspected(),"Seed cannot be equipped from inventory");
        inventory.SelectQuestEntry("item:"+ConsumableCatalog.ShrineReturnItemId);Check(inventory.ActivateInspected(),"Starter necklace can be equipped from merged item tab");
        var pause=inventory.GetComponentInParent<SettingsMenuToggle>();
        if(pause!=null)
        {
            wait=Time.realtimeSinceStartup+8f;while(Time.realtimeSinceStartup<wait)yield return null;
            pause.SetOpen(true);pause.GetComponent<PauseBookNavigation>().SelectSection(0);
            wait=Time.realtimeSinceStartup+1.2f;while(Time.realtimeSinceStartup<wait)yield return null;
            inventory.SelectQuestEntry("item:"+ConsumableCatalog.ShrineReturnItemId);
            yield return null;ScreenCapture.CaptureScreenshot("Library/ProjectPlayValidation/InventoryCurrent.png");yield return null;yield return null;
            inventory.SelectQuestEntry("item:"+LifeSeedItem.Id);
            var entries=(System.Collections.IEnumerable)typeof(InventoryConsumablesView).GetMethod("PresentedEntries",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(inventory,null);
            var ids=new HashSet<string>();int entryCount=0;foreach(var entry in entries){entryCount++;ids.Add((string)entry.GetType().GetField("id").GetValue(entry));}
            Check(ids.Count==entryCount,"Merged item tab does not duplicate item definitions");
            var detailScroll=inventory.selectionLabel.transform.parent as RectTransform;
            Check(detailScroll!=null&&detailScroll.anchoredPosition.y<15,"Expanded item details remain below heading");
            Check(inventory.selectionLabel.rectTransform.rect.height>detailScroll.rect.height,"Long item content can scroll to quick slots within the panel");
            yield return null;ScreenCapture.CaptureScreenshot("Library/ProjectPlayValidation/LifeSeedCurrent.png");yield return null;yield return null;
            inventory.SelectCategory(InventoryConsumablesView.Category.BossRelics);
            yield return null;ScreenCapture.CaptureScreenshot("Library/ProjectPlayValidation/RelicsCurrent.png");yield return null;yield return null;
            pause.Close();
        }

        if(failures.Count>0)throw new InvalidOperationException(string.Join("; ",failures));
        Check(errors.Count==0,"No runtime errors: "+string.Join("; ",errors));
    }
    static void Finish(Exception error)
    {
        foreach(var go in extras)if(go!=null){go.SetActive(false);Object.Destroy(go);}extras.Clear();
        finishing = true; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
        if(priest!=null)priest.GetComponent<World.NpcInteractable>()?.Close();if(player!=null)Object.Destroy(player);if(priest!=null)Object.Destroy(priest);if(target!=null)Object.Destroy(target); Time.timeScale=1;
        string result = (error == null ? "PASS" : "FAIL: " + error) + "\n" + string.Join("\n", checks);
        File.WriteAllText("Library/ProjectPlayValidation/results.txt", result);
        File.WriteAllText("Library/ProjectPlayValidation/status.txt", error == null ? "PASS" : "FAIL");
        if (error == null) Debug.Log(result); else Debug.LogError(result);
        EditorApplication.ExitPlaymode();
    }
    static void OnPlayMode(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Pending, false)) return;
        foreach (string key in Keys)
        {
            if (SessionState.GetBool(Pending + ".exists." + key, false)) PlayerPrefs.SetString(key, SessionState.GetString(Pending + ".value." + key, ""));
            else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save(); GameEconomy.ReloadFromSave(); EnemyDefeatState.ReloadFromSave(); QuestService.ReloadFromSave(); BossItemInventory.ReloadFromSave();
        string path = SessionState.GetString(Pending + ".start", "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        SessionState.SetBool(Pending, false); routine = null; finishing = false; frame = -1;
        File.AppendAllText("Library/ProjectPlayValidation/status.txt", "\nOriginal save and start scene restored");
        checks.Clear(); errors.Clear();failures.Clear();
    }
}
