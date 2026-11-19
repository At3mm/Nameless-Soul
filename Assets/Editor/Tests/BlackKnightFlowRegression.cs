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
public static class BlackKnightFlowRegression
{
    const string Pending = "BlackKnightFlowRegression.Pending";
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

    static BlackKnightFlowRegression()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (SessionState.GetBool(Pending, false)) StartPolling();
    }

    [MenuItem("Tools/Dialogue and Quests/Test Black Knight Flow %#F6")]
    public static void Run()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before running audio checks.");
        foreach (string key in Keys)
        {
            SessionState.SetBool(Pending + ".exists." + key, PlayerPrefs.HasKey(key));
            SessionState.SetString(Pending + ".value." + key, PlayerPrefs.GetString(key, ""));
        }
        SessionState.SetString(Pending + ".start", EditorSceneManager.playModeStartScene != null ? AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) : "");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuController.ScenePath);
        SessionState.SetBool(Pending, true);
        Directory.CreateDirectory("Library/BlackKnightQuestValidation");
        File.WriteAllText("Library/BlackKnightQuestValidation/status.txt", "RUNNING");
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
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Black Knight flow check timed out.");
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
    static GameObject player, boss, necro;
    static ItemDefinition testItem;
    static IEnumerator Checks()
    {
        foreach(var key in Keys)PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();EnemyDefeatState.ReloadFromSave();BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();GameEconomy.ReloadFromSave();
        Time.timeScale=1;
        var quest=AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/Resources/Quests/BlackKnightChallenge.asset");
        var dialogue=AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Dialogue/BlackKnightConversation.asset");
        Check(QuestAuthoring.Validate(quest,QuestDefinition.All).Count==0,"Black Knight quest configuration valid");
        Check(QuestService.Status(quest)==QuestStatus.Locked,"Quest locked while first Necromancer alive");
        Check(!QuestService.Accept(quest),"Cannot accept before first Necromancer defeat");
        Check(dialogue.ResolveStartNodeId()=="before_necromancer","Alternate dialogue while first boss alive");
        player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"));
        player.transform.position=Vector3.zero;
        var stats=player.GetComponent<CharacterStats>();
        var interactor=player.GetComponent<World.PlayerInteractor>();
        if(interactor==null)interactor=player.AddComponent<World.PlayerInteractor>();
        var consumables=player.GetComponent<PlayerConsumables>();if(consumables==null)consumables=player.AddComponent<PlayerConsumables>();
        testItem=ScriptableObject.CreateInstance<ItemDefinition>();testItem.itemId="runtime-item-regression";testItem.maxStack=5;testItem.useCooldown=0;
        testItem.effects=new[]{new GameItemDefinition.Effect{kind=ItemEffectKind.HealHealth,percent=25,duration=30}};
        Check(QuestService.GrantItem(testItem,2),"Runtime test item granted");
        stats.currentHP=stats.maxHP;Check(!consumables.TryUseItem(testItem)&&ItemInventory.Count(testItem)==2,"Full health does not consume item");
        stats.currentHP=stats.maxHP*.5f;
        Check(consumables.TryUseItem(testItem)&&Mathf.Approximately(stats.currentHP,stats.maxHP*.75f)&&ItemInventory.Count(testItem)==1,"Healing item applies and consumes once");
        testItem.consumable=false;stats.currentHP=stats.maxHP*.5f;
        Check(consumables.TryUseItem(testItem)&&ItemInventory.Count(testItem)==1,"Reusable item is not consumed");
        testItem.effects[0].kind=ItemEffectKind.RestoreStamina;stats.currentStamina=0;
        Check(consumables.TryUseItem(testItem)&&Mathf.Approximately(stats.currentStamina,stats.maxStamina*.25f),"Stamina item restores configured percentage");
        testItem.effects[0].kind=ItemEffectKind.Power;
        Check(consumables.TryUseItem(testItem)&&player.GetComponent<CharacterBuffs>().Has(CharacterBuffs.Kind.Damage),"Configured buff applies");
        testItem.effects[0].duration=0;
        Check(!consumables.TryUseItem(testItem)&&ItemInventory.Count(testItem)==1,"Invalid effect does not consume item");
        boss=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/BlackKnight.prefab"));boss.transform.position=Vector3.zero;
        var gate=boss.GetComponent<World.QuestBossEncounter>();var npc=gate.conversation;
        yield return null;
        Check(npc.Open(interactor)&&npc.CurrentNodeId=="before_necromancer","Actual NPC opens alternate branch");npc.Close();yield return null;yield return null;yield return null;
        string first=quest.requiredDefeatedEncounters[0];
        PlayerPrefs.SetString(EnemyDefeatState.SaveKey,JsonUtility.ToJson(new Defeats{ids=new List<string>{first}}));
        EnemyDefeatState.ReloadFromSave();QuestService.ReloadFromSave();
        Check(QuestService.Status(quest)==QuestStatus.Available,"First boss defeat unlocks quest");
        Check(npc.Open(interactor)&&npc.CurrentNodeId=="start","Actual NPC keeps authored intro after first boss defeat");
        int advances=0;while(npc.CurrentNodeId!="offer"&&advances++<20){npc.Advance();yield return null;}
        Check(npc.CurrentNodeId=="offer","Authored intro reaches quest offer");
        int accept=npc.VisibleChoices.ToList().FindIndex(c=>c.action==DialogueAction.AcceptQuest);
        Check(accept>=0&&npc.Choose(accept),"Accept quest through actual dialogue choice");
        Check(QuestService.Status(quest)==QuestStatus.Active&&npc.CurrentNodeId=="accepted","Quest accepted and acknowledgment shown");npc.Close();yield return null;yield return null;yield return null;
        Check(npc.Open(interactor)&&npc.CurrentNodeId=="progress","Reopening active quest goes to progress");npc.Close();yield return null;yield return null;yield return null;
        Check(!QuestService.TurnIn(quest),"Cannot report before final boss defeat");
        var staging=new GameObject("Inactive regression staging");staging.SetActive(false);
        necro=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/BossVariants/Necromancer_DeadForest.prefab"),staging.transform);
        necro.GetComponent<EnemyClassification>().persistentId=quest.objectives[0].encounterId;
        necro.transform.position=new Vector3(100,100,0);necro.transform.SetParent(null);Object.Destroy(staging);yield return null;
        int seedsBeforeBoss=GameEconomy.LifeSeeds;
        necro.GetComponent<CharacterHealth>().TakeDamageOverTime(10000000f);yield return null;
        Check(GameEconomy.LifeSeeds==seedsBeforeBoss+1,"Boss seed is credited immediately without pickup");
        Check(!Object.FindObjectsByType<World.LifeSeedPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(p=>p.gameObject.activeSelf&&p.pickupId=="elite-seed:"+quest.objectives[0].encounterId),"Boss creates no ground seed pickup");
        var relic=necro.GetComponent<BossItemReward>()?.item;
        Check(relic!=null&&relic.icon!=null&&relic.InventoryCategory==ItemCategory.BossRelic,"Necromancer has configured relic and icon");
        Check(ItemInventory.Count(relic)==1,"Actual boss death grants configured relic");
        Check(!BossItemReward.GrantForDeath(necro.GetComponent<CharacterHealth>())&&ItemInventory.Count(relic)==1,"Duplicate boss death cannot grant relic twice");
        BossItemInventory.ReloadFromSave();Check(ItemInventory.Count(relic)==1,"Configured relic survives save reload");
        Check(GameEconomy.EliteSeedDrops.Any(d=>d.pickupId=="elite-seed:"+quest.objectives[0].encounterId),"Boss death creates persisted life seed drop");
        Check(GameEconomy.RecordEliteSeedDrop(necro.GetComponent<EnemyClassification>())==null,"Boss seed cannot drop twice");
        GameEconomy.ReloadFromSave();Check(GameEconomy.EliteSeedDrops.Any(d=>d.pickupId=="elite-seed:"+quest.objectives[0].encounterId),"Boss seed survives save reload");
        Check(EnemyDefeatState.WasDefeated(quest.objectives[0].encounterId),"Actual final Necromancer death persisted");
        Check(QuestService.Status(quest)==QuestStatus.ReadyToReport,"Final boss death waits for NPC conversation");
        int gold=GameEconomy.Gold,exp=GameEconomy.Experience;
        Check(GameEconomy.LifeSeeds==seedsBeforeBoss+1,"Boss seed remains exactly one after reload and duplicate death");
        Check(npc.Open(interactor)&&npc.CurrentNodeId=="report","Final boss victory enters follow-up dialogue");
        Check(QuestService.Status(quest)==QuestStatus.Completed,"Follow-up acknowledges quest without reward report");
        Check(npc.VisibleChoices.Count==0,"Report has no claim-reward choice");
        Check(GameEconomy.Gold==gold&&GameEconomy.Experience==exp,"Follow-up grants no gold or EXP");
        int steps=0;while(npc.CurrentNodeId!="duel"&&steps++<20){npc.Advance();yield return null;}
        Check(npc.CurrentNodeId=="duel","Authored follow-up reaches final combat choice");
        Check(gate.IsFriendly,"Boss remains friendly before combat choice");
        int decline=npc.VisibleChoices.ToList().FindIndex(c=>c.action==DialogueAction.End);
        Check(decline>=0&&npc.Choose(decline),"Player can decline combat");
        float wait=Time.unscaledTime+gate.aggressionDelay+1f;while(Time.unscaledTime<wait)yield return null;
        Check(gate.CurrentStage==World.QuestBossEncounter.Stage.Friendly,"Declining keeps Black Knight friendly after dialogue closes");
        Check(npc.Open(interactor)&&npc.CurrentNodeId=="report","Declined challenge can be revisited");
        steps=0;while(npc.CurrentNodeId!="duel"&&steps++<20){npc.Advance();yield return null;}
        int fight=npc.VisibleChoices.ToList().FindIndex(c=>c.action==DialogueAction.ChallengeBoss);
        Check(fight>=0&&npc.Choose(fight),"Player explicitly accepts combat");
        Check(gate.IsFriendly,"Boss stays protected while closing dialogue");
        wait=Time.unscaledTime+gate.aggressionDelay+1f;while(Time.unscaledTime<wait)yield return null;
        Check(gate.CurrentStage==World.QuestBossEncounter.Stage.Hostile,"Only accepted challenge starts boss combat");
        Check(GameEconomy.Gold==gold&&GameEconomy.Experience==exp,"Combat acceptance does not pay quest rewards");
        QuestService.ReloadFromSave();Check(QuestService.Status(quest)==QuestStatus.Completed,"Completion survives save reload");
        Object.Destroy(boss);boss=null;Object.Destroy(necro);necro=null;yield return null;
        Check(errors.Count==0,"No runtime errors: "+string.Join("; ",errors));
    }
    [Serializable] sealed class Defeats {public List<string> ids=new List<string>();}
    static void Finish(Exception error)
    {
        finishing = true; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
        if(player!=null)Object.Destroy(player);if(boss!=null)Object.Destroy(boss);if(necro!=null)Object.Destroy(necro);if(testItem!=null)Object.Destroy(testItem); Time.timeScale=1;
        string result = (error == null ? "PASS" : "FAIL: " + error) + "\n" + string.Join("\n", checks);
        File.WriteAllText("Library/BlackKnightQuestValidation/results.txt", result);
        File.WriteAllText("Library/BlackKnightQuestValidation/status.txt", error == null ? "PASS" : "FAIL");
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
        File.AppendAllText("Library/BlackKnightQuestValidation/status.txt", "\nOriginal save and start scene restored");
        checks.Clear(); errors.Clear();
    }
}
