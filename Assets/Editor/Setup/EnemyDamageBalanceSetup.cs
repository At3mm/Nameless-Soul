using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnemyDamageBalanceSetup
{
    public static readonly string[] ProfilePaths={"Assets/Combat/Balance/01_AncientTrail.asset","Assets/Combat/Balance/02_FairyForest.asset","Assets/Combat/Balance/03_WildForest.asset","Assets/Combat/Balance/04_Deadlands.asset","Assets/Combat/Balance/05_Dungeon.asset"};
    public static readonly int[] Milestones={0,25,45,65,80};
    static readonly float[] BossHit={22,40,56,72,84},NormalHit={10,18,25,32,38},BossHP={450,1000,1450,1950,2400};
    static readonly int[] NormalXP={1500,3000,11000,31000,42000},EliteXP={3000,7000,20000,40000,60000},BossXP={12000,20000,60000,110000,180000},Gold={12,28,50,80,110};
    sealed class Archetype{public string id;public EnemyClassification.EnemyType kind;public string[] skills;}
    [MenuItem("Tools/Enemies/Apply Area Damage Balance")]
    public static void Apply(){ApplyProfiles(false);}
    [MenuItem("Tools/Enemies/Reset Area Progression Defaults")]
    public static void ResetDefaults(){ApplyProfiles(true);}
    static void ApplyProfiles(bool resetDefaults)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before applying balance.");
        Directory.CreateDirectory("Assets/Combat/Balance");AssetDatabase.Refresh();
        var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Characters"});
        var archetypes=new Dictionary<string,Archetype>();
        foreach(string guid in prefabs)
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));var area=go.GetComponent<EnemyAreaDamage>();var classification=go.GetComponent<EnemyClassification>();var manager=go.GetComponent<SkillDamageManager>();
            if(area==null||classification==null||manager==null||string.IsNullOrEmpty(area.archetypeId))continue;
            if(!archetypes.ContainsKey(area.archetypeId))archetypes.Add(area.archetypeId,new Archetype{id=area.archetypeId,kind=classification.enemyType,skills=manager.skills.ConvertAll(s=>s.skillName).ToArray()});
        }
        var profiles=new EnemyDamageProfile[5];
        for(int i=0;i<5;i++)
        {
            profiles[i]=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>(ProfilePaths[i]);
            bool created=profiles[i]==null;
            if(created){profiles[i]=ScriptableObject.CreateInstance<EnemyDamageProfile>();AssetDatabase.CreateAsset(profiles[i],ProfilePaths[i]);}
            if(created||resetDefaults)Configure(profiles[i],i,archetypes.Values);
            string areaPath="Assets/WorldAreas/area"+(i+1)+".asset";
            var world=AssetDatabase.LoadAssetAtPath<World.AreaDefinition>(areaPath);
            if(world!=null){Undo.RecordObject(world,"Bind area balance");world.enemyDamageProfile=profiles[i];EditorUtility.SetDirty(world);}
        }
        var final=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>("Assets/Combat/Balance/BlackKnightBoss.asset");
        if(final!=null&&resetDefaults)Configure(final,4,new[]{archetypes["black-knight-boss"]});
        foreach(string guid in prefabs)
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var source=asset.GetComponent<EnemyAreaDamage>();if(source==null||!archetypes.ContainsKey(source.archetypeId))continue;
            var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var damage=go.GetComponent<EnemyAreaDamage>();damage.fallbackProfile=profiles[FallbackTier(damage.archetypeId)];
                damage.ApplyForSpawn();PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        AssetDatabase.SaveAssets();
        int count=0;
        foreach(string path in new[]{"Assets/Scenes/WastelandScene.unity","Assets/Scenes/CaveScene.unity"})
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            // Unloaded scenes use their area profile at runtime; do not load two lighting setups together.
            if(opened)continue;
            try
            {
                Physics2D.SyncTransforms();
                foreach(var root in scene.GetRootGameObjects())foreach(var damage in root.GetComponentsInChildren<EnemyAreaDamage>(true))
                {
                    var stats=damage.GetComponent<CharacterStats>();var skills=damage.GetComponent<SkillDamageManager>();
                    Undo.RecordObjects(new UnityEngine.Object[]{stats,skills},"Apply spawn area progression balance");
                    if(!damage.ApplyForSpawn())continue;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(stats);PrefabUtility.RecordPrefabInstancePropertyModifications(skills);count++;
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.SaveAssets();Debug.Log("Applied five-area progression balance to loaded "+count+" enemies. Moves, animation events, phase rules, break timing, encounter IDs and save data preserved.");
    }
    static int FallbackTier(string id)
    {
        switch(id)
        {
            case "orc-rider":case "werebear":return 1;
            case "elite-orc":case "black-knight":case "necromancer":case "necromancer-bat":return 2;
            case "skeleton":case "armor-skeleton":case "greatsword-skeleton":return 3;
            case "black-knight-boss":case "demon-c":case "flame-golem":case "lava-slime":case "minotaur":return 4;
            default:return 0;
        }
    }
    static float HealthWeight(string id)
    {
        switch(id){case "slime":case "skeleton":return .8f;case "lava-slime":return .75f;case "werewolf":return .9f;case "armor-skeleton":return 1.3f;case "flame-golem":return 1.5f;case "necromancer-bat":return .25f;default:return 1;}
    }
    static float Potency(string id,string skill,int index)
    {
        if(index==0)return 1;
        if(id=="orc-rider"&&skill=="HeavyAttack")return .7f;
        if((id=="werebear"||id=="elite-orc")&&skill=="Skill2")return .65f;
        return 1.35f;
    }
    static void Configure(EnemyDamageProfile p,int tier,IEnumerable<Archetype> archetypes)
    {
        Undo.RecordObject(p,"Configure progression profile");
        p.recommendedPurchases=Milestones[tier];p.referencePlayerDefense=10f+Mathf.FloorToInt(Milestones[tier]*.1f)*GameEconomy.Settings.defensePerPurchase;
        p.maxComboHPFraction=.45f;p.useAreaRewards=true;p.normalGold=Gold[tier];p.eliteGold=Gold[tier]*5;p.bossGold=Gold[tier]*15;p.normalExperience=NormalXP[tier];p.eliteExperience=EliteXP[tier];p.bossExperience=BossXP[tier];
        p.notes="Progression reference: "+Milestones[tier]+" total purchases, roughly 40% HP / 30% ATK / 10% DEF / remainder Stamina. Boss normal hit removes "+BossHit[tier]+" HP at reference DEF; matching Elite hit is exactly 2/3. Damage and rewards remain tied to spawn. Moderate pacing: 85-95% purchase milestones on a single clear; boss reward about 2-3 upgrades. Combo HP budget 45%. Flame Golem is a lighter single-phase boss. No gate on boss access. Necromancer summons grant no currency.";
        p.enemies.Clear();
        foreach(var a in archetypes)
        {
            bool boss=a.kind==EnemyClassification.EnemyType.Boss,elite=a.kind==EnemyClassification.EnemyType.Elite;
            float damage=boss?BossHit[tier]:elite?BossHit[tier]*2f/3f:NormalHit[tier];
            if(a.id=="necromancer-bat")damage*=.5f;
            float hp=boss?BossHP[tier]:elite?BossHP[tier]*.35f:new[]{60f,90,120,150,180}[tier]*HealthWeight(a.id);
            float defense=boss?new[]{5f,7,9,11,13}[tier]:elite?new[]{4f,6,8,10,12}[tier]:new[]{2f,3,4,5,6}[tier];
            if(a.id=="armor-skeleton")defense+=2;
            if(a.id=="flame-golem"){damage=BossHit[tier]*.8f;hp=BossHP[tier]*.7f;defense=Mathf.Max(0,new[]{5f,7,9,11,13}[tier]-2);}
            if(a.id=="necromancer-bat")defense=0;
            var e=new EnemyDamageProfile.Enemy{archetypeId=a.id,attack=p.referencePlayerDefense+damage,trueDamage=0,criticalChance=0,criticalMultiplier=1.5f,balanceVitality=true,maxHP=Mathf.Round(hp),defense=defense};
            for(int i=0;i<a.skills.Length;i++)e.skills.Add(new EnemyDamageProfile.Skill{skillName=a.skills[i],damageMultiplier=(p.referencePlayerDefense+damage*Potency(a.id,a.skills[i],i))/e.attack});
            p.enemies.Add(e);
        }
        EditorUtility.SetDirty(p);
    }
}
