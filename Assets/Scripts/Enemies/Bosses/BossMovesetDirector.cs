using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Observes visible animation states and chains existing skills without editing clips.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCombat), typeof(EnemyClassification))]
public sealed class BossMovesetDirector : MonoBehaviour
{
    public enum PlayerMove { Any, Neutral, Attack, HeavyAttack, Dash, Parry }

    [Serializable]
    public sealed class Combo
    {
        public string name;
        public PlayerMove responseTo;
        [Min(0)] public float weight = 1;
        [Min(0)] public float minDistance;
        [Min(0)] public float maxDistance = 2;
        public string[] skills = Array.Empty<string>();
    }

    [Header("Observed player Animator states")]
    public string attackState = "PlayerNormalAttack";
    public string heavyAttackState = "PlayerSkill2";
    public string dashState = "PlayerDash";
    public string parryState = "PlayerParry";
    [Header("Reaction and combo pacing")]
    [Min(.1f)] public float reactionDelay = .3f;
    [Min(.1f)] public float observationMemory = 1.2f;
    [Range(0, 1)] public float comboChance = .8f;
    [Range(1, 6)] public int maxComboLength = 3;
    [Min(0)] public float stepGap = .15f;
    [Min(.1f)] public float recoveryTime = .8f;
    [Min(.5f)] public float animationTimeout = 5;
    public List<Combo> combos = new List<Combo>();

    public bool IsBossActive => isActiveAndEnabled && classification != null &&
        classification.enemyType == EnemyClassification.EnemyType.Boss;
    public bool IsControlling => IsBossActive && active;
    public bool CanStartAttack => !active && Time.time >= recoveryUntil;
    public PlayerMove ObservedMove { get; private set; }
    public string CurrentCombo { get; private set; }

    private EnemyClassification classification;
    private EnemyCombat combat;
    private EnemyAwareness awareness;
    private EnemyBreak broken;
    private CharacterHealth health;
    private AnimationEventHandler handler;
    private Animator animator;
    private Transform target;
    private CharacterHealth targetHealth;
    private Animator targetAnimator;
    private Vector2 home;
    private float leash;
    private Combo pending;
    private string[] sequence;
    private string currentSkill;
    private int step;
    private bool active, entered, waiting;
    private float startedAt, nextStepAt, recoveryUntil;
    private float observedAt = float.NegativeInfinity;
    private int observedState;
    private float observedNormalized;

    private void Awake()
    {
        classification = GetComponent<EnemyClassification>();
        combat = GetComponent<EnemyCombat>();
        awareness = GetComponent<EnemyAwareness>();
        broken = GetComponent<EnemyBreak>();
        health = GetComponent<CharacterHealth>();
        handler = GetComponent<AnimationEventHandler>();
        animator = GetComponent<Animator>();
    }

    private void BindTarget(Transform player)
    {
        if (target == player) return;
        target = player;
        targetHealth = player != null ? player.GetComponent<CharacterHealth>() : null;
        targetAnimator = player != null ? player.GetComponent<Animator>() : null;
        observedState = 0;
        observedAt = float.NegativeInfinity;
        ObservedMove = PlayerMove.Neutral;
    }

    private void Observe()
    {
        if (targetAnimator == null || !targetAnimator.isActiveAndEnabled ||
            (awareness != null && !awareness.HasLineOfSight(target))) return;
        var state = targetAnimator.GetCurrentAnimatorStateInfo(0);
        var move = state.IsName(attackState) ? PlayerMove.Attack :
            state.IsName(heavyAttackState) ? PlayerMove.HeavyAttack :
            state.IsName(dashState) ? PlayerMove.Dash :
            state.IsName(parryState) ? PlayerMove.Parry : PlayerMove.Neutral;
        if (state.fullPathHash != observedState || state.normalizedTime < observedNormalized)
        {
            observedState = state.fullPathHash;
            if (move != PlayerMove.Neutral)
            {
                ObservedMove = move;
                observedAt = Time.time;
            }
        }
        observedNormalized = state.normalizedTime;
    }

    public SkillData SelectOpening(List<SkillData> ready, Transform player)
    {
        pending = null;
        BindTarget(player);
        Observe();
        if (UnityEngine.Random.value > comboChance)
            return ready[UnityEngine.Random.Range(0, ready.Count)];
        float age = Time.time - observedAt;
        bool canReact = age >= reactionDelay && age <= observationMemory;
        float distance = Vector2.Distance(transform.position, player.position);
        var candidates = new List<Combo>();
        float total = 0;
        // Prefer a delayed response to a visible move, otherwise use a general combo.
        for (int pass = 0; pass < 2 && candidates.Count == 0; pass++)
            foreach (var combo in combos)
            {
                if (combo == null || combo.skills == null || combo.skills.Length == 0 || combo.weight <= 0 ||
                    distance < combo.minDistance || distance > combo.maxDistance) continue;
                if (pass == 0 ? !canReact || combo.responseTo != ObservedMove || combo.responseTo == PlayerMove.Any :
                    combo.responseTo != PlayerMove.Any) continue;
                if (!ready.Exists(s => s.skillName == combo.skills[0])) continue;
                if (Array.Exists(combo.skills,skill=>!combat.IsSkillAllowed(skill))) continue;
                candidates.Add(combo);
                total += combo.weight;
            }
        if (candidates.Count == 0) return ready[UnityEngine.Random.Range(0, ready.Count)];
        float roll = UnityEngine.Random.value * total;
        pending = candidates[candidates.Count - 1];
        foreach (var candidate in candidates)
        {
            roll -= candidate.weight;
            if (roll <= 0) { pending = candidate; break; }
        }
        return ready.Find(s => s.skillName == pending.skills[0]);
    }

    public void OnSkillStarted(string skill, Transform player, Vector2 start, float limit)
    {
        if (!active)
        {
            BindTarget(player);
            home = start;
            leash = limit;
            sequence = pending != null ? (string[])pending.skills.Clone() : new[] { skill };
            CurrentCombo = pending != null ? pending.name : skill;
            pending = null;
            step = 0;
            active = true;
        }
        currentSkill = skill;
        entered = false;
        waiting = false;
        startedAt = Time.time;
    }

    private void Update()
    {
        if (!IsBossActive) { if (active) CancelCombo(); return; }
        Observe();
        if (!active) return;
        if (target == null || !target.gameObject.activeInHierarchy || (targetHealth != null && targetHealth.isDead) ||
            (health != null && health.isDead) || (broken != null && broken.IsBroken) ||
            (awareness != null && !awareness.HasLineOfSight(target)) ||
            Vector2.Distance(transform.position, home) > leash)
        { combat.CancelAttack(); return; }

        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("TakeDamage") || state.IsName("Broken") || state.IsName("Die"))
        { combat.CancelAttack(); return; }
        if (!waiting)
        {
            if (state.IsName(currentSkill)) entered = true;
            if (Time.time - startedAt > animationTimeout) { combat.CancelAttack(); return; }
            if (!entered || state.IsName(currentSkill) || animator.IsInTransition(0) ||
                (handler != null && handler.isActionLocked)) return;
            waiting = true;
            nextStepAt = Time.time + stepGap;
        }
        if (Time.time < nextStepAt) return;
        if (step + 1 >= sequence.Length || step + 1 >= maxComboLength)
        { CancelCombo(); return; }
        string next = sequence[step + 1];
        var skill = combat.skillManager.skills.Find(s => s.skillName == next);
        if (skill == null || !combat.IsSkillAllowed(next) || !combat.skillManager.IsSkillReady(next) ||
            Vector2.Distance(transform.position, target.position) > skill.attackRange + Mathf.Abs(skill.hitboxOffset.x) + .25f)
        { CancelCombo(); return; }
        step++;
        if (!combat.TriggerSkill(next, target, home, leash)) CancelCombo();
    }

    public void CancelCombo()
    {
        if (active) recoveryUntil = Time.time + recoveryTime;
        active = false;
        pending = null;
        sequence = null;
        CurrentCombo = null;
    }

    private void OnDisable() => CancelCombo();
}
