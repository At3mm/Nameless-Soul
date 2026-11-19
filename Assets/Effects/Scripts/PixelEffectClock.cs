using UnityEngine;

/// <summary>Samples visuals without slowing gameplay or losing elapsed time.</summary>
public struct PixelEffectClock
{
    private float elapsed;
    public bool Tick(float deltaTime, float framesPerSecond)
    {
        elapsed += Mathf.Max(0f, deltaTime);
        float interval = 1f / PixelAnimationSettings.FPS;
        if (elapsed < interval) return false;
        elapsed %= interval;
        return true;
    }
}
