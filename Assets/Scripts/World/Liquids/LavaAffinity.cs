using UnityEngine;

/// <summary>Native lava creatures still wade and slow down, without taking environmental lava burn.</summary>
[DisallowMultipleComponent]
public sealed class LavaAffinity : MonoBehaviour
{
    public bool immuneToLavaBurn = true;
}
