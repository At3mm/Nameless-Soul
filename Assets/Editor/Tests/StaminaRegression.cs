using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class StaminaRegression
{
    [MenuItem("Tools/Diagnostics/Test Stamina (Play Mode)")]
    public static void RunMenu() { Debug.Log(Run()); }

    public static string Run()
    {
        if (!Application.isPlaying || Time.timeScale <= 0f) throw new InvalidOperationException("Run in unpaused Play Mode.");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab");
        var copy = UnityEngine.Object.Instantiate(source, new Vector3(10000, 10000), Quaternion.identity);
        copy.tag = "Untagged";
        var checks = new List<string>();
        try
        {
            copy.GetComponent<Rigidbody2D>().simulated = false;
            copy.GetComponent<PlayerCombat>().enabled = false;
            var controller = copy.GetComponent<PlayerController>(); controller.enabled = false;
            var stats = copy.GetComponent<CharacterStats>();
            var skills = copy.GetComponent<SkillDamageManager>();
            var buffs = copy.GetComponent<CharacterBuffs>();
            var debuffs = copy.GetComponent<CharacterDebuffs>();
            var dash = copy.GetComponent<PlayerDash>();
            Check(stats.currentStamina == stats.maxStamina && stats.maxStamina > 0, "Spawn fills stamina", checks);
            Check(skills.skills[0].staminaCost == 10 && skills.skills[1].staminaCost == 25, "Serialized skill costs preserved", checks);
            Check((int)CharacterBuffs.Kind.StaminaEfficiency == 3, "Efficiency enum keeps serialized index", checks);
            skills.skills = new List<SkillData> { new SkillData { skillName = "StaminaCheck", staminaCost = 10, cooldownTime = 0 } };
            stats.currentStamina = 20;
            Check(skills.MarkSkillUsed("StaminaCheck") && stats.currentStamina == 10, "Skill spends stamina", checks);
            stats.currentStamina = 9;
            Check(!skills.MarkSkillUsed("StaminaCheck") && stats.currentStamina == 9, "Insufficient stamina rejects skill without spending", checks);
            buffs.Apply(CharacterBuffs.Kind.StaminaEfficiency, 8, 25);
            debuffs.ApplyStress(6);
            Check(Mathf.Approximately(stats.StaminaCostMultiplier, 1.125f), "Efficiency and stress combine", checks);
            stats.currentStamina = 30;
            Check(skills.MarkSkillUsed("StaminaCheck") && Mathf.Approximately(stats.currentStamina, 18.75f), "Skill uses combined cost", checks);
            buffs.ClearAll(); debuffs.ClearAll();
            stats.currentStamina = dash.dashStaminaCost - 1;
            dash.TryDash(Vector2.right);
            Check(!dash.isDashing && stats.currentStamina == dash.dashStaminaCost - 1, "Insufficient stamina rejects dash", checks);
            stats.currentStamina = 50;
            dash.TryDash(Vector2.right);
            Check(dash.isDashing && Mathf.Approximately(stats.currentStamina, 50 - dash.dashStaminaCost), "Dash spends stamina", checks);
            dash.CancelDash();
            stats.currentStamina = 0;
            controller.staminaRecoveryDelay = 1; controller.staminaRecoveryPerSecond = 15;
            controller.DelayStaminaRecovery();
            Recover(controller, .5f);
            Check(stats.currentStamina == 0, "Recovery respects delay", checks);
            Recover(controller, 1f);
            Check(Mathf.Approximately(stats.currentStamina, 7.5f), "Recovery prorates elapsed delay", checks);
            Recover(controller, 100f);
            Check(stats.currentStamina == stats.maxStamina, "Recovery clamps to maximum", checks);
            return "PASS: " + checks.Count + " checks. " + string.Join("; ", checks);
        }
        finally { UnityEngine.Object.DestroyImmediate(copy); }
    }
    private static void Recover(PlayerController controller, float dt)
    {
        typeof(PlayerController).GetMethod("RecoverStamina", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(controller, new object[] { dt });
    }
    private static void Check(bool passed, string message, List<string> checks)
    {
        if (!passed) throw new InvalidOperationException("FAILED: " + message);
        checks.Add(message);
    }
}
