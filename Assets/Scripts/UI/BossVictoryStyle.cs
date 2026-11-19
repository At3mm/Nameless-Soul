using UnityEngine;

[CreateAssetMenu(menuName = "Game/Boss Victory Style")]
public sealed class BossVictoryStyle : ScriptableObject
{
    public Font font;
    public Sprite parchment;
    public string headline = "CƯỜNG ĐỊCH ĐÃ HẠ";
    public Color ink = new Color(.10f, .065f, .035f);
    public Color gold = new Color(.91f, .74f, .43f);
    public Color parchmentInk = new Color(.24f, .15f, .075f);
    [Range(0, .5f)] public float worldDim = .22f;
    [Min(.1f)] public float leadDuration = .3f;
    [Min(.2f)] public float revealDuration = .85f;
    [Min(.2f)] public float holdDuration = 2.1f;
    [Min(.1f)] public float fadeDuration = .5f;
    [Min(.5f)] public float rewardDuration = 3.2f;
    [HideInInspector] public int animationFPS = 12;
    [Range(0, 1)] public float volume = .8f;
    public AudioClip impact, resolve, reward;
}
