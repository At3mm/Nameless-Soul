using UnityEngine;

[CreateAssetMenu(menuName = "Game/Combat Audio Settings")]
public sealed class CombatAudioSettings : ScriptableObject
{
    public AudioClip[] hits;
    public AudioClip[] heavyHits;
    public AudioClip parry, dash, heal, cleanse;
    public AudioClip parryReady;
    public AudioClip enemyBreak;
    [Range(0, 1)] public float enemyBreakVolume = .85f;
    [Range(0, 1)] public float parryReadyVolume = .32f;
    public AudioClip attackSwing, skill2Cast, skill2Swing;
    public AudioClip[] attackSwingVariations, skill2SwingVariations;
    [Range(0, 1)] public float swingVolume = .45f, skillVolume = .65f;
    public AudioClip[] buffs, debuffs;
    [Range(0, 1)] public float volume = .55f;
    [Range(0, 1)] public float hitVolume = .75f, parryVolume = .85f, dashVolume = .45f, statusVolume = .5f;
    [Range(0,1)] public float enemyVolume = .4f;
    [Min(1)] public float audibleDistance = 25f;
}
