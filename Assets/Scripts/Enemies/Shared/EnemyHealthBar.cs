using UnityEngine;
using UnityEngine.UI;

/// <summary>A world-space health display, independent of the enemy's disabled death scripts.</summary>
[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private float verticalOffset = 0.8f;
    [SerializeField] private float fillDuration = 0.12f;
    [SerializeField] private float trailDelay = 0.35f;
    [SerializeField] private float trailDuration = 0.55f;
    [SerializeField] private float deathHold = 0.8f;
    [SerializeField, Range(1f, 30f)] private float effectFPS = 8f;
    [SerializeField, Min(.01f)] private float deathFadeDuration = .4f;
    private PixelEffectClock effectClock;

    private CharacterStats stats;
    private CharacterHealth health;
    private RectTransform fill;
    private RectTransform trail;
    private Image fillImage;
    private CanvasGroup group;
    private float displayed = 1f;
    private float delayed = 1f;
    private float previous = 1f;
    private float delay;
    private float flash;
    private float deathTime;

    private void Awake()
    {
        PixelEnemyBarStyle.World(this);
        effectFPS=12;
        stats = GetComponentInParent<CharacterStats>();
        health = GetComponentInParent<CharacterHealth>();
        fill = transform.Find("Track/Fill") as RectTransform;
        trail = transform.Find("Track/DamageTrail") as RectTransform;
        fillImage = fill != null ? fill.GetComponent<Image>() : null;
        group = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        displayed = delayed = previous = Ratio();
        SetWidth(fill, displayed);
        SetWidth(trail, delayed);
    }

    private void OnEnable()
    {
        deathTime = delay = flash = 0f;
        effectClock = default;
        // Start/LateUpdate runs after CharacterStats has initialized its HP.
        previous = displayed = delayed = -1f;
        if (group != null) group.alpha = 1f;
    }

    private float Ratio()
    {
        return stats != null && stats.maxHP > 0f
            ? Mathf.Clamp01(stats.currentHP / stats.maxHP) : 0f;
    }

    private void LateUpdate()
    {
        var classification = stats != null ? stats.GetComponent<EnemyClassification>() : null;
        if (classification != null && classification.UsesDragonBar)
        {
            if (group != null) group.alpha = 0f;
            return;
        }
        if (stats == null || fill == null || trail == null) return;
        float dt = Time.deltaTime;
        bool sample = effectClock.Tick(dt, effectFPS) || previous < 0f;
        float target = Ratio();
        if (previous < 0f) displayed = delayed = previous = target;
        if (target < previous)
        {
            delay = trailDelay;
            flash = 0.16f;
        }
        else if (target > previous)
        {
            delayed = target;
            delay = 0f;
        }
        previous = target;
        displayed = Mathf.MoveTowards(displayed, target, dt / Mathf.Max(0.01f, fillDuration));
        delay -= dt;
        if (delay <= 0f)
            delayed = Mathf.MoveTowards(delayed, target, dt / Mathf.Max(0.01f, trailDuration));
        delayed = Mathf.Max(delayed, displayed);
        if (sample || (displayed == target && delayed == target))
        {
            SetWidth(fill, displayed);
            SetWidth(trail, delayed);
        }
        flash = Mathf.Max(0f, flash - dt);
        if (sample && fillImage != null)
            fillImage.color = Color.Lerp(PixelEnemyBarStyle.Blood, new Color(.95f,.28f,.24f), flash / 0.16f);

        Vector3 anchor = stats.transform.position;
        // Character sheets contain transparent padding; use a stable head offset.
        transform.position = anchor + Vector3.up * verticalOffset;
        transform.rotation = Quaternion.identity;
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(
            Mathf.Abs(parentScale.x) > 0.0001f ? 0.01f / parentScale.x : 0.01f,
            Mathf.Abs(parentScale.y) > 0.0001f ? 0.01f / parentScale.y : 0.01f, 0.01f);

        if (health != null && health.isDead)
        {
            deathTime += dt;
            float alpha = 1f - Mathf.Clamp01((deathTime - deathHold) / Mathf.Max(.01f, deathFadeDuration));
            if (group != null && (sample || alpha == 0f)) group.alpha = alpha;
        }
        else
        {
            deathTime = 0f;
            if (group != null) group.alpha = 1f;
        }
    }

    private static void SetWidth(RectTransform rect, float ratio)
    {
        if (rect != null) rect.anchorMax = new Vector2(Mathf.Round(ratio*62)/62, 1f);
    }
}
