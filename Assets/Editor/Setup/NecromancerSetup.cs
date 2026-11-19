using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class NecromancerSetup
{
    const string Root="Assets/Sprites/Necromancer/";
    const string Anim=Root+"Animation/";
    const string Effects="Assets/Prefabs/Effects/Necromancer/";
    [MenuItem("Tools/Enemies/Create Missing Necromancer Assets")]
    public static void CreateMissing()
    {
        Directory.CreateDirectory(Anim);Directory.CreateDirectory(Root+"Effects");Directory.CreateDirectory(Effects);
        AssetDatabase.Refresh();
        var idle=Clip("NecromancerIdle","NecromancerIdle/Necromancer_Idle.png",true);
        var walk=Clip("NecromancerWalk","NecromancerWalk/Necromancer_Walk.png",true);
        var hurt=Clip("NecromancerHurt","NecromancerTakeDmg/Necromancer_Hurt.png",false,"",0);
        var death=Clip("NecromancerDeath","NecromancerDeath/Necromancer_DEATH.png",false,"DestroyCharacter",7);
        var slash=Clip("NecromancerAttack01","NecromancerAttack/Necromancer_Attack01.png",false,"ExecuteHit",5,"NormalAttack");
        var bolt=Clip("NecromancerAttack02","NecromancerAttack/Necromancer_Attack02.png",false,"SpawnSkillEffect",5,"HeavyAttack");
        var ritual=Clip("NecromancerSummon","NecromancerAttack/Necromancer_Summon.png",false,"SpawnSkillEffect",5,"Summon");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim+"Necromancer.controller");
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(Anim+"Necromancer.controller");
            controller.AddParameter("IsWalking",AnimatorControllerParameterType.Bool);
            var sm=controller.layers[0].stateMachine;
            var rest=sm.AddState("OrcIdle");rest.motion=idle;sm.defaultState=rest;
            var moving=sm.AddState("OrcWalk");moving.motion=walk;
            var a=rest.AddTransition(moving);a.hasExitTime=false;a.duration=0;a.AddCondition(AnimatorConditionMode.If,0,"IsWalking");
            a=moving.AddTransition(rest);a.hasExitTime=false;a.duration=0;a.AddCondition(AnimatorConditionMode.IfNot,0,"IsWalking");
            State(controller,sm,rest,"NormalAttack",slash,true);
            State(controller,sm,rest,"HeavyAttack",bolt,true);
            State(controller,sm,rest,"Summon",ritual,true);
            State(controller,sm,rest,"TakeDamage",hurt,true);
            State(controller,sm,rest,"Die",death,false);
            var broken=sm.AddState("Broken");broken.motion=idle;
        }
        var projectile=Effect("FallingBlast",true);
        var circle=Effect("RitualCircle",false);
        const string path="Assets/Prefabs/Characters/Necromancer.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null)
        {
            var go=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Characters/Orc.prefab");
            try
            {
                go.name="Necromancer";
                var old=go.GetComponent<EnemyCombat>();var obstacle=old.obstacleLayer;
                var combat=go.AddComponent<NecromancerCombat>();combat.obstacleLayer=obstacle;
                foreach(var duplicate in go.GetComponents<EnemyCombat>())
                    if(duplicate.GetType()==typeof(EnemyCombat))UnityEngine.Object.DestroyImmediate(duplicate);
                combat.skillManager=go.GetComponent<SkillDamageManager>();
                var point=new GameObject("CastPoint").transform;point.SetParent(go.transform,false);point.localPosition=new Vector3(.45f,.65f,0);
                combat.spells.Add(new NecromancerCombat.SpellBinding{skillName="HeavyAttack",prefab=projectile,castPoint=point});
                combat.spells.Add(new NecromancerCombat.SpellBinding{skillName="Summon",prefab=circle,castPoint=point});
                combat.skillManager.skills=new System.Collections.Generic.List<SkillData>{
                    new SkillData{skillName="NormalAttack",damageMultiplier=1,attackRange=.9f,hitboxOffset=new Vector2(.9f,0),cooldownTime=1.5f,knockbackForce=3},
                    new SkillData{skillName="HeavyAttack",damageMultiplier=1.2f,attackRange=.45f,cooldownTime=3,knockbackForce=2},
                    new SkillData{skillName="Summon",damageMultiplier=1.5f,attackRange=1.3f,cooldownTime=6,knockbackForce=4}};
                go.GetComponent<Animator>().runtimeAnimatorController=controller;
                var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=Sprites("NecromancerIdle/Necromancer_Idle.png")[0];renderer.color=Color.white;
                var classification=go.GetComponent<EnemyClassification>();classification.enemyType=EnemyClassification.EnemyType.Boss;classification.displayName="Necromancer";classification.persistentId="";go.tag="EnemyBoss";
                var stats=go.GetComponent<CharacterStats>();stats.maxHP=600;stats.currentHP=600;stats.ATK=20;
                var collider=go.GetComponent<CapsuleCollider2D>();
                if(collider!=null){collider.size=new Vector2(.85f,1.65f);collider.offset=new Vector2(-.05f,.25f);}
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("Necromancer assets ready. Existing clips, controller and prefabs were preserved.");
    }
    static Sprite[] Sprites(string path)=>AssetDatabase.LoadAllAssetsAtPath(Root+path).OfType<Sprite>().OrderBy(s=>int.Parse(s.name.Substring(s.name.LastIndexOf('_')+1))).ToArray();
    static AnimationClip Clip(string name,string sheet,bool loop,string action="",int frame=0,string argument="")
    {
        var path=Anim+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing!=null)return existing;
        var sprites=Sprites(sheet);if(sprites.Length==0)throw new InvalidOperationException("No sliced sprites: "+sheet);
        var clip=new AnimationClip{name=name,frameRate=12};
        var keys=new ObjectReferenceKeyframe[sprites.Length+1];
        for(int i=0;i<keys.Length;i++)keys[i]=new ObjectReferenceKeyframe{time=i/12f,value=sprites[Math.Min(i,sprites.Length-1)]};
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        if(!loop)
        {
            var events=new System.Collections.Generic.List<AnimationEvent>{new AnimationEvent{time=0,functionName="LockAction"}};
            if(!string.IsNullOrEmpty(action))events.Add(new AnimationEvent{time=frame/12f,functionName=action,stringParameter=argument});
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
    static NecromancerSpellEffect Effect(string name,bool projectile)
    {
        string path=Effects+name+".prefab";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(existing!=null)return existing.GetComponent<NecromancerSpellEffect>();
        string folder=Root+"Effects/"+(projectile?"Attack02":"Summon");
        var frames=AssetDatabase.FindAssets("t:Sprite",new[]{folder})
            .SelectMany(guid=>AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>())
            .Distinct().OrderBy(sprite=>int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_')+1))).ToArray();
        if(frames.Length==0)throw new InvalidOperationException("Add effect sprites before creating Necromancer assets: "+folder);
        var go=new GameObject(name);
        try
        {
            go.AddComponent<SpriteRenderer>().sprite=frames[0];
            var effect=go.AddComponent<NecromancerSpellEffect>();effect.frames=frames;
            effect.mode=NecromancerSpellEffect.Kind.TargetArea;
            effect.lifetime=frames.Length/12f;effect.activationDelay=(projectile?3:5)/12f;
            return PrefabUtility.SaveAsPrefabAsset(go,path).GetComponent<NecromancerSpellEffect>();
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
