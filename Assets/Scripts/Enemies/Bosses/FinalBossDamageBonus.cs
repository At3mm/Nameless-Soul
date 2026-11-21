using UnityEngine;

/// <summary>Hidden player HP-damage bonus against this final encounter; each distinct boss victory adds five percent.</summary>
[DisallowMultipleComponent]
public sealed class FinalBossDamageBonus : MonoBehaviour
{
    [Min(0f)] public float bonusPerBoss=.05f;
    public float DamageMultiplier=>1f+EnemyDefeatState.DefeatedBossCount*Mathf.Max(0,bonusPerBoss);
    public static float MultiplierFor(CharacterHealth target,Transform attacker)
    {
        if(target==null||attacker==null||attacker.GetComponentInParent<PlayerController>()==null)return 1f;
        var bonus=target.GetComponent<FinalBossDamageBonus>();
        return bonus!=null&&bonus.isActiveAndEnabled?bonus.DamageMultiplier:1f;
    }
}
