using System.Collections;
using UnityEngine;

public sealed class OrcRiderCombat : EnemyCombat
{
    public const string QuickSlash = "NormalAttack";
    public const string HeavySlash = "HeavyAttack";
    public const string SpinningSlash = "Skill3";

    [Header("Bước nhích khi bắt đầu đòn xoay")]
    [Min(0f)] public float spinAdvanceDistance = .3f;
    [Min(.02f)] public float spinAdvanceDuration = .12f;

    // The quick slash retains its existing lunge. Spin has its own short step.
    protected override bool ShouldLunge(SkillData skill) => skill.skillName == QuickSlash;

    protected override void OnAttackStarted(SkillData skill, Transform player, Vector2 home, float chaseLimit)
    {
        if (skill.skillName == SpinningSlash && spinAdvanceDistance > 0f)
            StartCoroutine(AdvanceSpin(player, home, chaseLimit));
    }

    private IEnumerator AdvanceSpin(Transform player, Vector2 home, float chaseLimit)
    {
        var body = GetComponent<Rigidbody2D>();
        var health = GetComponent<CharacterHealth>();
        var broken = GetComponent<EnemyBreak>();
        var awareness = GetComponent<EnemyAwareness>();
        var animator = GetComponent<Animator>();
        var ownCollider = GetComponent<Collider2D>();
        var targetCollider = player.GetComponent<Collider2D>();
        var hits = new RaycastHit2D[16];
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(obstacleLayer | skillManager.targetLayer | (1 << gameObject.layer));
        Vector2 direction = ((Vector2)player.position - body.position).normalized;
        float remaining = spinAdvanceDistance;
        float speed = spinAdvanceDistance / Mathf.Max(.02f, spinAdvanceDuration);
        while (remaining > .001f)
        {
            yield return new WaitForFixedUpdate();
            if (!isActiveAndEnabled || player == null || health.isDead || broken.IsBroken ||
                !awareness.HasLineOfSight(player)) yield break;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("TakeDamage") || state.IsName("Broken") || state.IsName("Die")) yield break;
            if (ownCollider != null && targetCollider != null)
            {
                var separation = ownCollider.Distance(targetCollider);
                if (separation.isOverlapped || separation.distance <= .04f) yield break;
            }
            float step = Mathf.Min(remaining, speed * Time.fixedDeltaTime);
            int count = body.Cast(direction, filter, hits, step + .04f);
            if (count == hits.Length) yield break;
            for (int i = 0; i < count; i++)
                if (hits[i].collider != null && !hits[i].transform.IsChildOf(transform))
                    step = Mathf.Min(step, Mathf.Max(0f, hits[i].distance - .04f));
            Vector2 next = body.position + direction * step;
            if (step <= .001f || Vector2.Distance(next, home) > chaseLimit) yield break;
            body.MovePosition(next);
            remaining -= step;
        }
    }

    private void OnDisable() => StopAllCoroutines();
}
