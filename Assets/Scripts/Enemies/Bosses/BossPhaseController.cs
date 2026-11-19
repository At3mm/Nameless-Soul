using System.Collections.Generic;
using UnityEngine;

/// <summary>One-way phase transition per encounter; restricts new attacks, never edits animation events.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats),typeof(EnemyClassification),typeof(SkillDamageManager))]
public sealed class BossPhaseController : MonoBehaviour
{
    [Tooltip("Disable for a single-phase encounter. Only Phase 1 Skills are used for the entire fight.")]
    public bool useSecondPhase=true;
    [Range(.01f,.99f)] public float phase2HealthThreshold=.5f;
    public List<string> phase1Skills=new List<string>();
    public List<string> phase2Skills=new List<string>();
    [Min(.1f)] public float transitionDuration=5;
    public string transitionState="PhaseTransition", idleState="OrcIdle";
    public bool IsTransitioning { get; private set; }
    public float TransitionRemaining=>IsTransitioning?Mathf.Max(0,transitionDuration-transitionElapsed):0;
    private float transitionElapsed,previousAnimatorSpeed,transitionClipLength;
    private Animator animator;
    private Rigidbody2D body;
    private RigidbodyConstraints2D previousConstraints;
    private AnimationEventHandler handler;
    private CharacterStats stats;
    private CharacterHealth health;
    private EnemyClassification classification;
    private int phase=1;
    public int CurrentPhase { get { RefreshPhase();return phase; } }
    private void Awake(){Cache();}
    private void Cache()
    {
        if(stats==null)stats=GetComponent<CharacterStats>();
        if(health==null)health=GetComponent<CharacterHealth>();
        if(classification==null)classification=GetComponent<EnemyClassification>();
        if(animator==null)animator=GetComponent<Animator>();
        if(body==null)body=GetComponent<Rigidbody2D>();
        if(handler==null)handler=GetComponent<AnimationEventHandler>();
    }
    private void OnEnable(){ResetForEncounter();}
    private void Update()
    {
        RefreshPhase();
        if(!IsTransitioning)return;
        transitionElapsed+=Time.deltaTime;
        if(health==null||health.isDead){FinishTransition(false);return;}
        if(transitionElapsed>=transitionDuration)FinishTransition(true);
    }
    private void LateUpdate(){if(IsTransitioning)MaintainTransition();}
    private void FixedUpdate(){if(IsTransitioning&&body!=null){body.linearVelocity=Vector2.zero;body.angularVelocity=0;}}
    public void EvaluatePhase(){RefreshPhase();}
    public static bool Protects(GameObject actor)
    {
        var controller=actor.GetComponent<BossPhaseController>();
        return controller!=null&&controller.isActiveAndEnabled&&controller.IsTransitioning;
    }
    private void RefreshPhase()
    {
        if(!Application.isPlaying||!isActiveAndEnabled||!useSecondPhase)return;
        Cache();
        if(classification==null||classification.enemyType!=EnemyClassification.EnemyType.Boss||
            stats==null||stats.maxHP<=0||stats.currentHP<=0||health!=null&&health.isDead)return;
        float threshold=stats.maxHP*phase2HealthThreshold;
        if(phase==1&&(stats.currentHP<=threshold||Mathf.Approximately(stats.currentHP,threshold)))
        {phase=2;BeginTransition();}
    }
    private void BeginTransition()
    {
        IsTransitioning=true;transitionElapsed=0;transitionClipLength=0;
        GetComponent<EnemyAudio>()?.PhaseTransition();
        previousAnimatorSpeed=animator!=null?animator.speed:1;
        if(body!=null){previousConstraints=body.constraints;body.constraints=RigidbodyConstraints2D.FreezeAll;body.linearVelocity=Vector2.zero;body.angularVelocity=0;}
        GetComponent<EnemyCombat>()?.CancelAttack();GetComponent<EliteOrcTargetedLeap>()?.CancelLeap();
        health?.CancelKnockback();GetComponent<PlayerStun>()?.CancelStun();GetComponent<EnemyBreak>()?.ClearBreak();
        var parry=GetComponent<CharacterParry>();if(parry!=null)parry.isParrying=false;
        if(animator!=null){foreach(var parameter in animator.parameters)if(parameter.type==AnimatorControllerParameterType.Trigger)animator.ResetTrigger(parameter.nameHash);animator.speed=0;}
        MaintainTransition();
    }
    private void MaintainTransition()
    {
        handler?.LockAction();
        if(body!=null){body.linearVelocity=Vector2.zero;body.angularVelocity=0;}
        float sample=Mathf.Floor(transitionElapsed*PixelAnimationSettings.FPS)/PixelAnimationSettings.FPS;
        if(animator==null)return;
        if(animator.HasState(0,Animator.StringToHash(transitionState)))
        {
            float progress=transitionClipLength>0?Mathf.Repeat(sample,transitionClipLength)/transitionClipLength:0;
            animator.Play(transitionState,0,progress);animator.Update(0);
            if(transitionClipLength<=0)transitionClipLength=Mathf.Max(1f/PixelAnimationSettings.FPS,animator.GetCurrentAnimatorStateInfo(0).length);
        }
    }
    private void FinishTransition(bool resume)
    {
        if(!IsTransitioning)return;
        IsTransitioning=false;
        if(body!=null){body.constraints=previousConstraints;body.linearVelocity=Vector2.zero;body.angularVelocity=0;}
        if(animator!=null)
        {
            animator.speed=previousAnimatorSpeed;
            if(resume&&animator.HasState(0,Animator.StringToHash(idleState)))animator.Play(idleState,0,0);
        }
        if(resume)handler?.UnlockAction();
    }
    public bool AllowsSkill(string skill)
    {
        Cache();
        if(!isActiveAndEnabled||classification==null||classification.enemyType!=EnemyClassification.EnemyType.Boss)return true;
        int current=useSecondPhase?CurrentPhase:1;
        return !IsTransitioning&&(current==1?phase1Skills:phase2Skills).Contains(skill);
    }
    public void ResetForEncounter(){FinishTransition(health==null||!health.isDead);phase=1;}
    private void OnDisable(){FinishTransition(health==null||!health.isDead);}
    public void SetDefaultSkills()
    {
        phase1Skills.Clear();phase2Skills.Clear();
        var manager=GetComponent<SkillDamageManager>();if(manager==null||manager.skills==null)return;
        foreach(var skill in manager.skills)
        {
            if(skill==null||string.IsNullOrEmpty(skill.skillName)||phase2Skills.Contains(skill.skillName))continue;
            phase2Skills.Add(skill.skillName);
            if(phase1Skills.Count==0)phase1Skills.Add(skill.skillName);
        }
    }
    private void Reset(){SetDefaultSkills();}
}
