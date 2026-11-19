using UnityEngine;

/// <summary>Captures spawn area once; crossing a boundary during combat does not change damage.</summary>
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
    private void Start(){ApplyForSpawn();}
    private void Update(){if(pending)ApplyForSpawn();}
    public bool ApplyForSpawn()
    {
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
        var profile=profileOverride!=null?profileOverride:SpawnArea!=null&&SpawnArea.enemyDamageProfile!=null?SpawnArea.enemyDamageProfile:fallbackProfile;
        pending=false;if(profile==null)return false;
        var entry=profile.Find(archetypeId);if(entry==null)return false;
        var stats=GetComponent<CharacterStats>();var manager=GetComponent<SkillDamageManager>();
        stats.ATK=entry.attack;stats.STR=entry.trueDamage;stats.CRIT_Rate=entry.criticalChance;stats.CRIT_Damage=entry.criticalMultiplier;
        foreach(var rule in entry.skills)
        {
            var skill=manager.skills.Find(candidate=>candidate.skillName==rule.skillName);
            if(skill!=null)skill.damageMultiplier=rule.damageMultiplier;
        }
        AppliedProfile=profile;return true;
    }
}
