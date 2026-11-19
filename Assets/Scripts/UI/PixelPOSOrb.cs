using UnityEngine;
using UnityEngine.UI;

/// <summary>21 by 21 pixel glass orb, with a bottom-up POS liquid level.</summary>
public class PixelPOSOrb : MaskableGraphic
{
    [Range(0f, 1f)] public float level;
    public Color liquidColor = new Color(0.12f, 0.6f, 1f);
    public void SetLevel(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(level, value)) return;
        level = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float step = Mathf.Min(rect.width, rect.height) / 21f;
        Vector2 origin = rect.center - Vector2.one * step * 10.5f;
        int liquidRows = Mathf.CeilToInt(level * 17f);
        for (int y = 0; y < 21; y++)
        for (int x = 0; x < 21; x++)
        {
            float distance = new Vector2(x - 10, y - 10).magnitude;
            if (distance > 10f) continue;
            Color tint;
            if (distance > 9f) tint = new Color(.09f, .14f, .23f);
            else if (distance > 8f) tint = y > 10 ? new Color(.45f, .75f, .84f) : new Color(.18f, .32f, .45f);
            else
            {
                bool filled = y - 2 < liquidRows;
                tint = filled ? liquidColor * (y < 6 ? .55f : y < 11 ? .78f : 1f) : new Color(.09f, .2f, .28f);
                if (filled && y == liquidRows + 1) tint = Color.Lerp(liquidColor, Color.white, .45f);
            }
            // Reflections stay on the glass even when empty.
            if ((x == 5 && y >= 12 && y <= 15) || (y == 16 && x >= 6 && x <= 8)) tint = new Color(.83f, .97f, 1f);
            if (x == 16 && y >= 6 && y <= 8) tint = new Color(.36f, .65f, .75f);
            tint.a = 1f;
            int first = vh.currentVertCount;
            Vector2 p = origin + new Vector2(x, y) * step;
            vh.AddVert(p, tint, Vector2.zero);
            vh.AddVert(p + new Vector2(step, 0), tint, Vector2.zero);
            vh.AddVert(p + Vector2.one * step, tint, Vector2.zero);
            vh.AddVert(p + new Vector2(0, step), tint, Vector2.zero);
            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first + 2, first + 3, first);
        }
    }
}
