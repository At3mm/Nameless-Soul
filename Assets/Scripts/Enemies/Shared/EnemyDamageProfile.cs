using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/Balance/Area Enemy Damage")]
public sealed class EnemyDamageProfile : ScriptableObject
{
    [TextArea] public string notes;
    [Serializable] public sealed class Skill
    {
        public string skillName;
        [Min(0)] public float damageMultiplier=1;
    }
    [Serializable] public sealed class Enemy
    {
        public string archetypeId;
        [Min(0)] public float attack=20,trueDamage=1;
        [Range(0,1)] public float criticalChance;
        [Min(1)] public float criticalMultiplier=1.5f;
        public List<Skill> skills=new List<Skill>();
    }
    public List<Enemy> enemies=new List<Enemy>();
    public Enemy Find(string id)=>enemies.Find(enemy=>enemy!=null&&enemy.archetypeId==id);
}
