using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Game/Balance/Area Enemy Damage")]
public sealed class EnemyDamageProfile : ScriptableObject
{
    [TextArea] public string notes;
    [Header("Progression reference (does not gate access)")]
    [Min(0)] public int recommendedPurchases;
    [Min(0)] public float referencePlayerDefense = 10f;
    [Header("Currency for enemies spawned in this area")]
    public bool useAreaRewards;
    [Min(0)] public int normalGold, normalExperience, eliteGold, eliteExperience, bossGold, bossExperience;
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
        public bool balanceVitality;
        [Min(1)] public float maxHP=100;
        [Min(0)] public float defense;
        public List<Skill> skills=new List<Skill>();
    }
    [Range(.1f,1f),Tooltip("Maximum actual player HP damage per skill or directed combo; DOT and other attackers remain separate.")]
    public float maxComboHPFraction=.45f;
    public List<Enemy> enemies=new List<Enemy>();
    public Enemy Find(string id)=>enemies.Find(enemy=>enemy!=null&&enemy.archetypeId==id);
    public void Rewards(EnemyClassification.EnemyType type,out int gold,out int experience)
    {
        gold=Mathf.Max(0,type==EnemyClassification.EnemyType.Boss?bossGold:type==EnemyClassification.EnemyType.Elite?eliteGold:normalGold);
        experience=Mathf.Max(0,type==EnemyClassification.EnemyType.Boss?bossExperience:type==EnemyClassification.EnemyType.Elite?eliteExperience:normalExperience);
    }
}
