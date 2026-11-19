using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(WaterPresence), typeof(Rigidbody2D))]
public sealed class PlayerWaterRipples : MonoBehaviour
{
    [Header("Water ripples")]
    [Min(0.05f)] public float stepInterval = 0.28f;
    [Min(0.1f)] public float lifetime = 0.65f;
    [Min(0.01f)] public float startRadius = 0.09f;
    [Min(0.02f)] public float endRadius = 0.45f;
    [Range(0.1f, 1f)] public float verticalScale = 0.45f;
    [Min(0.005f)] public float lineWidth = 0.025f;
    public Color rippleColor = new Color(0.75f, 0.95f, 1f, 0.7f);
    public Vector2 footOffset = new Vector2(0f, 0.04f);
    [Min(1)] public int pixelsPerUnit = 32;
    [Range(0f, 0.8f), Tooltip("Fraction of the feet collider height submerged; ignores transparent sprite padding.")]
    public float submergedFraction = 0.22f;
    public Material submergedMaterial;
    public Material rippleMaterial;

    [Header("Lava wading")]
    [Range(0f,.8f), Tooltip("Độ chìm khi lội dung nham, tính theo chiều cao collider chân.")]
    public float lavaSubmergedFraction = .42f;
    public Color lavaRippleColor = new Color(1f,.48f,.08f,.7f);
    public bool IsInLava { get; private set; }
    public float Immersion => immersion;

    private const int PoolSize = 8;
    private const int Segments = 32;
    private readonly LineRenderer[] rings = new LineRenderer[PoolSize];
    private readonly float[] ages = new float[PoolSize];
    private readonly Vector3[] centers = new Vector3[PoolSize];
    private WaterPresence water;
    private SpriteRenderer characterSprite;
    private Material originalMaterial;
    private MaterialPropertyBlock properties;
    private float immersion;
    private Rigidbody2D body;
    private Collider2D feet;
    private Material material;
    private bool ownsMaterial;
    private int nextRing;
    private float cooldown;
    private bool wasInWater;

    private void Awake()
    {
        WaterEffectDefaults.Configure(this);
        water = GetComponent<WaterPresence>();
        characterSprite = GetComponent<SpriteRenderer>();
        if (characterSprite != null) originalMaterial = characterSprite.sharedMaterial;
        properties = new MaterialPropertyBlock();
        body = GetComponent<Rigidbody2D>();
        feet = GetComponent<Collider2D>();
        material = rippleMaterial;
        if (material == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) { enabled = false; return; }
            material = new Material(shader);
            ownsMaterial = true;
        }
        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject("WaterRipple_" + i);
            go.transform.SetParent(transform, false);
            var ring = go.AddComponent<LineRenderer>();
            ring.sharedMaterial = material;
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = Segments * 2;
            ring.sortingLayerName = "Water";
            ring.sortingOrder = 5;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.enabled = false;
            rings[i] = ring;
            ages[i] = lifetime;
        }
    }

    private void LateUpdate()
    {
        // Sprite bounds include large transparent animation padding. Anchor to the
        // physical feet instead, and reject body-only overlap at the shoreline.
        Vector3 foot = feet != null
            ? new Vector3(feet.bounds.center.x, feet.bounds.min.y, transform.position.z)
            : transform.position;
        foot += (Vector3)footOffset;
        bool inWater = water.ContainsPoint(foot);
        bool inLava = inWater && water.IsLavaAtPoint(foot);
        IsInLava = inLava;
        immersion = Mathf.MoveTowards(immersion, inWater ? 1f : 0f, Time.deltaTime * 5f);
        float depth = (feet != null ? feet.bounds.size.y : 0.5f) * (inLava ? lavaSubmergedFraction : submergedFraction);
        float waterLine = foot.y + depth * immersion;
        Vector3 surfaceFoot = new Vector3(foot.x, waterLine - 0.5f / Mathf.Max(1, pixelsPerUnit), foot.z);
        if (characterSprite != null && submergedMaterial != null)
        {
            characterSprite.sharedMaterial = immersion > 0f ? submergedMaterial : originalMaterial;
            characterSprite.GetPropertyBlock(properties);
            properties.SetFloat("_WaterLine", waterLine);
            properties.SetFloat("_Immersion", immersion);
            characterSprite.SetPropertyBlock(properties);
        }
        cooldown -= Time.deltaTime;
        if (inWater && (!wasInWater || (body.linearVelocity.sqrMagnitude > 0.04f && cooldown <= 0f)))
        {
            centers[nextRing] = surfaceFoot;
            ages[nextRing] = 0f;
            rings[nextRing].enabled = true;
            nextRing = (nextRing + 1) % PoolSize;
            cooldown = Mathf.Max(0.05f, stepInterval);
        }
        wasInWater = inWater;

        for (int i = 0; i < PoolSize; i++)
        {
            if (!inWater) { rings[i].enabled = false; continue; }
            if (!rings[i].enabled) continue;
            centers[i] = surfaceFoot;
            ages[i] += Time.deltaTime;
            float t = Mathf.Clamp01(PixelAnimationSettings.Sample(ages[i]) / Mathf.Max(0.1f, lifetime));
            if (t >= 1f) { rings[i].enabled = false; continue; }
            float pixel = 1f / Mathf.Max(1, pixelsPerUnit);
            float radius = Mathf.Round(Mathf.Lerp(startRadius, endRadius, Mathf.Floor(t * 8f) / 8f) / pixel) * pixel;
            rings[i].sortingLayerName = inLava ? "Lava" : "Water";
            Color color = inLava ? lavaRippleColor : rippleColor;
            color.a *= (1f - t) * (1f - t);
            rings[i].startColor = rings[i].endColor = color;
            rings[i].startWidth = rings[i].endWidth = pixel;
            for (int j = 0; j < Segments; j++)
            {
                float angle = j * Mathf.PI * 2f / Segments;
                float nextAngle = ((j + 1) % Segments) * Mathf.PI * 2f / Segments;
                var point = new Vector3(Mathf.Round(Mathf.Cos(angle) * radius / pixel) * pixel,
                    Mathf.Round(Mathf.Sin(angle) * radius * verticalScale / pixel) * pixel, 0f);
                var next = new Vector3(Mathf.Round(Mathf.Cos(nextAngle) * radius / pixel) * pixel,
                    Mathf.Round(Mathf.Sin(nextAngle) * radius * verticalScale / pixel) * pixel, 0f);
                rings[i].SetPosition(j * 2, centers[i] + point);
                rings[i].SetPosition(j * 2 + 1, centers[i] + new Vector3(next.x, point.y, 0f));
            }
        }
    }

    private void OnDisable()
    {
        wasInWater = false;
        cooldown = 0f;
        immersion = 0f;
        IsInLava = false;
        if (characterSprite != null && originalMaterial != null) characterSprite.sharedMaterial = originalMaterial;
        if (characterSprite != null && properties != null)
        {
            characterSprite.GetPropertyBlock(properties);
            properties.SetFloat("_Immersion", 0f);
            characterSprite.SetPropertyBlock(properties);
        }
        foreach (var ring in rings) if (ring != null) ring.enabled = false;
    }

    private void OnDestroy()
    {
        if (ownsMaterial && material != null) Destroy(material);
    }
}
