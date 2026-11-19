using UnityEngine;

/// <summary>Tracks a landing point during Skill3, then commits before impact.</summary>
[RequireComponent(typeof(Animator), typeof(Rigidbody2D), typeof(EnemyCombat))]
public class EliteOrcTargetedLeap : MonoBehaviour
{
    public string skillName = "Skill3";
    public string animationState = "Attack03";
    public AnimationClip attackAnimation;
    [Min(0f)] public float takeoffTime = .25f;
    [Min(.01f)] public float landingTime = 1f;
    [Min(0f)] public float commitBeforeLanding = .18f;
    [Min(0f)] public float landingDistanceFromPlayer = .75f;
    [Min(0f)] public float maxLeapDistance = 4f;
    public bool IsLeaping { get; private set; }
    public Vector2 LandingPoint { get; private set; }

    private Transform target;
    private Vector2 origin, approach, home;
    private float leash, startedAt;
    private bool enteredState;
    private Rigidbody2D body;
    private Animator animator;
    private EnemyCombat combat;
    private CharacterHealth health;
    private SkillDamageManager damage;
    private EnemyAwareness awareness;
    private Vector2 lastVisibleTarget;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[16];

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        combat = GetComponent<EnemyCombat>();
        health = GetComponent<CharacterHealth>();
        damage = GetComponent<SkillDamageManager>();
        awareness = GetComponent<EnemyAwareness>();
    }

    public bool TryBegin(string selectedSkill, Transform player, Vector2 homePosition, float chaseLimit)
    {
        if (GetComponent<EnemyBreak>() != null && GetComponent<EnemyBreak>().IsBroken) return false;
        if (!isActiveAndEnabled || selectedSkill != skillName || player == null) return false;
        if (awareness != null && !awareness.HasLineOfSight(player)) return false;
        target = player;
        lastVisibleTarget = player.position;
        origin = body.position;
        home = homePosition;
        leash = Mathf.Max(0f, chaseLimit);
        approach = ((Vector2)player.position - origin).normalized;
        if (approach.sqrMagnitude < .001f) approach = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        LandingPoint = CalculateLandingPoint(player.position);
        enteredState = false;
        startedAt = Time.time;
        IsLeaping = true;
        body.linearVelocity = Vector2.zero;
        GetComponent<AnimationEventHandler>()?.LockAction();
        return true;
    }

    private Vector2 CalculateLandingPoint(Vector2 playerPosition)
    {
        Vector2 point = playerPosition - approach * Mathf.Max(0f, landingDistanceFromPlayer);
        point = origin + Vector2.ClampMagnitude(point - origin, Mathf.Max(0f, maxLeapDistance));
        return home + Vector2.ClampMagnitude(point - home, leash);
    }

    private bool SampleAnimation(out float clipTime)
    {
        clipTime = 0f;
        if (!IsLeaping) return false;
        if (health != null && health.isDead || target == null) { CancelLeap(); return false; }
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (!state.IsName(animationState))
        {
            if (enteredState || Time.time - startedAt > .35f || state.IsName("TakeDamage") || state.IsName("Die")) CancelLeap();
            return false;
        }
        enteredState = true;
        clipTime = state.normalizedTime * (attackAnimation != null ? attackAnimation.length : state.length);
        return true;
    }

    private void FixedUpdate()
    {
        if (!SampleAnimation(out float time) || time < takeoffTime) return;
        UpdateVisibleTarget();
        if (time < landingTime - Mathf.Max(0f, commitBeforeLanding))
            LandingPoint = CalculateLandingPoint(lastVisibleTarget);
        float remaining = Mathf.Max(Time.fixedDeltaTime, landingTime - time);
        Vector2 step = Vector2.Lerp(body.position, LandingPoint, Mathf.Clamp01(Time.fixedDeltaTime / remaining));
        body.MovePosition(SafePosition(step));
        AimAtTarget();
    }

    private Vector2 SafePosition(Vector2 desired)
    {
        Vector2 delta = desired - body.position;
        if (delta.sqrMagnitude < .000001f) return body.position;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(combat.obstacleLayer);
        filter.useTriggers = false;
        int count = body.Cast(delta.normalized, filter, hits, delta.magnitude + .03f);
        float distance = delta.magnitude;
        for (int i = 0; i < count; i++)
            distance = Mathf.Min(distance, Mathf.Max(0f, hits[i].distance - .03f));
        return body.position + delta.normalized * distance;
    }

    private void AimAtTarget()
    {
        if (damage == null || target == null) return;
        UpdateVisibleTarget();
        Vector2 direction = lastVisibleTarget - body.position;
        if (direction.sqrMagnitude < .001f) direction = approach;
        damage.currentAttackDirection = direction.normalized;
        if (Mathf.Abs(direction.x) > .01f)
            transform.localScale = new Vector3(direction.x > 0 ? 1 : -1, 1, 1);
    }

    private void UpdateVisibleTarget()
    {
        if (target != null && (awareness == null || awareness.HasLineOfSight(target)))
            lastVisibleTarget = target.position;
    }

    // Animation event, placed immediately before the first Skill3 damage event.
    public void LandTargetedLeap()
    {
        if (!SampleAnimation(out _)) return;
        body.position = SafePosition(LandingPoint);
        AimAtTarget();
        CancelLeap();
    }

    public void CancelLeap()
    {
        if (IsLeaping && body != null) body.linearVelocity = Vector2.zero;
        IsLeaping = false;
        target = null;
    }
    private void OnDisable() { CancelLeap(); }
}
