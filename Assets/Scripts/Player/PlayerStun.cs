using UnityEngine;

/// <summary>Exhaustion stun affects only this character; enemies keep updating.</summary>
[RequireComponent(typeof(CharacterStats), typeof(CharacterHealth), typeof(Animator))]
public class PlayerStun : MonoBehaviour
{
    [Header("Choáng khi Stamina thấp và mất máu")]
    public bool enableExhaustionStun = true;
    [Range(0f, 1f)] public float stunChance = 0.35f;
    [Range(0f, 100f), Tooltip("Chỉ có thể choáng khi Stamina còn dưới phần trăm này của maxStamina.")]
    public float exhaustedStaminaPercent = 8f;
    [Min(0f)] public float baseStunDuration = 2f;
    [Tooltip("Clip tổng hợp nhận đòn, ngã và đứng dậy; không chứa sự kiện chết.")]
    public AnimationClip stunAnimation;
    public bool IsStunned { get; private set; }
    public float RemainingDuration { get; private set; }
    public float EffectiveDuration => Mathf.Max(0f, baseStunDuration) * (1f - Mathf.Clamp01(stats != null ? stats.EffectiveResistance : 0f));

    private CharacterStats stats;
    private CharacterHealth health;
    private Animator animator;
    private AnimationEventHandler handler;
    private PlayerController controller;
    private PlayerDash dash;
    private CharacterParry parry;
    private Rigidbody2D body;
    private static readonly int StunFlag = Animator.StringToHash("IsStunned");

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        health = GetComponent<CharacterHealth>();
        animator = GetComponent<Animator>();
        handler = GetComponent<AnimationEventHandler>();
        controller = GetComponent<PlayerController>();
        dash = GetComponent<PlayerDash>();
        parry = GetComponent<CharacterParry>();
        body = GetComponent<Rigidbody2D>();
    }

    public bool TryStunOnDamage(float damage)
    {
        if (BossPhaseController.Protects(gameObject) || !isActiveAndEnabled || !enableExhaustionStun || IsStunned || health.isDead ||
            damage <= 0f || stats.maxStamina <= 0f ||
            stats.currentStamina >= stats.maxStamina * Mathf.Clamp(exhaustedStaminaPercent, 0f, 100f) * 0.01f) return false;
        float duration = EffectiveDuration;
        if (duration <= 0f || Random.value >= Mathf.Clamp01(stunChance)) return false;
        IsStunned = true;
        RemainingDuration = duration;
        if (controller != null) controller.StopSprinting();
        if (dash != null) dash.CancelDash();
        if (parry != null) parry.isParrying = false;
        if (body != null) body.linearVelocity = Vector2.zero;
        if (handler != null) handler.LockAction();
        animator.ResetTrigger("PlayerNormalAttack");
        animator.ResetTrigger("PlayerParry");
        animator.ResetTrigger("PlayerSkill2");
        animator.SetBool("IsWalking", false);
        animator.SetBool("IsRunning", false);
        animator.SetBool(StunFlag, true);
        animator.SetFloat("StunAnimationSpeed", stunAnimation != null ? stunAnimation.length / duration : 1f);
        animator.Play("Stunned", 0, 0f);
        return true;
    }

    private void Update() { Tick(Time.deltaTime); }

    private void Tick(float deltaTime)
    {
        if (!IsStunned) return;
        if (health.isDead) { EndStun(false); return; }
        RemainingDuration = Mathf.Max(0f, RemainingDuration - Mathf.Max(0f, deltaTime));
        if (RemainingDuration <= 0f) EndStun(true);
    }

    private void EndStun(bool restoreControl)
    {
        IsStunned = false;
        RemainingDuration = 0f;
        animator.SetBool(StunFlag, false);
        if (restoreControl && handler != null) handler.UnlockAction();
    }

    private void OnDisable()
    {
        if (IsStunned) EndStun(health != null && !health.isDead);
    }
    public void CancelStun(){if(IsStunned)EndStun(true);}
}
