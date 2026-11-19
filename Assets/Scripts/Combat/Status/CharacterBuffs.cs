using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats))]
public sealed class CharacterBuffs : MonoBehaviour
{
    public enum Kind { Damage = 0, MovementSpeed = 1, Resistance = 2, StaminaEfficiency = 3, DamageReduction = 4 }
    [Serializable]
    private sealed class ActiveBuff
    {
        public Kind kind;
        public float percent;
        public float remaining, duration;
    }

    [Header("Default buff applications (%)")]
    [Min(0.1f)] public float defaultDuration = 8f;
    [Min(0f)] public float damageBonusPercent = 25f;
    [Min(0f)] public float movementBonusPercent = 20f;
    [Range(0f, 100f)] public float resistanceBonusPercent = 20f;
    [Range(0f, 100f)] public float staminaCostReductionPercent = 25f;
    [Range(0f, 100f)] public float damageReductionPercent = 20f;
    [SerializeField] private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();

    public float DamageMultiplier => 1f + GetPercent(Kind.Damage) * 0.01f;
    public float MovementMultiplier => 1f + GetPercent(Kind.MovementSpeed) * 0.01f;
    public float ResistanceBonus => Mathf.Clamp01(GetPercent(Kind.Resistance) * 0.01f);
    public float StaminaCostMultiplier => 1f - Mathf.Clamp01(GetPercent(Kind.StaminaEfficiency) * 0.01f);
    public float IncomingDamageMultiplier => 1f - Mathf.Clamp01(GetPercent(Kind.DamageReduction) * 0.01f);

    private void Awake() { ClearAll(); }
    private void Update() { Advance(Time.deltaTime); }
    private static bool Valid(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    public bool Has(Kind kind) { return GetRemaining(kind) > 0f; }
    public float GetRemaining(Kind kind)
    {
        if (!isActiveAndEnabled) return 0f;
        var buff = activeBuffs.Find(b => b.kind == kind);
        return buff != null ? buff.remaining : 0f;
    }
    public float GetDuration(Kind kind){var buff=activeBuffs.Find(b=>b.kind==kind);return buff!=null?buff.duration:0;}
    public float GetPercent(Kind kind)
    {
        if (!isActiveAndEnabled) return 0f;
        var buff = activeBuffs.Find(b => b.kind == kind && b.remaining > 0f);
        return buff != null ? buff.percent : 0f;
    }
    public void Apply(Kind kind)
    {
        float percent;
        switch (kind)
        {
            case Kind.Damage: percent = damageBonusPercent; break;
            case Kind.MovementSpeed: percent = movementBonusPercent; break;
            case Kind.Resistance: percent = resistanceBonusPercent; break;
            case Kind.StaminaEfficiency: percent = staminaCostReductionPercent; break;
            case Kind.DamageReduction: percent = damageReductionPercent; break;
            default: return;
        }
        Apply(kind, defaultDuration, percent);
    }

    // Different kinds coexist. Reapplying a kind refreshes duration and retains
    // the strongest potency, rather than multiplying the base stat repeatedly.
    public void Apply(Kind kind, float duration, float percent)
    {
        if (!isActiveAndEnabled || !Enum.IsDefined(typeof(Kind), kind) ||
            !Valid(duration) || !Valid(percent) || duration <= 0f || percent <= 0f) return;
        var health = GetComponent<CharacterHealth>();
        if (health != null && health.isDead) return;
        if (kind == Kind.Resistance || kind == Kind.StaminaEfficiency || kind == Kind.DamageReduction)
            percent = Mathf.Min(percent, 100f);
        var buff = activeBuffs.Find(b => b.kind == kind);
        if (buff == null) activeBuffs.Add(new ActiveBuff { kind = kind, percent = percent, remaining = duration, duration = duration });
        else { buff.percent = Mathf.Max(buff.percent, percent); buff.remaining = Mathf.Max(buff.remaining, duration); buff.duration=buff.remaining; }
    }
    private void Advance(float deltaTime)
    {
        if (!Valid(deltaTime) || deltaTime <= 0f) return;
        foreach (var buff in activeBuffs) buff.remaining = Mathf.Max(0f, buff.remaining - deltaTime);
        activeBuffs.RemoveAll(b => b.remaining <= 0f);
    }
    public void Remove(Kind kind) { activeBuffs.RemoveAll(b => b.kind == kind); }
    [ContextMenu("Buffs/Clear all")]
    public void ClearAll() { activeBuffs.Clear(); }
    private void OnDisable() { ClearAll(); }
    [ContextMenu("Buffs/Apply damage (Play Mode)")]
    private void TestDamage() { if (Application.isPlaying) Apply(Kind.Damage); }
    [ContextMenu("Buffs/Apply movement speed (Play Mode)")]
    private void TestSpeed() { if (Application.isPlaying) Apply(Kind.MovementSpeed); }
    [ContextMenu("Buffs/Apply resistance (Play Mode)")]
    private void TestResistance() { if (Application.isPlaying) Apply(Kind.Resistance); }
    [ContextMenu("Buffs/Apply Stamina efficiency (Play Mode)")]
    private void TestStamina() { if (Application.isPlaying) Apply(Kind.StaminaEfficiency); }
    [ContextMenu("Buffs/Apply damage reduction (Play Mode)")]
    private void TestReduction() { if (Application.isPlaying) Apply(Kind.DamageReduction); }
}
