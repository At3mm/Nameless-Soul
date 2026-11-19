using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
public class PlayerDash : MonoBehaviour
{
    [Header("Cài đặt Dash")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.8f;
    [Header("Chi phí Stamina")]
    [Min(0f), Tooltip("Stamina tiêu hao cho mỗi lần dash. Không đủ Stamina sẽ không thể dash.")]
    public float dashStaminaCost = 20f;
    private float lastDashTime = -10f;

    private Rigidbody2D rb;
    private Animator animator;
    private CharacterInvulnerability invulnerability;
    private AnimationEventHandler animHandler;
    private CharacterStats stats;
    private CharacterHealth health;
    private PlayerController controller;
    private float dashAnimationLength = 0.2f;

    [HideInInspector] public bool isDashing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        invulnerability = GetComponent<CharacterInvulnerability>();
        animHandler = GetComponent<AnimationEventHandler>();
        stats = GetComponent<CharacterStats>();
        health = GetComponent<CharacterHealth>();
        controller = GetComponent<PlayerController>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == "PlayerDash") dashAnimationLength = clip.length;
        }
    }

    public void ResetCooldown() { lastDashTime = Time.time - Mathf.Max(0f,dashCooldown); }

    public void TryDash(Vector2 inputDirection)
    {
        if (!isActiveAndEnabled || (health != null && health.isDead)) return;
        // Chặn Dash nếu đang bị khóa hành động hoặc đang trong thời gian hồi
        if (animHandler != null && animHandler.isActionLocked) return;
        if (Time.time < lastDashTime + dashCooldown || isDashing) return;

        float cost = Mathf.Max(0f, dashStaminaCost) * (stats != null ? stats.StaminaCostMultiplier : 1f);
        if (stats.currentStamina < cost)
        {
            GetComponent<World.PlayerInteractor>()?.ShowNotice("Không đủ Stamina để lướt.");
            return;
        }
        stats.currentStamina = Mathf.Max(0f, stats.currentStamina - cost);
        if (controller != null)
        {
            controller.StopSprinting();
            controller.DelayStaminaRecovery();
        }

        StartCoroutine(PerformDash(inputDirection));
    }

    private IEnumerator PerformDash(Vector2 inputDirection)
    {
        isDashing = true;
        CombatSFX.Dash(transform.position);
        lastDashTime = Time.time;

        if (invulnerability != null)
        {
            invulnerability.GrantInvulnerability(dashDuration);
        }

        Vector2 dashDirection = inputDirection.sqrMagnitude > 0.01f
            ? inputDirection.normalized
            : new Vector2(Mathf.Sign(transform.localScale.x), 0f);

        if (animator != null)
        {
            animator.SetFloat("DashAnimationSpeed", dashAnimationLength / Mathf.Max(0.01f, dashDuration));
            animator.SetBool("IsDashing", true);
        }

        float timer = 0f;
        while (timer < dashDuration)
        {
            rb.linearVelocity = dashDirection * dashSpeed * (stats != null ? stats.MovementEffectMultiplier : 1f);
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isDashing = false;
        if (animator != null) animator.SetBool("IsDashing", false);
        if (controller != null)
        {
            controller.DelayStaminaRecovery();
            controller.BeginSprintAfterDash();
        }
    }

    private void OnDisable()
    {
        CancelDash();
    }

    public void CancelDash()
    {
        StopAllCoroutines();
        if (isDashing && rb != null) rb.linearVelocity = Vector2.zero;
        isDashing = false;
        if (animator != null) animator.SetBool("IsDashing", false);
        if (controller != null) controller.StopSprinting();
    }
}
