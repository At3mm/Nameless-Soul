using UnityEngine;

// Presentation only: never changes combat, movement, damage or the actor Animator.
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(CharacterHealth))]
public sealed class CharacterDebuffVFX : MonoBehaviour
{
    public Sprite[] burningFrames;
    public Sprite[] poisonFrames;
    public Sprite[] stressFrames;
    public Material effectMaterial;
    public Material tintMaterial;
    [HideInInspector] public float framesPerSecond = 12f;
    [Range(0f, 1f)] public float opacity = 0.75f;
    [Range(0.3f, 1f)] public float particleScale = 0.7f;
    [Range(0f, 0.5f)] public float tintStrength = 0.23f;
    [Min(0.5f), Tooltip("Seconds for one slow fade in/out of a debuff color.")]
    public float tintPulseDuration = 1.8f;
    public Color burningTint = new Color(1f, 0.28f, 0.08f);
    public Color poisonTint = new Color(0.55f, 0.95f, 0.25f);
    public Color stressTint = new Color(0.5f, 0.6f, 1f);
    [Header("Healing color")]
    public Color healingTint = new Color(0.5f, 1f, 0.65f);
    [Min(0.5f)] public float healingTintDuration = 1.6f;
    private float healedAt = float.NegativeInfinity;
    private CharacterDebuffs debuffs;
    private bool IsBurning => debuffs != null && debuffs.IsBurning;
    private bool IsPoisoned => debuffs != null && debuffs.IsPoisoned;
    private bool IsStressed => debuffs != null && debuffs.IsStressed;
    public void RefreshStatusSource() { debuffs = GetComponent<CharacterDebuffs>(); }
    private CharacterHealth health;
    private SpriteRenderer actor;
    private Collider2D feet;
    private CharacterInvulnerability invulnerability;
    private SpriteRenderer tint;
    private MaterialPropertyBlock tintProperties;
    private readonly SpriteRenderer[] channels = new SpriteRenderer[3];
    public int VisibleEffects
    {
        get { int count = 0; foreach (var r in channels) if (r != null && r.enabled) count++; return count; }
    }
    private void Awake()
    {
        StatusEffectDefaults.Configure(this);
        debuffs = GetComponent<CharacterDebuffs>();
        health = GetComponent<CharacterHealth>();
        actor = GetComponent<SpriteRenderer>();
        feet = GetComponent<Collider2D>();
        invulnerability = GetComponent<CharacterInvulnerability>();
        tintProperties = new MaterialPropertyBlock();
        if (actor != null && tintMaterial != null)
        {
            var child = new GameObject("DebuffColorOverlay");
            child.transform.SetParent(transform, false);
            tint = child.AddComponent<SpriteRenderer>();
            tint.sharedMaterial = tintMaterial;
            tint.enabled = false;
        }
        for (int i = 0; i < channels.Length; i++)
        {
            var child = new GameObject(new[] { "BurningVFX", "PoisonVFX", "StressVFX" }[i]);
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            if (effectMaterial != null) renderer.sharedMaterial = effectMaterial;
            renderer.enabled = false;
            channels[i] = renderer;
        }
    }
    private void LateUpdate()
    {
        bool alive = health == null || !health.isDead;
        Draw(0, alive && IsBurning, burningFrames, 2);
        Draw(1, alive && IsPoisoned, poisonFrames, 2);
        Draw(2, alive && IsStressed, stressFrames, 2);
        DrawTint(alive);
    }
    private void OnEnable()
    {
        if (health == null) health = GetComponent<CharacterHealth>();
        if (health != null) health.Healed += OnHealed;
    }
    private void OnHealed(float amount)
    {
        if (amount > 0f) healedAt = Time.time;
    }
    private void Draw(int index, bool active, Sprite[] frames, int sortingOffset)
    {
        var renderer = channels[index];
        if (renderer == null) return;
        renderer.enabled = active && frames != null && frames.Length > 0;
        if (!renderer.enabled) return;
        int frame = Mathf.FloorToInt(Time.time * PixelAnimationSettings.FPS) % frames.Length;
        renderer.sprite = frames[frame];
        renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
        // Small floating clusters beside the torso, never a floor effect.
        renderer.transform.position = feet != null && feet.enabled
            ? new Vector3(feet.bounds.center.x, feet.bounds.center.y + index * 0.12f, transform.position.z)
            : transform.position;
        renderer.transform.localScale = new Vector3(transform.lossyScale.x < 0f ? -particleScale : particleScale, particleScale, 1f);
        if (actor != null)
        {
            renderer.sortingLayerID = actor.sortingLayerID;
            renderer.sortingOrder = actor.sortingOrder + sortingOffset;
        }
    }
    private void DrawTint(bool alive)
    {
        if (tint == null || actor == null) return;
        Color color = EvaluateTint(Time.time);
        tint.enabled = alive && actor.enabled && color.a > 0.001f &&
            (invulnerability == null || !invulnerability.isInvulnerable);
        if (!tint.enabled) return;
        tint.sprite = actor.sprite;
        tint.flipX = actor.flipX;
        tint.flipY = actor.flipY;
        tint.color = color;
        tint.sortingLayerID = actor.sortingLayerID;
        tint.sortingOrder = actor.sortingOrder + 1;
        // Copy water clipping parameters while leaving the actor's property block untouched.
        actor.GetPropertyBlock(tintProperties);
        tint.SetPropertyBlock(tintProperties);
    }
    private Color EvaluateTint(float time)
    {
        float healingDuration = Mathf.Max(0.5f, healingTintDuration);
        float elapsed = time - healedAt;
        Color color;
        float phase;
        // Healing briefly takes priority; debuff particles remain visible.
        if (elapsed >= 0f && elapsed < healingDuration)
        {
            color = healingTint;
            phase = (Mathf.Floor(elapsed * PixelAnimationSettings.FPS) / PixelAnimationSettings.FPS) / healingDuration;
        }
        else
        {
            int count = (IsBurning ? 1 : 0) + (IsPoisoned ? 1 : 0) + (IsStressed ? 1 : 0);
            if (count == 0) return Color.clear;
            float sampledTime = Mathf.Floor(time * PixelAnimationSettings.FPS) / PixelAnimationSettings.FPS;
            float cycle = sampledTime / Mathf.Max(0.5f, tintPulseDuration);
            int chosen = Mathf.FloorToInt(cycle) % count;
            color = stressTint;
            if (IsBurning && chosen-- == 0) color = burningTint;
            else if (IsPoisoned && chosen-- == 0) color = poisonTint;
            phase = Mathf.Repeat(cycle, 1f);
        }
        // A slow full fade, reaching zero between pulses/color changes.
        float pulse = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
        pulse = Mathf.Round(pulse * 4f) / 4f;
        color.a = tintStrength * pulse * (actor != null ? actor.color.a : 1f);
        return color;
    }
    private void OnDisable()
    {
        if (health != null) health.Healed -= OnHealed;
        healedAt = float.NegativeInfinity;
        foreach (var renderer in channels) if (renderer != null) renderer.enabled = false;
        if (tint != null) tint.enabled = false;
    }
}
