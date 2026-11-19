using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Creates only missing Black Knight assets; running again preserves edited clips and prefab.</summary>
public static class BlackKnightSetup
{
    const string Root="Assets/Sprites/Black Knight/", Anim=Root+"Animation/";
    public const string PrefabPath="Assets/Prefabs/Characters/BlackKnight.prefab";
    [MenuItem("Tools/Enemies/Create Missing Black Knight Assets")]
    public static void CreateMissing()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Anim);AssetDatabase.Refresh();
        var idle=Clip("BlackKnightIdle","BlackKnightIdle/Black Knight_A_Idle.png",true);
        var walk=Clip("BlackKnightWalk","BlackKnightWalk/Black Knight_A_Walk.png",true);
        var hurt=Clip("BlackKnightHurt","BlackKnightTakeDmg/Black Knight_A_Hurt.png",false);
        var death=Clip("BlackKnightDeath","BlackKnightDeath/Black Knight_A_Death.png",false,E(3,"DestroyCharacter"));
        var slash=Clip("BlackKnightAttack01","BlackKnightAttack/Black Knight_A_Attack01.png",false,E(5,"ExecuteHit","NormalAttack"),E(11,"ExecuteHit","NormalAttack"));
        var thrust=Clip("BlackKnightAttack02","BlackKnightAttack/Black Knight_A_Attack02.png",false,E(5,"BeginThrust"),E(11,"EndSpecialMovement"),E(11,"ExecuteHit","Skill2"));
        var leap=Clip("BlackKnightAttack03","BlackKnightAttack/Black Knight_A_Attack03.png",false,E(3,"BeginLeap"),E(9,"EndSpecialMovement"),E(10,"ExecuteHit","Skill3"));
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim+"BlackKnight.controller");
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(Anim+"BlackKnight.controller");
            controller.AddParameter("IsWalking",AnimatorControllerParameterType.Bool);
            controller.AddParameter(new AnimatorControllerParameter{name="SpecialAttackSpeed",type=AnimatorControllerParameterType.Float,defaultFloat=1});
            var sm=controller.layers[0].stateMachine;
            var rest=sm.AddState("BlackKnightIdle");rest.motion=idle;sm.defaultState=rest;
            var moving=sm.AddState("BlackKnightWalk");moving.motion=walk;
            var t=rest.AddTransition(moving);t.duration=0;t.hasExitTime=false;t.AddCondition(AnimatorConditionMode.If,0,"IsWalking");
            t=moving.AddTransition(rest);t.duration=0;t.hasExitTime=false;t.AddCondition(AnimatorConditionMode.IfNot,0,"IsWalking");
            State(controller,sm,rest,"NormalAttack",slash,true,false);
            State(controller,sm,rest,"Skill2",thrust,true,true);State(controller,sm,rest,"Skill3",leap,true,true);
            State(controller,sm,rest,"TakeDamage",hurt,true,false);State(controller,sm,rest,"Die",death,false,false);
            sm.AddState("Broken").motion=idle;sm.AddState("PhaseTransition").motion=hurt;
        }
        Profiles();
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null)
        {
            var go=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Characters/Orc.prefab");
            try
            {
                go.name="BlackKnight";
                var previous=go.GetComponent<EnemyCombat>();var combat=go.AddComponent<BlackKnightCombat>();combat.obstacleLayer=previous.obstacleLayer;
                foreach(var old in go.GetComponents<EnemyCombat>())if(old.GetType()==typeof(EnemyCombat))UnityEngine.Object.DestroyImmediate(old);
                combat.skillManager=go.GetComponent<SkillDamageManager>();
                combat.skillManager.skills=new List<SkillData>{
                    new SkillData{skillName="NormalAttack",damageMultiplier=1,attackRange=1.25f,hitboxOffset=new Vector2(1.25f,0),cooldownTime=1.7f,knockbackForce=3},
                    new SkillData{skillName="Skill2",damageMultiplier=1.3f,attackRange=1.15f,hitboxOffset=new Vector2(1.6f,0),cooldownTime=4,knockbackForce=5},
                    new SkillData{skillName="Skill3",damageMultiplier=1.5f,attackRange=2.4f,hitboxOffset=new Vector2(.6f,0),cooldownTime=6,knockbackForce=6}};
                var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=controller;
                var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=Sprites("BlackKnightIdle/Black Knight_A_Idle.png")[0];renderer.color=Color.white;
                var c=go.GetComponent<EnemyClassification>();c.enemyType=EnemyClassification.EnemyType.Boss;c.displayName="Black Knight";c.questBossId="black-knight";c.persistentId="";c.preservePersistentId=false;go.tag="EnemyBoss";
                var stats=go.GetComponent<CharacterStats>();stats.maxHP=800;stats.currentHP=800;stats.maxStamina=180;stats.currentStamina=180;stats.ATK=35;stats.STR=3;stats.DEF=12;stats.CRIT_Rate=0;
                var collider=go.GetComponent<CapsuleCollider2D>();collider.size=new Vector2(.9f,1.6f);collider.offset=new Vector2(0,.05f);
                var awareness=go.GetComponent<EnemyAwareness>();awareness.chaseTriggerDistance=10;awareness.instantAggroDistance=4;awareness.maxChaseDistance=12;
                var ai=go.GetComponent<EnemyBehavior>();ai.patrolRadius=2;ai.patrolSpeed=1.6f;ai.chaseSpeed=2.8f;
                var broken=go.GetComponent<EnemyBreak>();broken.idleState="BlackKnightIdle";broken.barHeight=1.5f;
                var phase=go.GetComponent<BossPhaseController>();if(phase==null)phase=go.AddComponent<BossPhaseController>();
                phase.useSecondPhase=true;phase.phase1Skills=new List<string>{"NormalAttack","Skill2"};phase.phase2Skills=new List<string>{"NormalAttack","Skill2","Skill3"};phase.idleState="BlackKnightIdle";phase.transitionState="PhaseTransition";phase.transitionDuration=5;
                var director=go.GetComponent<BossMovesetDirector>();if(director==null)director=go.AddComponent<BossMovesetDirector>();
                director.maxComboLength=2;director.comboChance=.65f;director.recoveryTime=.9f;director.combos=new List<BossMovesetDirector.Combo>{
                    Combo("Slash then thrust",BossMovesetDirector.PlayerMove.Any,2.6f,"NormalAttack","Skill2"),
                    Combo("Punish dash",BossMovesetDirector.PlayerMove.Dash,6,"Skill2","NormalAttack"),
                    Combo("Leap pressure",BossMovesetDirector.PlayerMove.Parry,7,"Skill3","NormalAttack")};
                var area=go.GetComponent<EnemyAreaDamage>();if(area==null)area=go.AddComponent<EnemyAreaDamage>();area.archetypeId="black-knight";area.profileOverride=null;area.fallbackProfile=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>("Assets/Combat/Balance/03_WildForest.asset");
                var audio=go.GetComponent<EnemyAudio>();if(audio==null)audio=go.AddComponent<EnemyAudio>();audio.profile=AudioProfile();
                PrefabUtility.SaveAsPrefabAsset(go,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        AssetDatabase.SaveAssets();Debug.Log("Black Knight assets ready: 12 FPS, event-driven prepared thrust/leap, two phases. Existing clips/controller/prefab preserved.");
    }
    static BossMovesetDirector.Combo Combo(string name,BossMovesetDirector.PlayerMove response,float range,params string[] skills)=>new BossMovesetDirector.Combo{name=name,responseTo=response,maxDistance=range,skills=skills};
    static AnimationEvent E(int frame,string function,string argument="")=>new AnimationEvent{time=frame/12f,functionName=function,stringParameter=argument};
    static Sprite[] Sprites(string sheet)=>AssetDatabase.LoadAllAssetsAtPath(Root+sheet).OfType<Sprite>().OrderBy(s=>int.Parse(s.name.Substring(s.name.LastIndexOf('_')+1))).ToArray();
    static AnimationClip Clip(string name,string sheet,bool loop,params AnimationEvent[] extra)
    {
        string path=Anim+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing!=null)return existing;
        var frames=Sprites(sheet);if(frames.Length==0)throw new InvalidOperationException("Missing sliced sprites: "+sheet);
        var clip=new AnimationClip{name=name,frameRate=12};
        var keys=new ObjectReferenceKeyframe[frames.Length+1];for(int i=0;i<keys.Length;i++)keys[i]=new ObjectReferenceKeyframe{time=i/12f,value=frames[Math.Min(i,frames.Length-1)]};
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        if(!loop){var events=new List<AnimationEvent>{E(0,"LockAction")};events.AddRange(extra);if(!extra.Any(e=>e.functionName=="DestroyCharacter"))events.Add(new AnimationEvent{time=(frames.Length-.1f)/12f,functionName="UnlockAction"});AnimationUtility.SetAnimationEvents(clip,events.OrderBy(e=>e.time).ToArray());}
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
    static void State(AnimatorController controller,AnimatorStateMachine sm,AnimatorState idle,string name,AnimationClip clip,bool returns,bool prepared)
    {
        controller.AddParameter(name,AnimatorControllerParameterType.Trigger);var state=sm.AddState(name);state.motion=clip;
        if(prepared){state.speedParameter="SpecialAttackSpeed";state.speedParameterActive=true;}
        var enter=sm.AddAnyStateTransition(state);enter.duration=0;enter.hasExitTime=false;enter.canTransitionToSelf=false;enter.AddCondition(AnimatorConditionMode.If,0,name);
        if(returns){var exit=state.AddTransition(idle);exit.hasExitTime=true;exit.exitTime=1;exit.duration=0;}
    }
    static void Profiles()
    {
        var paths=new[]{"01_AncientTrail","02_FairyForest","03_WildForest","04_Deadlands"};
        for(int i=0;i<paths.Length;i++)
        {
            var p=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>("Assets/Combat/Balance/"+paths[i]+".asset");if(p==null||p.Find("black-knight")!=null)continue;
            var entry=new EnemyDamageProfile.Enemy{archetypeId="black-knight",attack=new[]{30f,32,35,38}[i],trueDamage=i<2?2:3,criticalChance=0};
            entry.skills.Add(new EnemyDamageProfile.Skill{skillName="NormalAttack",damageMultiplier=1});entry.skills.Add(new EnemyDamageProfile.Skill{skillName="Skill2",damageMultiplier=1.3f});entry.skills.Add(new EnemyDamageProfile.Skill{skillName="Skill3",damageMultiplier=1.5f});p.enemies.Add(entry);EditorUtility.SetDirty(p);
        }
    }
    static EnemyAudioProfile AudioProfile()
    {
        const string path="Assets/Effects/Audio/Enemies/Profiles/BlackKnight.asset";var existing=AssetDatabase.LoadAssetAtPath<EnemyAudioProfile>(path);if(existing!=null)return existing;
        var source=AssetDatabase.LoadAssetAtPath<EnemyAudioProfile>("Assets/Effects/Audio/Enemies/Profiles/Orc.asset");if(source==null)return null;
        var profile=UnityEngine.Object.Instantiate(source);profile.name="BlackKnight";profile.pitch=.85f;AssetDatabase.CreateAsset(profile,path);return profile;
    }
}
