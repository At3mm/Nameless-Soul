using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class ModerateProgressionValidation
{
    static readonly List<string> results=new List<string>();
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);results.Add("PASS "+message);}
    public static string RunAssets()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Asset audit requires Edit Mode.");
        results.Clear();var profile=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>(EnemyDamageBalanceSetup.ProfilePaths[4]);
        var flame=profile.Find("flame-golem");var knight=profile.Find("black-knight-boss");
        Check(flame.maxHP<knight.maxHP&&flame.attack<knight.attack&&flame.defense<knight.defense,"Flame Golem is weaker than final Black Knight in HP, ATK and DEF");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ModerateProgressionSetup.GolemPath);
        Check(prefab.CompareTag("EnemyBoss")&&prefab.GetComponent<EnemyClassification>().enemyType==EnemyClassification.EnemyType.Boss,"Flame Golem boss classification");
        Check(!prefab.GetComponent<BossPhaseController>().useSecondPhase,"Flame Golem single phase");
        Check(prefab.GetComponent<BossItemReward>().item!=null&&prefab.GetComponent<BossItemReward>().item.icon!=null,"Flame Golem relic and icon");
        var rewards=prefab.GetComponent<EnemyCurrencyReward>();
        Check(!rewards.useDefaultRewards&&rewards.experience==150000,"Flame Golem lower currency reward");
        var body=prefab.GetComponent<CapsuleCollider2D>();
        Check(Mathf.Abs(body.size.x-2.375f)<.001f&&Mathf.Abs(body.size.y-2.75f)<.001f,"Flame Golem collider matches enlarged sprite");
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Sprites/Flame Golem"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Check(importer.spritePixelsPerUnit==10&&importer.filterMode==FilterMode.Point&&!importer.mipmapEnabled,"10 PPU and crisp sprites: "+Path.GetFileName(path));
        }
        for(int tier=1;tier<5;tier++)
        {
            var p=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>(EnemyDamageBalanceSetup.ProfilePaths[tier]);
            int cost=GameEconomy.Settings.UpgradeCost(p.recommendedPurchases);
            Check(p.bossExperience>=cost*2&&p.bossExperience<=cost*3,"Boss reward buys 2-3 reference upgrades, tier "+(tier+1));
            Check(p.eliteExperience>=cost*.7f&&p.eliteExperience<=cost,"Elite reward buys .7-1 reference upgrade, tier "+(tier+1));
            Check(p.maxComboHPFraction>=.35f&&p.maxComboHPFraction<=.45f,"Finite multi-hit budget, tier "+(tier+1));
            Check(World.NpcShop.AreaBuyPrice(25,p.normalGold)==p.normalGold*4,"Useful medicine purchase equals four ordinary enemies, tier "+(tier+1));
        }
        var legacy=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>("Assets/Combat/Balance/BlackKnightBoss.asset");
        Check(legacy.Find("black-knight-boss").maxHP==knight.maxHP&&legacy.bossExperience==profile.bossExperience,"Legacy Black Knight override stays consistent");
        int seeds=0;for(int i=0;i<7;i++)seeds+=GameEconomy.Settings.PotionSeedCost(i);
        Check(seeds==12&&GameEconomy.Settings.PotionSeedCost(7)==0,"Seed progression costs twelve seeds, hard cap at ten flasks");
        return Save("assets.txt");
    }
    public static string RunPlay()
    {
        if(!Application.isPlaying||!GameFreshRunTool.IsTesting)throw new InvalidOperationException("Run only in the isolated fresh-run test slot.");
        results.Clear();GameObject player=null,boss=null,second=null;
        try
        {
            player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"));
            player.transform.position=new Vector3(10000,10000,0);
            player.GetComponent<PlayerController>().enabled=false;player.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
            var stats=player.GetComponent<CharacterStats>();stats.maxHP=stats.currentHP=1000;stats.DEF=0;
            var health=player.GetComponent<CharacterHealth>();health.damageInvulnerabilityDuration=0;
            var immunity=player.GetComponent<CharacterInvulnerability>();if(immunity!=null)immunity.isInvulnerable=false;
            boss=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModerateProgressionSetup.GolemPath));
            boss.transform.position=new Vector3(10010,10000,0);boss.GetComponent<EnemyBehavior>().enabled=false;
            boss.GetComponent<EnemyClassification>().persistentId="moderate-balance-test-golem";
            var manager=boss.GetComponent<SkillDamageManager>();
            var damage=boss.GetComponent<EnemyAreaDamage>();damage.profileOverride=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>(EnemyDamageBalanceSetup.ProfilePaths[4]);damage.ApplyForSpawn();
            manager.BeginDamageSequence();
            for(int i=0;i<4;i++){if(immunity!=null)immunity.isInvulnerable=false;health.TakeDamage(200,0,0,false,boss.transform);}
            Check(Mathf.Abs(stats.currentHP-550)<.01f,"Actual four-hit attack stops at 45% of maximum player HP");
            health.TakeDamageOverTime(10);Check(Mathf.Abs(stats.currentHP-540)<.01f,"Environmental damage is independent of combo budget");
            if(immunity!=null)immunity.isInvulnerable=false;
            manager.ResetCooldowns();manager.skills[0].lastUsedTime=-100;
            Check(manager.MarkSkillUsed(manager.skills[0].skillName),"Next valid attack starts");
            health.TakeDamage(100,0,0,false,boss.transform);Check(Mathf.Abs(stats.currentHP-440)<.01f,"Next attack gets a fresh damage budget");
            stats.currentHP=1000;manager.BeginDamageSequence();
            if(immunity!=null)
            {
                immunity.isInvulnerable=true;health.TakeDamage(200,0,0,false,boss.transform);
                Check(stats.currentHP==1000,"Invulnerable player takes no damage");
                immunity.isInvulnerable=false;
            }
            for(int i=0;i<3;i++)health.TakeDamage(200,0,0,false,boss.transform);
            Check(Mathf.Abs(stats.currentHP-550)<.01f,"Dodged hit does not spend the later damage budget");
            second=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModerateProgressionSetup.GolemPath));
            second.transform.position=new Vector3(10020,10000,0);second.GetComponent<EnemyBehavior>().enabled=false;
            second.GetComponent<EnemyAreaDamage>().ApplyForSpawn();
            health.TakeDamage(100,0,0,false,second.transform);
            Check(Mathf.Abs(stats.currentHP-450)<.01f,"Independent enemies have independent budgets");
            var director=boss.GetComponent<BossMovesetDirector>();
            director.OnSkillStarted("NormalAttack",player.transform,boss.transform.position,100);
            manager.skills[1].lastUsedTime=-100;Check(manager.MarkSkillUsed(manager.skills[1].skillName),"Directed follow-up skill starts");
            float before=stats.currentHP;health.TakeDamage(100,0,0,false,boss.transform);
            Check(Mathf.Abs(stats.currentHP-before)<.01f,"Follow-up skill does not reset the active combo budget");
            director.CancelCombo();
            var potion=player.GetComponent<PlayerHealthPotion>();Check(potion.capacity==3,"Fresh game starts with three flasks");
            GameEconomy.GrantLifeSeedItem(3);
            Check(GameEconomy.SacrificeLifeSeed(potion)&&GameEconomy.SacrificeLifeSeed(potion)&&potion.capacity==5,"First two capacity upgrades cost one seed each");
            Check(GameEconomy.LifeSeeds==1&&!GameEconomy.SacrificeLifeSeed(potion)&&GameEconomy.LifeSeeds==1,"Insufficient seeds do not partly consume inventory");
            GameEconomy.GrantLifeSeedItem(10);
            for(int i=0;i<5;i++)Check(GameEconomy.SacrificeLifeSeed(potion),"Two-seed capacity upgrade "+(i+3));
            Check(potion.capacity==10&&GameEconomy.LifeSeeds==1&&!GameEconomy.SacrificeLifeSeed(potion),"Ten-flask cap leaves spare seed intact");
            GameEconomy.ReloadFromSave();Check(potion.capacity==10&&GameEconomy.PotionCapacityUpgrades==7,"Capacity upgrades persist after reloading wallet");
            int gold=GameEconomy.Gold,xp=GameEconomy.Experience,seeds=GameEconomy.LifeSeeds;
            var rewardItem=boss.GetComponent<BossItemReward>().item;
            boss.GetComponent<CharacterHealth>().TakeDamageOverTime(100000);
            Check(GameEconomy.Gold-gold==1320&&GameEconomy.Experience-xp==150000,"Flame Golem death grants configured Gold and EXP");
            Check(GameEconomy.LifeSeeds-seeds==1,"Flame Golem death directly grants one life seed");
            Check(ItemInventory.Count(rewardItem)==1,"Flame Golem death directly grants its relic");
            Check(EnemyDefeatState.WasDefeated(boss.GetComponent<EnemyClassification>().DefeatId),"Flame Golem defeat is saved");
            Check(!boss.GetComponent<EnemyCurrencyReward>().TryClaim()&&ItemInventory.Count(rewardItem)==1,"Rewards cannot be claimed twice");
            return Save("runtime.txt");
        }
        finally
        {
            if(player!=null)Object.Destroy(player);if(boss!=null)Object.Destroy(boss);if(second!=null)Object.Destroy(second);
        }
    }
    static string Save(string name)
    {
        Directory.CreateDirectory("Library/ModerateBalance-20261120");
        string report=string.Join(Environment.NewLine,results);File.WriteAllText("Library/ModerateBalance-20261120/"+name,report);return report;
    }
}
