using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class NecromancerMinionSetup
{
    const string Root="Assets/Sprites/Necromancer/SummonedEnemy/";
    const string Anim=Root+"Animation/";
    public const string PrefabPath="Assets/Prefabs/Characters/NecromancerBat.prefab";
    [MenuItem("Tools/Enemies/Create Missing Necromancer Minion Assets")]
    public static void CreateMissing()
    {
        Directory.CreateDirectory(Anim);AssetDatabase.Refresh();
        var flying=Clip("BatFlying","Walk/Bat_Flying.png",true);
        var attack=Clip("BatAttack01","Attack/Bat_Attack01.png",false,"ExecuteHit",3,"NormalAttack");
        var attack2=Clip("BatAttack02","Attack2/Bat_Attack02.png",false,"ExecuteHit",4,"HeavyAttack");
        var hurt=Clip("BatHurt","TakeDmg/Bat_Hurt.png",false);
        var death=Clip("BatDeath","Death/Bat_Death.png",false,"DestroyCharacter",3);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim+"NecromancerBat.controller");
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(Anim+"NecromancerBat.controller");
            controller.AddParameter("IsWalking",AnimatorControllerParameterType.Bool);
            var sm=controller.layers[0].stateMachine;var idle=sm.AddState("Flying");idle.motion=flying;sm.defaultState=idle;
            State(controller,sm,idle,"NormalAttack",attack,true);State(controller,sm,idle,"HeavyAttack",attack2,true);
            State(controller,sm,idle,"TakeDamage",hurt,true);State(controller,sm,idle,"Die",death,false);
            var broken=sm.AddState("Broken");broken.motion=hurt;
        }
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null)
        {
            var go=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Characters/Orc.prefab");
            try
            {
                go.name="NecromancerBat";go.AddComponent<EnemyFlight>();go.AddComponent<NecromancerSummonedMinion>();
                var water=go.GetComponent<WaterPresence>();if(water!=null)UnityEngine.Object.DestroyImmediate(water);
                var footsteps=go.GetComponent<CharacterFootsteps>();if(footsteps!=null)UnityEngine.Object.DestroyImmediate(footsteps);
                var reward=go.GetComponent<EnemyCurrencyReward>();if(reward==null)reward=go.AddComponent<EnemyCurrencyReward>();reward.rewardsEnabled=false;
                var stats=go.GetComponent<CharacterStats>();stats.maxHP=40;stats.currentHP=40;stats.ATK=8;stats.DEF=0;stats.STR=0;
                var classification=go.GetComponent<EnemyClassification>();classification.enemyType=EnemyClassification.EnemyType.Normal;classification.displayName="Dơi triệu hồi";classification.persistentId="";go.tag="EnemyNormal";
                go.GetComponent<Animator>().runtimeAnimatorController=controller;go.GetComponent<SpriteRenderer>().sprite=Sprites("Walk/Bat_Flying.png")[0];
                var collider=go.GetComponent<CapsuleCollider2D>();collider.size=new Vector2(.35f,.4f);collider.offset=new Vector2(0,.18f);
                var rb=go.GetComponent<Rigidbody2D>();rb.gravityScale=0;rb.constraints=RigidbodyConstraints2D.FreezeRotation;
                var ai=go.GetComponent<EnemyBehavior>();ai.relentlessPursuit=true;ai.patrolSpeed=1.6f;ai.chaseSpeed=2.8f;ai.retreatSpeed=2.1f;ai.sprintSpeed=3.4f;
                var combat=go.GetComponent<EnemyCombat>();combat.lungeSpeed=3;combat.lungeDuration=.12f;
                var manager=go.GetComponent<SkillDamageManager>();combat.skillManager=manager;
                manager.skills=new System.Collections.Generic.List<SkillData>{
                    new SkillData{skillName="NormalAttack",damageMultiplier=1,attackRange=.5f,hitboxOffset=new Vector2(.4f,0),cooldownTime=1.1f,knockbackForce=1},
                    new SkillData{skillName="HeavyAttack",damageMultiplier=1.4f,attackRange=.65f,hitboxOffset=new Vector2(.35f,0),cooldownTime=2.6f,knockbackForce=2}};
                var breakState=go.GetComponent<EnemyBreak>();if(breakState!=null)UnityEngine.Object.DestroyImmediate(breakState);
                PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        var ritual=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Effects/Necromancer/RitualCircle.prefab");
        try
        {
            var effect=ritual.GetComponent<NecromancerSpellEffect>();
            if(effect.summonedEnemy==null){effect.summonedEnemy=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);PrefabUtility.SaveAsPrefabAsset(ritual,"Assets/Prefabs/Effects/Necromancer/RitualCircle.prefab");}
        }
        finally{PrefabUtility.UnloadPrefabContents(ritual);}
        AssetDatabase.SaveAssets();
    }
    static Sprite[] Sprites(string path)=>AssetDatabase.LoadAllAssetsAtPath(Root+path).OfType<Sprite>().OrderBy(s=>int.Parse(s.name.Substring(s.name.LastIndexOf('_')+1))).ToArray();
    static AnimationClip Clip(string name,string sheet,bool loop,string action="",int frame=0,string argument="")
    {
        string path=Anim+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing!=null)return existing;
        var sprites=Sprites(sheet);if(sprites.Length==0)throw new InvalidOperationException("Missing sliced sprites: "+sheet);
        var clip=new AnimationClip{name=name,frameRate=12};var keys=new ObjectReferenceKeyframe[sprites.Length+1];
        for(int i=0;i<keys.Length;i++)keys[i]=new ObjectReferenceKeyframe{time=i/12f,value=sprites[Math.Min(i,sprites.Length-1)]};
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        if(!loop)
        {
            var events=new System.Collections.Generic.List<AnimationEvent>{new AnimationEvent{time=0,functionName="LockAction"}};
            if(action!="")events.Add(new AnimationEvent{time=frame/12f,functionName=action,stringParameter=argument});
            if(action!="DestroyCharacter")events.Add(new AnimationEvent{time=(sprites.Length-.1f)/12f,functionName="UnlockAction"});
            AnimationUtility.SetAnimationEvents(clip,events.ToArray());
        }
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
    static void State(AnimatorController controller,AnimatorStateMachine sm,AnimatorState idle,string name,AnimationClip clip,bool returns)
    {
        controller.AddParameter(name,AnimatorControllerParameterType.Trigger);var state=sm.AddState(name);state.motion=clip;
        var enter=sm.AddAnyStateTransition(state);enter.hasExitTime=false;enter.duration=0;enter.canTransitionToSelf=false;enter.AddCondition(AnimatorConditionMode.If,0,name);
        if(returns){var exit=state.AddTransition(idle);exit.hasExitTime=true;exit.exitTime=1;exit.duration=0;}
    }
}
