using UnityEngine;

/// <summary>Stamina refills after a quiet interval; an active break has a fixed recovery duration.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats), typeof(CharacterHealth), typeof(Animator))]
public sealed class EnemyBreak : MonoBehaviour
{
    [Min(0.1f)] public float recoveryDelay = 4f;
    [Min(0f)] public float staminaDamageMultiplier = 1f;
    [Range(0f, 100f)] public float defenseReductionPercent = 50f;
    public string brokenState = "Broken";
    public string idleState = "OrcIdle";
    public bool showWorldBar = true;
    [Min(0f)] public float barHeight = 0.66f;
    public bool IsBroken { get; private set; }
    public float RecoveryRemaining => Mathf.Max(0f, recoveryDelay - quietTime);
    public float DefenseMultiplier => isActiveAndEnabled && IsBroken ? 1f - Mathf.Clamp01(defenseReductionPercent * .01f) : 1f;
    public float Ratio => stats != null && stats.maxStamina > 0f ? Mathf.Clamp01(stats.currentStamina / stats.maxStamina) : 0f;

    private CharacterStats stats;
    private CharacterHealth health;
    private Animator animator;
    private AnimationEventHandler handler;
    private Rigidbody2D body;
    private EnemyCombat combat;
    private EliteOrcTargetedLeap leap;
    private float quietTime;
    private AnimatorControllerParameter[] animationParameters;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>(); health = GetComponent<CharacterHealth>();
        animator = GetComponent<Animator>(); handler = GetComponent<AnimationEventHandler>();
        body = GetComponent<Rigidbody2D>(); combat = GetComponent<EnemyCombat>(); leap = GetComponent<EliteOrcTargetedLeap>();
        animationParameters = animator.parameters;
    }
    private void OnEnable()
    {
        quietTime = 0f; IsBroken = false;
        if (stats != null) stats.currentStamina = Mathf.Max(0f, stats.maxStamina);
    }
    private void Start() { if (showWorldBar) EnemyStaminaBar.Create(this); }
    public void ReceiveHit(float staminaDamage)
    {
        if (BossPhaseController.Protects(gameObject) || !isActiveAndEnabled || health.isDead || staminaDamage < 0f || float.IsNaN(staminaDamage) || float.IsInfinity(staminaDamage)) return;
        if (IsBroken)
        {
            MaintainBrokenState();
            return;
        }
        quietTime = 0f;
        if (!IsBroken)
        {
            stats.currentStamina = Mathf.Max(0f, stats.currentStamina - staminaDamage * Mathf.Max(0f, staminaDamageMultiplier));
            if (stats.currentStamina <= 0f) BeginBreak();
        }
    }
    private void BeginBreak()
    {
        if (IsBroken || health.isDead || stats.maxStamina <= 0f) return;
        IsBroken = true; stats.currentStamina = 0f; quietTime = 0f;
        CombatSFX.EnemyBroken(transform.position);
        health.CancelKnockback();
        if (combat != null) combat.CancelAttack();
        if (leap != null) leap.CancelLeap();
        var parry = GetComponent<CharacterParry>(); if (parry != null) parry.isParrying = false;
        MaintainBrokenState(true);
    }
    private void Update() { Tick(Time.deltaTime); }
    private void LateUpdate()
    {
        // Animator Any State transitions and late animation events must not escape a live break.
        if (IsBroken && !health.isDead) MaintainBrokenState();
    }

    private void MaintainBrokenState(bool forceAnimation = false)
    {
        stats.currentStamina = Mathf.Max(0f,stats.maxStamina)*Mathf.Clamp01(quietTime/Mathf.Max(.1f,recoveryDelay));
        if (handler != null) handler.LockAction();
        if (body != null) body.linearVelocity = Vector2.zero;
        animator.SetBool("IsWalking", false);
        foreach (var parameter in animationParameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name != "Die")
                animator.ResetTrigger(parameter.nameHash);
        int state = Animator.StringToHash(brokenState);
        if (animator.HasState(0, state) &&
            (forceAnimation || !animator.GetCurrentAnimatorStateInfo(0).IsName(brokenState) || animator.IsInTransition(0)))
        {
            animator.Play(state, 0, 0f);
            animator.Update(0f);
        }
    }
    private void FixedUpdate() { if (IsBroken && body != null) body.linearVelocity = Vector2.zero; }
    private void Tick(float dt)
    {
        if(BossPhaseController.Protects(gameObject))return;
        if (health.isDead || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
        if (!IsBroken && stats.maxStamina > 0f && stats.currentStamina <= 0f) BeginBreak();
        if (!IsBroken && stats.currentStamina >= stats.maxStamina) return;
        quietTime += dt;
        if(IsBroken)stats.currentStamina=Mathf.Max(0f,stats.maxStamina)*Mathf.Clamp01(quietTime/Mathf.Max(.1f,recoveryDelay));
        if (quietTime < Mathf.Max(.1f, recoveryDelay)) return;
        stats.currentStamina = Mathf.Max(0f, stats.maxStamina);
        bool wasBroken = IsBroken; IsBroken = false; quietTime = 0f;
        if (wasBroken)
        {
            if (handler != null) handler.UnlockAction();
            if (animator.HasState(0, Animator.StringToHash(idleState))) animator.Play(idleState, 0, 0f);
        }
    }
    public void ClearBreak()
    {
        IsBroken=false;quietTime=0;
        if(stats!=null)stats.currentStamina=Mathf.Max(0,stats.maxStamina);
    }
    private void OnDisable()
    {
        bool wasBroken = IsBroken; IsBroken = false;
        if (wasBroken && health != null && !health.isDead)
        {
            stats.currentStamina = Mathf.Max(0f, stats.maxStamina);
            if (handler != null) handler.UnlockAction();
            if (animator != null && animator.HasState(0, Animator.StringToHash(idleState))) animator.Play(idleState, 0, 0f);
        }
    }
}
