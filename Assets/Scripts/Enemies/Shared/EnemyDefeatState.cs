using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Persistent one-time defeats. Ordinary enemies never enter this save.</summary>
public static class EnemyDefeatState
{
    public const string SaveKey="Noname.DefeatedEnemies.v1";
    [Serializable] private sealed class Record { public List<string> ids=new List<string>(); public List<string> bossIds=new List<string>(); }
    static HashSet<string> defeated,defeatedBosses;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ReloadFromSave(){defeated=null;defeatedBosses=null;}
    static void Load()
    {
        if(defeated!=null)return;
        Record record=null;
        try{record=JsonUtility.FromJson<Record>(PlayerPrefs.GetString(SaveKey,""));}catch(ArgumentException){}
        defeated=record!=null && record.ids!=null?new HashSet<string>(record.ids):new HashSet<string>();
        defeatedBosses=new HashSet<string>();
        if(record!=null&&record.bossIds!=null)
            foreach(string id in record.bossIds)if(!string.IsNullOrEmpty(id)&&defeated.Contains(id))defeatedBosses.Add(id);
        // Older saves recorded Boss and Elite IDs together. Only known boss encounters migrate.
        var catalog=Resources.Load<BossDefeatCatalog>("BossDefeatCatalog");
        if(catalog!=null)foreach(string id in catalog.bossEncounterIds)
            if(!string.IsNullOrEmpty(id)&&defeated.Contains(id))defeatedBosses.Add(id);
    }
    public static int DefeatedBossCount { get { Load();return defeatedBosses.Count; } }
    public static bool WasDefeated(string id){Load();return !string.IsNullOrEmpty(id) && defeated.Contains(id);}
    public static bool RecordDefeat(EnemyClassification enemy)
    {
        if(!Application.isPlaying || enemy==null || enemy.enemyType==EnemyClassification.EnemyType.Normal)return false;
        var health=enemy.GetComponent<CharacterHealth>();if(health==null || !health.isDead)return false;
        Load();bool added=defeated.Add(enemy.DefeatId);
        bool bossAdded=enemy.enemyType==EnemyClassification.EnemyType.Boss&&enemy.questBossId!="black-knight-boss"&&
            enemy.GetComponent<FinalBossDamageBonus>()==null&&defeatedBosses.Add(enemy.DefeatId);
        if(!added&&!bossAdded)return false;
        SaveRecord();return added;
    }
    static void SaveRecord()
    {
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(new Record{ids=new List<string>(defeated),bossIds=new List<string>(defeatedBosses)}));PlayerPrefs.Save();
    }
    public static void ClearSaved(){PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();ReloadFromSave();}
#if UNITY_EDITOR
    public static void ForgetForTesting(IEnumerable<string> ids)
    {
        Load();bool changed=false;foreach(var id in ids){changed|=defeated.Remove(id);defeatedBosses.Remove(id);}
        if(!changed)return;
        SaveRecord();
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
