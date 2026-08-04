using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class CharacterHealth : MonoBehaviour
{
    private CharacterStats stats;
    private Rigidbody2D rb;
    private Animator animator;
    private CharacterInvulnerability invulnerability;
    private CharacterParry parryComponent;
    private AnimationEventHandler animHandler;

    [HideInInspector] public bool isDead = false;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        invulnerability = GetComponent<CharacterInvulnerability>();
        parryComponent = GetComponent<CharacterParry>();
        animHandler = GetComponent<AnimationEventHandler>();
    }

    public void TakeDamage(float physDamage, float trueDamage, float knockbackForce, bool canBeParried, Transform attacker)
    {
        if (isDead) return;

        // 1. KIỂM TRA PARRY
        if (canBeParried && parryComponent != null && parryComponent.isParrying)
        {
            parryComponent.ExecuteParry(attacker);
            return;
        }

        // 2. KIỂM TRA VÔ ĐỊCH (I-FRAMES)
        if (invulnerability != null && invulnerability.isInvulnerable) return;

        // 3. KHÓA HÀNH ĐỘNG & ÉP ANIMATION
        if (animHandler != null)
        {
            animHandler.LockAction();
        }

        if (animator != null)
        {
            animator.Play("TakeDamage", -1, 0f);
        }

        // 4. TÍNH SÁT THƯƠNG
        float actualPhysDamage = Mathf.Max(0, physDamage - stats.DEF);
        float totalDamage = actualPhysDamage + trueDamage;
        stats.currentHP -= totalDamage;

        Debug.Log($"<color=red><b>{gameObject.name}</b></color> nhận {totalDamage} DMG. HP còn: {stats.currentHP}");

        if (stats.currentHP <= 0)
        {
            Die();
            return;
        }

        if (invulnerability != null) invulnerability.GrantInvulnerability(0.3f);

        // 5. TÍNH LỰC VÀ BẬT COROUTINE ĐẨY LÙI
        float resistanceMultiplier = Mathf.Clamp01(1f - stats.RES);
        float finalKnockback = knockbackForce * resistanceMultiplier;

        if (rb != null && attacker != null && finalKnockback > 0f)
        {
            Vector2 knockbackDir = (transform.position - attacker.position).normalized;

            // Dừng Coroutine đẩy lùi cũ (nếu có) và khởi chạy Coroutine đẩy lùi mới
            StopCoroutine("ApplyKnockbackRoutine");
            StartCoroutine(ApplyKnockbackRoutine(knockbackDir, finalKnockback));
        }
    }

    /// <summary>
    /// Coroutine cưỡng chế duy trì vận tốc trượt lùi trong thời gian ngắn (0.12s)
    /// Tránh việc bị FixedUpdate hoặc Linear Drag dập tắt lực tức thì.
    /// </summary>
    private IEnumerator ApplyKnockbackRoutine(Vector2 direction, float force)
    {
        float timer = 0f;
        float duration = 0.25f; // Thời gian duy trì lực đẩy trượt lùi

        while (timer < duration)
        {
            if (isDead) yield break;

            // Cưỡng chế gán vận tốc đẩy lùi trượt mượt mà
            rb.linearVelocity = direction * force;

            timer += Time.deltaTime;
            yield return null;
        }

        // Sau khi hết thời gian đẩy cưỡng chế, giảm tốc mượt về 0
        rb.linearVelocity = Vector2.zero;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();

        if (animator != null) animator.Play("Die", -1, 0f);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;

        MonoBehaviour[] scriptsToDisable = GetComponents<MonoBehaviour>();
        foreach (var script in scriptsToDisable)
        {
            if (script != this && script != stats && script.GetType() != typeof(AnimationEventHandler))
            {
                script.enabled = false;
            }
        }
    }
}