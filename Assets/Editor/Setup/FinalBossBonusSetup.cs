using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FinalBossBonusSetup
{
    [MenuItem("Tools/Enemies/Rebuild Final Boss Victory Bonus")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        const string prefabPath="Assets/Prefabs/Characters/BlackKnightBoss.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var bonus=prefab.GetComponent<FinalBossDamageBonus>()??prefab.AddComponent<FinalBossDamageBonus>();bonus.bonusPerBoss=.05f;
            PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var catalog=AssetDatabase.LoadAssetAtPath<BossDefeatCatalog>("Assets/Resources/BossDefeatCatalog.asset");
        if(catalog==null){catalog=ScriptableObject.CreateInstance<BossDefeatCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Resources/BossDefeatCatalog.asset");}
        var ids=new HashSet<string>();
        foreach(string path in new[]{"Assets/Scenes/WastelandScene.unity","Assets/Scenes/CaveScene.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                foreach(var root in scene.GetRootGameObjects())foreach(var enemy in root.GetComponentsInChildren<EnemyClassification>(true))
                    if(enemy.enemyType==EnemyClassification.EnemyType.Boss&&enemy.questBossId!="black-knight-boss"&&enemy.GetComponent<FinalBossDamageBonus>()==null)
                        ids.Add(enemy.DefeatId);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        catalog.bossEncounterIds=ids.OrderBy(id=>id,StringComparer.Ordinal).ToList();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        EnemyDefeatState.ReloadFromSave();
        Debug.Log("Final boss hidden bonus configured; "+ids.Count+" previous boss encounters recognized.");
    }
}
