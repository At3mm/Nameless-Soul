using UnityEngine;

[CreateAssetMenu(menuName="Game/Pixel Animation Settings")]
public sealed class PixelAnimationSettings : ScriptableObject
{
    [Range(1,30)] public int framesPerSecond=12;
    private static PixelAnimationSettings cached;
    public static int FPS
    {
        get { if(cached==null)cached=Resources.Load<PixelAnimationSettings>("DefaultPixelAnimation");return cached!=null?Mathf.Max(1,cached.framesPerSecond):12; }
    }
    public static float Sample(float time)=>Mathf.Floor(time*FPS)/FPS;
    public static float Progress(float elapsed,float duration)
    {
        if(duration<=0||elapsed>=duration)return 1;
        return Mathf.Clamp01(Sample(Mathf.Max(0,elapsed))/duration);
    }
}
