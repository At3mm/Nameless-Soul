using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class FinalBossBonusValidation
{
    [Serializable] sealed class LegacySave { public List<string> ids=new List<string>(); }
    static readonly List<string> results=new List<string>();
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);results.Add("PASS "+message);}
    static bool Defeat(string id,EnemyClassification.EnemyType type,bool final=false)
    {
        var go=new GameObject("Bonus test defeat");go.SetActive(false);
        try
        {
            var enemy=go.AddComponent<EnemyClassification>();enemy.enemyType=type;enemy.persistentId=id;enemy.questBossId=final?"black-knight-boss":"necromancer";
            if(final)go.AddComponent<FinalBossDamageBonus>();
            go.SetActive(true);enemy.GetComponent<CharacterHealth>().isDead=true;
            return EnemyDefeatState.RecordDefeat(enemy);
        }
        finally{Object.Destroy(go);}
    }
    public static string Run()
    {
        if(!Application.isPlaying||!GameFreshRunTool.IsTesting)throw new InvalidOperationException("Requires isolated fresh-run Play Mode.");
        results.Clear();GameObject player=null,target=null,other=null,enemySource=null;
        try
        {
            EnemyDefeatState.ClearSaved();
            player=new GameObject("Bonus test player");player.AddComponent<PlayerController>().enabled=false;
            target=new GameObject("Bonus test final boss");var stats=target.AddComponent<CharacterStats>();stats.maxHP=stats.currentHP=1000;stats.DEF=50;
            var health=target.AddComponent<CharacterHealth>();target.AddComponent<FinalBossDamageBonus>();
            Check(EnemyDefeatState.DefeatedBossCount==0,"New run starts without bonus");
            health.TakeDamage(70,0,0,false,player.transform);Check(Mathf.Abs(stats.currentHP-980)<.001f,"Zero victories: actual post-defense hit is 20");
            Defeat("bonus-normal",EnemyClassification.EnemyType.Normal);
            Defeat("bonus-elite",EnemyClassification.EnemyType.Elite);
            Check(EnemyDefeatState.DefeatedBossCount==0,"Ordinary enemies and Elite do not count");
            Check(Defeat("bonus-necro-1",EnemyClassification.EnemyType.Boss),"First boss victory recorded");
            stats.currentHP=1000;health.TakeDamage(70,0,0,false,player.transform);
            Check(Mathf.Abs(stats.currentHP-979)<.001f,"One boss: exactly 5% more post-defense HP damage");
            Check(!Defeat("bonus-necro-1",EnemyClassification.EnemyType.Boss)&&EnemyDefeatState.DefeatedBossCount==1,"Repeated encounter does not stack");
            Defeat("bonus-necro-2",EnemyClassification.EnemyType.Boss);
            Check(EnemyDefeatState.DefeatedBossCount==2,"Two encounters of the same boss type count separately");
            stats.currentHP=1000;health.TakeDamage(70,0,0,false,player.transform);
            Check(Mathf.Abs(stats.currentHP-978)<.001f,"Two bosses: additive 10%, not compounded");
            for(int i=0;i<3;i++)Defeat("bonus-extra-"+i,EnemyClassification.EnemyType.Boss);
            stats.currentHP=1000;health.TakeDamage(70,5,0,false,player.transform);
            Check(Mathf.Abs(stats.currentHP-968.75f)<.001f,"Five bosses: physical and true HP damage gain 25%");
            Check(stats.DEF==50&&player.GetComponent<CharacterStats>().ATK==20,"Bonus does not change base attack or boss defense");
            EnemyDefeatState.ReloadFromSave();
            Check(EnemyDefeatState.DefeatedBossCount==5,"Boss count persists across save reload");
            Defeat("bonus-final",EnemyClassification.EnemyType.Boss,true);
            Check(EnemyDefeatState.DefeatedBossCount==5,"Final boss does not count toward its own bonus");
            enemySource=new GameObject("Bonus test non-player");
            stats.currentHP=1000;health.TakeDamage(70,0,0,false,enemySource.transform);
            Check(Mathf.Abs(stats.currentHP-980)<.001f,"Non-player damage is not boosted");
            other=new GameObject("Bonus test ordinary target");var otherStats=other.AddComponent<CharacterStats>();otherStats.maxHP=otherStats.currentHP=1000;otherStats.DEF=50;
            other.AddComponent<CharacterHealth>().TakeDamage(70,0,0,false,player.transform);
            Check(Mathf.Abs(otherStats.currentHP-980)<.001f,"Other bosses and enemies are not boosted");
            EnemyDefeatState.ForgetForTesting(new[]{"bonus-necro-1"});
            Check(EnemyDefeatState.DefeatedBossCount==4,"Test reset removes matching boss progress");
            var catalog=Resources.Load<BossDefeatCatalog>("BossDefeatCatalog");
            Check(catalog!=null&&catalog.bossEncounterIds.Count==5,"Five preceding boss encounters are registered for legacy saves");
            var legacy=new LegacySave();legacy.ids.Add(catalog.bossEncounterIds[0]);legacy.ids.Add(catalog.bossEncounterIds[0]);legacy.ids.Add("legacy-elite");
            PlayerPrefs.SetString(EnemyDefeatState.SaveKey,JsonUtility.ToJson(legacy));PlayerPrefs.Save();EnemyDefeatState.ReloadFromSave();
            Check(EnemyDefeatState.DefeatedBossCount==1,"Legacy save recognizes known boss once and excludes unknown Elite");
            stats.currentHP=1000;health.TakeDamage(70,0,0,false,player.transform);
            Check(Mathf.Abs(stats.currentHP-979)<.001f,"Legacy boss victory immediately grants 5% damage");
            EnemyDefeatState.ClearSaved();Check(EnemyDefeatState.DefeatedBossCount==0,"New-game reset clears all boss bonuses");
            var finalPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/BlackKnightBoss.prefab");
            var earlyKnight=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/BlackKnight.prefab");
            Check(finalPrefab.GetComponent<FinalBossDamageBonus>()!=null&&earlyKnight.GetComponent<FinalBossDamageBonus>()==null,"Only final Black Knight prefab receives the hidden mechanic");
            Directory.CreateDirectory("Library/FinalBossBonus-20261120");
            string report=string.Join(Environment.NewLine,results);File.WriteAllText("Library/FinalBossBonus-20261120/results.txt",report);return report;
        }
        finally
        {
            if(player!=null)Object.Destroy(player);if(target!=null)Object.Destroy(target);if(other!=null)Object.Destroy(other);if(enemySource!=null)Object.Destroy(enemySource);
        }
    }
}
