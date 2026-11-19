using UnityEngine;

[CreateAssetMenu(menuName="Game/Effects/Dash Effect")]
public sealed class DashEffectAsset : ScriptableObject
{
    public Sprite[] frames;
    public Material material;
    public Color tint=Color.white;
    [Min(.01f)]public float emissionSpacing=.28f;
    [Min(.01f)]public float emissionInterval=.055f;
    public Vector2 offset=new Vector2(0,.04f);
    [Min(.01f)]public float scale=1;
    private static DashEffectAsset cached;
    public static DashEffectAsset Default { get { if(cached==null)cached=Resources.Load<DashEffectAsset>("DefaultDashEffect");return cached; } }
}
