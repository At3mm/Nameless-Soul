using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class EnemyBreakRegression
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    [MenuItem("Tools/Diagnostics/Test Enemy Break (Play Mode)")]
    public static void RunMenu() { Debug.Log(Run()); }
    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale <= 0) throw new InvalidOperationException("Run in unpaused Play Mode.");
        var checks = new List<string>();
        foreach (var path in new[] { "Assets/Prefabs/Characters/Orc.prefab", "Assets/Prefabs/Characters/EliteOrc.prefab" })
        {
            var enemy = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), new Vector3(10000,10000), Quaternion.identity);
            enemy.tag = "Untagged";
            enemy.GetComponent<EnemyCurrencyReward>().rewardsEnabled = false;
            var target = new GameObject("BreakTest target");
            target.transform.position = enemy.transform.position + Vector3.right * .4f; target.layer = 31;
            var targetStats = target.AddComponent<CharacterStats>(); target.AddComponent<CharacterHealth>(); target.AddComponent<BoxCollider2D>();
            try
            {
                var ai = enemy.GetComponent<EnemyBehavior>();
                typeof(EnemyBehavior).GetMethod("Start",PrivateInstance).Invoke(ai,null); ai.enabled = false;
                var body = enemy.GetComponent<Rigidbody2D>(); body.simulated = false;
                var stats = enemy.GetComponent<CharacterStats>(); var health = enemy.GetComponent<CharacterHealth>();
                var breaker = enemy.GetComponent<EnemyBreak>(); var handler = enemy.GetComponent<AnimationEventHandler>();
                var combat = enemy.GetComponent<EnemyCombat>(); var skills = enemy.GetComponent<SkillDamageManager>();
                var animator = enemy.GetComponent<Animator>(); var leap = enemy.GetComponent<EliteOrcTargetedLeap>();
                stats.maxHP = stats.currentHP = 10000; stats.DEF = 10; stats.maxStamina = stats.currentStamina = 100;
                health.damageInvulnerabilityDuration = 0;
                Check(!breaker.IsBroken && breaker.Ratio==1, "Initial full stamina",checks);
                CheckParry(enemy,target,checks);
                health.TakeDamage(20,0,0,false,null);
                Check(stats.currentStamina==80 && stats.currentHP==9990, "Direct hit drains stamina and HP separately",checks);
                Tick(breaker,1);
                float wait = breaker.RecoveryRemaining;
                health.TakeDamageOverTime(1);
                Check(stats.currentStamina==80 && Mathf.Approximately(wait,breaker.RecoveryRemaining), "Damage over time does not drain or delay stamina",checks);
                stats.DEF = 100;
                float hp = stats.currentHP;
                health.TakeDamage(5,0,0,false,null);
                Check(stats.currentHP==hp && stats.currentStamina==75, "Armor blocking HP still loses stamina",checks);
                stats.DEF = 10;
                Tick(breaker,breaker.recoveryDelay);
                Check(stats.currentStamina==100 && !breaker.IsBroken, "Partially drained stamina refills after quiet interval",checks);
                var incoming = target.AddComponent<SkillDamageManager>();
                targetStats.ATK = 20; targetStats.STR = 5; targetStats.CRIT_Rate = 0;
                incoming.skills.Add(new SkillData{skillName="BreakDamageTest",attackRange=2,staminaDamageMultiplier=1.8f,gainsPostureOnHit=false});
                incoming.targetLayer = 1<<enemy.layer;
                body.simulated = true; Physics2D.SyncTransforms(); incoming.ExecuteHit("BreakDamageTest"); body.simulated = false;
                Check(Mathf.Approximately(stats.currentStamina,55), "Attack animation applies per-skill stamina damage multiplier",checks);
                Tick(breaker,breaker.recoveryDelay);
                var invulnerability = enemy.AddComponent<CharacterInvulnerability>();
                typeof(CharacterHealth).GetField("invulnerability",PrivateInstance).SetValue(health,invulnerability);
                invulnerability.isInvulnerable = true;
                health.TakeDamage(20,0,0,false,null);
                Check(stats.currentStamina==100, "Rejected invulnerable hit does not drain stamina",checks);
                invulnerability.isInvulnerable = false;
                string attackName = skills.skills[0].skillName;
                skills.skills = new List<SkillData>{new SkillData{skillName=attackName,cooldownTime=0,attackRange=2,hitboxOffset=Vector2.zero}};
                skills.targetLayer = 1<<31; skills.currentAttackDirection = Vector2.right;
                Check(combat.TriggerAttack(target.transform,enemy.transform.position,8), "Can attack before break",checks);
                if(leap!=null) Check(leap.TryBegin("Skill3",target.transform,enemy.transform.position,8), "Elite leap starts before break",checks);
                stats.currentStamina = 5;
                health.TakeDamage(20,0,0,false,null);
                Check(breaker.IsBroken && stats.currentStamina==0, "Exhaustion triggers break",checks);
                ai.currentState = EnemyBehavior.OrcState.Patrol;
                Check(enemy.GetComponent<EnemyClassification>().IsEngaged, "Breaking a patrolling enemy counts as engaged",checks);
                Check(Mathf.Approximately(stats.EffectiveDefense,5) && stats.DEF==10, "Break halves effective defense without editing base",checks);
                Check(!combat.IsLunging && (leap==null || !leap.IsLeaping), "Break cancels active lunge and leap",checks);
                handler.UnlockAction();
                Check(handler.isActionLocked, "Late animation unlock cannot end break",checks);
                animator.Update(.01f);
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Broken"), "Broken pose plays",checks);
                body.linearVelocity = Vector2.right*5;
                typeof(EnemyBehavior).GetMethod("Update",PrivateInstance).Invoke(ai,null);
                Check(body.linearVelocity==Vector2.zero, "AI cannot move during break",checks);
                Check(!combat.TriggerAttack(target.transform,enemy.transform.position,8) && !skills.MarkSkillUsed(attackName), "Break rejects attacks and skill use",checks);
                Physics2D.SyncTransforms();
                float targetHP = targetStats.currentHP;
                skills.ExecuteHit(attackName);
                Check(targetStats.currentHP==targetHP, "Queued attack animation cannot damage during break",checks);
                hp = stats.currentHP;
                health.TakeDamage(20,0,0,false,null);
                Check(Mathf.Approximately(hp-stats.currentHP,15), "Hits during break use reduced defense",checks);
                animator.Update(.01f);
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Broken"), "Further hits preserve broken animation",checks);
                string interrupt = path.Contains("EliteOrc") ? "Skill1" : "TakeDamage";
                animator.SetTrigger(interrupt);
                animator.Play("TakeDamage", 0, 0f);
                animator.Update(.01f);
                handler.isActionLocked = false;
                stats.currentStamina = 25;
                typeof(EnemyBreak).GetMethod("LateUpdate",PrivateInstance).Invoke(breaker,null);
                animator.Update(.01f);
                Check(breaker.IsBroken && animator.GetCurrentAnimatorStateInfo(0).IsName("Broken") &&
                    handler.isActionLocked && stats.currentStamina==0,
                    "Late animation interruption cannot escape break or restore actions/stamina",checks);
                Tick(breaker,breaker.recoveryDelay-.1f);
                float beforeLateHit = breaker.RecoveryRemaining;
                health.TakeDamage(1,0,0,false,null,0);
                Check(Mathf.Approximately(breaker.RecoveryRemaining,beforeLateHit), "Hit near recovery does not extend active break",checks);
                breaker.ReceiveHit(100);
                Check(breaker.IsBroken && Mathf.Approximately(breaker.RecoveryRemaining,beforeLateHit), "Repeated hits preserve break without resetting its timer",checks);
                float remaining = breaker.RecoveryRemaining; Tick(breaker,0);
                Check(Mathf.Approximately(remaining,breaker.RecoveryRemaining), "Pause freezes recovery",checks);
                Tick(breaker,.11f); animator.Update(.01f);
                Check(!breaker.IsBroken && stats.currentStamina==100 && stats.EffectiveDefense==10 && !handler.isActionLocked, "Fixed break duration restores stamina, defense and action despite further hits",checks);
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName(breaker.idleState), "Recovery returns to idle",checks);
                breaker.ReceiveHit(100); breaker.enabled = false;
                Check(!handler.isActionLocked && stats.EffectiveDefense==10, "Disabling break safely releases living enemy",checks);
                breaker.enabled = true; breaker.ReceiveHit(100);
                health.TakeDamageOverTime(20000); Tick(breaker,10);
                Check(health.isDead && !combat.enabled && !breaker.enabled, "Death cannot recover or reactivate AI",checks);
                animator.Update(.01f);
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Die"), "Death replaces break pose",checks);
            }
            finally { UnityEngine.Object.DestroyImmediate(enemy); UnityEngine.Object.DestroyImmediate(target); }
        }
        return "PASS: "+checks.Count+" checks. "+string.Join("; ",checks);
    }
    private static void Tick(EnemyBreak source,float dt) { typeof(EnemyBreak).GetMethod("Tick",PrivateInstance).Invoke(source,new object[]{dt}); }
    private static void CheckParry(GameObject enemy,GameObject target,List<string> checks)
    {
        var defender=target.GetComponent<CharacterHealth>();var defenderStats=target.GetComponent<CharacterStats>();
        var parry=target.AddComponent<CharacterParry>();parry.recoilKnockbackForce=0;
        typeof(CharacterHealth).GetField("parryComponent",PrivateInstance).SetValue(defender,parry);
        var stats=enemy.GetComponent<CharacterStats>();var health=enemy.GetComponent<CharacterHealth>();var breaker=enemy.GetComponent<EnemyBreak>();
        var iframe=enemy.AddComponent<CharacterInvulnerability>();iframe.isInvulnerable=true;
        typeof(CharacterHealth).GetField("invulnerability",PrivateInstance).SetValue(health,iframe);
        var hitbox=new GameObject("Parried attack child");hitbox.transform.SetParent(enemy.transform,false);
        float enemyHP=stats.currentHP,playerHP=defenderStats.currentHP;
        try
        {
            parry.EnableParryWindow();defender.TakeDamage(20,0,0,true,hitbox.transform);
            Check(stats.currentStamina==75 && stats.currentHP==enemyHP && defenderStats.currentHP==playerHP,
                "Parry child hitbox drains 25 stamina through attacker HP i-frames without damaging either HP",checks);
            parry.ExecuteParry(hitbox.transform);
            Check(stats.currentStamina==75,"Consumed parry window cannot drain twice",checks);
            Tick(breaker,breaker.recoveryDelay-.1f);
            parry.EnableParryWindow();defender.TakeDamage(20,0,0,true,enemy.transform);
            Check(stats.currentStamina==50 && Mathf.Approximately(breaker.RecoveryRemaining,breaker.recoveryDelay),
                "Successful parry resets enemy stamina recovery delay",checks);
            parry.EnableParryWindow();defender.TakeDamage(20,0,0,true,enemy.transform);
            parry.EnableParryWindow();defender.TakeDamage(20,0,0,true,enemy.transform);
            Check(breaker.IsBroken && stats.currentStamina==0 && stats.EffectiveDefense==5,
                "Four successful default parries exhaust 100 stamina and trigger reduced-defense break",checks);
            Tick(breaker,breaker.recoveryDelay);
            Check(!breaker.IsBroken && stats.currentStamina==100,"Enemy recovers from parry-induced break",checks);
            parry.EnableParryWindow();defender.TakeDamage(20,0,0,false,enemy.transform);
            Check(stats.currentStamina==100 && defenderStats.currentHP<playerHP,"Unparryable hit does not drain attacker stamina",checks);
            parry.DisableParryWindow();defenderStats.currentHP=playerHP;
            parry.enabled=false;parry.isParrying=true;defender.TakeDamage(20,0,0,true,enemy.transform);
            Check(stats.currentStamina==100 && defenderStats.currentHP<playerHP,"Disabled parry cannot deflect or drain stamina",checks);
        }
        finally
        {
            parry.enabled=true;parry.DisableParryWindow();defenderStats.currentHP=playerHP;
            typeof(CharacterHealth).GetField("invulnerability",PrivateInstance).SetValue(health,null);
            UnityEngine.Object.DestroyImmediate(iframe);UnityEngine.Object.DestroyImmediate(hitbox);
        }
    }
    private static void Check(bool result,string message,List<string> checks)
    {
        if(!result) throw new InvalidOperationException("FAILED: "+message);
        checks.Add(message);
    }
}
