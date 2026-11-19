using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent one-time defeats. Ordinary enemies never enter this save.</summary>
public static class EnemyDefeatState
{
    public const string SaveKey="Noname.DefeatedEnemies.v1";
    [Serializable] private sealed class Record { public List<string> ids=new List<string>(); }
    static HashSet<string> defeated;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ReloadFromSave(){defeated=null;}
    static void Load()
    {
        if(defeated!=null)return;
        Record record=null;
        try{record=JsonUtility.FromJson<Record>(PlayerPrefs.GetString(SaveKey,""));}catch(ArgumentException){}
        defeated=record!=null && record.ids!=null?new HashSet<string>(record.ids):new HashSet<string>();
    }
    public static bool WasDefeated(string id){Load();return !string.IsNullOrEmpty(id) && defeated.Contains(id);}
    public static bool RecordDefeat(EnemyClassification enemy)
    {
        if(!Application.isPlaying || enemy==null || enemy.enemyType==EnemyClassification.EnemyType.Normal)return false;
        var health=enemy.GetComponent<CharacterHealth>();if(health==null || !health.isDead)return false;
        Load();if(!defeated.Add(enemy.DefeatId))return false;
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new Record{ids=new List<string>(defeated)}));PlayerPrefs.Save();return true;
    }
    public static void ClearSaved(){PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();ReloadFromSave();}
#if UNITY_EDITOR
    public static void ForgetForTesting(IEnumerable<string> ids)
    {
        Load();bool changed=false;foreach(var id in ids)changed|=defeated.Remove(id);
        if(!changed)return;
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new Record{ids=new List<string>(defeated)}));PlayerPrefs.Save();
    }
#endif
    public static string GetIdentity(EnemyClassification enemy)
    {
        if(!string.IsNullOrWhiteSpace(enemy.persistentId))return enemy.persistentId;
#if UNITY_EDITOR
        string sceneId=GetEditorSceneIdentity(enemy);if(!string.IsNullOrEmpty(sceneId))return sceneId;
#endif
        // Runtime-spawned encounters should provide a persistentId in their spawner.
        var path=enemy.name;var parent=enemy.transform.parent;
        while(parent!=null){path=parent.name+"/"+path;parent=parent.parent;}
        var p=enemy.transform.position;
        return enemy.gameObject.scene.path+"|"+path+"|"+p.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+p.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture);
    }
#if UNITY_EDITOR
    public static string GetEditorSceneIdentity(EnemyClassification enemy)
    {
        var global=UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(enemy);
        return global.identifierType==2 && global.targetObjectId!=0?enemy.gameObject.scene.path+"|"+global:null;
    }
#endif
}
