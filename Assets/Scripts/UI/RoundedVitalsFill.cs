using UnityEngine;
using UnityEngine.UI;

/// <summary>Image-compatible fill whose moving end stays rounded at every value.</summary>
[AddComponentMenu("UI/Rounded Vitals Fill")]
public class RoundedVitalsFill : Image
{
    [Min(0f)] public float cornerRadius = 3f;
    [Range(0f, 1f)] public float topHighlight = 0.28f;
    [Range(0f, 1f)] public float bottomBrightness = 0.52f;
    [Min(1f)] public float pixelSize = 3f;

    protected override void OnPopulateMesh(VertexHelper helper)
    {
        helper.Clear();
        var rect = GetPixelAdjustedRect();
        float width = rect.width * Mathf.Clamp01(fillAmount);
        if (width <= 0f || rect.height <= 0f) return;
        float radius = Mathf.Min(cornerRadius, rect.height * 0.5f, width * 0.5f);
        // Hard-edged horizontal pixel bands, including stepped rounded caps.
        width = Mathf.Min(rect.width, Mathf.Ceil(width / pixelSize) * pixelSize);
        for (float y = 0; y < rect.height; y += pixelSize)
        {
            float top = Mathf.Min(y + pixelSize, rect.height);
            float inset = Mathf.Ceil(Inset((y + top) * .5f, rect.height, radius) / pixelSize) * pixelSize;
            inset = Mathf.Min(inset, width * .5f);
            Color tint = Shade((y + top) * .5f / rect.height);
            int start = helper.currentVertCount;
            helper.AddVert(new Vector3(rect.xMin + inset, rect.yMin + y), tint, Vector2.zero);
            helper.AddVert(new Vector3(rect.xMin + width - inset, rect.yMin + y), tint, Vector2.zero);
            helper.AddVert(new Vector3(rect.xMin + width - inset, rect.yMin + top), tint, Vector2.zero);
            helper.AddVert(new Vector3(rect.xMin + inset, rect.yMin + top), tint, Vector2.zero);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start + 2, start + 3, start);
        }
    }


    private static float Inset(float y, float height, float radius)
    {
        if (radius <= 0f) return 0f;
        float distance = y < radius ? radius - y : y > height - radius ? y - (height - radius) : 0f;
        return radius - Mathf.Sqrt(Mathf.Max(0f, radius * radius - distance * distance));
    }

    private Color Shade(float height)
    {
        Color result;
        if (height >= 0.8f)
            result = Color.Lerp(color, Color.white, topHighlight * (height - 0.8f) / 0.2f);
        else
            result = color * Mathf.Lerp(bottomBrightness, 1f, height / 0.8f);
        result.a = color.a;
        return result;
    }
}
