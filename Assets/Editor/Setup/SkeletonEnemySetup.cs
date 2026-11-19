using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Creates missing skeleton assets. Existing authored animation events and prefabs are preserved.</summary>
public static class SkeletonEnemySetup
{
    private sealed class Spec
    {
        public string folder,prefix,sheet,id,title;
        public bool elite;
        public int attacks;
        public float hp,stamina,defense,height;
        public Vector2 body,offset;
        public int[][] hitFrames;
        public SkillData[] skills;
    }
    public static readonly string[] Prefabs={"Assets/Prefabs/Characters/Skeleton.prefab","Assets/Prefabs/Characters/ArmorSkeleton.prefab","Assets/Prefabs/Characters/GreatswordSkeleton.prefab"};
    [MenuItem("Tools/Enemies/Create Missing Skeleton Assets")]
    public static void CreateMissing()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi tạo tài nguyên.");
        var specs=new[]{
            new Spec{folder="Skeleton",prefix="Skeleton",sheet="Skeleton",id="skeleton",title="Skeleton",attacks=2,hp=80,stamina=45,defense=2,height=.65f,body=new Vector2(.5f,.72f),offset=new Vector2(0,-.04f),hitFrames=new[]{new[]{3},new[]{3}},skills=new[]{Skill("NormalAttack",1,.65f,.5f,1.5f),Skill("HeavyAttack",1.25f,.8f,.55f,2.5f)}},
            new Spec{folder="Armor Skeleton",prefix="ArmorSkeleton",sheet="Armored Skeleton",id="armor-skeleton",title="Armor Skeleton",attacks=2,hp=140,stamina=75,defense=8,height=.9f,body=new Vector2(.6f,.95f),offset=new Vector2(0,.08f),hitFrames=new[]{new[]{4},new[]{2,6}},skills=new[]{Skill("NormalAttack",1.05f,1,.55f,1.8f),Skill("HeavyAttack",.65f,.8f,.15f,4)}},
            new Spec{folder="Greatsword Skeleton",prefix="GreatswordSkeleton",sheet="Greatsword Skeleton",id="greatsword-skeleton",title="Greatsword Skeleton",elite=true,attacks=3,hp=450,stamina=130,defense=10,height=1.65f,body=new Vector2(.85f,2),offset=new Vector2(.1f,.05f),hitFrames=new[]{new[]{5},new[]{7},new[]{4}},skills=new[]{Skill("NormalAttack",1,1.35f,1.1f,2.3f),Skill("HeavyAttack",1.45f,1.55f,1,4.5f),Skill("Skill3",1.2f,1.2f,2,3.5f)}}};
        foreach(var spec in specs)Build(spec);
        Debug.Log("Skeleton assets ready: two Normal enemies and one Elite; 12 FPS. Existing clips/controllers/prefabs preserved.");
    }
    private static SkillData Skill(string name,float multiplier,float radius,float forward,float cooldown)=>new SkillData{skillName=name,damageMultiplier=multiplier,attackRange=radius,hitboxOffset=new Vector2(forward,0),cooldownTime=cooldown,knockbackForce=3};
    private static string Root(Spec s)=>"Assets/Sprites/"+s.folder+"/";
    private static string Sheet(Spec s,string part,string suffix)=>Root(s)+s.prefix+part+"/"+s.sheet+"_"+suffix+".png";
    private static Sprite[] Sprites(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.rect.x).ToArray();
    private static void Build(Spec s)
    {
        PrepareSprites(s);
        string anim=Root(s)+"Animation/";
        var idle=Clip(anim+s.prefix+"Idle.anim",Sheet(s,"Idle","Idle"),true);
        var walk=Clip(anim+s.prefix+"Walk.anim",Sheet(s,"Walk","Walk"),true);
        var hurt=Clip(anim+s.prefix+"Hurt.anim",Sheet(s,"TakeDmg","Hurt"),false);
        var death=Clip(anim+s.prefix+"Death.anim",Sheet(s,"Death","Death"),false,true);
        var attacks=new AnimationClip[s.attacks];
        for(int i=0;i<s.attacks;i++)attacks[i]=Clip(anim+s.prefix+"Attack"+(i+1).ToString("00")+".anim",Sheet(s,"Attack","Attack"+(i+1).ToString("00")),false,false,s.skills[i].skillName,s.hitFrames[i]);
        string controllerPath=anim+s.prefix+".controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("IsWalking",AnimatorControllerParameterType.Bool);
            var sm=controller.layers[0].stateMachine;
            var rest=sm.AddState(s.prefix+"Idle");rest.motion=idle;sm.defaultState=rest;
            var moving=sm.AddState(s.prefix+"Walk");moving.motion=walk;
            var t=rest.AddTransition(moving);t.hasExitTime=false;t.duration=0;t.AddCondition(AnimatorConditionMode.If,0,"IsWalking");
            t=moving.AddTransition(rest);t.hasExitTime=false;t.duration=0;t.AddCondition(AnimatorConditionMode.IfNot,0,"IsWalking");
            for(int i=0;i<s.attacks;i++)State(controller,sm,rest,s.skills[i].skillName,attacks[i],true);
            State(controller,sm,rest,"TakeDamage",hurt,true);State(controller,sm,rest,"Die",death,false);
            sm.AddState("Broken").motion=hurt;
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        Profiles(s);
        string prefabPath="Assets/Prefabs/Characters/"+s.prefix+".prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)!=null)return;
        var go=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Characters/Orc.prefab");
        try
        {
            go.name=s.prefix;go.transform.localScale=Vector3.one;
            go.GetComponent<Animator>().runtimeAnimatorController=controller;
            var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=Sprites(Sheet(s,"Idle","Idle"))[0];renderer.color=Color.white;
            var c=go.GetComponent<EnemyClassification>();c.enemyType=s.elite?EnemyClassification.EnemyType.Elite:EnemyClassification.EnemyType.Normal;c.displayName=s.title;c.questBossId=s.id;c.persistentId="";c.preservePersistentId=false;
            go.tag=s.elite?"EnemyElite":"EnemyNormal";
            var stats=go.GetComponent<CharacterStats>();stats.maxHP=stats.currentHP=s.hp;stats.maxStamina=stats.currentStamina=s.stamina;stats.DEF=s.defense;stats.ATK=s.elite?28:s.id=="skeleton"?20:22;stats.STR=s.elite?1:0;stats.CRIT_Rate=0;
            var manager=go.GetComponent<SkillDamageManager>();manager.characterTransform=go.transform;manager.skills=s.skills.ToList();
            var combat=go.GetComponent<EnemyCombat>();combat.skillManager=manager;combat.lungeSpeed=s.elite?3.5f:2;combat.lungeDuration=.1f;
            var body=go.GetComponent<CapsuleCollider2D>();body.size=s.body;body.offset=s.offset;
            var ai=go.GetComponent<EnemyBehavior>();ai.patrolSpeed=s.elite?1.4f:1.5f;ai.chaseSpeed=s.elite?2.2f:2.5f;
            var broken=go.GetComponent<EnemyBreak>();broken.idleState=s.prefix+"Idle";broken.brokenState="Broken";broken.barHeight=s.height-.14f;
            foreach(var bar in go.GetComponentsInChildren<EnemyHealthBar>(true)){var serialized=new SerializedObject(bar);serialized.FindProperty("verticalOffset").floatValue=s.height;serialized.ApplyModifiedPropertiesWithoutUndo();PixelEnemyBarStyle.World(bar);bar.gameObject.SetActive(!s.elite);}
            var area=go.GetComponent<EnemyAreaDamage>();area.archetypeId=s.id;area.profileOverride=null;area.fallbackProfile=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>("Assets/Combat/Balance/01_AncientTrail.asset");
            var audio=go.GetComponent<EnemyAudio>();audio.profile=Audio(s);
            PrefabUtility.SaveAsPrefabAsset(go,prefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(go);}
    }
    private static void PrepareSprites(Spec s)
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root(s).TrimEnd('/')}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool fix=s.prefix=="ArmorSkeleton"&&path.EndsWith("_Attack02.png")&&Sprites(path).Length!=9;
            bool change=importer.filterMode!=FilterMode.Point||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed||fix;
            if(!change)continue;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            if(fix)
            {
                importer.spritePixelsPerUnit=25;importer.spriteImportMode=SpriteImportMode.Multiple;
#pragma warning disable 0618
                importer.spritesheet=Enumerable.Range(0,9).Select(i=>new SpriteMetaData{name="Armored Skeleton_Attack02_"+i,rect=new Rect(i*100,0,100,100),pivot=new Vector2(.5f,.5f),alignment=(int)SpriteAlignment.Center}).ToArray();
#pragma warning restore 0618
            }
            importer.SaveAndReimport();
        }
    }
    private static AnimationClip Clip(string path,string sheet,bool loop,bool death=false,string skill=null,int[] hitFrames=null)
    {
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(existing!=null)return existing;
        var frames=Sprites(sheet);if(frames.Length==0)throw new InvalidOperationException("Thiếu sprite: "+sheet);
        var clip=new AnimationClip{name=System.IO.Path.GetFileNameWithoutExtension(path),frameRate=12};
        var keys=frames.Select((sprite,i)=>new ObjectReferenceKeyframe{time=i/12f,value=sprite}).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.stopTime=frames.Length/12f;AnimationUtility.SetAnimationClipSettings(clip,settings);
        if(!loop)
        {
            var events=new List<AnimationEvent>{new AnimationEvent{time=0,functionName="LockAction"}};
            if(hitFrames!=null)foreach(int frame in hitFrames)events.Add(new AnimationEvent{time=frame/12f,functionName="ExecuteHit",stringParameter=skill});
            events.Add(new AnimationEvent{time=(frames.Length-.05f)/12f,functionName=death?"DestroyCharacter":"UnlockAction"});
            AnimationUtility.SetAnimationEvents(clip,events.OrderBy(e=>e.time).ToArray());
        }
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
    private static void State(AnimatorController controller,AnimatorStateMachine sm,AnimatorState idle,string name,AnimationClip clip,bool returns)
    {
        controller.AddParameter(name,AnimatorControllerParameterType.Trigger);var state=sm.AddState(name);state.motion=clip;
        var enter=sm.AddAnyStateTransition(state);enter.hasExitTime=false;enter.duration=0;enter.canTransitionToSelf=false;enter.AddCondition(AnimatorConditionMode.If,0,name);
        if(returns){var exit=state.AddTransition(idle);exit.hasExitTime=true;exit.exitTime=1;exit.duration=0;}
    }
    private static EnemyAudioProfile Audio(Spec s)
    {
        string path="Assets/Effects/Audio/Enemies/Profiles/"+s.prefix+".asset";var profile=AssetDatabase.LoadAssetAtPath<EnemyAudioProfile>(path);if(profile!=null)return profile;
        var source=AssetDatabase.LoadAssetAtPath<EnemyAudioProfile>("Assets/Effects/Audio/Enemies/Profiles/Orc.asset");if(source==null)return null;
        profile=UnityEngine.Object.Instantiate(source);profile.name=s.prefix;profile.pitch=s.elite?.85f:1.1f;AssetDatabase.CreateAsset(profile,path);return profile;
    }
    private static void Profiles(Spec s)
    {
        var paths=new[]{"01_AncientTrail","02_FairyForest","03_WildForest","04_Deadlands"};
        var attacks=s.elite?new float[]{28,32,37,43}:s.id=="skeleton"?new float[]{20,24,28,33}:new float[]{22,26,31,36};
        for(int i=0;i<paths.Length;i++)
        {
            var profile=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>("Assets/Combat/Balance/"+paths[i]+".asset");if(profile==null||profile.Find(s.id)!=null)continue;
            var entry=new EnemyDamageProfile.Enemy{archetypeId=s.id,attack=attacks[i],trueDamage=s.elite?(i<2?1:2):(i<2?0:1),criticalChance=0};
            foreach(var skill in s.skills)entry.skills.Add(new EnemyDamageProfile.Skill{skillName=skill.skillName,damageMultiplier=skill.damageMultiplier});
            profile.enemies.Add(entry);EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        }
    }
}
