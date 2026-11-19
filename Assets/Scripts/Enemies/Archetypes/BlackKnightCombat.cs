using System.Collections;
using UnityEngine;

/// <summary>Animation-event driven windups and committed, collision-safe advances.</summary>
[DisallowMultipleComponent]
public sealed class BlackKnightCombat : EnemyCombat
{
    public const string Slash="NormalAttack", Thrust="Skill2", Leap="Skill3";
    private const string SpeedParameter="SpecialAttackSpeed";
    [Header("Attack selection distance")]
    [Min(.1f)] public float slashDistance=2.6f,thrustDistance=6f,leapDistance=7f;
    [Header("Skill 2 — prepared thrust")]
    [Min(0)] public float thrustDelay=.15f;
    [Min(.02f)] public float thrustMoveDuration=.28f;
    [Min(0)] public float maxThrustDistance=4.5f;
    [Header("Skill 3 — prepared aerial leap")]
    [Min(0)] public float leapDelay=.15f;
    [Min(.02f)] public float leapMoveDuration=.45f;
    [Min(0)] public float maxLeapDistance=5.5f;
    [Header("Movement safety")]
    [Min(0)] public float stopDistance=1.25f;
    [Min(.01f)] public float collisionSkin=.04f;
    public bool IsPreparing { get; private set; }
    public bool IsAdvancing { get; private set; }
    public Vector2 CommittedDestination { get; private set; }
    private Transform target;
    private Vector2 home;
    private float leash,startedAt;
    private string activeSkill;
    private bool entered,begun;
    private Coroutine movement;
    private Rigidbody2D body;
    private Animator knightAnimator;
    private CharacterHealth health;
    private EnemyBreak broken;
    private EnemyAwareness sight;
    private readonly RaycastHit2D[] hits=new RaycastHit2D[32];
    private void Cache()
    {
        if(body==null)body=GetComponent<Rigidbody2D>();
        if(knightAnimator==null)knightAnimator=GetComponent<Animator>();
        if(health==null)health=GetComponent<CharacterHealth>();
        if(broken==null)broken=GetComponent<EnemyBreak>();
        if(sight==null)sight=GetComponent<EnemyAwareness>();
    }
    private float Range(string skill)=>skill==Thrust?thrustDistance:skill==Leap?leapDistance:slashDistance;
    public override bool IsInAttackRange(Transform player)
    {
        Cache();if(player==null||sight!=null&&!sight.HasLineOfSight(player)||skillManager==null)return false;
        foreach(var skill in skillManager.skills)
            if(IsSkillAllowed(skill.skillName)&&skillManager.IsSkillReady(skill.skillName)&&CanSelectSkill(skill,player))return true;
        return false;
    }
    protected override bool CanSelectSkill(SkillData skill,Transform player)=>player!=null&&Vector2.Distance(transform.position,player.position)<=Range(skill.skillName);
    protected override bool ShouldLunge(SkillData skill)=>false;
    protected override void OnAttackStarted(SkillData skill,Transform player,Vector2 homePosition,float chaseLimit)
    {
        Cache();CancelSpecial();target=player;home=homePosition;leash=Mathf.Max(0,chaseLimit);activeSkill=skill.skillName;
        startedAt=Time.time;entered=begun=false;body.linearVelocity=Vector2.zero;GetComponent<AnimationEventHandler>()?.LockAction();
    }
    // Place these on the prepared pose. The extra windup holds only this attack state,
    // not Animator.speed, so hurt/death and phase transition animation cannot be frozen.
    public void BeginThrust()=>BeginSpecial(Thrust);
    public void BeginLeap()=>BeginSpecial(Leap);
    private void BeginSpecial(string skill)
    {
        Cache();if(begun||activeSkill!=skill||!CanContinue())return;
        begun=true;movement=StartCoroutine(PrepareAndAdvance(skill==Leap));
    }
    private bool CanContinue()
    {
        if(!isActiveAndEnabled||target==null||!target.gameObject.activeInHierarchy||health!=null&&health.isDead||broken!=null&&broken.IsBroken||BossPhaseController.Protects(gameObject))return false;
        var targetHealth=target.GetComponent<CharacterHealth>();
        if(targetHealth!=null&&(targetHealth.isDead||targetHealth.IsUntargetable))return false;
        return knightAnimator!=null&&knightAnimator.GetCurrentAnimatorStateInfo(0).IsName(activeSkill)&&
            (sight==null||sight.HasLineOfSight(target));
    }
    private IEnumerator PrepareAndAdvance(bool leap)
    {
        IsPreparing=true;knightAnimator.SetFloat(SpeedParameter,0);
        float waited=0,delay=Mathf.Max(0,leap?leapDelay:thrustDelay);
        while(waited<delay)
        {
            if(!CanContinue()){FinishMovement();yield break;}
            body.linearVelocity=Vector2.zero;yield return null;waited+=Time.deltaTime;
        }
        if(!CanContinue()){FinishMovement();yield break;}
        IsPreparing=false;IsAdvancing=true;knightAnimator.SetFloat(SpeedParameter,1);
        Vector2 origin=body.position,direction=(Vector2)target.position-origin;
        float distance=direction.magnitude;direction=distance>.001f?direction/distance:new Vector2(Mathf.Sign(transform.localScale.x),0);
        float travel=Mathf.Min(Mathf.Max(0,distance-stopDistance),Mathf.Max(0,leap?maxLeapDistance:maxThrustDistance));
        CommittedDestination=home+Vector2.ClampMagnitude(origin+direction*travel-home,leash);
        skillManager.currentAttackDirection=direction;
        if(Mathf.Abs(direction.x)>.01f){var scale=transform.localScale;scale.x=Mathf.Abs(scale.x)*(direction.x>0?1:-1);transform.localScale=scale;}
        float elapsed=0,duration=Mathf.Max(.02f,leap?leapMoveDuration:thrustMoveDuration);
        var filter=new ContactFilter2D{useTriggers=false};filter.SetLayerMask(obstacleLayer|skillManager.targetLayer|(1<<gameObject.layer));
        while(elapsed<duration)
        {
            yield return new WaitForFixedUpdate();
            if(!CanContinue()){FinishMovement();yield break;}
            elapsed+=Time.fixedDeltaTime;
            Vector2 desired=Vector2.Lerp(origin,CommittedDestination,Mathf.Clamp01(elapsed/duration));
            Vector2 delta=desired-body.position;
            if(delta.sqrMagnitude<.000001f)continue;
            float step=delta.magnitude;
            int count=body.Cast(delta.normalized,filter,hits,step+collisionSkin);
            if(count==hits.Length){FinishMovement();yield break;}
            for(int i=0;i<count;i++)if(hits[i].collider!=null&&!hits[i].transform.IsChildOf(transform))step=Mathf.Min(step,Mathf.Max(0,hits[i].distance-collisionSkin));
            if(step<=.001f){FinishMovement();yield break;}
            body.MovePosition(body.position+delta.normalized*step);
        }
        FinishMovement();
    }
    // Stops at the actual landing/impact frame. Never snaps through a wall to catch the target.
    public void EndSpecialMovement(){if(movement!=null)StopCoroutine(movement);FinishMovement();}
    private void FinishMovement()
    {
        movement=null;IsPreparing=IsAdvancing=false;
        if(knightAnimator!=null&&knightAnimator.runtimeAnimatorController!=null)knightAnimator.SetFloat(SpeedParameter,1);
        if(body!=null)body.linearVelocity=Vector2.zero;
    }
    private void CancelSpecial(){EndSpecialMovement();activeSkill=null;target=null;}
    private void Update()
    {
        if(activeSkill==null)return;
        Cache();var state=knightAnimator.GetCurrentAnimatorStateInfo(0);
        if(state.IsName(activeSkill))entered=true;
        if(health!=null&&health.isDead||broken!=null&&broken.IsBroken||BossPhaseController.Protects(gameObject)||
            !state.IsName(activeSkill)&&(entered||Time.time-startedAt>.4f)||target==null)CancelSpecial();
    }
    public override void CancelAttack(){CancelSpecial();base.CancelAttack();}
    private void OnDisable(){CancelSpecial();}
}
