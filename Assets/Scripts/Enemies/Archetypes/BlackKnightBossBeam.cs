using System.Collections.Generic;
using UnityEngine;

/// <summary>A committed flying bolt. Swept collision prevents tunneling through targets and walls.</summary>
[DisallowMultipleComponent]
public sealed class BlackKnightBossBeam : MonoBehaviour
{
    [Header("Visual animation")]
    [SerializeField] private SpriteRenderer _visual;
    [SerializeField] private Animator _visualAnimator;
    [SerializeField] private Rect[] _frameBounds;
    [SerializeField, Min(1)] private float _framesPerSecond = 12f;
    [SerializeField] private Vector2 _nativeVisibleSize = new Vector2(2.9f, 5.7f);
    [Header("Projectile geometry")]
    [SerializeField, Min(.05f)] private float _width = 1f;
    [SerializeField, Min(.05f)] private float _maxLength = 2f;
    [SerializeField, Min(.01f)] private float _lifetime = 1.5f;
    [SerializeField, HideInInspector] private float _damageDuration = 3f / 12f;
    [SerializeField, Min(.1f)] private float _speed = 12f;
    [SerializeField, Min(.1f)] private float _maxTravelDistance = 12f;
    [SerializeField, Min(1)] private int _travelAnimationFrames = 3;
    [SerializeField, Min(0)] private float _wallSkin = .04f;
    private SkillDamageManager _owner;
    private CharacterHealth _ownerHealth;
    private EnemyBreak _ownerBreak;
    private Animator _ownerAnimator;
    private LayerMask _obstacles;
    private float _age;
    private bool _initialized;
    private readonly RaycastHit2D[] _wallHits = new RaycastHit2D[64];
    private readonly Collider2D[] _overlaps = new Collider2D[64];
    private readonly HashSet<CharacterHealth> _processed = new HashSet<CharacterHealth>();
    private readonly List<CharacterHealth> _targets = new List<CharacterHealth>();
    public Vector2 Direction { get; private set; }
    public float VisibleLength { get; private set; }
    public int ProcessedTargetCount => _processed.Count;

    public void Initialize(SkillDamageManager owner, Vector2 direction, LayerMask obstacles)
    {
        if (_initialized || owner == null || direction.sqrMagnitude < .0001f ||
            _visual == null || _visualAnimator == null || _frameBounds == null || _frameBounds.Length == 0)
        { Destroy(gameObject); return; }
        _owner = owner;
        _ownerHealth = owner.GetComponent<CharacterHealth>();
        _ownerBreak = owner.GetComponent<EnemyBreak>();
        _ownerAnimator = owner.GetComponent<Animator>();
        _obstacles = obstacles;
        Direction = direction.normalized;
        transform.rotation = Quaternion.identity;
        var casterVisual = owner.GetComponent<SpriteRenderer>();
        if (casterVisual != null)
        {
            _visual.sortingLayerID = casterVisual.sortingLayerID;
            _visual.sortingOrder = casterVisual.sortingOrder + 2;
        }
        _visual.color = Color.white;
        _age = 0;
        _initialized = true;
        _visualAnimator.Play("Beam", 0, 0);
        _visualAnimator.Update(0);
        UpdateGeometry(false);
    }

    private float _travelled;
    private bool _finished;

    private bool CanContinue()
    {
        // A released projectile is independent of the caster's current animation.
        return _owner != null && _owner.isActiveAndEnabled && _ownerHealth != null && !_ownerHealth.isDead &&
            (_ownerBreak == null || !_ownerBreak.IsBroken) && !BossPhaseController.Protects(_owner.gameObject);
    }

    private void Update()
    {
        if (!_initialized || _finished) return;
        if (!CanContinue()) { Finish(); return; }
        if (Time.deltaTime <= 0) return;
        _age += Time.deltaTime;
        if (_age >= _lifetime || _travelled >= _maxTravelDistance) { Finish(); return; }
        UpdateGeometry(false);
        float step = Mathf.Min(_speed * Time.deltaTime, _maxTravelDistance - _travelled);
        Rect bounds = CurrentFrameBounds();
        float sx = _width / Mathf.Max(.01f, _nativeVisibleSize.x);
        float sy = _maxLength / Mathf.Max(.01f, _nativeVisibleSize.y);
        Vector2 center = (Vector2)transform.position + new Vector2(bounds.center.x * sx, bounds.center.y * sy);
        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(_obstacles | _owner.targetLayer);
        int count = Physics2D.BoxCast(center, new Vector2(bounds.width * sx, bounds.height * sy),
            0f, Direction, filter, _wallHits, step);
        if (count == _wallHits.Length) { Finish(); return; }
        int nearest = -1;
        float distance = float.PositiveInfinity;
        bool nearestIsWall = false;
        for (int i = 0; i < count; i++)
        {
            var collider = _wallHits[i].collider;
            if (collider == null || collider.transform.IsChildOf(_owner.transform)) continue;
            int layer = 1 << collider.gameObject.layer;
            bool wall = (_obstacles.value & layer) != 0 && !collider.isTrigger;
            bool target = (_owner.targetLayer.value & layer) != 0 && collider.GetComponentInParent<CharacterHealth>() != null;
            if (!wall && !target) continue;
            if (_wallHits[i].distance < distance || (Mathf.Approximately(_wallHits[i].distance, distance) && wall && !nearestIsWall))
            { nearest = i; distance = _wallHits[i].distance; nearestIsWall = wall; }
        }
        float move = nearest >= 0 ? Mathf.Max(0, Mathf.Min(step, distance - _wallSkin)) : step;
        transform.position += (Vector3)(Direction * move);
        _travelled += move;
        if (nearest >= 0)
        {
            if (!nearestIsWall)
            {
                var health = _wallHits[nearest].collider.GetComponentInParent<CharacterHealth>();
                if (health != null && health.isActiveAndEnabled && !health.isDead && !health.IsUntargetable && _processed.Add(health))
                {
                    _targets.Clear();
                    _targets.Add(health);
                    _owner.ExecuteHitOnTargets("Skill3", _targets);
                }
            }
            Finish();
        }
        else if (_travelled >= _maxTravelDistance) Finish();
    }

    private Rect CurrentFrameBounds()
    {
        int frames = Mathf.Clamp(_travelAnimationFrames, 1, _frameBounds.Length);
        int index = Mathf.FloorToInt(_age * _framesPerSecond) % frames;
        return _frameBounds[index];
    }

    private void UpdateGeometry(bool unused)
    {
        int frames = Mathf.Clamp(_travelAnimationFrames, 1, _frameBounds.Length);
        int index = Mathf.FloorToInt(_age * _framesPerSecond) % frames;
        _visualAnimator.speed = 0;
        _visualAnimator.Play("Beam", 0, index / (float)_frameBounds.Length);
        _visualAnimator.Update(0);
        VisibleLength = _maxLength;
        _visual.transform.localRotation = Quaternion.identity;
        _visual.transform.localScale = new Vector3(_width / Mathf.Max(.01f, _nativeVisibleSize.x),
            _maxLength / Mathf.Max(.01f, _nativeVisibleSize.y), 1);
        _visual.enabled = true;
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        _width = Mathf.Max(.05f, _width);
        _maxLength = Mathf.Max(.05f, _maxLength);
        _lifetime = Mathf.Max(.01f, _lifetime);
        _damageDuration = Mathf.Clamp(_damageDuration, .01f, _lifetime);
        _framesPerSecond = Mathf.Max(1, _framesPerSecond);
    }
}
