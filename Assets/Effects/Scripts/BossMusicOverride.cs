using UnityEngine;

[DisallowMultipleComponent,RequireComponent(typeof(EnemyClassification))]
public sealed class BossMusicOverride : MonoBehaviour
{
    public AudioClip combatMusic;
}
