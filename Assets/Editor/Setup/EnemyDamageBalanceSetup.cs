using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnemyDamageBalanceSetup
{
    const string Folder="Assets/Combat/Balance/";
    [MenuItem("Tools/Enemies/Apply Area Damage Balance")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        var profiles=new[]{Profile("01_AncientTrail",0),Profile("02_FairyForest",1),Profile("03_WildForest",2),Profile("04_Deadlands",3)};
        var areas=new[]{"Assets/WorldAreas/area11.asset","Assets/WorldAreas/area12.asset","Assets/WorldAreas/area2.asset","Assets/WorldAreas/area3.asset"};
        for(int i=0;i<areas.Length;i++){var area=AssetDatabase.LoadAssetAtPath<World.AreaDefinition>(areas[i]);if(area!=null&&area.enemyDamageProfile==null){Undo.RecordObject(area,"Assign area damage preset");area.enemyDamageProfile=profiles[i];EditorUtility.SetDirty(area);}}
        var names=new[]{"Slime","Orc","Werewolf","Werebear","EliteOrc","OrcRider","Necromancer","NecromancerBat"};
        var ids=new[]{"slime","orc","werewolf","werebear","elite-orc","orc-rider","necromancer","necromancer-bat"};
        for(int i=0;i<names.Length;i++)
        {
            string path="Assets/Prefabs/Characters/"+names[i]+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
            try{var damage=go.GetComponent<EnemyAreaDamage>();if(damage==null)damage=go.AddComponent<EnemyAreaDamage>();if(string.IsNullOrEmpty(damage.archetypeId))damage.archetypeId=ids[i];if(damage.fallbackProfile==null)damage.fallbackProfile=profiles[0];PrefabUtility.SaveAsPrefabAsset(go,path);}
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        AssetDatabase.SaveAssets();Physics2D.SyncTransforms();int count=0;var group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Balance enemy damage by spawn area");
        foreach(var damage in Object.FindObjectsByType<EnemyAreaDamage>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(!damage.gameObject.scene.IsValid())continue;
            var stats=damage.GetComponent<CharacterStats>();var skills=damage.GetComponent<SkillDamageManager>();Undo.RecordObject(stats,"Balance enemy attack");Undo.RecordObject(skills,"Balance enemy skills");
            if(!damage.ApplyForSpawn())continue;count++;PrefabUtility.RecordPrefabInstancePropertyModifications(stats);PrefabUtility.RecordPrefabInstancePropertyModifications(skills);EditorSceneManager.MarkSceneDirty(damage.gameObject.scene);
        }
        Undo.CollapseUndoOperations(group);AssetDatabase.SaveAssets();Debug.Log("Applied area damage presets to "+count+" placed enemies. HP, clips, events, cooldowns and phase selections were preserved.");
    }
    static EnemyDamageProfile Profile(string name,int tier)
    {
        string path=Folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>(path);if(existing!=null)return existing;
        var profile=ScriptableObject.CreateInstance<EnemyDamageProfile>();profile.notes="Tier "+(tier+1)+". Reference: player 100 HP / 10 DEF, 1 second hit immunity. Enemy critical chance is zero. Spawn area stays fixed during chase.";
        Add(profile,"slime",new[]{18f,20,23,26}[tier],new[]{0f,1,1,2}[tier],new[]{"NormalAttack","HeavyAttack"},new[]{.85f,1.35f});
        Add(profile,"orc",new[]{20f,22,25,28}[tier],new[]{0f,1,2,2}[tier],new[]{"NormalAttack","HeavyAttack"},new[]{1f,1.4f});
        Add(profile,"werewolf",new[]{22f,24,27,30}[tier],new[]{1f,1,2,2}[tier],new[]{"NormalAttack","TeleportAttack"},new[]{1f,1.25f});
        Add(profile,"werebear",new[]{27f,29,32,35}[tier],new[]{1f,2,2,3}[tier],new[]{"Skill1","Skill2","Skill3"},new[]{.95f,.8f,1.45f});
        Add(profile,"elite-orc",new[]{28f,30,33,36}[tier],new[]{1f,2,2,3}[tier],new[]{"Skill1","Skill2","Skill3"},new[]{.95f,.75f,1.4f});
        Add(profile,"orc-rider",new[]{26f,28,31,34}[tier],new[]{2f,2,3,3}[tier],new[]{"NormalAttack","HeavyAttack","Skill3"},new[]{1f,1.4f,1.1f});
        Add(profile,"necromancer",new[]{28f,31,34,37}[tier],new[]{2f,3,3,3}[tier],new[]{"NormalAttack","HeavyAttack","Summon"},new[]{1f,1.25f,1.5f});
        Add(profile,"necromancer-bat",new[]{13f,14,15,16}[tier],1,new[]{"NormalAttack","HeavyAttack"},new[]{1f,1.4f});
        AssetDatabase.CreateAsset(profile,path);return profile;
    }
    static void Add(EnemyDamageProfile profile,string id,float attack,float trueDamage,string[] names,float[] values)
    {
        var enemy=new EnemyDamageProfile.Enemy{archetypeId=id,attack=attack,trueDamage=trueDamage,criticalChance=0};
        for(int i=0;i<names.Length;i++)enemy.skills.Add(new EnemyDamageProfile.Skill{skillName=names[i],damageMultiplier=values[i]});profile.enemies.Add(enemy);
    }
}
