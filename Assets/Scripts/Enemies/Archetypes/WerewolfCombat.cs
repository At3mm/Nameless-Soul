using UnityEngine;

public sealed class WerewolfCombat : EnemyCombat
{
    public const string Slash = "NormalAttack";
    public const string Teleport = "TeleportAttack";

    [Header("Xuất hiện lại trong chiêu teleport")]
    [Min(.1f)] public float reappearDistance = .8f;
    [Min(.1f)] public float maxTeleportDistance = 4f;
    [SerializeField] private Transform attackPoint;

    private Transform target;
    private CharacterHealth targetHealth;
    private CharacterHealth health;
    private EnemyBreak breakState;
    private EnemyAwareness awareness;
    private Animator attackAnimator;
    private Rigidbody2D body;
    private Collider2D bodyCollider;
    private Vector2 home;
    private float chaseLimit;
    private bool teleportArmed;
    private readonly Collider2D[] landingHits = new Collider2D[16];
    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[1];

    protected new void Awake()
    {
        base.Awake();
        health = GetComponent<CharacterHealth>();
        breakState = GetComponent<EnemyBreak>();
        awareness = GetComponent<EnemyAwareness>();
        attackAnimator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        if (attackPoint == null) attackPoint = transform.Find("AttackPoint");
    }

    protected override bool ShouldLunge(SkillData skill) => skill.skillName == Slash;

    protected override void OnAttackStarted(SkillData skill, Transform player, Vector2 start, float limit)
    {
        target = player;
        targetHealth = player.GetComponent<CharacterHealth>();
        home = start;
        chaseLimit = Mathf.Max(0f, limit);
        teleportArmed = skill.skillName == Teleport;
    }

    // Place this event on the first visible frame after the disappearance.
    public void ReappearNearTarget(AnimationEvent animationEvent)
    {
        if (!teleportArmed || !CanContinueTeleport() || !animationEvent.isFiredByAnimator ||
            !animationEvent.animatorStateInfo.IsName(Teleport)) return;
        teleportArmed = false;
        if (!awareness.HasLineOfSight(target)) return;

        Vector2 playerPosition = target.position;
        Vector2 nearSide = (body.position - playerPosition).normalized;
        if (nearSide.sqrMagnitude < .001f) nearSide = Vector2.left;
        var skill = skillManager.skills.Find(item => item.skillName == Teleport);
        if (skill == null) return;
        float spacing = Mathf.Min(Mathf.Max(.1f, reappearDistance),
            Mathf.Max(.1f, skill.attackRange + Mathf.Abs(skill.hitboxOffset.x) - .05f));

        // Try nearby landing positions, starting on the side closest to the wolf.
        for (int i = 0; i < 8; i++)
        {
            Vector2 direction = Quaternion.Euler(0, 0, i * 45f) * nearSide;
            Vector2 landing = playerPosition + direction * spacing;
            if (!CanLand(landing)) continue;

            body.linearVelocity = Vector2.zero;
            body.position = landing;
            transform.position = new Vector3(landing.x, landing.y, transform.position.z);
            AimAt(playerPosition);
            Physics2D.SyncTransforms();
            if (attackPoint != null)
                skill.hitboxOffset = new Vector2(Mathf.Abs(attackPoint.localPosition.x), attackPoint.localPosition.y);
            return;
        }
    }

    private bool CanLand(Vector2 landing)
    {
        if ((landing - body.position).sqrMagnitude > maxTeleportDistance * maxTeleportDistance ||
            (landing - home).sqrMagnitude > chaseLimit * chaseLimit) return false;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(obstacleLayer);
        if (Physics2D.Linecast(body.position, landing, filter, wallHits) > 0) return false;

        filter.SetLayerMask(obstacleLayer | (1 << target.gameObject.layer) | (1 << gameObject.layer));
        Vector2 colliderOffset = (Vector2)bodyCollider.bounds.center - body.position;
        Vector2 size = (Vector2)bodyCollider.bounds.size + Vector2.one * .02f;
        int count = Physics2D.OverlapBox(landing + colliderOffset, size, 0, filter, landingHits);
        if (count == landingHits.Length) return false;
        for (int i = 0; i < count; i++)
            if (!landingHits[i].transform.IsChildOf(transform)) return false;
        return true;
    }

    private bool CanContinueTeleport()
    {
        return isActiveAndEnabled && target != null && targetHealth != null && !targetHealth.isDead &&
            !health.isDead && !breakState.IsBroken &&
            attackAnimator.GetCurrentAnimatorStateInfo(0).IsName(Teleport) && !attackAnimator.IsInTransition(0);
    }

    private void AimAt(Vector2 position)
    {
        Vector2 direction = (position - body.position).normalized;
        skillManager.currentAttackDirection = direction;
        var scale = transform.localScale;
        if (Mathf.Abs(direction.x) > .01f) scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction.x);
        transform.localScale = scale;
    }

    public override void CancelAttack()
    {
        teleportArmed = false;
        base.CancelAttack();
    }

    private void OnDisable()
    {
        teleportArmed = false;
        target = null;
    }
}
