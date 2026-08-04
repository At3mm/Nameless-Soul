using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Cài đặt Dash")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.8f;
    private float lastDashTime = -10f;

    private Rigidbody2D rb;
    private Animator animator;
    private CharacterInvulnerability invulnerability;
    private AnimationEventHandler animHandler;

    [HideInInspector] public bool isDashing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        invulnerability = GetComponent<CharacterInvulnerability>();
        animHandler = GetComponent<AnimationEventHandler>();
    }

    public void TryDash(Vector2 inputDirection)
    {
        // Chặn Dash nếu đang bị khóa hành động hoặc đang trong thời gian hồi
        if (animHandler != null && animHandler.isActionLocked) return;
        if (Time.time < lastDashTime + dashCooldown || isDashing) return;

        StartCoroutine(PerformDash(inputDirection));
    }

    private IEnumerator PerformDash(Vector2 inputDirection)
    {
        isDashing = true;
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
            animator.SetTrigger("PlayerDash");
        }

        float timer = 0f;
        while (timer < dashDuration)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isDashing = false;
    }
}