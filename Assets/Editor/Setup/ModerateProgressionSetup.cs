using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reproducible moderate progression defaults; does not modify animation clips or player saves.</summary>
public static class ModerateProgressionSetup
{
    public const string GolemPath="Assets/Prefabs/Characters/FlameGolem.prefab";
    public const string RelicPath="Assets/Resources/BossRewards/FlameGolemMemory.asset";
    [MenuItem("Tools/Enemies/Apply Moderate Progression")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        // Save current editor edits before opening another scene; the operation preserves existing content.
        for(int i=0;i<SceneManager.sceneCount;i++)
        {
            var scene=SceneManager.GetSceneAt(i);
            if(scene.isDirty&&!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save "+scene.path);
        }
        float factor=1;
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Sprites/Flame Golem"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(path.Contains("_Idle"))factor=importer.spritePixelsPerUnit/10f;
        }
        var relic=AssetDatabase.LoadAssetAtPath<BossItemDefinition>(RelicPath);
        if(relic==null)
        {
            relic=ScriptableObject.CreateInstance<BossItemDefinition>();relic.itemId="flame-golem-memory";
            relic.displayName="Lõi hỏa thạch";
            relic.description="Lõi đá còn cháy âm ỉ của Flame Golem. Minh chứng chiến thắng trước kẻ canh giữ dung nham.";
            relic.category=ItemCategory.BossRelic;relic.roles=ItemRole.BossEvidence;relic.rarity=ItemRarity.Epic;
            relic.maxStack=1;relic.consumable=false;relic.icon=ConsumableCatalog.Default.Find(ConsumableKind.BurnRemedy).icon;
            AssetDatabase.CreateAsset(relic,RelicPath);
        }
        var go=PrefabUtility.LoadPrefabContents(GolemPath);
        try
        {
            var c=go.GetComponent<EnemyClassification>();c.enemyType=EnemyClassification.EnemyType.Boss;go.tag="EnemyBoss";
            if(factor!=1)
            {
                foreach(var box in go.GetComponents<BoxCollider2D>()){box.size*=factor;box.offset*=factor;}
                foreach(var skill in go.GetComponent<SkillDamageManager>().skills){skill.attackRange*=factor;skill.hitboxOffset*=factor;}
                var point=go.transform.Find("AttackPoint");if(point!=null)point.localPosition*=factor;
                var breaks=go.GetComponent<EnemyBreak>();if(breaks!=null)breaks.barHeight*=factor;
            }
            var capsule=go.GetComponent<CapsuleCollider2D>();
            var spec=CaveCreatureSetup.Specs.First(s=>s.prefix=="FlameGolem");
            if(capsule!=null){capsule.size=spec.body;capsule.offset=spec.offset;}
            var phase=go.GetComponent<BossPhaseController>()??go.AddComponent<BossPhaseController>();
            phase.useSecondPhase=false;phase.idleState="FlameGolemIdle";phase.transitionState="TakeDamage";
            phase.phase1Skills=go.GetComponent<SkillDamageManager>().skills.ConvertAll(s=>s.skillName);
            phase.phase2Skills=new System.Collections.Generic.List<string>(phase.phase1Skills);
            var director=go.GetComponent<BossMovesetDirector>()??go.AddComponent<BossMovesetDirector>();
            director.maxComboLength=2;director.comboChance=.55f;director.reactionDelay=.45f;director.stepGap=.3f;director.recoveryTime=1.2f;
            if(director.combos.Count==0)director.combos.Add(new BossMovesetDirector.Combo{name="Stone pressure",responseTo=BossMovesetDirector.PlayerMove.Any,maxDistance=3.8f,skills=new[]{"NormalAttack","HeavyAttack"}});
            var ai=go.GetComponent<EnemyBehavior>();ai.minHesitationTime=.65f;ai.maxHesitationTime=1.4f;
            var manager=go.GetComponent<SkillDamageManager>();
            for(int i=0;i<manager.skills.Count;i++)manager.skills[i].cooldownTime=Mathf.Max(manager.skills[i].cooldownTime,i==0?1.6f:i==1?5f:7f);
            var reward=go.GetComponent<EnemyCurrencyReward>()??go.AddComponent<EnemyCurrencyReward>();
            reward.useDefaultRewards=false;reward.gold=1320;reward.experience=150000;
            var item=go.GetComponent<BossItemReward>()??go.AddComponent<BossItemReward>();item.item=relic;
            c.RefreshWorldHealthBars();
            PrefabUtility.SaveAsPrefabAsset(go,GolemPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(go);}
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Sprites/Flame Golem"}))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if(!Mathf.Approximately(importer.spritePixelsPerUnit,10)){importer.spritePixelsPerUnit=10;importer.SaveAndReimport();}
        }
        var economy=Resources.Load<EconomySettings>("DefaultEconomy");economy.potionSeedCosts=new[]{1,1,2,2,2,2,2};EditorUtility.SetDirty(economy);
        var seed=LifeSeedItem.Definition;
        seed.description="Tự nhận khi hạ Elite hoặc Boss. Hiến tế tại Tượng thần để tăng sức chứa bình máu từ 3 lên tối đa 10. Hai lần tăng đầu cần 1 hạt/lần, các lần sau cần 2 hạt/lần. Không dùng trực tiếp hoặc gắn vào ô nhanh.";
        EditorUtility.SetDirty(seed);
        AssetDatabase.SaveAssets();
        try
        {
            foreach(string path in new[]{"Assets/Scenes/WastelandScene.unity","Assets/Scenes/CaveScene.unity"})
                if(!SceneManager.GetSceneByPath(path).isLoaded)EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            EnemyDamageBalanceSetup.ResetDefaults();
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);if(scene.path!="Assets/Scenes/WastelandScene.unity"&&scene.path!="Assets/Scenes/CaveScene.unity")continue;
                foreach(var root in scene.GetRootGameObjects())foreach(var damage in root.GetComponentsInChildren<EnemyAreaDamage>(true))
                {
                    var c=damage.GetComponent<EnemyClassification>();var director=damage.GetComponent<BossMovesetDirector>();
                    if(c.enemyType==EnemyClassification.EnemyType.Boss&&director!=null)
                    {
                        director.reactionDelay=Mathf.Max(.35f,director.reactionDelay);director.stepGap=Mathf.Max(.25f,director.stepGap);
                        director.recoveryTime=Mathf.Max(1f,director.recoveryTime);director.comboChance=Mathf.Min(.7f,director.comboChance);
                        EditorUtility.SetDirty(director);PrefabUtility.RecordPrefabInstancePropertyModifications(director);
                    }
                    if(damage.archetypeId=="flame-golem")
                    {
                        c.enemyType=EnemyClassification.EnemyType.Boss;damage.gameObject.tag="EnemyBoss";
                        var reward=damage.GetComponent<EnemyCurrencyReward>();if(reward!=null){reward.useDefaultRewards=false;reward.experience=150000;reward.gold=1320;PrefabUtility.RecordPrefabInstancePropertyModifications(reward);}
                        c.RefreshWorldHealthBars();EditorUtility.SetDirty(c);PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            foreach(var sceneSetup in setup)
                if(sceneSetup.isActive){var scene=SceneManager.GetSceneByPath(sceneSetup.path);if(scene.IsValid())SceneManager.SetActiveScene(scene);}
            for(int i=SceneManager.sceneCount-1;i>=0;i--)
            {
                var scene=SceneManager.GetSceneAt(i);
                if(!setup.Any(s=>s.path==scene.path))EditorSceneManager.CloseScene(scene,true);
            }
        }
        // Prefabs placed later use the same moderate recovery windows.
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Characters"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.GetComponent<BossMovesetDirector>()==null)continue;
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var d=prefab.GetComponent<BossMovesetDirector>();d.reactionDelay=Mathf.Max(.35f,d.reactionDelay);d.stepGap=Mathf.Max(.25f,d.stepGap);
                d.recoveryTime=Mathf.Max(1f,d.recoveryTime);d.comboChance=Mathf.Min(.7f,d.comboChance);
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Moderate progression applied; Flame Golem boss uses 10 PPU. Animation clips and saves preserved.");
    }
}
