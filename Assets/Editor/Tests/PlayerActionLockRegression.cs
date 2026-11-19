using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Play-mode regression checks on an isolated copy; never spends the real player's resources.</summary>
public static class PlayerActionLockRegression
{
    [MenuItem("Tools/Diagnostics/Test Player Action Locks (Play Mode)")]
    public static void RunMenu() { Debug.Log(Run()); }

    public static string Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var source = GameObject.FindGameObjectWithTag("Player");
        if (source == null) throw new InvalidOperationException("Player not found.");
        var menu = UnityEngine.Object.FindFirstObjectByType<SettingsMenuToggle>();
        if (menu != null && (menu.IsOpen || menu.IsAnimating))
            throw new InvalidOperationException("Close the pause menu before running diagnostics.");
        var results = new List<string>();
        var copy = UnityEngine.Object.Instantiate(source, new Vector3(10000, 10000), Quaternion.identity);
        copy.name = "ActionLockRegression_Copy";
        copy.tag = "Untagged";
        try
        {
            copy.GetComponent<PlayerController>().enabled = false;
            copy.GetComponent<PlayerCombat>().enabled = false;
            copy.GetComponent<PlayerDash>().enabled = false;
            copy.GetComponent<Rigidbody2D>().simulated = false;
            foreach (var collider in copy.GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            var animator = copy.GetComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var handler = copy.GetComponent<AnimationEventHandler>();
            var parry = copy.GetComponent<CharacterParry>();
            var health = copy.GetComponent<CharacterHealth>();
            health.isDead = false;
            animator.Rebind();
            animator.Update(0);
            foreach (string origin in new[] { "PlayerIdle", "PlayerWalk", "PlayerRun" })
            foreach (string action in new[] { "PlayerNormalAttack", "PlayerParry", "PlayerSkill2" })
            for (int repeat = 0; repeat < 5; repeat++)
            {
                animator.SetBool("IsWalking", origin != "PlayerIdle");
                animator.SetBool("IsRunning", origin == "PlayerRun");
                animator.Play(origin, 0, 0);
                animator.Update(0);
                handler.LockAction();
                animator.SetTrigger(action);
                Step(animator, .04f);
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName(action) ||
                    (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(action)),
                    "Action really enters: " + origin + " -> " + action, results);
                Step(animator, 2f);
                Check(!handler.isActionLocked && !parry.isParrying, origin + " -> " + action, results);
            }
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
            animator.Play("PlayerParry", 0, 0);
            Step(animator, .12f);
            Check(parry.isParrying, "Parry window actually opens", results);
            animator.Play("TakeDamage", 0, 0);
            animator.Update(0);
            Check(!parry.isParrying && handler.isActionLocked, "Hit interrupts parry without dropping hit lock", results);
            Step(animator, 1f);
            Check(!handler.isActionLocked, "Hit finishes and unlocks", results);
            animator.Play("PlayerNormalAttack", 0, 0);
            Step(animator, .1f);
            animator.Play("TakeDamage", 0, 0);
            Step(animator, 1f);
            Check(!handler.isActionLocked, "Hit interrupts attack and unlocks", results);

            // Simulate a missing final animation event by cutting directly to movement.
            foreach (string state in new[] { "PlayerIdle", "PlayerWalk", "PlayerRun", "PlayerDash" })
            {
                animator.Play("PlayerNormalAttack", 0, 0);
                Step(animator, .1f);
                handler.LockAction();
                parry.isParrying = true;
                animator.Play(state, 0, 0);
                animator.Update(0);
                Check(!handler.isActionLocked && !parry.isParrying, "Interrupted event recovery -> " + state, results);
            }
            if (menu != null)
            {
                var advance = typeof(SettingsMenuToggle).GetMethod("AdvanceBookAnimation", BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (string state in new[] { "PlayerNormalAttack", "PlayerParry", "PlayerSkill2", "TakeDamage" })
                {
                    animator.Play(state, 0, 0);
                    Step(animator, .1f);
                    float saved = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    menu.SetOpen(true);
                    advance.Invoke(menu, new object[] { 1f });
                    animator.Update(Time.deltaTime * Time.timeScale);
                    Check(Time.timeScale == 0f && Mathf.Approximately(saved, animator.GetCurrentAnimatorStateInfo(0).normalizedTime), "Pause freezes " + state, results);
                    menu.SetOpen(false);
                    advance.Invoke(menu, new object[] { 1f });
                    Step(animator, 2f);
                    Check(Time.timeScale > 0f && !handler.isActionLocked, "Resume unlocks " + state, results);
                }
            }
            health.isDead = true;
            handler.LockAction();
            animator.Play("PlayerIdle", 0, 0);
            animator.Update(0);
            Check(handler.isActionLocked, "Recovery does not unlock a dead character", results);
            return "PASS: " + results.Count + " checks. " + string.Join("; ", results);
        }
        finally
        {
            if (menu != null && menu.IsOpen)
            {
                menu.SetOpen(false);
                typeof(SettingsMenuToggle).GetMethod("AdvanceBookAnimation", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(menu, new object[] { 1f });
            }
            UnityEngine.Object.DestroyImmediate(copy);
        }
    }

    private static void Step(Animator animator, float seconds)
    {
        for (float time = 0; time < seconds; time += .02f) animator.Update(.02f);
    }
    private static void Check(bool passed, string label, List<string> results)
    {
        if (!passed) throw new InvalidOperationException("FAILED: " + label);
        results.Add(label);
    }
}
