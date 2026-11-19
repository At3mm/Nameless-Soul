using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Opt-in NPC quest flow regression. Original save and start scene are restored.</summary>
[InitializeOnLoad]
public static class CaveCreatureRegression
{
    const string Pending = "CaveCreatureRegression.Pending";
    const string Profiles = "Assets/Effects/Audio/Enemies/Profiles/";
    static readonly string[] Actors = { "Orc", "EliteOrc", "OrcRider", "Slime", "Werewolf", "Werebear", "Necromancer", "NecromancerBat", "Skeleton", "ArmorSkeleton", "GreatswordSkeleton", "BlackKnight", "BlackKnightBoss" };
    static readonly string[] Keys = { World.CheckpointService.SaveKey, World.CheckpointService.UnlockSaveKey, GameEconomy.SaveKey, EnemyDefeatState.SaveKey, BossItemInventory.SaveKey, QuestService.SaveKey, "Noname.PreviousRun.v1" };
    static readonly List<string> checks = new List<string>();
    static readonly List<string> errors = new List<string>();
    static IEnumerator routine;
    static double deadline;
    static int frame = -1;
    static bool finishing;
    static GameObject actor;

    static CaveCreatureRegression()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (SessionState.GetBool(Pending, false)) StartPolling();
    }

    [MenuItem("Tools/Enemies/Test Cave Creatures and Seed Item")]
    public static void Run()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before running creature checks.");
        foreach (string key in Keys)
        {
            SessionState.SetBool(Pending + ".exists." + key, PlayerPrefs.HasKey(key));
            SessionState.SetString(Pending + ".value." + key, PlayerPrefs.GetString(key, ""));
        }
        SessionState.SetString(Pending + ".start", EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuController.ScenePath);
        SessionState.SetBool(Pending, true);
        Directory.CreateDirectory("Library/CaveCreatureValidation");
        File.WriteAllText("Library/CaveCreatureValidation/status.txt", "RUNNING");
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
            if (!Application.isPlaying || EditorApplication.isCompiling || Object.FindFirstObjectByType<MainMenuController>() == null || frame == Time.frameCount) return;
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
    static IEnumerator Checks()
    {
        foreach(var key in Keys)PlayerPrefs.DeleteKey(key);
        EnemyDefeatState.ReloadFromSave();BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();GameEconomy.ReloadFromSave();Time.timeScale=1;
        var seed=LifeSeedItem.Definition;
        Check(seed!=null&&seed.icon!=null&&seed.InventoryCategory==ItemCategory.Support&&!seed.CanUse,"Seed is an editable item with icon, in general Items tab, shrine-only");
        Check(ItemInventory.Count(seed)==0,"Seed wallet initially empty");
        Check(QuestService.GrantItem(seed,2,"seed-test-receipt")&&GameEconomy.LifeSeeds==2&&ItemInventory.Count(seed)==2,"Generic grant credits existing seed wallet");
        Check(!QuestService.GrantItem(seed,2,"seed-test-receipt")&&QuestService.HasItemReceipt("seed-test-receipt"),"Seed pickup receipt prevents duplicate grants");
        GameEconomy.ReloadFromSave();QuestService.ReloadFromSave();
        Check(ItemInventory.Count(seed)==2&&ItemInventory.Owned(ItemCategory.Support).Count(i=>i.itemId==LifeSeedItem.Id)==1,"Reload shows one seed item with saved count");
        Check(!GameEconomy.ToggleCustomQuickItem(seed),"Seed cannot be equipped as a consumable");
        GameEconomy.AddGold(100);
        Check(GameEconomy.TryPurchase(10,()=>QuestService.GrantItem(seed,1))&&GameEconomy.LifeSeeds==3&&GameEconomy.Gold==90,"Optional shop grant uses atomic seed wallet");
        Check(GameEconomy.TrySellItem(5,()=>ItemInventory.Remove(seed,1))&&GameEconomy.LifeSeeds==2&&GameEconomy.Gold==95,"Generic removal updates seed wallet");
        var q=ScriptableObject.CreateInstance<QuestDefinition>();q.questId="seed-test-quest";q.title="Seed test";q.objectives.Add(new QuestDefinition.BossObjective{relic=seed,count=1});q.lifeSeeds=1;q.items.Add(new QuestDefinition.ItemReward{item=seed,count=2});
        Check(QuestService.Accept(q)&&QuestService.Status(q)==QuestStatus.ReadyToReport&&QuestService.TurnIn(q)&&GameEconomy.LifeSeeds==5,"Quest seed item rewards and legacy seed rewards combine without duplicate inventory");
        Check(!QuestService.TurnIn(q)&&GameEconomy.LifeSeeds==5,"Repeated quest turn-in does not duplicate seeds");Object.Destroy(q);
        player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"));player.transform.position=new Vector3(100,100,0);
        var interactor=player.GetComponent<World.PlayerInteractor>();if(interactor==null)interactor=player.AddComponent<World.PlayerInteractor>();
        var controls=player.GetComponent<PlayerController>();if(controls!=null)controls.enabled=false;
        player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
        player.GetComponent<CharacterHealth>().damageInvulnerabilityDuration=0;
        var playerStats=player.GetComponent<CharacterStats>();playerStats.maxHP=playerStats.currentHP=10000;playerStats.DEF=0;
        yield return null;
        var potion=player.GetComponent<PlayerHealthPotion>();
        Check(potion!=null&&potion.capacity==3,"New game still starts with three healing flasks");
        GameEconomy.GrantLifeSeedItem(3);
        for(int i=0;i<7;i++)Check(GameEconomy.SacrificeLifeSeed(potion),"Sacrifice seed "+(i+1)+" increases flask capacity");
        Check(potion.capacity==10&&GameEconomy.LifeSeeds==1,"Seven seeds raise capacity from three to ten");
        Check(!GameEconomy.SacrificeLifeSeed(potion)&&GameEconomy.LifeSeeds==1,"Capacity cap does not consume an extra seed");
        foreach(var spec in CaveCreatureSetup.Specs)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/"+spec.prefix+".prefab");
            Check(prefab!=null&&prefab.GetComponent<EnemyClassification>().enemyType==(spec.elite?EnemyClassification.EnemyType.Elite:EnemyClassification.EnemyType.Normal),spec.prefix+" classification correct");
            Check(prefab.GetComponent<EnemyAudio>()?.profile!=null,spec.prefix+" audio profile present");
            foreach(var path in Directory.GetFiles("Assets/Sprites/"+spec.folder,"*.png",SearchOption.AllDirectories))
            {
                var texture=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                var frames=AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\','/')).OfType<Sprite>().ToArray();
                Check(texture.spritePixelsPerUnit==spec.ppu&&texture.filterMode==FilterMode.Point&&!texture.mipmapEnabled&&texture.textureCompression==TextureImporterCompression.Uncompressed&&frames.Length>0&&frames.All(f=>f.rect.width==100&&f.rect.height==100),spec.prefix+" 100px slicing and PPU: "+Path.GetFileName(path));
            }
            var clips=prefab.GetComponent<Animator>().runtimeAnimatorController.animationClips.Distinct().ToArray();
            Check(clips.All(c=>c.frameRate==12),spec.prefix+" all clips 12 FPS");
            foreach(var skill in spec.skills)
            {
                var clip=clips.First(c=>c.name.Contains("Attack")&&AnimationUtility.GetAnimationEvents(c).Any(e=>e.stringParameter==skill.skillName));
                Check(AnimationUtility.GetAnimationEvents(clip).Any(e=>e.functionName=="ExecuteHit"&&e.stringParameter==skill.skillName)&&AnimationUtility.GetAnimationEvents(clip).Any(e=>e.functionName=="UnlockAction"),spec.prefix+" "+skill.skillName+" hit and unlock events exist");
            }
            target=Object.Instantiate(prefab);target.transform.position=new Vector3(100,100,0);player.transform.position=new Vector3(120,100,0);yield return null;
            Check(target.GetComponent<EnemyBehavior>().currentState==EnemyBehavior.OrcState.Patrol,spec.prefix+" retains patrol logic");
            player.transform.position=new Vector3(101,100,0);target.GetComponent<EnemyAwareness>().MaxAlert();yield return null;yield return null;
            Check(target.GetComponent<EnemyClassification>().IsEngaged,spec.prefix+" detects and engages player");
            var ai=target.GetComponent<EnemyBehavior>();ai.enabled=false;target.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
            var manager=target.GetComponent<SkillDamageManager>();var combat=target.GetComponent<EnemyCombat>();var animator=target.GetComponent<Animator>();
            foreach(var skill in manager.skills)
            {
                playerStats.currentHP=playerStats.maxHP;var immunity=player.GetComponent<CharacterInvulnerability>();if(immunity!=null)immunity.isInvulnerable=false;
                var status=player.GetComponent<CharacterDebuffs>();if(status!=null){status.Remove(CharacterDebuffs.Kind.Burning);status.Remove(CharacterDebuffs.Kind.Poison);status.Remove(CharacterDebuffs.Kind.Stress);}
                target.GetComponent<CharacterStats>().currentStamina=target.GetComponent<CharacterStats>().maxStamina;
                combat.CancelAttack();target.GetComponent<AnimationEventHandler>().isActionLocked=false;animator.Play(spec.prefix+"Idle",0,0);manager.ResetCooldowns();skill.knockbackForce=0;
                player.transform.position=target.transform.position+Vector3.right*Mathf.Max(.5f,skill.hitboxOffset.x);Physics2D.SyncTransforms();
                // Let Animator settle and avoid a float-rounding boundary at exactly reset cooldown time.
                yield return null;
                Check(combat.TriggerSkill(skill.skillName,player.transform,target.transform.position,20),spec.prefix+" starts "+skill.skillName);
                yield return null;
                float until=Time.time+1.3f;
                while(Time.time<until)yield return null;
                Check(playerStats.currentHP<playerStats.maxHP,spec.prefix+" "+skill.skillName+" animation event deals damage");
                Check(!target.GetComponent<AnimationEventHandler>().isActionLocked,spec.prefix+" "+skill.skillName+" unlocks after animation");
            }
            int before=GameEconomy.LifeSeeds;target.GetComponent<CharacterHealth>().TakeDamageOverTime(1000000);yield return null;
            Check(GameEconomy.LifeSeeds==before+(spec.elite?1:0),spec.prefix+" death grants seeds only for Elite");
            if(spec.elite)Check(GameEconomy.RecordEliteSeedDrop(target.GetComponent<EnemyClassification>())==null,"Minotaur encounter cannot pay seed twice");
            Object.Destroy(target);target=null;yield return null;
        }
        Object.Destroy(player);player=null;yield return null;
        Check(errors.Count==0,"No runtime errors: "+string.Join("; ",errors));
    }
    static void Finish(Exception error)
    {
        finishing = true; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
        if(priest!=null)priest.GetComponent<World.NpcInteractable>()?.Close();if(player!=null)Object.Destroy(player);if(priest!=null)Object.Destroy(priest);if(target!=null)Object.Destroy(target); Time.timeScale=1;
        string result = (error == null ? "PASS" : "FAIL: " + error) + "\n" + string.Join("\n", checks);
        File.WriteAllText("Library/CaveCreatureValidation/results.txt", result);
        File.WriteAllText("Library/CaveCreatureValidation/status.txt", error == null ? "PASS" : "FAIL");
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
        File.AppendAllText("Library/CaveCreatureValidation/status.txt", "\nOriginal save and start scene restored");
        checks.Clear(); errors.Clear();
    }
}
