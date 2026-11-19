using UnityEngine;

[CreateAssetMenu(menuName = "Game/Menu Audio Settings")]
public sealed class MenuAudioSettings : ScriptableObject
{
    public AudioClip open, close, move, confirm, back, error, page, purchase;
    [Range(0,1)] public float volume = .5f;
}
