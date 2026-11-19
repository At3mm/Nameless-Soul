using UnityEngine;

[CreateAssetMenu(menuName="Game/UI/Currency Gain Notice")]
public sealed class CurrencyGainNoticeSettings : ScriptableObject
{
    public Sprite goldIcon,experienceIcon;
    public Color background=new Color(.055f,.065f,.045f,.94f);
    public Color goldColor=new Color(.94f,.78f,.44f);
    public Color experienceColor=new Color(.66f,.86f,.82f);
    [Min(.1f)] public float holdDuration=3;
    [Min(.01f)] public float enterDuration=.25f,exitDuration=.4f;
    [Tooltip("Right-edge margin and vertical offset, at a reference resolution of 1280x720.")]
    public Vector2 position=new Vector2(-28,25);
}
