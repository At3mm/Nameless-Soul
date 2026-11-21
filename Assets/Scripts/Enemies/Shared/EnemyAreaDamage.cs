using UnityEngine;

/// <summary>Captures spawn balance once; chasing across an area boundary does not rescale damage or rewards.</summary>
[DisallowMultipleComponent,RequireComponent(typeof(CharacterStats),typeof(SkillDamageManager))]
public sealed class EnemyAreaDamage : MonoBehaviour
{
    public string archetypeId;
    public EnemyDamageProfile fallbackProfile;
    [Tooltip("Optional explicit profile. Summoned enemies inherit the caster's profile.")] public EnemyDamageProfile profileOverride;
    public EnemyDamageProfile AppliedProfile { get; private set; }
    public World.AreaDefinition SpawnArea { get; private set; }
    private bool pending;
    private static int syncedFrame=-1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSync(){syncedFrame=-1;}
    private void OnEnable(){pending=true;}
    private void Start(){if(pending)ApplyForSpawn();}
    private void Update(){if(pending)ApplyForSpawn();}
    public EnemyDamageProfile ResolveProfile()
    {
        // Once captured, rewards and repeated application use the original spawn area.
        if(Application.isPlaying && AppliedProfile!=null)return AppliedProfile;
        if(syncedFrame!=Time.frameCount){Physics2D.SyncTransforms();syncedFrame=Time.frameCount;}
        var area=World.AreaManager.FindArea(transform.position,gameObject.scene.handle);
#if UNITY_EDITOR
        if(!Application.isPlaying)
        {
            area=null;
            foreach(var zone in FindObjectsByType<World.AreaTrigger>(FindObjectsSortMode.None))
                if(zone.isActiveAndEnabled&&zone.gameObject.scene==gameObject.scene&&zone.area!=null&&zone.Contains(transform.position)&&
                    (area==null||zone.priority>area.priority||zone.priority==area.priority&&string.CompareOrdinal(zone.area.areaId,area.area.areaId)<0))area=zone;
        }
#endif
        SpawnArea=area!=null?area.area:null;
        return profileOverride!=null?profileOverride:SpawnArea!=null&&SpawnArea.enemyDamageProfile!=null?SpawnArea.enemyDamageProfile:fallbackProfile;
    }
    public bool ApplyForSpawn()
    {
        var profile=ResolveProfile();pending=false;if(profile==null)return false;
        var entry=profile.Find(archetypeId);if(entry==null)return false;
        var stats=GetComponent<CharacterStats>();var manager=GetComponent<SkillDamageManager>();
        bool first=AppliedProfile==null;
        if(entry.balanceVitality)
        {
            float ratio=stats.maxHP>0f?Mathf.Clamp01(stats.currentHP/stats.maxHP):1f;
            stats.maxHP=Mathf.Max(1f,entry.maxHP);stats.DEF=Mathf.Max(0f,entry.defense);
            // Initial spawn starts full; reapplying a profile cannot heal an injured/dead enemy.
            var health=GetComponent<CharacterHealth>();
            stats.currentHP=health!=null&&health.isDead?0f:(!Application.isPlaying||first?stats.maxHP:stats.maxHP*ratio);
        }
        stats.ATK=entry.attack;stats.STR=entry.trueDamage;stats.CRIT_Rate=entry.criticalChance;stats.CRIT_Damage=entry.criticalMultiplier;
        foreach(var rule in entry.skills)
        {
            var skill=manager.skills.Find(candidate=>candidate!=null&&candidate.skillName==rule.skillName);
            if(skill!=null)skill.damageMultiplier=rule.damageMultiplier;
        }
        AppliedProfile=profile;return true;
    }
}
