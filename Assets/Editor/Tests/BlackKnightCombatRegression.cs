using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Play-mode integration check using real animation events, physics and HP damage.</summary>
public static class BlackKnightCombatRegression
{
    public static string Status { get; private set; }="Idle";
    public static int Passed { get; private set; }
    public static string Failure { get; private set; }
    private static readonly Stack<IEnumerator> jobs=new Stack<IEnumerator>();
    private static GameObject actor,target,barrier;
    private static BlackKnightCombat combat;
    private static Animator animator;
    private static Rigidbody2D body;
    private static readonly Vector2 Origin=new Vector2(24000,24000);
    private static float previousScale;
    private static int lastFrame;
    [MenuItem("Tools/Enemies/Test Black Knight Combat (Play Mode)")]
    public static void Begin()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play Mode before running this test.");
        if(jobs.Count>0)throw new InvalidOperationException("Black Knight regression already running.");
        Failure=null;Passed=0;Status="Running";previousScale=Time.timeScale;Time.timeScale=1;lastFrame=-1;
        jobs.Push(Run());EditorApplication.update+=Tick;
    }
    private static void Tick()
    {
        if(!Application.isPlaying){Finish("Stopped: exited Play Mode");return;}
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        try
        {
            while(jobs.Count>0)
            {
                var job=jobs.Peek();if(!job.MoveNext()){jobs.Pop();continue;}
                if(job.Current is IEnumerator nested){jobs.Push(nested);continue;}return;
            }
            Finish("Passed "+Passed+" checks");
        }
        catch(Exception error){Failure=error.ToString();Debug.LogError("Black Knight regression: "+Failure);Finish("FAILED: "+error.Message);}
    }
    private static void Finish(string status)
    {
        EditorApplication.update-=Tick;jobs.Clear();Status=status;
        if(actor!=null)UnityEngine.Object.Destroy(actor);if(target!=null)UnityEngine.Object.Destroy(target);if(barrier!=null)UnityEngine.Object.Destroy(barrier);
        actor=target=barrier=null;Time.timeScale=previousScale;
    }
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);Passed++;}
    private static IEnumerator Wait(float duration,bool real=false)
    {float start=real?Time.realtimeSinceStartup:Time.time;while((real?Time.realtimeSinceStartup:Time.time)-start<duration)yield return null;}
    private static IEnumerator Until(Func<bool> condition,string reason,float timeout=3)
    {float start=Time.realtimeSinceStartup;while(!condition()){if(Time.realtimeSinceStartup-start>timeout)throw new Exception("Timed out: "+reason);yield return null;}}
    private static void Reset(float distance)
    {
        combat.CancelAttack();actor.GetComponent<EnemyBreak>().ClearBreak();
        actor.transform.position=Origin;body.position=Origin;body.linearVelocity=Vector2.zero;
        target.transform.position=Origin+Vector2.right*distance;target.GetComponent<Rigidbody2D>().position=Origin+Vector2.right*distance;
        actor.GetComponent<CharacterHealth>().isDead=false;actor.GetComponent<CharacterStats>().currentHP=actor.GetComponent<CharacterStats>().maxHP;
        animator.Play("BlackKnightIdle",0,0);animator.Update(0);actor.GetComponent<AnimationEventHandler>().UnlockAction();
        actor.GetComponent<SkillDamageManager>().ResetCooldowns();
        // Allow for float cancellation when testing a reset and attack in the same frame.
        foreach(var skill in actor.GetComponent<SkillDamageManager>().skills)skill.lastUsedTime-=.01f;
        Physics2D.SyncTransforms();
    }
    private static IEnumerator Run()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BlackKnightSetup.PrefabPath);
        var staging=new GameObject("Black Knight regression staging");staging.SetActive(false);
        actor=UnityEngine.Object.Instantiate(prefab,(Vector3)Origin,Quaternion.identity,staging.transform);
        var questGate=actor.GetComponent<World.QuestBossEncounter>();if(questGate!=null)questGate.enabled=false;
        actor.GetComponent<EnemyBehavior>().enabled=false;actor.GetComponent<BossMovesetDirector>().enabled=false;actor.GetComponent<BossPhaseController>().enabled=false;actor.GetComponent<EnemyAreaDamage>().enabled=false;
        actor.GetComponent<EnemyClassification>().persistentId="black-knight-regression:"+Guid.NewGuid().ToString("N");
        var reward=actor.GetComponent<EnemyCurrencyReward>();if(reward==null)reward=actor.AddComponent<EnemyCurrencyReward>();reward.rewardsEnabled=false;
        actor.transform.SetParent(null,true);actor.SetActive(true);UnityEngine.Object.Destroy(staging);
        combat=actor.GetComponent<BlackKnightCombat>();animator=actor.GetComponent<Animator>();body=actor.GetComponent<Rigidbody2D>();
        target=new GameObject("Black Knight test target");target.layer=LayerMask.NameToLayer("Player");
        var targetBody=target.AddComponent<Rigidbody2D>();targetBody.bodyType=RigidbodyType2D.Kinematic;
        target.AddComponent<CapsuleCollider2D>().size=new Vector2(.4f,.6f);target.AddComponent<CharacterInvulnerability>();
        var targetHealth=target.AddComponent<CharacterHealth>();targetHealth.damageInvulnerabilityDuration=1;
        var targetStats=target.GetComponent<CharacterStats>();targetStats.maxHP=10000;targetStats.currentHP=10000;targetStats.DEF=10;
        yield return null;
        Check(actor.GetComponents<EnemyCombat>().Length==1,"Duplicate combat components");
        Check(actor.CompareTag("EnemyBoss"),"Expected boss tag");
        Status="Testing normal attack damage";Reset(2);float before=targetStats.currentHP;
        Check(combat.TriggerSkill("NormalAttack",target.transform,Origin,12),"Normal attack refused");
        yield return Wait(1.6f);Check(targetStats.currentHP<before,"Normal attack event did not damage target");
        Status="Testing prepared thrust and pause";Reset(5.5f);
        Check(combat.TriggerSkill("Skill2",target.transform,Origin,12),"Thrust refused");
        yield return Until(()=>combat.IsPreparing,"thrust windup");
        Check(Vector2.Distance(body.position,Origin)<.04f,"Thrust moved before preparation");
        Time.timeScale=0;float pose=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return Wait(.15f,true);
        Check(combat.IsPreparing&&!combat.IsAdvancing&&Vector2.Distance(body.position,Origin)<.04f,"Pause consumed windup or moved actor");
        Check(Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime-pose)<.01f,"Prepared pose advanced while paused");Time.timeScale=1;
        yield return Until(()=>combat.IsAdvancing,"thrust launch");
        var committed=combat.CommittedDestination;targetBody.position+=Vector2.up*3;Physics2D.SyncTransforms();
        yield return Until(()=>!combat.IsAdvancing,"thrust completion");
        Check(Vector2.Distance(body.position,Origin)>2,"Thrust did not advance");
        Check(Vector2.Distance(body.position,Origin)<=combat.maxThrustDistance+.1f,"Thrust exceeded configured distance");
        Check(Vector2.Distance(combat.CommittedDestination,committed)<.01f&&Mathf.Abs(body.position.y-Origin.y)<.1f,"Thrust tracked dodge after committing");
        yield return Wait(1);
        Status="Testing aerial leap and landing damage";Reset(6.5f);before=targetStats.currentHP;
        Check(combat.TriggerSkill("Skill3",target.transform,Origin,12),"Leap refused");
        yield return Until(()=>combat.IsPreparing,"leap windup");Check(Vector2.Distance(body.position,Origin)<.04f,"Leap moved before preparation");
        yield return Until(()=>combat.IsAdvancing,"leap launch");yield return Until(()=>!combat.IsAdvancing,"leap completion");
        Check(Vector2.Distance(body.position,Origin)>3,"Leap did not advance");Check(Vector2.Distance(body.position,Origin)<=combat.maxLeapDistance+.1f,"Leap exceeded maximum");
        yield return Wait(.8f);Check(targetStats.currentHP<before,"Leap landing event did not damage target");
        Status="Testing collision stop";Reset(5.5f);
        barrier=new GameObject("Black Knight regression solid barrier");barrier.layer=0;barrier.transform.position=Origin+Vector2.right*2.5f;barrier.AddComponent<BoxCollider2D>().size=new Vector2(.5f,6);
        Physics2D.SyncTransforms();Check(combat.TriggerSkill("Skill2",target.transform,Origin,12),"Collision test thrust refused");
        yield return Until(()=>combat.IsPreparing,"collision windup");yield return Wait(.9f);
        Check(body.position.x<Origin.x+2,"Thrust crossed solid barrier");UnityEngine.Object.Destroy(barrier);barrier=null;
        yield return Wait(.5f);
        Status="Testing hurt and break interruption";Reset(5.5f);combat.TriggerSkill("Skill2",target.transform,Origin,12);
        yield return Until(()=>combat.IsPreparing,"hurt windup");actor.GetComponent<CharacterHealth>().TakeDamage(30,0,0,false,target.transform);
        yield return Wait(.25f);Check(!combat.IsPreparing&&!combat.IsAdvancing&&Vector2.Distance(body.position,Origin)<.04f,"Hurt did not cancel preparation");
        Check(Mathf.Approximately(animator.GetFloat("SpecialAttackSpeed"),1),"Hurt left attack speed frozen");
        yield return Wait(.7f);Reset(6.5f);combat.TriggerSkill("Skill3",target.transform,Origin,12);
        yield return Until(()=>combat.IsPreparing,"break windup");actor.GetComponent<EnemyBreak>().ReceiveHit(10000);
        Check(actor.GetComponent<EnemyBreak>().IsBroken&&!combat.IsPreparing&&!combat.IsAdvancing,"Break did not cancel leap");
        Check(Mathf.Approximately(animator.GetFloat("SpecialAttackSpeed"),1),"Break left attack speed frozen");
        Status="Testing phase transition during preparation";Reset(5.5f);var phase=actor.GetComponent<BossPhaseController>();phase.enabled=true;phase.ResetForEncounter();
        Check(!combat.IsSkillAllowed("Skill3")&&combat.IsSkillAllowed("Skill2"),"Wrong phase 1 moveset");combat.TriggerSkill("Skill2",target.transform,Origin,12);
        yield return Until(()=>combat.IsPreparing,"phase windup");actor.GetComponent<CharacterStats>().currentHP=actor.GetComponent<CharacterStats>().maxHP*.5f;phase.EvaluatePhase();
        Check(phase.IsTransitioning&&!combat.IsPreparing&&!combat.IsAdvancing,"Phase did not cancel thrust");
        yield return Until(()=>!phase.IsTransitioning,"five-second phase transition",7);
        Check(phase.CurrentPhase==2&&combat.IsSkillAllowed("Skill3"),"Phase 2 missing leap");Check(Mathf.Approximately(animator.speed,1)&&Mathf.Approximately(animator.GetFloat("SpecialAttackSpeed"),1),"Phase restore left animator frozen");
        Check(actor.GetComponent<CharacterStats>().currentHP>0&&targetStats.currentHP>0,"Test actors unexpectedly died");
    }
}
