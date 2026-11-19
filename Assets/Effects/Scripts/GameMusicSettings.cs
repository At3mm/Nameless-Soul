using UnityEngine;

[CreateAssetMenu(menuName="Game/Audio/Game Music")]
public sealed class GameMusicSettings : ScriptableObject
{
    public AudioClip menu, exploration, boss;
    [Range(0,1)] public float volume=.4f;
    [Min(.05f)] public float crossfadeSeconds=1.2f;
    [Min(0)] public float disengageDelay=2.5f;
    [Min(1)] public float maximumBossDistance=25;
    [Range(0,1)] public float pausedVolume=.5f;
}
