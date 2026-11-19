using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class PlayerStunRegression
{
    [MenuItem("Tools/Diagnostics/Test Exhaustion Stun (Play Mode)")]
    public static void RunMenu() { Debug.Log(Run()); }

    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale <= 0f) throw new InvalidOperationException("Run in unpaused Play Mode.");
        var player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"), new Vector3(10000, 10000), Quaternion.identity);
        player.tag = "Untagged";
        GameObject enemy = null;
        var checks = new List<string>();
        try
        {
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<PlayerCombat>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            var stats = player.GetComponent<CharacterStats>();
            var health = player.GetComponent<CharacterHealth>();
            var stun = player.GetComponent<PlayerStun>();
            var handler = player.GetComponent<AnimationEventHandler>();
            var invulnerability = player.GetComponent<CharacterInvulnerability>();
            var animator = player.GetComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            stats.currentHP = 1000;
            stats.RES = 0;
            stats.currentStamina = stats.maxStamina * .08f;
            stun.stunChance = 1;
            Hit(health, invulnerability);
            Check(!stun.IsStunned, "Stamina exactly 8 percent does not stun", checks);
            stats.currentStamina = stats.maxStamina * .079f;
            Hit(health, invulnerability);
            Check(stun.IsStunned, "Stamina below 8 percent can stun", checks);
            Tick(stun, 2f);
            stats.currentStamina = 0;
            stun.stunChance = 0;
            Hit(health, invulnerability);
            Check(!stun.IsStunned, "Zero chance does not stun", checks);
            stun.stunChance = 1;
            Hit(health, invulnerability);
            Check(stun.IsStunned && Mathf.Approximately(stun.RemainingDuration, 2f), "Zero RES gives two seconds", checks);
            animator.Update(.02f);
            Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Stunned"), "Stun animation plays", checks);
            handler.UnlockAction();
            Check(handler.isActionLocked, "Late unlock event cannot cancel stun", checks);
            var guard = ScriptableObject.CreateInstance<PlayerActionStateGuard>();
            try { guard.OnStateEnter(animator, animator.GetCurrentAnimatorStateInfo(0), 0); }
            finally { UnityEngine.Object.DestroyImmediate(guard); }
            Check(handler.isActionLocked, "Movement recovery guard cannot cancel stun", checks);
            float stamina = stats.currentStamina;
            player.GetComponent<PlayerDash>().TryDash(Vector2.right);
            Check(!player.GetComponent<PlayerDash>().isDashing && stats.currentStamina == stamina, "Dash blocked while stunned", checks);
            bool canAct = (bool)typeof(PlayerCombat).GetMethod("CanAct", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player.GetComponent<PlayerCombat>(), null);
            Check(!canAct, "Combat blocked while stunned", checks);
            Tick(stun, .5f);
            float remaining = stun.RemainingDuration;
            float hp = stats.currentHP;
            Hit(health, invulnerability);
            Check(stats.currentHP < hp && Mathf.Approximately(remaining, stun.RemainingDuration), "Further hits damage without extending stun", checks);
            Tick(stun, 0f);
            Check(Mathf.Approximately(remaining, stun.RemainingDuration), "Zero scaled time freezes recovery", checks);
            enemy = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/EliteOrc.prefab"), new Vector3(10002, 10000), Quaternion.identity);
            Check(enemy.GetComponent<EnemyBehavior>().enabled && enemy.GetComponent<EnemyCombat>().TriggerAttack(player.transform, enemy.transform.position, 8f) && Time.timeScale > 0f,
                "Enemy combat can attack a stunned living target", checks);
            Tick(stun, 2f);
            animator.Update(.02f);
            Check(!stun.IsStunned && !handler.isActionLocked && animator.GetCurrentAnimatorStateInfo(0).IsName("PlayerIdle"), "Timer restores controls and idle", checks);
            stats.RES = .5f;
            Hit(health, invulnerability);
            Check(stun.IsStunned && Mathf.Approximately(stun.RemainingDuration, 1f), "RES 0.5 halves duration", checks);
            Tick(stun, 1f);
            stats.RES = 1f;
            Hit(health, invulnerability);
            Check(!stun.IsStunned, "RES 1 prevents stun", checks);
            stats.RES = 0;
            invulnerability.isInvulnerable = true;
            HitWithoutReset(health);
            Check(!stun.IsStunned, "Invulnerable hit does not stun", checks);
            invulnerability.isInvulnerable = false;
            player.GetComponent<CharacterParry>().isParrying = true;
            health.TakeDamage(0, 1, 0, true, null);
            Check(!stun.IsStunned, "Successful parry does not stun", checks);
            player.GetComponent<CharacterParry>().isParrying = false;
            Hit(health, invulnerability);
            health.TakeDamage(0, 0, 0, false, null);
            stats.currentHP = .5f;
            Hit(health, invulnerability);
            Check(health.isDead && !stun.IsStunned && !player.GetComponent<PlayerController>().enabled, "Death overrides stun without reviving controls", checks);
            Check(AnimationUtility.GetAnimationEvents(stun.stunAnimation).Length == 0,
                "Stun clip has no death cleanup event", checks);
            return "PASS: " + checks.Count + " checks. " + string.Join("; ", checks);
        }
        finally
        {
            if (enemy != null) UnityEngine.Object.DestroyImmediate(enemy);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    private static void Hit(CharacterHealth health, CharacterInvulnerability invulnerability)
    {
        invulnerability.isInvulnerable = false;
        HitWithoutReset(health);
    }
    private static void HitWithoutReset(CharacterHealth health) { health.TakeDamage(0, 1, 0, false, null); }
    private static void Tick(PlayerStun stun, float time)
    {
        typeof(PlayerStun).GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(stun, new object[] { time });
    }
    private static void Check(bool passed, string message, List<string> checks)
    {
        if (!passed) throw new InvalidOperationException("FAILED: " + message);
        checks.Add(message);
    }
}
