using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/Enemy Audio Profile")]
public sealed class EnemyAudioProfile : ScriptableObject
{
    [Serializable] public sealed class SkillCue { public string skillName;public AudioClip[] clips; }
    public AudioClip[] attacks,hurt,death,phaseTransition;
    [Tooltip("Use profile attack clips instead of the shared enemy weapon swing. Enable for weapon/armor profiles, not creature vocals.")]
    public bool replaceDefaultWeaponSwing;
    public List<SkillCue> skills=new List<SkillCue>();
    [Range(0,1)] public float volume=.8f;
    [Range(.5f,1.5f)] public float pitch=1;
    [Min(.1f)] public float attackInterval=.45f,hurtInterval=.5f;
}
