using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class NecromancerSpellEffect : MonoBehaviour
{
    public enum Kind { Projectile, TargetArea }
    public Kind mode;
    public Sprite[] frames;
    [Min(.05f)]public float speed=4.5f;
    [Min(.05f)]public float lifetime=2.5f;
    [Min(0), Tooltip("Seconds after this effect spawns before damage or summoning. Match the impact frame at 12 FPS.")]public float activationDelay=.4f;
    [Min(.01f)]public float collisionRadius=.15f;
    [Tooltip("Optional enemy prefab to spawn when the ritual activates. Leave empty for an area spell.")]
    public GameObject summonedEnemy;
    private SkillDamageManager owner;
    private CharacterHealth ownerHealth;
    private string skillName;
    private Vector2 direction;
    private LayerMask blockers;
    private SpriteRenderer visual;
    private float age;
    private bool applied,initialized;
    private readonly RaycastHit2D[] hits=new RaycastHit2D[16];
    public void Initialize(SkillDamageManager caster,string skill,Vector2 aim,LayerMask obstacles)
    {
        owner=caster;skillName=skill;direction=aim.sqrMagnitude>.001f?aim.normalized:Vector2.right;blockers=obstacles;
        if(owner==null){Destroy(gameObject);return;}
        ownerHealth=owner.GetComponent<CharacterHealth>();visual=GetComponent<SpriteRenderer>();
        var reference=owner.GetComponent<SpriteRenderer>();
        if(reference!=null){visual.sortingLayerID=reference.sortingLayerID;visual.sortingOrder=reference.sortingOrder+2;}
        if(mode==Kind.Projectile)transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        initialized=true;Draw();
    }
    private void Update()
    {
        if(!initialized)return;
        if(owner==null||!owner.isActiveAndEnabled||ownerHealth==null||ownerHealth.isDead||BossPhaseController.Protects(owner.gameObject)){Destroy(gameObject);return;}
        var broken=owner.GetComponent<EnemyBreak>();if(broken!=null&&broken.IsBroken){Destroy(gameObject);return;}
        age+=Time.deltaTime;
        if(age>=lifetime){Destroy(gameObject);return;}
        if(mode==Kind.Projectile)
        {
            float distance=speed*Time.deltaTime;
            var filter=new ContactFilter2D{useTriggers=false};filter.SetLayerMask(blockers|owner.targetLayer);
            int count=Physics2D.CircleCast(transform.position,collisionRadius,direction,filter,hits,distance);
            int nearest=-1;float best=float.PositiveInfinity;
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].collider.transform.IsChildOf(owner.transform)&&hits[i].distance<best){nearest=i;best=hits[i].distance;}
            if(nearest>=0)
            {
                var hit=hits[nearest];transform.position=(Vector2)transform.position+direction*hit.distance;
                if((owner.targetLayer.value&(1<<hit.collider.gameObject.layer))!=0)owner.ExecuteHitAtPosition(skillName,hit.point);
                Destroy(gameObject);return;
            }
            transform.position+=(Vector3)(direction*distance);
        }
        else if(!applied&&age>=activationDelay)
        {
            applied=true;
            if(summonedEnemy!=null)
            {
                var summoner=owner.GetComponent<NecromancerCombat>();
                if(summoner!=null)summoner.TrySummon(summonedEnemy,transform.position);
            }
            else owner.ExecuteHitAtPosition(skillName,transform.position);
        }
        Draw();
    }
    private void Draw()
    {
        if(frames==null||frames.Length==0)return;
        int frame=Mathf.FloorToInt(age*PixelAnimationSettings.FPS);
        visual.sprite=frames[mode==Kind.Projectile?frame%frames.Length:Mathf.Min(frame,frames.Length-1)];
    }
}
