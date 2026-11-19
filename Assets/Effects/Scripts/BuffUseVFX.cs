using UnityEngine;

/// <summary>A brief pixel accent at either side of the body after consuming a buff.</summary>
[DisallowMultipleComponent]
public sealed class BuffUseVFX : MonoBehaviour
{
    const int Size = 32, FrameCount = 4;
    static float Fps => PixelAnimationSettings.FPS;
    static Sprite[][] frames;
    static Material material;
    SpriteRenderer visual;
    CharacterHealth health;
    Collider2D body;
    SpriteRenderer character;
    float age;
    int kind;

    public static void Play(GameObject actor, CharacterBuffs.Kind buff)
    {
        if (!Application.isPlaying || actor == null || !actor.activeInHierarchy || (int)buff < 0 || (int)buff >= 5) return;
        var health = actor.GetComponent<CharacterHealth>();
        if (health != null && health.isDead) return;
        var effect = actor.GetComponent<BuffUseVFX>();
        if (effect == null) effect = actor.AddComponent<BuffUseVFX>();
        effect.enabled = true;
        effect.Begin(buff);
    }

    void Begin(CharacterBuffs.Kind buff)
    {
        EnsureFrames();
        health = GetComponent<CharacterHealth>();
        body = GetComponent<Collider2D>();
        character = GetComponent<SpriteRenderer>();
        if (character == null) character = GetComponentInChildren<SpriteRenderer>();
        if (visual == null)
        {
            var go = new GameObject("Buff use pixels");
            go.transform.SetParent(transform, false);
            visual = go.AddComponent<SpriteRenderer>();
            if (material != null) visual.sharedMaterial = material;
        }
        kind = (int)buff;
        age = 0f;
        visual.sprite = frames[kind][0];
        visual.gameObject.SetActive(true);
        PositionVisual();
    }

    void LateUpdate()
    {
        if (visual == null || !visual.gameObject.activeSelf) return;
        age += Time.deltaTime;
        int frame = Mathf.FloorToInt(age * Fps);
        if (frame >= FrameCount || (health != null && health.isDead)) { visual.gameObject.SetActive(false); return; }
        visual.sprite = frames[kind][frame];
        PositionVisual();
    }

    void PositionVisual()
    {
        Vector3 center = body != null && body.enabled ? body.bounds.center : transform.position + Vector3.up * .4f;
        center.x = Mathf.Round(center.x * 32f) / 32f;
        center.y = Mathf.Round(center.y * 32f) / 32f;
        visual.transform.position = center;
        if (character != null)
        {
            visual.sortingLayerID = character.sortingLayerID;
            visual.sortingOrder = character.sortingOrder + 3;
        }
    }

    void OnDisable() { if (visual != null) visual.gameObject.SetActive(false); }

    static void EnsureFrames()
    {
        if (frames != null && frames[0][0] != null) return;
        frames = new Sprite[5][];
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader != null) material = new Material(shader) { name = "Buff pixels unlit", hideFlags = HideFlags.HideAndDontSave };
        Color32[] colors = { new Color32(255, 139, 92, 255), new Color32(113, 225, 244, 255),
            new Color32(203, 166, 255, 255), new Color32(160, 229, 122, 255), new Color32(255, 220, 125, 255) };
        // Small distinct glyphs; central body pixels remain completely transparent.
        string[][] glyphs = {
            new[]{"00100", "01110", "10101", "00100", "00100"},
            new[]{"10010", "01001", "00100", "01001", "10010"},
            new[]{"00100", "01010", "10001", "01010", "00100"},
            new[]{"00100", "00100", "11111", "00100", "00100"},
            new[]{"11111", "10001", "10001", "01010", "00100"}
        };
        for (int k = 0; k < 5; k++)
        {
            frames[k] = new Sprite[FrameCount];
            for (int f = 0; f < FrameCount; f++)
            {
                var pixels = new Color32[Size * Size];
                var color = colors[k];
                color.a = (byte)(f == 3 ? 130 : 220);
                for (int side = 0; side < 2; side++)
                {
                    int originX = side == 0 ? 2 : 25;
                    int originY = 11 + f * 2;
                    for (int y = 0; y < 5; y++)
                        for (int x = 0; x < 5; x++)
                            if (glyphs[k][y][x] == '1') pixels[(originY + 4 - y) * Size + originX + x] = color;
                    pixels[(originY + 7) * Size + originX + (f % 4)] = new Color32(255, 244, 214, color.a);
                }
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
                { name = "BuffUse_" + k + "_" + f, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                frames[k][f] = Sprite.Create(texture, new Rect(0, 0, Size, Size), Vector2.one * .5f, 32f);
                frames[k][f].hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
}
