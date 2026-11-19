using UnityEngine;

/// <summary>Stepped pixel fade inside an open doorway; wall occlusion is handled by sorting.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(500)]
public sealed class PlayerWallFade : MonoBehaviour
{
    public SpriteRenderer visual;
    public MapPropAnimation[] doors;
    [Tooltip("Door-local dark opening, measured from the bottom of the door.")]
    public Rect darkOpening = new Rect(-0.65f, 0.2f, 1.3f, 2.8f);
    [Tooltip("Keep the body hidden until its bottom clears the upper wall edge (door-local units).")]
    [Min(0f)] public float bodyClearance = 0.45f;
    [Range(0f, 1f)] public float hiddenAlpha = 0f;
    [Min(0.01f)] public float fadeDuration = 0.5f;
    private float originalAlpha = 1f;
    private float currentAlpha = 1f;
    private float tick;

    private void OnEnable()
    {
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        if (visual != null) originalAlpha = currentAlpha = visual.color.a;
        tick = 0f;
    }

    public bool IsInsideDoorway()
    {
        if (doors == null) return false;
        foreach (var door in doors)
        {
            if (door == null || !door.isActiveAndEnabled || !door.IsOpen) continue;
            Vector2 point = door.transform.InverseTransformPoint(transform.position);
            if (point.x >= darkOpening.xMin && point.x <= darkOpening.xMax &&
                point.y >= darkOpening.yMin && point.y - bodyClearance <= darkOpening.yMax) return true;
        }
        return false;
    }

    private void LateUpdate()
    {
        if (visual == null) return;
        float frameTime = 1f / PixelAnimationSettings.FPS;
        tick += Time.deltaTime;
        if (tick < frameTime) return;
        int frames = Mathf.FloorToInt(tick / frameTime);
        tick -= frames * frameTime;
        currentAlpha = Mathf.MoveTowards(currentAlpha, IsInsideDoorway() ? originalAlpha * hiddenAlpha : originalAlpha,
            originalAlpha * frames * frameTime / Mathf.Max(0.01f, fadeDuration));
        var color = visual.color;
        color.a = currentAlpha;
        visual.color = color;
    }

    private void OnDisable()
    {
        if (visual == null) return;
        var color = visual.color;
        color.a = originalAlpha;
        visual.color = color;
    }
}
