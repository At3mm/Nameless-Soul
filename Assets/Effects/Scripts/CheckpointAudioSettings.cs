using UnityEngine;

[CreateAssetMenu(menuName="Game/Audio/Checkpoint Activation")]
public sealed class CheckpointAudioSettings : ScriptableObject
{
    public AudioClip activation;
    [Range(0,1)] public float volume=.55f;
    [Range(.5f,1.5f)] public float pitch=.9f;
    private static CheckpointAudioSettings cached;
    public static CheckpointAudioSettings Default { get { if(cached==null)cached=Resources.Load<CheckpointAudioSettings>("DefaultCheckpointAudio");return cached; } }
}
