using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class EnemyVisionRegression
{
    [MenuItem("Tools/Diagnostics/Test Enemy Obstructed Vision (Play Mode)")]
    public static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run in Play Mode.");
        var checks = new List<string>();
        var objects = new List<GameObject>();
        try
        {
            var position = new Vector3(20000, 20000, 0);
            var player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab"), position + Vector3.right * 2, Quaternion.identity);
            objects.Add(player);
            player.tag = "Untagged";
            var cover = new GameObject("GroundObstacles", typeof(Tilemap), typeof(BoxCollider2D));
            objects.Add(cover);
            cover.transform.position = position + Vector3.right;
            var box = cover.GetComponent<BoxCollider2D>();
            box.size = new Vector2(.25f, 4f);

            foreach (string prefab in new[] { "Orc", "EliteOrc" })
            {
                cover.name = "GroundObstacles";
                box.enabled = false;
                box.isTrigger = false;
                player.transform.position = position + Vector3.right * 2;
                var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Characters/{prefab}.prefab"), position, Quaternion.identity);
                objects.Add(enemy);
                var awareness = enemy.GetComponent<EnemyAwareness>();
                var ai = enemy.GetComponent<EnemyBehavior>();
                var combat = enemy.GetComponent<EnemyCombat>();
                Call(awareness, "Start");
                Call(ai, "Start");
                Set(ai, "player", player.transform);
                Set(ai, "playerHealth", player.GetComponent<CharacterHealth>());
                Physics2D.SyncTransforms();
                Check(awareness.HasLineOfSight(player.transform), prefab + ": visible without cover", checks);
                Check(ai.IsInCombatWith(player.transform), prefab + ": open instant-aggro range still detects player", checks);

                box.enabled = true;
                Physics2D.SyncTransforms();
                Check(!awareness.HasLineOfSight(player.transform), prefab + ": GroundObstacles blocks vision", checks);
                Check(!ai.IsInCombatWith(player.transform), prefab + ": no instant aggro through cover", checks);
                Call(ai, "CheckState", 2f);
                Check(ai.currentState == EnemyBehavior.OrcState.Patrol, prefab + ": hidden player cannot trigger chase", checks);
                Check(!combat.IsInAttackRange(player.transform) && !combat.TriggerAttack(player.transform, position, 8f),
                    prefab + ": blocked player cannot start an attack", checks);

                cover.name = "Walls_Cliffs";
                Check(!awareness.HasLineOfSight(player.transform), prefab + ": Walls_Cliffs blocks vision", checks);
                box.isTrigger = true;
                Check(awareness.HasLineOfSight(player.transform), prefab + ": trigger volumes do not hide player", checks);
                box.isTrigger = false;
                cover.name = "Lakes";
                Check(awareness.HasLineOfSight(player.transform), prefab + ": unrelated Default-layer map does not block vision", checks);

                cover.name = "GroundObstacles";
                cover.transform.position = position + new Vector3(1, 5, 0);
                Physics2D.SyncTransforms();
                Check(awareness.HasLineOfSight(player.transform), prefab + ": vision returns through an opening", checks);
                ai.currentState = EnemyBehavior.OrcState.Patrol;
                Call(ai, "Update");
                Check(ai.currentState != EnemyBehavior.OrcState.Patrol, prefab + ": reacquires a visible player", checks);
                var lastSeen = (Vector2)Get(ai, "lastSeenPlayerPosition");

                cover.transform.position = position + Vector3.right;
                player.transform.position = position + new Vector3(2, 1, 0);
                Physics2D.SyncTransforms();
                Set(ai, "lastSeenPlayerTime", Time.time);
                ai.currentState = EnemyBehavior.OrcState.Chase;
                Call(ai, "Update");
                Check((Vector2)Get(ai, "lastSeenPlayerPosition") == lastSeen,
                    prefab + ": hidden movement never updates last seen position", checks);
                Check(ai.currentState == EnemyBehavior.OrcState.Chase,
                    prefab + ": searches last known position briefly", checks);
                Set(ai, "lastSeenPlayerTime", Time.time - awareness.lostSightMemory - 1);
                Call(ai, "CheckState", 2f);
                Check(ai.currentState == EnemyBehavior.OrcState.Return && awareness.currentAlertTime == 0,
                    prefab + ": lost sight expires and enemy returns home", checks);
                var leap = enemy.GetComponent<EliteOrcTargetedLeap>();
                if (leap != null)
                {
                    Check(!leap.TryBegin(leap.skillName, player.transform, position, 8f),
                        "Elite leap cannot begin through cover", checks);
                    box.enabled = false;
                    Physics2D.SyncTransforms();
                    Check(leap.TryBegin(leap.skillName, player.transform, position, 8f),
                        "Elite leap can begin with clear sight", checks);
                    var seen = (Vector2)Get(leap, "lastVisibleTarget");
                    box.enabled = true;
                    player.transform.position = position + new Vector3(2, -1, 0);
                    Physics2D.SyncTransforms();
                    Call(leap, "AimAtTarget");
                    Check((Vector2)Get(leap, "lastVisibleTarget") == seen,
                        "Elite leap cannot retarget movement behind cover", checks);
                    leap.CancelLeap();
                    enemy.GetComponent<AnimationEventHandler>()?.UnlockAction();
                }
                enemy.SetActive(false);
                cover.transform.position = position + Vector3.right;
            }
            return "PASS\n" + string.Join("\n", checks);
        }
        finally
        {
            foreach (var item in objects)
                if (item != null) { item.SetActive(false); Object.Destroy(item); }
        }
    }

    private static void Call(object target, string method, params object[] args) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Get(object target, string field) =>
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Check(bool condition, string description, List<string> checks)
    {
        if (!condition) throw new InvalidOperationException(description);
        checks.Add("PASS: " + description);
    }
}
