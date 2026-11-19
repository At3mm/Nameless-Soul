using UnityEngine;

[CreateAssetMenu(menuName = "Game/Default Status Effects")]
public sealed class StatusEffectDefaults : ScriptableObject
{
    public Sprite[] burningFrames, poisonFrames, stressFrames;
    public Material particleMaterial, tintMaterial;
    public GameObject healingPrefab;
    [Min(0.05f)] public float healingDuration = 0.75f;
    public Vector3 healingOffset;
    [HideInInspector] public float framesPerSecond = 12f;
    [Range(0f, 1f)] public float particleOpacity = 0.75f;
    [Range(0.3f, 1f)] public float particleScale = 0.7f;
    [Range(0f, 0.5f)] public float tintStrength = 0.23f;
    [Min(0.5f)] public float tintPulseDuration = 1.8f;
    [Min(0.5f)] public float healingTintDuration = 1.6f;
    public Color burningTint = new Color(1f, 0.28f, 0.08f);
    public Color poisonTint = new Color(0.55f, 0.95f, 0.25f);
    public Color stressTint = new Color(0.5f, 0.6f, 1f);
    public Color healingTint = new Color(0.5f, 1f, 0.65f);

    private static StatusEffectDefaults cached;
    public static StatusEffectDefaults Current
    {
        get { if (cached == null) cached = Resources.Load<StatusEffectDefaults>("DefaultStatusEffects"); return cached; }
    }
    public static void EnsureVisuals(GameObject actor, bool healing = false)
    {
        if (!Application.isPlaying || Current == null) return;
        var status = actor.GetComponent<CharacterDebuffVFX>();
        if (status == null) status = actor.AddComponent<CharacterDebuffVFX>();
        status.RefreshStatusSource();
        if (healing && actor.GetComponent<CharacterHealingVFX>() == null)
            actor.AddComponent<CharacterHealingVFX>();
    }
    public static void Configure(CharacterDebuffVFX v)
    {
        var d = Current;
        if (d == null) return;
        v.burningFrames = d.burningFrames; v.poisonFrames = d.poisonFrames; v.stressFrames = d.stressFrames;
        v.effectMaterial = d.particleMaterial; v.tintMaterial = d.tintMaterial;
        v.framesPerSecond = PixelAnimationSettings.FPS; v.opacity = d.particleOpacity; v.particleScale = d.particleScale;
        v.tintStrength = d.tintStrength; v.tintPulseDuration = d.tintPulseDuration;
        v.healingTintDuration = d.healingTintDuration;
        v.burningTint = d.burningTint; v.poisonTint = d.poisonTint; v.stressTint = d.stressTint; v.healingTint = d.healingTint;
    }
    public static void Configure(CharacterHealingVFX v)
    {
        var d = Current;
        if (d == null) return;
        v.effectPrefab = d.healingPrefab; v.duration = d.healingDuration; v.localOffset = d.healingOffset;
    }
}
