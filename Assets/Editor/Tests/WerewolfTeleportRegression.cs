using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class WerewolfTeleportRegression
{
    [MenuItem("Tools/Diagnostics/Test Werewolf Teleport (Play Mode)")]
    public static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
        var checks = new List<string>();
        var objects = new List<GameObject>();
        try
        {
            for (int scenario = 0; scenario < 4; scenario++)
            {
                Vector3 origin = new Vector3(20000 + scenario * 20, 20000, 0);
                var dummy = new GameObject("Teleport test target");
                objects.Add(dummy);
                dummy.layer = 6;
                dummy.transform.position = origin + Vector3.right * .9f;
                var targetStats = dummy.AddComponent<CharacterStats>();
                targetStats.maxHP = targetStats.currentHP = 10000;
                targetStats.DEF = 0;
                targetStats.RES = 1;
                var targetHealth = dummy.AddComponent<CharacterHealth>();
                dummy.AddComponent<BoxCollider2D>().size = Vector2.one * .3f;
                var wolf = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Characters/Werewolf.prefab"), origin, Quaternion.identity);
                objects.Add(wolf);
                wolf.GetComponent<EnemyBehavior>().enabled = false;
                var combat = wolf.GetComponent<WerewolfCombat>();
                var manager = wolf.GetComponent<SkillDamageManager>();
                var animator = wolf.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var skill = manager.skills.Find(s => s.skillName == WerewolfCombat.Teleport);
                Check(skill != null, "TeleportAttack exists in prefab", checks);
                manager.skills = new List<SkillData> { skill };
                manager.ResetCooldowns();
                skill.knockbackForce = 0;
                wolf.GetComponent<CharacterStats>().CRIT_Rate = 0;
                Physics2D.SyncTransforms();
                Check(combat.TriggerAttack(dummy.transform, origin, 8), "Skill starts through normal combat entry", checks);
                Check(!combat.TriggerAttack(dummy.transform, origin, 8), "Cooldown prevents immediate reuse", checks);
                Step(animator, .6f);
                Check(wolf.GetComponent<CharacterHealth>().IsUntargetable,
                    "Actual BeginUntargetable event protects wolf", checks);
                dummy.transform.position = origin + Vector3.right * (scenario == 2 ? 10 : 3);
                if (scenario == 1)
                {
                    var cover = new GameObject("GroundObstacles", typeof(Tilemap), typeof(BoxCollider2D));
                    objects.Add(cover);
                    cover.transform.position = origin + Vector3.right * 1.5f;
                    cover.GetComponent<BoxCollider2D>().size = new Vector2(.25f, 8);
                }
                if (scenario == 3) targetHealth.isDead = true;
                Physics2D.SyncTransforms();
                Step(animator, .48f);
                float moved = Vector2.Distance(wolf.transform.position, origin);
                if (scenario == 0)
                {
                    Check(moved > 1.5f && Vector2.Distance(wolf.transform.position, dummy.transform.position) < .9f,
                        "ReappearNearTarget follows player movement during disappearance", checks);
                    Check(!wolf.GetComponent<CharacterHealth>().IsUntargetable,
                        "Actual EndUntargetable event restores targeting", checks);
                    Check(targetStats.currentHP == 10000, "No damage before ExecuteHit markers", checks);
                    Step(animator, .2f);
                    Check(targetStats.currentHP < 10000, "ExecuteHit TeleportAttack deals damage after reappearance", checks);
                    Check(skill.hitboxOffset == (Vector2)wolf.transform.Find("AttackPoint").localPosition,
                        "Hitbox uses configured AttackPoint", checks);
                    Step(animator, .7f);
                    Check(!wolf.GetComponent<AnimationEventHandler>().isActionLocked,
                        "UnlockAction event releases action at end", checks);
                    Check(!animator.GetCurrentAnimatorStateInfo(0).IsName(WerewolfCombat.Teleport),
                        "Animator exits teleport state", checks);
                }
                else
                    Check(moved < .05f, new[] { "", "Cover blocks teleport", "Distance limit blocks teleport", "Dead target blocks teleport" }[scenario], checks);
                wolf.SetActive(false);
                dummy.SetActive(false);
            }
            return "PASS Werewolf teleport: " + string.Join("\nPASS ", checks);
        }
        finally
        {
            foreach (var go in objects) if (go != null) Object.DestroyImmediate(go);
        }
    }

    private static void Step(Animator animator, float seconds)
    {
        for (float elapsed = 0; elapsed < seconds - .0001f; elapsed += .01f)
        {
            animator.Update(.01f);
            Physics2D.SyncTransforms();
        }
    }

    private static void Check(bool condition, string label, List<string> checks)
    {
        if (!condition) throw new Exception("FAIL: " + label);
        checks.Add(label);
    }
}
