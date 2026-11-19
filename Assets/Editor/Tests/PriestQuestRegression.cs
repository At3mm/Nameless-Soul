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
public static class PriestQuestRegression
{
    const string Pending = "PriestQuestRegression.Pending";
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

    static PriestQuestRegression()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (SessionState.GetBool(Pending, false)) StartPolling();
    }

    [MenuItem("Tools/Dialogue and Quests/Test Priest Quest and Shop")]
    public static void Run()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before running audio checks.");
        foreach (string key in Keys)
        {
            SessionState.SetBool(Pending + ".exists." + key, PlayerPrefs.HasKey(key));
            SessionState.SetString(Pending + ".value." + key, PlayerPrefs.GetString(key, ""));
        }
        string folder="Assets/Resources/Quests/__AutoQuestValidation_"+Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets/Resources/Quests",Path.GetFileName(folder));SessionState.SetString(Pending+".folder",folder);
        var evidence=ScriptableObject.CreateInstance<ItemDefinition>();evidence.itemId="auto-quest-evidence-"+Guid.NewGuid().ToString("N");evidence.displayName="Test evidence";evidence.maxStack=1;
        AssetDatabase.CreateAsset(evidence,folder+"/Evidence.asset");
        var qa=ScriptableObject.CreateInstance<QuestDefinition>();qa.questId="auto-quest-a-"+Guid.NewGuid().ToString("N");qa.title="Auto A";qa.gold=7;
        qa.objectives.Add(new QuestDefinition.BossObjective{relic=evidence,count=1});qa.items.Add(new QuestDefinition.ItemReward{item=evidence,count=1});AssetDatabase.CreateAsset(qa,folder+"/EvidenceQuest.asset");
        var qb=ScriptableObject.CreateInstance<QuestDefinition>();qb.questId="auto-quest-b-"+Guid.NewGuid().ToString("N");qb.title="Auto B";qb.gold=3;qb.prerequisites.Add(qa);
        qb.objectives.Add(new QuestDefinition.BossObjective{relic=evidence,count=1});AssetDatabase.CreateAsset(qb,folder+"/ChainQuest.asset");
        SessionState.SetString(Pending + ".start", EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuController.ScenePath);
        SessionState.SetBool(Pending, true);
        Directory.CreateDirectory("Library/PriestQuestValidation");
        File.WriteAllText("Library/PriestQuestValidation/status.txt", "RUNNING");
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
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Priest quest check timed out.");
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
        PlayerPrefs.SetString(GameEconomy.SaveKey,"{\"version\":1,\"eliteSeedDrops\":[{\"scenePath\":\"Assets/Scenes/CaveScene.unity\",\"pickupId\":\"elite-seed:legacy-test\"}]}");
        GameEconomy.ReloadFromSave();World.EliteLifeSeedDrops.Restore(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Check(GameEconomy.LifeSeeds==1,"Old uncollected seed on another map is automatically credited");
        World.EliteLifeSeedDrops.Restore(UnityEngine.SceneManagement.SceneManager.GetActiveScene());GameEconomy.ReloadFromSave();
        Check(GameEconomy.LifeSeeds==1,"Old seed migration cannot duplicate rewards");
        foreach(var key in Keys)PlayerPrefs.DeleteKey(key);
        var quest=Resources.Load<QuestDefinition>("Quests/DefeatTheWerebear");
        PlayerPrefs.SetString(QuestService.SaveKey,"{\"quests\":[{\"id\":\""+quest.questId+"\",\"completed\":true}],\"kills\":[],\"items\":[]}");
        EnemyDefeatState.ReloadFromSave();BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();GameEconomy.ReloadFromSave();Time.timeScale=1;
        Check(QuestService.Status(quest)==QuestStatus.Completed&&GameEconomy.Gold==0&&!QuestService.HasPendingRewards(quest),"Old completed saves migrate without paying rewards again");
        foreach(var key in Keys)PlayerPrefs.DeleteKey(key);
        EnemyDefeatState.ReloadFromSave();BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();GameEconomy.ReloadFromSave();
        Check(QuestAuthoring.Validate(quest,QuestDefinition.All).Count==0,"Werebear quest configuration valid");
        player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"));player.transform.position=Vector3.zero;
        var interactor=player.GetComponent<World.PlayerInteractor>();if(interactor==null)interactor=player.AddComponent<World.PlayerInteractor>();
        priest=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Priest.prefab"));priest.transform.position=Vector3.zero;
        var npc=priest.GetComponent<World.NpcInteractable>();var shop=priest.GetComponent<World.NpcShop>();yield return null;
        Check(!shop.IsUnlocked&&npc.Open(interactor)&&npc.CurrentNodeId=="offer","First conversation accepts active quest and shows authored introduction");
        Check(QuestService.Status(quest)==QuestStatus.Active,"Incomplete quest stays active");
        var canvas=(GameObject)typeof(World.NpcInteractable).GetField("view",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(npc);
        shop.Open(interactor,canvas,npc);Check(!shop.IsOpen&&!shop.Trade(0,1),"Shop open and trade blocked before objective completion");
        npc.Close();yield return null;yield return null;yield return null;
        var staging=new GameObject("Priest regression staging");staging.SetActive(false);
        target=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Werebear.prefab"),staging.transform);
        target.GetComponent<EnemyClassification>().persistentId=quest.objectives[0].encounterId;target.transform.position=new Vector3(100,100,0);target.transform.SetParent(null);Object.Destroy(staging);yield return null;
        int seedsBeforeElite=GameEconomy.LifeSeeds;
        target.GetComponent<CharacterHealth>().TakeDamageOverTime(10000000f);yield return null;
        Check(GameEconomy.LifeSeeds==seedsBeforeElite+1,"Elite seed is credited immediately without pickup");
        Check(GameEconomy.EliteSeedDrops.Any(d=>d.pickupId=="elite-seed:"+quest.objectives[0].encounterId),"Elite death creates persisted life seed drop");
        Check(GameEconomy.RecordEliteSeedDrop(target.GetComponent<EnemyClassification>())==null,"Elite seed cannot drop twice");
        Check(QuestService.Status(quest)==QuestStatus.ReadyToReport&&!shop.IsUnlocked,"Werebear death waits for NPC and leaves shop locked");
        Check(quest.items.TrueForAll(r=>ItemInventory.Count(r.item)==0),"No quest item reward before returning to NPC");
        int beforeReturn=GameEconomy.Gold,expBeforeReturn=GameEconomy.Experience,seedsBeforeReturn=GameEconomy.LifeSeeds;
        QuestService.ProcessAutomaticCompletion();QuestService.ReloadFromSave();
        Check(QuestService.Status(quest)==QuestStatus.ReadyToReport&&GameEconomy.Gold==beforeReturn,"Reload preserves ready quest without paying");
        var unrelated=ScriptableObject.CreateInstance<DialogueDefinition>();unrelated.TurnInReadyQuests();Object.Destroy(unrelated);
        Check(QuestService.Status(quest)==QuestStatus.ReadyToReport,"Unrelated dialogue cannot turn in quest");
        Check(npc.Open(interactor)&&npc.CurrentNodeId=="report","Returning to quest NPC automatically enters authored follow-up");
        Check(QuestService.Status(quest)==QuestStatus.Completed&&shop.IsUnlocked&&!QuestService.HasPendingRewards(quest),"Conversation auto turns in quest and unlocks shop");
        Check(GameEconomy.Gold==beforeReturn+quest.gold&&GameEconomy.Experience==expBeforeReturn+quest.experience&&GameEconomy.LifeSeeds==seedsBeforeReturn+quest.lifeSeeds,"Quest rewards paid only on NPC return");
        Check(quest.items.TrueForAll(r=>ItemInventory.Count(r.item)>=r.count),"Item rewards delivered on NPC return");
        int gold=GameEconomy.Gold,exp=GameEconomy.Experience,seeds=GameEconomy.LifeSeeds;
        Check(!QuestService.HasSeenCompletionDialogue(quest),"Follow-up not marked seen before final line");
        int steps=0;while(npc.CurrentNodeId!="start"&&npc.IsOpen&&steps++<30){npc.Advance();yield return null;}
        Check(npc.CurrentNodeId=="start"&&QuestService.HasSeenCompletionDialogue(quest),"Full custom follow-up ends at shop menu and records viewing");
        Check(GameEconomy.Gold==gold&&GameEconomy.Experience==exp,"Follow-up conversation grants no duplicate rewards");
        int trade=npc.VisibleChoices.ToList().FindIndex(c=>c.action==DialogueAction.OpenShop);Check(trade>=0&&npc.Choose(trade)&&shop.IsOpen,"Unlocked menu opens shop");
        Check(shop.AvailableConsumables.SequenceEqual(new[]{ConsumableKind.SoulPurification})&&!shop.includeCustomItems,"Priest stocks only Soul Purification");
        int incense=GameEconomy.ConsumableCount(ConsumableKind.SoulPurification);
        Check(shop.Trade(0,1)&&GameEconomy.ConsumableCount(ConsumableKind.SoulPurification)==incense+1,"Priest purchase gives Soul Purification rather than item index zero");npc.Close();yield return null;yield return null;yield return null;
        Check(npc.Open(interactor)&&npc.CurrentNodeId=="start","Later visits skip already viewed completion story");npc.Close();yield return null;yield return null;yield return null;
        PlayerPrefs.DeleteKey(QuestService.SaveKey);PlayerPrefs.DeleteKey(GameEconomy.SaveKey);QuestService.ReloadFromSave();GameEconomy.ReloadFromSave();
        Check(npc.Open(interactor)&&npc.CurrentNodeId=="report"&&QuestService.Status(quest)==QuestStatus.Completed&&shop.IsUnlocked,"Prior Werebear victory auto accepts, completes and rewards at first conversation");
        npc.Close();Object.Destroy(priest);priest=null;Object.Destroy(target);target=null;yield return null;yield return null;yield return null;
        priest=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/NPCs/NPC_Wizard.prefab"));priest.transform.position=Vector3.zero;yield return null;
        var wizard=priest.GetComponent<World.NpcInteractable>();var wizardShop=priest.GetComponent<World.NpcShop>();
        Check(wizardShop.AvailableConsumables.Length==8&&!wizardShop.AvailableConsumables.Contains(ConsumableKind.SoulPurification)&&!wizardShop.includeCustomItems,"Wizard stocks eight potions without purification incense");
        Check(wizard.Open(interactor),"Wizard conversation opens");
        int advances=0;while(!wizardShop.IsOpen&&advances++<10){wizard.Advance();yield return null;}
        GameEconomy.AddGold(1000);wizardShop.SelectItem(7);int protection=GameEconomy.ConsumableCount(ConsumableKind.Protection);
        Check(wizardShop.Trade(0,1)&&GameEconomy.ConsumableCount(ConsumableKind.Protection)==protection+1,"Wizard sells correct potion on second page");
        wizard.Close();Object.Destroy(priest);priest=null;yield return null;yield return null;yield return null;
        string folder=SessionState.GetString(Pending+".folder","");
        var qa=AssetDatabase.LoadAssetAtPath<QuestDefinition>(folder+"/EvidenceQuest.asset");var qb=AssetDatabase.LoadAssetAtPath<QuestDefinition>(folder+"/ChainQuest.asset");var item=qa.objectives[0].relic;
        Check(QuestService.GrantItem(item,1)&&QuestService.Accept(qa),"Generic evidence quest accepted with prior item");
        Check(QuestService.Status(qa)==QuestStatus.ReadyToReport&&QuestService.TurnIn(qa)&&QuestService.HasPendingRewards(qa),"Full stack still completes quest and queues reward");
        Check(QuestService.Status(qb)==QuestStatus.Available&&QuestService.Accept(qb)&&QuestService.Status(qb)==QuestStatus.ReadyToReport&&QuestService.TurnIn(qb),"Chained quest unlocks and completes immediately when its objective is already satisfied");
        int before=GameEconomy.Gold;Check(ItemInventory.Remove(item,1),"Inventory space freed");
        Check(!QuestService.HasPendingRewards(qa)&&ItemInventory.Count(item)==1&&GameEconomy.Gold==before+qa.gold,"Pending rewards delivered automatically when capacity is available");
        before=GameEconomy.Gold;QuestService.ProcessAutomaticCompletion();QuestService.ReloadFromSave();
        Check(GameEconomy.Gold==before&&ItemInventory.Count(item)==1,"Queued rewards delivered only once");
        PlayerPrefs.DeleteKey(QuestService.SaveKey);PlayerPrefs.DeleteKey(GameEconomy.SaveKey);QuestService.ReloadFromSave();GameEconomy.ReloadFromSave();
        PlayerPrefs.SetString(QuestService.SaveKey,"{\"quests\":[{\"id\":\""+qb.questId+"\",\"completed\":false}],\"kills\":[],\"items\":[{\"id\":\""+item.itemId+"\",\"count\":1}]}");
        QuestService.ReloadFromSave();Check(QuestService.Status(qb)==QuestStatus.ReadyToReport&&GameEconomy.Gold==0,"Old ready save waits for NPC return");
        var returnDialogue=ScriptableObject.CreateInstance<DialogueDefinition>();returnDialogue.autoTurnInQuests.Add(qb);returnDialogue.TurnInReadyQuests();Object.Destroy(returnDialogue);
        Check(QuestService.Status(qb)==QuestStatus.Completed&&GameEconomy.Gold==qb.gold,"Explicit NPC quest list turns in migrated quest");
        QuestService.ReloadFromSave();Check(GameEconomy.Gold==qb.gold,"Migrated reward does not repeat");
        Object.Destroy(player);player=null;yield return null;
        Check(errors.Count==0,"No runtime errors: "+string.Join("; ",errors));
    }
    static void Finish(Exception error)
    {
        finishing = true; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
        if(priest!=null)priest.GetComponent<World.NpcInteractable>()?.Close();if(player!=null)Object.Destroy(player);if(priest!=null)Object.Destroy(priest);if(target!=null)Object.Destroy(target); Time.timeScale=1;
        string result = (error == null ? "PASS" : "FAIL: " + error) + "\n" + string.Join("\n", checks);
        File.WriteAllText("Library/PriestQuestValidation/results.txt", result);
        File.WriteAllText("Library/PriestQuestValidation/status.txt", error == null ? "PASS" : "FAIL");
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
        string testFolder=SessionState.GetString(Pending+".folder","");
        if(testFolder.StartsWith("Assets/Resources/Quests/__AutoQuestValidation_",StringComparison.Ordinal))AssetDatabase.DeleteAsset(testFolder);
        SessionState.SetBool(Pending, false); routine = null; finishing = false; frame = -1;
        File.AppendAllText("Library/PriestQuestValidation/status.txt", "\nOriginal save and start scene restored");
        checks.Clear(); errors.Clear();
    }
}
