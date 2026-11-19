using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class NecromancerCombat : EnemyCombat
{
    public const string Slash="NormalAttack", FallingBlast="HeavyAttack", Ritual="Summon";
    [Serializable]public sealed class SpellBinding
    {
        public string skillName;
        public NecromancerSpellEffect prefab;
        public Transform castPoint;
    }
    public List<SpellBinding> spells=new List<SpellBinding>();
    [Min(.1f)] public float castRange=6;
    [Min(.1f)] public float meleeRange=1.8f;
    [Min(1)] public int summonsPerCast=2;
    [Min(0), Tooltip("Maximum living summons. Zero allows every cast to summon its full group.")] public int summonLimit;
    private readonly List<CharacterHealth> minions=new List<CharacterHealth>();
    public int LivingSummons { get { minions.RemoveAll(m=>m==null||m.isDead);return minions.Count; } }
    private Transform target;
    private string activeSkill;
    private bool released;
    public override bool IsInAttackRange(Transform player)
    {
        bool hasRangedSkill=skillManager!=null&&skillManager.skills.Exists(skill=>skill.skillName!=Slash&&IsSkillAllowed(skill.skillName));
        if(player==null||Vector2.Distance(transform.position,player.position)>(hasRangedSkill?castRange:meleeRange))return false;
        var sight=GetComponent<EnemyAwareness>();return sight==null||sight.HasLineOfSight(player);
    }
    protected override bool CanSelectSkill(SkillData skill,Transform player)
    {
        if(player==null)return false;
        if(skill.skillName==Ritual&&summonLimit>0&&LivingSummons+summonsPerCast>summonLimit)return false;
        return Vector2.Distance(transform.position,player.position)<=(skill.skillName==Slash?meleeRange:castRange);
    }
    protected override bool ShouldLunge(SkillData skill)=>false;
    protected override void OnAttackStarted(SkillData skill,Transform player,Vector2 home,float limit)
    {target=player;activeSkill=skill.skillName;released=false;}
    public void SpawnSkillEffect(string skillName)
    {
        var health=GetComponent<CharacterHealth>();var broken=GetComponent<EnemyBreak>();
        if(!isActiveAndEnabled||released||activeSkill!=skillName||target==null||health==null||health.isDead||broken!=null&&broken.IsBroken)return;
        var targetHealth=target.GetComponent<CharacterHealth>();if(targetHealth==null||targetHealth.isDead)return;
        var binding=spells.Find(item=>item.skillName==skillName);
        if(binding==null||binding.prefab==null)return;
        var sight=GetComponent<EnemyAwareness>();if(sight!=null&&!sight.HasLineOfSight(target))return;
        released=true;
        GetComponent<EnemyAudio>()?.Attack(skillName);
        Vector2 origin=binding.castPoint!=null?(Vector2)binding.castPoint.position:(Vector2)transform.position+Vector2.up*.8f;
        Vector2 direction=((Vector2)target.position-origin).normalized;
        var position=binding.prefab.mode==NecromancerSpellEffect.Kind.Projectile?origin:(Vector2)target.position;
        var effect=Instantiate(binding.prefab,position,Quaternion.identity);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(effect.gameObject,gameObject.scene);
        effect.Initialize(skillManager,skillName,direction,obstacleLayer);
    }
    public override void CancelAttack(){base.CancelAttack();activeSkill=null;target=null;released=true;}
    public bool TrySummon(GameObject prefab,Vector2 center)
    {
        var health=GetComponent<CharacterHealth>();
        int count=Mathf.Max(1,summonsPerCast);
        if(prefab==null||health==null||health.isDead||!isActiveAndEnabled||summonLimit>0&&LivingSummons+count>summonLimit)return false;
        var broken=GetComponent<EnemyBreak>();if(broken!=null&&broken.IsBroken)return false;
        var points=new List<Vector2>();
        var filter=new ContactFilter2D{useTriggers=false};filter.SetLayerMask(obstacleLayer|skillManager.targetLayer|(1<<gameObject.layer));
        var overlaps=new Collider2D[1];
        for(int i=0;i<32&&points.Count<count;i++)
        {
            float angle=(i%8)*Mathf.PI/4;
            Vector2 point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(.9f+(i/8)*.45f);
            if(Physics2D.OverlapCircle(point,.23f,filter,overlaps)>0)continue;
            if(points.Exists(other=>Vector2.Distance(other,point)<.6f))continue;
            points.Add(point);
        }
        if(points.Count<count)return false;
        foreach(var point in points)
        {
            var staging=new GameObject("Summon staging");staging.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(staging,gameObject.scene);
            var instance=Instantiate(prefab,point,Quaternion.identity,staging.transform);instance.SetActive(false);
            var minion=instance.GetComponent<NecromancerSummonedMinion>();
            if(minion==null)minion=instance.AddComponent<NecromancerSummonedMinion>();
            minion.Bind(health);instance.transform.SetParent(null,true);instance.SetActive(true);Destroy(staging);
            var damage=instance.GetComponent<EnemyAreaDamage>();var ownerDamage=GetComponent<EnemyAreaDamage>();
            if(damage!=null&&ownerDamage!=null&&ownerDamage.AppliedProfile!=null)damage.profileOverride=ownerDamage.AppliedProfile;
            var summonedHealth=instance.GetComponent<CharacterHealth>();if(summonedHealth!=null)minions.Add(summonedHealth);
            instance.GetComponent<EnemyAwareness>()?.MaxAlert();
        }
        return true;
    }
}
