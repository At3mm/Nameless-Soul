using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor-only reset of owned save keys. Never deletes all PlayerPrefs or project assets.</summary>
[InitializeOnLoad]
public sealed class GameResetTool : EditorWindow
{
    private const string PendingKey = "Noname.ResetTool.Pending";
    private const string PlayKey = "Noname.ResetTool.Restart";
    private const string BackupPath = "Library/DeveloperTools/LastGameResetBackup.json";
    private static readonly string[] Keys = { World.CheckpointService.SaveKey, World.CheckpointService.UnlockSaveKey, GameEconomy.SaveKey, EnemyDefeatState.SaveKey, BossItemInventory.SaveKey, QuestService.SaveKey };
    [Serializable] private sealed class Entry { public string key, value; public bool existed; }
    [Serializable] private sealed class Backup { public string created; public List<Entry> entries = new List<Entry>(); }

    static GameResetTool()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
        EditorApplication.delayCall += ResumePending;
    }
    [MenuItem("Tools/Game Testing/Reset Game")]
    public static void OpenWindow() { GetWindow<GameResetTool>("Reset Game").minSize = new Vector2(390,290); }
    private void OnGUI()
    {
        EditorGUILayout.LabelField("RESET GAME ĐỂ TEST",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Xóa checkpoint, tượng đã mở, Gold/EXP, chỉ số đã nâng và danh sách Elite/Boss đã hạ. Play lại sẽ tạo nhân vật và quái từ trạng thái ban đầu của scene đang mở.\n\nDữ liệu cũ tự được sao lưu trước khi reset.",MessageType.Info);
        EditorGUILayout.LabelField("Gold / EXP",GameEconomy.Gold+" / "+GameEconomy.Experience);
        EditorGUILayout.LabelField("Số lần nâng chỉ số",GameEconomy.TotalPurchases.ToString());
        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || SessionState.GetString(PendingKey,"")!=""))
        {
            if (GUILayout.Button("Reset tiến trình và Play lại",GUILayout.Height(34))) RequestReset(true);
            if (GUILayout.Button("Chỉ reset dữ liệu lưu",GUILayout.Height(28))) RequestReset(false);
            using (new EditorGUI.DisabledScope(!File.Exists(BackupPath)))
                if (GUILayout.Button("Khôi phục bản trước lần reset gần nhất",GUILayout.Height(28))) RequestRestore();
        }
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Không sửa prefab, scene, asset hay cấu hình game.",EditorStyles.wordWrappedMiniLabel);
    }
    public static void RequestReset(bool restartPlay)
    {
        Queue("reset",restartPlay);
    }
    public static void RequestRestore() { Queue("restore",false); }
    private static void Queue(string action,bool restart)
    {
        if (EditorApplication.isCompiling || SessionState.GetString(PendingKey,"")!="") return;
        SessionState.SetString(PendingKey,action); SessionState.SetBool(PlayKey,restart);
        if (EditorApplication.isPlaying) EditorApplication.isPlaying=false;
        else EditorApplication.delayCall += ResumePending;
    }
    private static void OnPlayState(PlayModeStateChange state)
    {
        if (state==PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += ResumePending;
    }
    private static void ResumePending()
    {
        string action=SessionState.GetString(PendingKey,"");
        if (action=="" || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        bool restart=SessionState.GetBool(PlayKey,false);
        SessionState.EraseString(PendingKey); SessionState.EraseBool(PlayKey);
        try
        {
            if (action=="reset") ResetSaveData(); else RestoreSaveData();
            Time.timeScale=1;
            if (restart) EditorApplication.EnterPlaymode();
            RepaintWindows();
        }
        catch (Exception error) { Debug.LogException(error); }
    }
    public static void ResetSaveData()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before resetting saves.");
        var backup=new Backup { created=DateTime.Now.ToString("O") };
        foreach (string key in Keys) backup.entries.Add(new Entry { key=key,existed=PlayerPrefs.HasKey(key),value=PlayerPrefs.GetString(key,"") });
        Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
        File.WriteAllText(BackupPath,JsonUtility.ToJson(backup,true));
        World.CheckpointService.ClearSaved();
        EnemyDefeatState.ClearSaved();PlayerPrefs.DeleteKey(QuestService.SaveKey);QuestService.ReloadFromSave();
        PlayerPrefs.DeleteKey(BossItemInventory.SaveKey); BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();
        PlayerPrefs.DeleteKey(GameEconomy.SaveKey); PlayerPrefs.Save(); GameEconomy.ReloadFromSave();
        Debug.Log("[Game Reset] Đã reset checkpoint, tượng, Gold/EXP, nâng chỉ số và Elite/Boss đã hạ. Bản sao lưu: "+BackupPath);
    }
    public static void RestoreSaveData()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before restoring saves.");
        var backup=JsonUtility.FromJson<Backup>(File.ReadAllText(BackupPath));
        if (backup==null || backup.entries==null || !Array.TrueForAll(Keys,key=>key==QuestService.SaveKey || key==EnemyDefeatState.SaveKey || key==BossItemInventory.SaveKey || backup.entries.Exists(e=>e!=null && e.key==key)))
            throw new InvalidDataException("Invalid game-reset backup.");
        foreach (string key in Keys)
        {
            var entry=backup.entries.Find(e=>e.key==key);
            if (entry!=null && entry.existed) PlayerPrefs.SetString(key,entry.value); else PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save(); GameEconomy.ReloadFromSave();
        EnemyDefeatState.ReloadFromSave();
        BossItemInventory.ReloadFromSave();QuestService.ReloadFromSave();
        Debug.Log("[Game Reset] Đã khôi phục dữ liệu trước lần reset gần nhất.");
    }
    private static void RepaintWindows()
    {
        foreach (var window in Resources.FindObjectsOfTypeAll<GameResetTool>()) window.Repaint();
    }
}
