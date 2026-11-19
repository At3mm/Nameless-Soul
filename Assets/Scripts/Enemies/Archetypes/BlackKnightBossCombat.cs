using UnityEngine;

/// <summary>Variant C attacks: animation-driven melee and a separately aimed beam (not variant A's leap).</summary>
[DisallowMultipleComponent]
public sealed class BlackKnightBossCombat : EnemyCombat
{
    [Header("Attack selection")]
    [SerializeField, Min(.1f)] private float _slashDistance = 3.4f;
    [SerializeField, Min(.1f)] private float _heavyDistance = 4.2f;
    [SerializeField, Min(.1f)] private float _beamDistance = 9.3f;
    [Header("Beam release")]
    [SerializeField] private BlackKnightBossBeam _beamPrefab;
    [SerializeField] private Transform _beamOrigin;
    [SerializeField, Min(0)] private float _muzzleOffset = 1.3f;
    [SerializeField, Min(.1f)] private float _stateEntryTimeout = .5f;
    private Animator _animator;
    private Rigidbody2D _body;
    private CharacterHealth _health;
    private EnemyBreak _broken;
    private EnemyAwareness _awareness;
    private AnimationEventHandler _action;
    private Transform _target;
    private CharacterHealth _targetHealth;
    private Collider2D _targetCollider;
    private string _activeSkill;
    private float _startedAt;
    private bool _entered;
    private bool _released;
    private BlackKnightBossBeam _beam;
    public bool IsPreparingBeam => _activeSkill == "Skill3" && !_released;
    public Vector2 CommittedDirection { get; private set; }
    public BlackKnightBossBeam ActiveBeam => _beam;

    protected new void Awake()
    {
        base.Awake();
        _animator = GetComponent<Animator>();
        _body = GetComponent<Rigidbody2D>();
        _health = GetComponent<CharacterHealth>();
        _broken = GetComponent<EnemyBreak>();
        _awareness = GetComponent<EnemyAwareness>();
        _action = GetComponent<AnimationEventHandler>();
    }

    private float Range(string skill) => skill == "Skill3" ? _beamDistance : skill == "Skill2" ? _heavyDistance : _slashDistance;
    protected override bool ShouldLunge(SkillData skill) => false;
    protected override bool CanSelectSkill(SkillData skill, Transform target)
    {
        return skill != null && target != null && (skill.skillName != "Skill3" || _beamPrefab != null) &&
            Vector2.Distance(transform.position, target.position) <= Range(skill.skillName);
    }

    public override bool IsInAttackRange(Transform player)
    {
        if (player == null || skillManager == null || (_awareness != null && !_awareness.HasLineOfSight(player))) return false;
        foreach (var skill in skillManager.skills)
            if (skill != null && IsSkillAllowed(skill.skillName) && skillManager.IsSkillReady(skill.skillName) && CanSelectSkill(skill, player)) return true;
        return false;
    }

    protected override void OnAttackStarted(SkillData skill, Transform player, Vector2 home, float chaseLimit)
    {
        ClearCurrent();
        _activeSkill = skill.skillName;
        _target = player;
        _targetHealth = player.GetComponent<CharacterHealth>();
        _targetCollider = player.GetComponent<Collider2D>();
        _startedAt = Time.time;
        _entered = _released = false;
        _body.linearVelocity = Vector2.zero;
        _action?.LockAction();
        UpdateAim();
    }

    private bool TargetValid()
    {
        return _target != null && _target.gameObject.activeInHierarchy &&
            (_targetHealth == null || (!_targetHealth.isDead && !_targetHealth.IsUntargetable));
    }

    private Vector2 Origin => _beamOrigin != null ? (Vector2)_beamOrigin.position : (Vector2)transform.position;
    private void UpdateAim()
    {
        if (!TargetValid()) return;
        Vector2 point = _targetCollider != null ? (Vector2)_targetCollider.bounds.center : (Vector2)_target.position;
        Vector2 direction = point - (_activeSkill == "Skill3" ? Origin : (Vector2)transform.position);
        if (direction.sqrMagnitude < .0001f) return;
        skillManager.currentAttackDirection = direction.normalized;
        if (Mathf.Abs(direction.x) > .01f)
        {
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction.x > 0 ? 1 : -1);
            transform.localScale = scale;
        }
    }

    // Invoked only on Attack03's first beam frame. Re-entrant events cannot spawn another beam.
    public void FireBeam()
    {
        if (_activeSkill != "Skill3" || _released || _beamPrefab == null || !TargetValid() ||
            _health == null || _health.isDead || (_broken != null && _broken.IsBroken) ||
            BossPhaseController.Protects(gameObject) || !_animator.GetCurrentAnimatorStateInfo(0).IsName("Skill3") ||
            (_awareness != null && !_awareness.HasLineOfSight(_target))) return;
        UpdateAim();
        Vector2 aim = skillManager.currentAttackDirection;
        if (aim.sqrMagnitude < .0001f) return;
        _released = true;
        CommittedDirection = aim.normalized;
        _beam = Instantiate(_beamPrefab, Origin + CommittedDirection * _muzzleOffset, Quaternion.identity);
        _beam.Initialize(skillManager, CommittedDirection, obstacleLayer);
        GetComponent<EnemyAudio>()?.Attack("Skill3");
    }

    private void Update()
    {
        if (_activeSkill == null) return;
        var state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName(_activeSkill)) _entered = true;
        if (_health.isDead || (_broken != null && _broken.IsBroken) || BossPhaseController.Protects(gameObject) || !TargetValid())
        { CancelAttack(); return; }
        if (!state.IsName(_activeSkill) && (_entered || Time.time - _startedAt > _stateEntryTimeout))
        {
            if (state.IsName("TakeDamage") || state.IsName("Broken") || state.IsName("Die")) CancelAttack();
            else ClearCurrent(false); // Release the projectile; keep the director's next combo step.
            return;
        }
        if (IsPreparingBeam) UpdateAim();
    }

    private void ClearCurrent(bool cancelProjectile = true)
    {
        if (cancelProjectile && _beam != null) { _beam.gameObject.SetActive(false); Destroy(_beam.gameObject); }
        _beam = null;
        _target = null;
        _targetHealth = null;
        _targetCollider = null;
        _activeSkill = null;
        _released = false;
    }

    public override void CancelAttack()
    {
        ClearCurrent();
        base.CancelAttack();
        if (_health != null && !_health.isDead && (_broken == null || !_broken.IsBroken) &&
            !BossPhaseController.Protects(gameObject) && _animator != null &&
            !_animator.GetCurrentAnimatorStateInfo(0).IsName("TakeDamage")) _action?.UnlockAction();
    }

    private void OnDisable() { CancelAttack(); }
}
