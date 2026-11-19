using UnityEngine;

[CreateAssetMenu(menuName = "Game/Default Water Effects")]
public sealed class WaterEffectDefaults : ScriptableObject
{
    public Material submergedMaterial;
    public Material rippleMaterial;
    [Min(0.05f)] public float stepInterval = 0.28f;
    [Min(0.1f)] public float lifetime = 0.65f;
    [Min(0.01f)] public float startRadius = 0.09f;
    [Min(0.02f)] public float endRadius = 0.45f;
    [Range(0.1f, 1f)] public float verticalScale = 0.45f;
    public Color rippleColor = new Color(0.75f, 0.95f, 1f, 0.7f);
    public Vector2 footOffset = new Vector2(0f, 0.04f);
    [Min(1)] public int pixelsPerUnit = 32;
    [Range(0f, 0.8f)] public float submergedFraction = 0.22f;
    private static WaterEffectDefaults cached;
    public static WaterEffectDefaults Current
    {
        get { if (cached == null) cached = Resources.Load<WaterEffectDefaults>("DefaultWaterEffects"); return cached; }
    }
    public static void EnsureVisuals(GameObject actor)
    {
        if (!Application.isPlaying || Current == null || actor.GetComponent<SpriteRenderer>() == null ||
            actor.GetComponent<Rigidbody2D>() == null) return;
        if (actor.GetComponent<PlayerWaterRipples>() == null) actor.AddComponent<PlayerWaterRipples>();
    }
    public static void Configure(PlayerWaterRipples v)
    {
        var d = Current;
        if (d == null) return;
        v.submergedMaterial = d.submergedMaterial; v.rippleMaterial = d.rippleMaterial;
        v.stepInterval = d.stepInterval; v.lifetime = d.lifetime;
        v.startRadius = d.startRadius; v.endRadius = d.endRadius; v.verticalScale = d.verticalScale;
        v.rippleColor = d.rippleColor; v.footOffset = d.footOffset;
        v.pixelsPerUnit = d.pixelsPerUnit; v.submergedFraction = d.submergedFraction;
    }
}
