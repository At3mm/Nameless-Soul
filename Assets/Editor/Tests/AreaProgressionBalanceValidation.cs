using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AreaProgressionBalanceValidation
{
    [Serializable] public sealed class Row
    {
        public string area;
        public int expectedPurchases,routePurchases,normalXP,eliteXP,bossXP,normalGold,eliteGold,bossGold;
        public float bossNormalHit,eliteNormalHit,bossHP;
    }
    [Serializable] public sealed class Report { public int checks; public List<Row> areas=new List<Row>(); }
    static void Check(bool ok,string reason,Report r){if(!ok)throw new InvalidOperationException("Balance validation: "+reason);r.checks++;}
    static float Hit(EnemyDamageProfile.Enemy e,EnemyDamageProfile.Skill s,float defense)=>Mathf.Max(0f,e.attack*s.damageMultiplier-defense)+e.trueDamage;
    static float ActualHit(GameObject victim,EnemyDamageProfile.Enemy enemy,EnemyDamageProfile.Skill skill,float defense)
    {
        var stats=victim.GetComponent<CharacterStats>();var health=victim.GetComponent<CharacterHealth>();
        typeof(CharacterHealth).GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(health,null);
        stats.maxHP=1000;stats.currentHP=1000;stats.DEF=defense;health.isDead=false;
        health.TakeDamage(enemy.attack*skill.damageMultiplier,enemy.trueDamage,0,false,null,0);
        return 1000-stats.currentHP;
    }
    static int Purchases(long xp)
    {
        int n=0;while(n<200){int cost=GameEconomy.Settings.UpgradeCost(n);if(cost<1||xp<cost)break;xp-=cost;n++;}return n;
    }
    [MenuItem("Tools/Enemies/Validate Area Progression Balance")]
    public static void ValidateMenu(){Debug.Log(JsonUtility.ToJson(Run(),true));}
    public static Report Run()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Run balance audit outside Play Mode.");
        var report=new Report();long xp=0;var normalCounts=new[]{4,13,21,11,12};float previousHit=0;int previousXP=0,previousGold=0;
        string savedWallet=PlayerPrefs.GetString(GameEconomy.SaveKey,"");
        for(int tier=0;tier<5;tier++)
        {
            var profile=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>(EnemyDamageBalanceSetup.ProfilePaths[tier]);
            var area=AssetDatabase.LoadAssetAtPath<World.AreaDefinition>("Assets/WorldAreas/area"+(tier+1)+".asset");
            Check(profile!=null&&area!=null&&area.enemyDamageProfile==profile,"unique profile binding for area "+(tier+1),report);
            Check(profile.useAreaRewards,"area rewards enabled",report);
            var boss=profile.Find("orc-rider");var elite=profile.Find("werebear");var normal=profile.Find("orc");
            float bh=Hit(boss,boss.skills[0],profile.referencePlayerDefense),eh=Hit(elite,elite.skills[0],profile.referencePlayerDefense);
            Check(Mathf.Abs(eh-bh*2f/3f)<.005f,"Elite normal hit equals 2/3 Boss HP damage",report);
            Check(bh>previousHit&&profile.normalExperience>previousXP&&profile.normalGold>previousGold,"area strength and rewards increase",report);
            Check(profile.bossExperience>profile.eliteExperience&&profile.eliteExperience>profile.normalExperience,"XP role order",report);
            Check(profile.bossGold>profile.eliteGold&&profile.eliteGold>profile.normalGold,"Gold role order",report);
            foreach(var entry in profile.enemies)
            {
                Check(entry.balanceVitality&&entry.maxHP>0&&entry.defense>=0&&entry.criticalChance==0,"valid deterministic stats: "+entry.archetypeId,report);
                foreach(var skill in entry.skills)Check(skill.damageMultiplier>0&&!float.IsNaN(skill.damageMultiplier)&&!float.IsInfinity(skill.damageMultiplier),"valid skill multiplier",report);
            }
            var dummy=new GameObject("Balance audit");dummy.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                var classification=dummy.AddComponent<EnemyClassification>();dummy.AddComponent<SkillDamageManager>();var damage=dummy.AddComponent<EnemyAreaDamage>();damage.archetypeId="orc";damage.profileOverride=profile;
                var currency=dummy.AddComponent<EnemyCurrencyReward>();
                foreach(EnemyClassification.EnemyType type in Enum.GetValues(typeof(EnemyClassification.EnemyType)))
                {
                    classification.enemyType=type;currency.GetBaseRewards(out int gold,out int exp);profile.Rewards(type,out int expectedGold,out int expectedXP);
                    Check(gold==expectedGold&&exp==expectedXP,"currency component resolves area for "+type,report);
                }
                currency.useDefaultRewards=false;currency.gold=7;currency.experience=13;currency.GetBaseRewards(out int customGold,out int customXP);
                Check(customGold==7&&customXP==13,"explicit rewards preserved",report);
                float actualBoss=ActualHit(dummy,boss,boss.skills[0],profile.referencePlayerDefense);
                float actualElite=ActualHit(dummy,elite,elite.skills[0],profile.referencePlayerDefense);
                Check(Mathf.Abs(actualBoss-bh)<.005f&&Mathf.Abs(actualElite-eh)<.005f,"actual CharacterHealth damage agrees with profile",report);
                Check(Mathf.Abs(actualElite-actualBoss*2f/3f)<.005f,"actual Elite/Boss ratio after DEF",report);
                currency.rewardsEnabled=false;dummy.GetComponent<CharacterHealth>().isDead=true;Check(!currency.TryClaim(),"disabled summon rewards cannot be claimed",report);
            }
            finally{UnityEngine.Object.DestroyImmediate(dummy);}
            xp+=(long)normalCounts[tier]*profile.normalExperience+(tier>0?profile.eliteExperience:0);
            int route=Purchases(xp);if(tier>0)Check(route>=Mathf.FloorToInt(profile.recommendedPurchases*.85f)&&route<=profile.recommendedPurchases,"route reaches upgrade milestone before boss",report);
            report.areas.Add(new Row{area=area.displayName,expectedPurchases=profile.recommendedPurchases,routePurchases=route,normalXP=profile.normalExperience,eliteXP=profile.eliteExperience,bossXP=profile.bossExperience,normalGold=profile.normalGold,eliteGold=profile.eliteGold,bossGold=profile.bossGold,bossNormalHit=bh,eliteNormalHit=eh,bossHP=boss.maxHP});
            if(tier==1)xp+=4000; // Existing Priest report reward, optional on the route.
            if(tier>0)xp+=profile.bossExperience;
            previousHit=bh;previousXP=profile.normalExperience;previousGold=profile.normalGold;
        }
        var bat=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/NecromancerBat.prefab").GetComponent<EnemyCurrencyReward>();Check(bat!=null&&!bat.rewardsEnabled,"summons remain unprofitable",report);
        Check(PlayerPrefs.GetString(GameEconomy.SaveKey,"")==savedWallet,"player wallet/save untouched by validation",report);
        Directory.CreateDirectory("Library/ProgressionBalance-20261120");File.WriteAllText("Library/ProgressionBalance-20261120/results.json",JsonUtility.ToJson(report,true));return report;
    }
}
