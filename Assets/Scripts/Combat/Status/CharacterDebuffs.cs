using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterHealth))]
public sealed class CharacterDebuffs : MonoBehaviour
{
    public enum Kind { Burning, Poison, Stress }

    [Serializable]
    private sealed class DamageOverTime
    {
        public float remaining, total;
        public float maxHPPercentPerSecond;
        public float pendingTime;
    }

    [Header("Default applications")]
    [Min(0.1f)] public float burnDuration = 5f;
    [UnityEngine.Serialization.FormerlySerializedAs("burnDamagePerSecond")]
    [Min(0f), Tooltip("Percent of maximum HP lost per second. 4 means 4% max HP per second.")]
    public float burnMaxHPPercentPerSecond = 4f;
    [Min(0.1f)] public float poisonDuration = 8f;
    [UnityEngine.Serialization.FormerlySerializedAs("poisonDamagePerSecond")]
    [Min(0f), Tooltip("Percent of maximum HP lost per second. 2 means 2% max HP per second.")]
    public float poisonMaxHPPercentPerSecond = 2f;
    [Min(0.1f)] public float stressDuration = 6f;
    [Range(0.05f, 1f)] public float stressMovementMultiplier = 0.75f;
    [Min(1f)] public float stressStaminaCostMultiplier = 1.5f;
    [Min(0.05f)] public float damageTickInterval = 0.5f;

    [Header("Status buildup (100 = activation)")]
    [Min(1)] public float buildupThreshold=100;
    [Min(0)] public float buildupRecoveryPerSecond=12;
    [Min(0)] public float buildupRecoveryDelay=1.5f;
    [Min(0)] public float defaultBuildupPerHit=35;
    private readonly float[] buildup=new float[3], recoveryWait=new float[3];
    private readonly HashSet<MonoBehaviour>[] exposures={new HashSet<MonoBehaviour>(),new HashSet<MonoBehaviour>(),new HashSet<MonoBehaviour>()};
    public void SetExposure(Kind kind,MonoBehaviour source,bool active)
    {
        int i=(int)kind;if(i<0 || i>=3 || source==null)return;
        if(active)exposures[i].Add(source);else exposures[i].Remove(source);
    }
    private bool IsExposed(Kind kind)
    {
        var sources=exposures[(int)kind];sources.RemoveWhere(s=>s==null || !s.isActiveAndEnabled ||
            (s is PoisonZone && !((PoisonZone)s).IsExposing(health)) ||
            (s is LavaZone && !((LavaZone)s).IsExposing(health)));
        return sources.Count>0;
    }
    public float GetBuildup(Kind kind){int i=(int)kind;return i>=0 && i<3?buildup[i]:0;}
    public float GetRemaining(Kind kind){return kind==Kind.Burning?BurnRemaining:kind==Kind.Poison?PoisonRemaining:kind==Kind.Stress?StressRemaining:0;}
    public float GetDuration(Kind kind){return kind==Kind.Burning?burning.total:kind==Kind.Poison?poison.total:kind==Kind.Stress?stressTotal:0;}
    public bool AddBuildup(Kind kind,float amount,float duration=0,float damagePercent=0)
    {
        int i=(int)kind;if(i<0 || i>=3 || !CanApply() || !Valid(amount) || amount<=0 || GetRemaining(kind)>0)return false;
        float resistance=stats!=null?Mathf.Clamp01(stats.EffectiveResistance):0;
        buildup[i]=Mathf.Min(Mathf.Max(1,buildupThreshold),buildup[i]+amount*(1-resistance));recoveryWait[i]=Mathf.Max(0,buildupRecoveryDelay);
        if(buildup[i]<Mathf.Max(1,buildupThreshold))return false;
        buildup[i]=0;recoveryWait[i]=0;
        if(kind==Kind.Burning)ApplyBurn(duration>0?duration:burnDuration,damagePercent>0?damagePercent:burnMaxHPPercentPerSecond);
        else if(kind==Kind.Poison)ApplyPoison(duration>0?duration:poisonDuration,damagePercent>0?damagePercent:poisonMaxHPPercentPerSecond);
        else ApplyStress(duration>0?duration:stressDuration);
        return true;
    }
    [Header("Runtime state (seconds)")]
    [SerializeField] private DamageOverTime burning = new DamageOverTime();
    [SerializeField] private DamageOverTime poison = new DamageOverTime();
    [SerializeField] private float stressRemaining;
    private float stressTotal;
    private CharacterHealth health;
    private CharacterStats stats;

    public bool IsBurning => isActiveAndEnabled && burning.remaining > 0f;
    public bool IsPoisoned => isActiveAndEnabled && poison.remaining > 0f;
    public bool IsStressed => isActiveAndEnabled && stressRemaining > 0f;
    public float MovementMultiplier => IsStressed ? Mathf.Clamp(stressMovementMultiplier, 0.05f, 1f) : 1f;
    public float StaminaCostMultiplier => IsStressed ? Mathf.Max(1f, stressStaminaCostMultiplier) : 1f;
    public float BurnRemaining => burning.remaining;
    public float PoisonRemaining => poison.remaining;
    public float StressRemaining => stressRemaining;

    private void Awake() { health = GetComponent<CharacterHealth>(); stats = GetComponent<CharacterStats>(); ClearAll(); }
    private void Update() { Advance(Time.deltaTime); }

    public void Apply(Kind kind)
    {
        AddBuildup(kind,defaultBuildupPerHit);
    }

    public void ApplyBurn(float duration, float maxHPPercentPerSecond) { ApplyDot(burning, duration, maxHPPercentPerSecond); }
    public void ApplyPoison(float duration, float maxHPPercentPerSecond) { ApplyDot(poison, duration, maxHPPercentPerSecond); }
    public void ApplyStress(float duration)
    {
        if (!CanApply() || !Valid(duration) || duration <= 0f) return;
        bool fresh = stressRemaining <= 0f;
        stressRemaining = Mathf.Max(stressRemaining, duration);
        stressTotal=stressRemaining;
        if (fresh) CombatSFX.Debuff(Kind.Stress, transform.position);
        StatusEffectDefaults.EnsureVisuals(gameObject);
    }

    private bool CanApply() { return isActiveAndEnabled && health != null && !health.isDead && !World.QuestBossEncounter.Protects(gameObject) && !BossPhaseController.Protects(gameObject); }
    private static bool Valid(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    private void ApplyDot(DamageOverTime state, float duration, float maxHPPercentPerSecond)
    {
        if (!CanApply() || !Valid(duration) || !Valid(maxHPPercentPerSecond) || duration <= 0f || maxHPPercentPerSecond <= 0f) return;
        bool fresh = state.remaining <= 0f;
        if (fresh) { state.maxHPPercentPerSecond = maxHPPercentPerSecond; state.pendingTime = 0f; }
        else state.maxHPPercentPerSecond = Mathf.Max(state.maxHPPercentPerSecond, maxHPPercentPerSecond);
        state.remaining = Mathf.Max(state.remaining, duration);
        state.total=state.remaining;
        if (fresh) CombatSFX.Debuff(state == burning ? Kind.Burning : Kind.Poison, transform.position);
        StatusEffectDefaults.EnsureVisuals(gameObject);
    }

    // A single tick may include both effects. DOT bypasses hit i-frames and never
    // triggers stagger, parry, knockback, or additional invulnerability frames.
    private void Advance(float deltaTime)
    {
        if (!CanApply()) { ClearAll(); return; }
        if (!Valid(deltaTime) || deltaTime <= 0f) return;
        for(int i=0;i<3;i++)
        {
            float recovering=Mathf.Max(0,deltaTime-recoveryWait[i]);recoveryWait[i]=Mathf.Max(0,recoveryWait[i]-deltaTime);
            buildup[i]=Mathf.Max(0,buildup[i]-Mathf.Max(0,buildupRecoveryPerSecond)*recovering);
        }
        float damage = AdvanceDot(burning, deltaTime, IsExposed(Kind.Burning)) + AdvanceDot(poison, deltaTime, IsExposed(Kind.Poison));
        if(!IsExposed(Kind.Stress))stressRemaining = Mathf.Max(0f, stressRemaining - deltaTime);
        if (damage > 0f) health.TakeDamageOverTime(damage);
    }

    private float AdvanceDot(DamageOverTime state, float deltaTime, bool held)
    {
        if (state.remaining <= 0f) return 0f;
        state.pendingTime += held?deltaTime:Mathf.Min(state.remaining, deltaTime);
        if(!held)state.remaining = Mathf.Max(0f, state.remaining - deltaTime);
        if (state.remaining > 0f && state.pendingTime < Mathf.Max(0.05f, damageTickInterval)) return 0f;
        float damage = state.pendingTime * state.maxHPPercentPerSecond * 0.01f * Mathf.Max(0f, stats.maxHP);
        state.pendingTime = 0f;
        return damage;
    }

    public void Remove(Kind kind)
    {
        int index=(int)kind;if(index<0 || index>=3)return;buildup[index]=recoveryWait[index]=0;
        if (kind == Kind.Burning) ResetDot(burning);
        else if (kind == Kind.Poison) ResetDot(poison);
        else stressRemaining = 0f;
    }
    private static void ResetDot(DamageOverTime state) { state.remaining = state.total = state.pendingTime = state.maxHPPercentPerSecond = 0f; }
    [ContextMenu("Debuffs/Clear all")]
    public void ClearAll() { ResetDot(burning); ResetDot(poison); stressRemaining = stressTotal = 0f;Array.Clear(buildup,0,3);Array.Clear(recoveryWait,0,3);foreach(var sources in exposures)sources.Clear(); }
    private void OnDisable() { ClearAll(); }
    [ContextMenu("Debuffs/Apply burning (Play Mode)")]
    private void TestBurn() { if (Application.isPlaying) ApplyBurn(burnDuration,burnMaxHPPercentPerSecond); }
    [ContextMenu("Debuffs/Apply poison (Play Mode)")]
    private void TestPoison() { if (Application.isPlaying) ApplyPoison(poisonDuration,poisonMaxHPPercentPerSecond); }
    [ContextMenu("Debuffs/Apply stress (Play Mode)")]
    private void TestStress() { if (Application.isPlaying) ApplyStress(stressDuration); }
}
