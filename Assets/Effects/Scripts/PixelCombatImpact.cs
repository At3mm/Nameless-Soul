using System.Collections.Generic;
using UnityEngine;

/// <summary>Small, stepped combat accents. No colliders, Animator, hit stop or input changes.</summary>
public sealed class PixelCombatImpact : MonoBehaviour
{
    const int Size = 48, FrameCount = 6;
    Sprite[] customFrames;
    static Sprite[][] frames;
    static Material material;
    static readonly List<PixelCombatImpact> pool = new List<PixelCombatImpact>();
    SpriteRenderer visual;
    float age;
    int kind, shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPool() { pool.Clear(); }

    public static void Blood(CharacterHealth victim, Transform attacker)
    {
        Emit(victim, attacker, false);
    }

    public static void Parry(CharacterHealth defender, Transform attacker)
    {
        if (defender != null) Emit(defender, attacker, true);
    }

    static void Emit(CharacterHealth target, Transform attacker, bool spark)
    {
        if (target == null || !Application.isPlaying) return;
        EnsureFrames();
        var body = target.GetComponent<Collider2D>();
        Vector3 center = body != null ? body.bounds.center : target.transform.position + Vector3.up * .35f;
        Vector2 toward = attacker != null ? (Vector2)(attacker.position - center) : Vector2.left;
        if (toward.sqrMagnitude < .001f) toward = Vector2.left;
        toward.Normalize();
        Spawn(target.transform, center + (Vector3)(toward * (spark ? .3f : .08f)), spark ? toward : -toward, spark ? 1 : 0);
    }

    public static void Motion(Transform owner, Vector2 direction, bool dash, DashEffectAsset asset = null)
    {
        if (owner == null || !Application.isPlaying) return;
        EnsureFrames();
        // Character roots are aligned to the feet; do not use the torso collider center.
        if(dash && asset==null)asset=DashEffectAsset.Default;
        var offset=dash && asset!=null?(Vector3)asset.offset:Vector3.up*.04f;
        Spawn(owner, owner.position + offset, -direction.normalized, dash ? 3 : 2, dash?asset:null);
    }

    static void Spawn(Transform owner, Vector3 position, Vector2 direction, int effectKind, DashEffectAsset asset = null)
    {
        PixelCombatImpact effect = null;
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (pool[i] == null) { pool.RemoveAt(i); continue; }
            if (!pool[i].gameObject.activeSelf) effect = pool[i];
        }
        if (effect == null)
        {
            if (pool.Count >= 24) return;
            var go = new GameObject("Pixel Combat Impact");
            effect = go.AddComponent<PixelCombatImpact>();
            effect.visual = go.AddComponent<SpriteRenderer>();
            if (material != null) effect.visual.sharedMaterial = material;
            pool.Add(effect);
        }
        var reference = owner.GetComponent<SpriteRenderer>();
        if (reference == null) reference = owner.GetComponentInChildren<SpriteRenderer>();
        effect.visual.sortingLayerID = reference != null ? reference.sortingLayerID : SortingLayer.NameToID("Characters");
        effect.visual.sortingOrder = reference != null ? reference.sortingOrder + (effectKind >= 2 ? -1 : 3) : 3;
        effect.kind = effectKind;
        effect.customFrames=asset!=null && asset.frames!=null && asset.frames.Length>0?asset.frames:null;
        effect.visual.sharedMaterial=asset!=null && asset.material!=null?asset.material:material;
        effect.visual.color=asset!=null?asset.tint:Color.white;
        effect.transform.localScale=Vector3.one*(asset!=null?asset.scale:1);
        effect.age = 0f;
        effect.shown = 0;
        effect.visual.sprite = effect.customFrames!=null?effect.customFrames[0]:frames[effect.kind][0];
        float angle = Mathf.Round(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg / 45f) * 45f;
        position.x = Mathf.Round(position.x * 32f) / 32f;
        position.y = Mathf.Round(position.y * 32f) / 32f;
        effect.transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, angle));
        effect.gameObject.SetActive(true);
    }

    void Update()
    {
        age += Time.deltaTime;
        int frame = Mathf.FloorToInt(age * PixelAnimationSettings.FPS);
        int count=customFrames!=null?customFrames.Length:kind==3?3:FrameCount;
        if (frame >= count) { gameObject.SetActive(false); return; }
        if (frame != shown) { shown = frame; visual.sprite = customFrames!=null?customFrames[frame]:frames[kind][frame]; }
    }

    static void EnsureFrames()
    {
        if (frames != null && frames[0][0] != null) return;
        frames = new Sprite[4][];
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader != null) material = new Material(shader) { name = "Pixel impact unlit", hideFlags = HideFlags.HideAndDontSave };
        for (int k = 0; k < 4; k++)
        {
            frames[k] = new Sprite[FrameCount];
            for (int f = 0; f < FrameCount; f++)
            {
                var pixels = new Color32[Size * Size];
                if (k >= 2)
                {
                    // Sparse opaque clusters shrink instead of using a smooth particle fade.
                    for (int p = 0; p < 3; p++)
                    {
                        if (f >= 4 && p % 2 == 0) continue;
                        int x = 24 + p * (k == 3 ? 1 : 2) + f;
                        int y = 24 + (p % 3 - 1) * (2 + f / 2);
                        int length = Mathf.Max(1, 3 - f);
                        var color = k == 3 ? new Color32(201, 211, 200, (byte)(135 - f * 16)) : new Color32(183, 165, 131, (byte)(130 - f * 16));
                        for (int n = 0; n < length; n++) Dot(pixels, x + n, y, color);
                        if (f < 2) Dot(pixels, x, y + 1, color);
                    }
                }
                else
                {
                for (int p = 0; p < (k == 0 ? 9 : 11); p++)
                {
                    float angle = k == 0 ? (-65f + p * 16f) * Mathf.Deg2Rad : (p * 137.5f) * Mathf.Deg2Rad;
                    float distance = 2f + f * (1.7f + (p % 3) * .55f);
                    int x = 24 + Mathf.RoundToInt(Mathf.Cos(angle) * distance);
                    int y = 24 + Mathf.RoundToInt(Mathf.Sin(angle) * distance - f * f * (k == 0 ? .17f : .08f));
                    if (f >= 4 && p % 2 == 0) continue;
                    Color32 color = k == 0
                        ? (p % 3 == 0 ? new Color32(237, 68, 62, 255) : new Color32(151, 29, 45, 255))
                        : (f < 2 ? new Color32(255, 249, 199, 255) : new Color32(255, 166, 47, 255));
                    int length = k == 1 && f < 4 ? 3 : (f < 3 ? 2 : 1);
                    for (int n = 0; n < length; n++)
                    {
                        int px = x - Mathf.RoundToInt(Mathf.Cos(angle) * n);
                        int py = y - Mathf.RoundToInt(Mathf.Sin(angle) * n);
                        Dot(pixels, px, py, color);
                        if (k == 0 && f < 3) Dot(pixels, px, py + 1, color);
                    }
                }
                if (k == 1 && f < 2)
                    for (int n = -4 + f; n <= 4 - f; n++)
                    {
                        Dot(pixels, 24 + n, 24, new Color32(255, 253, 224, 255));
                        Dot(pixels, 24, 24 + n, new Color32(255, 228, 139, 255));
                    }
                }
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
                { name = (k == 0 ? "Blood" : "Parry") + f, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                frames[k][f] = Sprite.Create(texture, new Rect(0, 0, Size, Size), Vector2.one * .5f, 32f);
                frames[k][f].hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }

    static void Dot(Color32[] pixels, int x, int y, Color32 color)
    {
        if (x >= 0 && x < Size && y >= 0 && y < Size) pixels[y * Size + x] = color;
    }
}
