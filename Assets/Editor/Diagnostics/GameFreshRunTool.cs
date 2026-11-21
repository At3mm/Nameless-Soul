using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Runs the opening flow in a temporary empty save slot, then restores the player's real progress.</summary>
[InitializeOnLoad]
public sealed class GameFreshRunTool : EditorWindow
{
    private const string ActiveKey="Noname.FreshRun.Active", StartedKey="Noname.FreshRun.Started", PendingKey="Noname.FreshRun.Pending";
    private const string BackupPath="Library/DeveloperTools/FreshRunBackup.json";
    private static readonly string[] Keys={World.CheckpointService.SaveKey,World.CheckpointService.UnlockSaveKey,GameEconomy.SaveKey,EnemyDefeatState.SaveKey,BossItemInventory.SaveKey,QuestService.SaveKey,GameplayTutorial.SaveKey,"Noname.PreviousRun.v1"};
    [Serializable] private sealed class Entry { public string key,value; public bool existed; }
    [Serializable] private sealed class Backup { public List<Entry> entries=new List<Entry>(); public string playStart; public bool hadVolume; public float volume; }
    public static bool IsTesting => SessionState.GetBool(ActiveKey,false);
    static GameFreshRunTool()
    {
        EditorApplication.playModeStateChanged+=OnState;
        EditorApplication.delayCall+=Process;
    }
    [MenuItem("Tools/Game Testing/Test From Beginning")]
    public static void OpenWindow(){GetWindow<GameFreshRunTool>("Test từ đầu").minSize=new Vector2(410,190);}
    private void OnGUI()
    {
        EditorGUILayout.LabelField("TEST GAME TỪ ĐẦU",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Tạo lượt thử với save sạch từ MainMenu. Khi dừng Play, tiến trình và âm lượng cũ tự được khôi phục.",MessageType.Info);
        using(new EditorGUI.DisabledScope(EditorApplication.isCompiling||SessionState.GetBool(PendingKey,false)))
        {
            using(new EditorGUI.DisabledScope(IsTesting))
                if(GUILayout.Button("Chạy thử từ đầu",GUILayout.Height(36)))StartFreshRun();
            using(new EditorGUI.DisabledScope(!IsTesting&&!File.Exists(BackupPath)))
                if(GUILayout.Button("Dừng test và khôi phục save",GUILayout.Height(30)))StopAndRestore();
        }
        if(IsTesting)EditorGUILayout.LabelField("Đang dùng save thử. Dừng Play để trở lại save cũ.");
    }
    public static void StartFreshRun()
    {
        if(IsTesting||SessionState.GetBool(PendingKey,false)||EditorApplication.isCompiling)return;
        SessionState.SetBool(PendingKey,true);
        if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();else EditorApplication.delayCall+=Process;
    }
    private static void Process()
    {
        if(!SessionState.GetBool(PendingKey,false)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
        SessionState.SetBool(PendingKey,false);
        var menu=AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuController.ScenePath);
        if(menu==null)throw new FileNotFoundException("MainMenu scene is missing.");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var backup=new Backup{playStart=EditorSceneManager.playModeStartScene!=null?AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene):"",hadVolume=PlayerPrefs.HasKey("Noname.MasterVolume"),volume=PlayerPrefs.GetFloat("Noname.MasterVolume",1)};
        foreach(var key in Keys)backup.entries.Add(new Entry{key=key,existed=PlayerPrefs.HasKey(key),value=PlayerPrefs.GetString(key,"")});
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));File.WriteAllText(BackupPath,JsonUtility.ToJson(backup,true));
        SessionState.SetBool(ActiveKey,true);SessionState.SetBool(StartedKey,false);
        foreach(var key in Keys)PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();ReloadCaches();
        EditorSceneManager.playModeStartScene=menu;
        EditorApplication.EnterPlaymode();
    }
    private static void OnState(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&IsTesting)SessionState.SetBool(StartedKey,true);
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            if(IsTesting)Restore();
            EditorApplication.delayCall+=Process;
        }
    }
    public static void StopAndRestore()
    {
        if(EditorApplication.isPlaying){if(!IsTesting)return;EditorApplication.ExitPlaymode();}
        else if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(BackupPath))Restore();
    }
    private static void Restore()
    {
        var backup=JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
        if(backup==null||backup.entries==null||!Array.TrueForAll(Keys,key=>key==QuestService.SaveKey||key==GameplayTutorial.SaveKey||backup.entries.Exists(entry=>entry.key==key)))throw new InvalidDataException("Fresh-run backup is incomplete.");
        if(!backup.entries.Exists(entry=>entry.key==QuestService.SaveKey))PlayerPrefs.DeleteKey(QuestService.SaveKey);
        if(!backup.entries.Exists(entry=>entry.key==GameplayTutorial.SaveKey))PlayerPrefs.DeleteKey(GameplayTutorial.SaveKey);
        foreach(var entry in backup.entries){if(entry.existed)PlayerPrefs.SetString(entry.key,entry.value);else PlayerPrefs.DeleteKey(entry.key);}
        if(backup.hadVolume)PlayerPrefs.SetFloat("Noname.MasterVolume",backup.volume);else PlayerPrefs.DeleteKey("Noname.MasterVolume");
        PlayerPrefs.Save();ReloadCaches();AudioListener.volume=backup.volume;Time.timeScale=1;
        EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(backup.playStart)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(backup.playStart);
        SessionState.SetBool(ActiveKey,false);SessionState.SetBool(StartedKey,false);
        Debug.Log("[Test từ đầu] Đã khôi phục save và âm lượng trước khi test.");
    }
    private static void ReloadCaches(){GameEconomy.ReloadFromSave();EnemyDefeatState.ReloadFromSave();BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();}
}
