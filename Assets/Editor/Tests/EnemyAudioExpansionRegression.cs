using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Opt-in playback/import checks; preserves the player's save and start scene.</summary>
[InitializeOnLoad]
public static class EnemyAudioExpansionRegression
{
    const string Pending = "EnemyAudioExpansionRegression.Pending";
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

    static EnemyAudioExpansionRegression()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (SessionState.GetBool(Pending, false)) StartPolling();
    }

    [MenuItem("Tools/Enemies/Test Expanded Audio %#F8")]
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
        Directory.CreateDirectory("Library/EnemyAudioExpansion");
        File.WriteAllText("Library/EnemyAudioExpansion/status.txt", "RUNNING");
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
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Audio check timed out.");
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
    static void StopVoices()
    {
        var pool = Object.FindFirstObjectByType<EnemyAudioPool>();
        if (pool != null) foreach (var voice in pool.GetComponents<AudioSource>()) voice.Stop();
        var shared = Object.FindFirstObjectByType<CombatSFX>();
        if (shared != null) foreach (var voice in shared.GetComponents<AudioSource>()) voice.Stop();
    }
    static AudioSource[] Playing()
    {
        var pool = Object.FindFirstObjectByType<EnemyAudioPool>();
        return pool == null ? new AudioSource[0] : pool.GetComponents<AudioSource>().Where(s => s.isPlaying).ToArray();
    }
    static AudioClip Expected(EnemyAudioProfile p, string skill)
    {
        var cue = p.skills.Find(s => s.skillName == skill);
        return cue != null && cue.clips != null && cue.clips.Length > 0 ? cue.clips[0] : p.attacks[0];
    }
    static IEnumerator Checks()
    {
        int clipCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Effects/Audio/Enemies/Expansion" }))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
            Check(clip != null && clip.samples > 0 && clip.length > .01f, "Decoded " + clip.name);
            clip.LoadAudioData();
            float[] samples = new float[clip.samples * clip.channels];
            Check(clip.GetData(samples, 0) && samples.Any(s => Mathf.Abs(s) > .0001f) && samples.All(s => !float.IsNaN(s) && !float.IsInfinity(s)), "Valid signal " + clip.name);
            Check(clip.channels == 1, "Mono import " + clip.name);
            clipCount++;
        }
        Check(clipCount == 41, "All 41 imported clips available");
        foreach (string name in Actors)
        {
            var staging = new GameObject("Audio test staging"); staging.SetActive(false);
            actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/" + name + ".prefab"), staging.transform);
            var classification = actor.GetComponent<EnemyClassification>();
            if (classification != null) classification.persistentId = "audio-check:" + Guid.NewGuid().ToString("N");
            actor.transform.position = Camera.main != null ? Camera.main.transform.position + Vector3.forward * 10 : Vector3.zero;
            actor.transform.SetParent(null); actor.SetActive(true); Object.Destroy(staging);
            var audio = actor.GetComponent<EnemyAudio>();
            Check(audio != null && audio.profile != null, name + " has bound profile");
            var encounter = actor.GetComponent<World.QuestBossEncounter>();
            if (encounter != null && encounter.isActiveAndEnabled && encounter.IsFriendly)
            {
                Check(!audio.enabled, name + " stays silent while friendly NPC");
                typeof(World.QuestBossEncounter).GetMethod("BecomeHostile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(encounter, null);
                Check(audio.enabled, name + " enables audio after becoming hostile");
            }
            var p = audio.profile;
            Check(p.attacks.Length > 0 && p.hurt.Length > 0 && p.death.Length > 0 && p.attacks.Concat(p.hurt).Concat(p.death).Concat(p.phaseTransition).Concat(p.skills.SelectMany(s => s.clips)).All(c => c != null), name + " has no missing clips");
            StopVoices(); audio.Attack("NormalAttack");
            Check(Playing().Any(s => s.clip == Expected(p, "NormalAttack")), name + " attack playback");
            StopVoices(); audio.Hurt(); Check(Playing().Any(s => p.hurt.Contains(s.clip)), name + " hurt playback");
            StopVoices(); audio.Death(); Check(Playing().Length == 1, name + " death playback");
            audio.Death(); Check(Playing().Length == 1, name + " death does not duplicate");
            if (p.phaseTransition.Length > 0) { StopVoices(); audio.PhaseTransition(); Check(Playing().Length == 1, name + " phase playback"); }
            foreach (var cue in p.skills)
            {
                float until = Time.unscaledTime + p.attackInterval + .05f;
                while (Time.unscaledTime < until) yield return null;
                StopVoices(); audio.Attack(cue.skillName);
                Check(Playing().Any(s => cue.clips.Contains(s.clip)), name + " skill " + cue.skillName);
            }
            if (p.replaceDefaultWeaponSwing)
            {
                float until = Time.unscaledTime + p.attackInterval + .05f;
                while (Time.unscaledTime < until) yield return null;
                StopVoices();
                var manager = actor.GetComponent<SkillDamageManager>();
                manager.ExecuteHit("NormalAttack");
                Check(Playing().Length == 1, name + " damage-frame custom swing");
                var shared = Object.FindFirstObjectByType<CombatSFX>();
                Check(shared == null || !shared.GetComponents<AudioSource>().Any(s => s.isPlaying), name + " does not stack shared swing");
            }
            Object.Destroy(actor); actor = null;
            yield return null;
        }
        StopVoices();
        var first = AssetDatabase.LoadAssetAtPath<EnemyAudioProfile>(Profiles + "Orc.asset");
        EnemyAudioPool.Play(first.attacks[0], Camera.main != null ? Camera.main.transform.position : Vector3.zero, .7f, 1, false);
        Time.timeScale = 0;
        float end = Time.unscaledTime + .15f;
        while (Time.unscaledTime < end) yield return null;
        Check(!Playing().Any(), "Voices pause with gameplay");
        Time.timeScale = 1;
        yield return null;
        Check(errors.Count == 0, "No runtime errors: " + string.Join("; ", errors));
    }
    static void Finish(Exception error)
    {
        finishing = true; EditorApplication.update -= Poll; Application.logMessageReceived -= OnLog;
        StopVoices(); if (actor != null) Object.Destroy(actor); Time.timeScale = 1;
        string result = (error == null ? "PASS" : "FAIL: " + error) + "\n" + string.Join("\n", checks);
        File.WriteAllText("Library/EnemyAudioExpansion/results.txt", result);
        File.WriteAllText("Library/EnemyAudioExpansion/status.txt", error == null ? "PASS" : "FAIL");
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
        File.AppendAllText("Library/EnemyAudioExpansion/status.txt", "\nOriginal save and start scene restored");
        checks.Clear(); errors.Clear();
    }
}
