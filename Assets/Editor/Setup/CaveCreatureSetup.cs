using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Imports the supplied 100px sheets and creates missing assets without replacing authored clips.</summary>
public static class CaveCreatureSetup
{
    public sealed class Spec
    {
        public string folder,prefix,sheet,id,audio;
        public bool elite,boss;
        public int attacks;
        public float hp,stamina,defense,bar,ppu;
        public Vector2 body,offset;
        public int[] hits;
        public float[] attackByArea;
        public SkillData[] skills;
        public string[] lunges;
    }
    public static Spec[] Specs => new[]{
        new Spec{folder="Demon C",prefix="DemonC",sheet="Demon_C",id="demon-c",audio="Werewolf",attacks=2,ppu=25,hp=110,stamina=60,defense=4,bar=1.0f,body=new Vector2(.65f,.85f),offset=new Vector2(0,.04f),hits=new[]{3,3},attackByArea=new float[]{20,24,28,33},skills=new[]{Skill("NormalAttack",1,.85f,.65f,1.6f,15),Skill("HeavyAttack",1.3f,1.15f,.65f,3f,20)},lunges=new[]{"NormalAttack"}},
        new Spec{folder="Flame Golem",prefix="FlameGolem",sheet="Flame Golem",id="flame-golem",audio="Werebear",boss=true,attacks=3,ppu=10,hp=190,stamina=95,defense=9,bar=1.25f,body=new Vector2(2.375f,2.75f),offset=new Vector2(.05f,.375f),hits=new[]{6,6,5},attackByArea=new float[]{24,28,33,38},skills=new[]{Skill("NormalAttack",1,2.5f,1.625f,2.1f,15),Skill("HeavyAttack",1.35f,3.375f,1.375f,5f,30),Skill("Skill3",1.2f,3.125f,1.625f,7f,45)},lunges=new[]{"NormalAttack"}},
        new Spec{folder="Lava Slime",prefix="LavaSlime",sheet="Lava Slime",id="lava-slime",audio="Slime",attacks=2,ppu=25,hp=90,stamina=50,defense=3,bar=.7f,body=new Vector2(.65f,.43f),offset=new Vector2(-.05f,-.05f),hits=new[]{3,5},attackByArea=new float[]{18,22,27,31},skills=new[]{Skill("NormalAttack",1,.8f,.6f,1.5f,20),Skill("HeavyAttack",1.25f,1.3f,1.05f,3.5f,35)},lunges=new[]{"NormalAttack"}},
        new Spec{folder="Minotaur",prefix="Minotaur",sheet="Minotaur",id="minotaur",audio="OrcRider",elite=true,attacks=3,ppu=10,hp=600,stamina=150,defense=12,bar=1.75f,body=new Vector2(1.5f,1.9f),offset=new Vector2(.1f,.15f),hits=new[]{5,4,4},attackByArea=new float[]{30,35,41,48},skills=new[]{Skill("NormalAttack",1.15f,2.3f,1.8f,2.8f),Skill("HeavyAttack",1.25f,1.8f,2.1f,4f),Skill("Skill3",1.45f,2.5f,1.8f,5f)},lunges=new[]{"HeavyAttack"}}
    };
    static SkillData Skill(string id,float multiplier,float range,float offset,float cooldown,float burn=0) => new SkillData{skillName=id,damageMultiplier=multiplier,attackRange=range,hitboxOffset=new Vector2(offset,0),cooldownTime=cooldown,knockbackForce=3,burnBuildup=burn};
    static string Root(Spec s)=>"Assets/Sprites/"+s.folder+"/";
    static string Sheet(Spec s,string part,string suffix)=>Root(s)+s.prefix+part+"/"+s.sheet+"_"+suffix+".png";
    static Sprite[] Frames(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderByDescending(s=>s.rect.y).ThenBy(s=>s.rect.x).ToArray();
    [MenuItem("Tools/Enemies/Create Missing Cave Creature Assets")]
    public static void CreateMissing()
    {
        foreach(var spec in Specs){ImportSprites(spec.prefix);BuildEnemy(spec.prefix);}
        CreateSeedItem();
    }
    public static void ImportSprites(string prefix)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi import.");
        var spec=Specs.First(s=>s.prefix==prefix);
        foreach(var path in Directory.GetFiles(Root(spec),"*.png",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')))
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null){AssetDatabase.ImportAsset(path);importer=(TextureImporter)AssetImporter.GetAtPath(path);}
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            if(width%100!=0||height%100!=0)throw new InvalidOperationException("Không chia hết ô 100x100: "+path);
            if(importer.textureType==TextureImporterType.Sprite&&importer.spriteImportMode==SpriteImportMode.Multiple&&importer.spritePixelsPerUnit==spec.ppu&&importer.filterMode==FilterMode.Point&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed&&Frames(path).Length==width/100*(height/100))continue;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=spec.ppu;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;
            var cells=new List<SpriteMetaData>();int index=0;
            for(int y=height-100;y>=0;y-=100)for(int x=0;x<width;x+=100)
                cells.Add(new SpriteMetaData{name=Path.GetFileNameWithoutExtension(path)+"_"+index++,rect=new Rect(x,y,100,100),pivot=new Vector2(.5f,.5f),alignment=(int)SpriteAlignment.Center});
#pragma warning disable 0618
            importer.spritesheet=cells.ToArray();
#pragma warning restore 0618
            importer.SaveAndReimport();
        }
    }
    public static void BuildEnemy(string prefix)
    {
        var s=Specs.First(x=>x.prefix==prefix);string anim=Root(s)+"Animation/";
        var idle=Clip(anim+s.prefix+"Idle.anim",Sheet(s,"Idle","Idle"),true);
        var walk=Clip(anim+s.prefix+"Walk.anim",Sheet(s,"Walk","Walk"),true);
        var hurt=Clip(anim+s.prefix+"Hurt.anim",Sheet(s,"TakeDmg","Hurt"),false);
        var death=Clip(anim+s.prefix+"Death.anim",Sheet(s,"Death","Death"),false,true);
        var attacks=Enumerable.Range(0,s.attacks).Select(i=>Clip(anim+s.prefix+"Attack"+(i+1).ToString("00")+".anim",Sheet(s,"Attack","Attack"+(i+1).ToString("00")),false,false,s.skills[i].skillName,s.hits[i])).ToArray();
        if(s.prefix=="LavaSlime")Clip(anim+"LavaSlimeSpike.anim",Sheet(s,"Effects","Spike"),false,false,null,-1,false);
        string controllerPath=anim+s.prefix+".controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("IsWalking",AnimatorControllerParameterType.Bool);
            var sm=controller.layers[0].stateMachine;var rest=sm.AddState(s.prefix+"Idle");rest.motion=idle;sm.defaultState=rest;
            var move=sm.AddState(s.prefix+"Walk");move.motion=walk;
            var t=rest.AddTransition(move);t.hasExitTime=false;t.duration=0;t.AddCondition(AnimatorConditionMode.If,0,"IsWalking");
            t=move.AddTransition(rest);t.hasExitTime=false;t.duration=0;t.AddCondition(AnimatorConditionMode.IfNot,0,"IsWalking");
            for(int i=0;i<s.attacks;i++)State(controller,sm,rest,s.skills[i].skillName,attacks[i],true);
            State(controller,sm,rest,"TakeDamage",hurt,true);State(controller,sm,rest,"Die",death,false);sm.AddState("Broken").motion=hurt;
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        Balance(s);
        string prefab="Assets/Prefabs/Characters/"+s.prefix+".prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(prefab)!=null)return;
        var go=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Characters/Orc.prefab");
        try
        {
            go.name=s.prefix;go.transform.localScale=Vector3.one;go.GetComponent<Animator>().runtimeAnimatorController=controller;
            var visual=go.GetComponent<SpriteRenderer>();visual.sprite=Frames(Sheet(s,"Idle","Idle"))[0];visual.color=Color.white;
            var classification=go.GetComponent<EnemyClassification>();classification.enemyType=s.boss?EnemyClassification.EnemyType.Boss:s.elite?EnemyClassification.EnemyType.Elite:EnemyClassification.EnemyType.Normal;classification.displayName=s.prefix=="DemonC"?"Demon C":s.folder;classification.questBossId=s.id;classification.persistentId="";classification.preservePersistentId=false;go.tag=s.boss?"EnemyBoss":s.elite?"EnemyElite":"EnemyNormal";
            var stats=go.GetComponent<CharacterStats>();stats.maxHP=stats.currentHP=s.hp;stats.maxStamina=stats.currentStamina=s.stamina;stats.DEF=s.defense;stats.ATK=s.attackByArea[3];stats.STR=s.elite?1:0;stats.CRIT_Rate=0;
            var manager=go.GetComponent<SkillDamageManager>();manager.characterTransform=go.transform;manager.skills=s.skills.ToList();
            var combat=go.GetComponent<EnemyCombat>();combat.skillManager=manager;combat.restrictLungeSkills=true;combat.lungeSkillNames=s.lunges.ToList();combat.lungeSpeed=s.elite?4:2;combat.lungeDuration=.1f;
            var body=go.GetComponent<CapsuleCollider2D>();body.size=s.body;body.offset=s.offset;
            var ai=go.GetComponent<EnemyBehavior>();ai.patrolSpeed=s.prefix=="FlameGolem"?1:s.elite?1.4f:1.6f;ai.chaseSpeed=s.prefix=="FlameGolem"?1.8f:s.elite?2.5f:2.8f;ai.maxAutoObstacleRadius=s.elite?.55f:.32f;
            var broken=go.GetComponent<EnemyBreak>();broken.idleState=s.prefix+"Idle";broken.brokenState="Broken";broken.barHeight=s.bar-.14f;
            foreach(var bar in go.GetComponentsInChildren<EnemyHealthBar>(true)){var serialized=new SerializedObject(bar);serialized.FindProperty("verticalOffset").floatValue=s.bar;serialized.ApplyModifiedPropertiesWithoutUndo();PixelEnemyBarStyle.World(bar);bar.gameObject.SetActive(!s.elite);}
            var area=go.GetComponent<EnemyAreaDamage>();area.archetypeId=s.id;area.profileOverride=null;area.fallbackProfile=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>("Assets/Combat/Balance/04_Deadlands.asset");
            go.GetComponent<EnemyAudio>().profile=Audio(s);
            if(s.prefix=="LavaSlime"||s.prefix=="FlameGolem")
            {
                if(go.GetComponent<LavaAffinity>()==null)go.AddComponent<LavaAffinity>();
                var wading=go.GetComponent<PlayerWaterRipples>()??go.AddComponent<PlayerWaterRipples>();
                wading.lavaSubmergedFraction=s.prefix=="LavaSlime"?.48f:.4f;
            }
            PrefabUtility.SaveAsPrefabAsset(go,prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(go);}
    }
    static AnimationClip Clip(string path,string sheet,bool loop,bool death=false,string skill=null,int hit=-1,bool action=true)
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip!=null)return clip;
        var frames=Frames(sheet);if(frames.Length==0)throw new InvalidOperationException("Thiếu sprite: "+sheet);
        clip=new AnimationClip{name=Path.GetFileNameWithoutExtension(path),frameRate=12};
        var keys=frames.Select((s,i)=>new ObjectReferenceKeyframe{time=i/12f,value=s}).ToList();
        AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys.ToArray());
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
        if(!loop&&action)
        {
            var events=new List<AnimationEvent>{new AnimationEvent{time=0,functionName="LockAction"}};
            if(hit>=0){if(hit>=frames.Length)throw new InvalidOperationException("Hit frame ngoài clip: "+path);events.Add(new AnimationEvent{time=hit/12f,functionName="ExecuteHit",stringParameter=skill});}
            events.Add(new AnimationEvent{time=(frames.Length-.05f)/12f,functionName=death?"DestroyCharacter":"UnlockAction"});AnimationUtility.SetAnimationEvents(clip,events.ToArray());
        }
        AssetDatabase.CreateAsset(clip,path);return clip;
    }
    static void State(AnimatorController controller,AnimatorStateMachine sm,AnimatorState idle,string name,AnimationClip clip,bool returns)
    {
        controller.AddParameter(name,AnimatorControllerParameterType.Trigger);var state=sm.AddState(name);state.motion=clip;
        var enter=sm.AddAnyStateTransition(state);enter.hasExitTime=false;enter.duration=0;enter.canTransitionToSelf=false;enter.AddCondition(AnimatorConditionMode.If,0,name);
        if(returns){var exit=state.AddTransition(idle);exit.hasExitTime=true;exit.exitTime=1;exit.duration=0;}
    }
    static EnemyAudioProfile Audio(Spec s)
    {
        string path="Assets/Effects/Audio/Enemies/Profiles/"+s.prefix+".asset";var profile=AssetDatabase.LoadAssetAtPath<EnemyAudioProfile>(path);if(profile!=null)return profile;
        profile=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyAudioProfile>("Assets/Effects/Audio/Enemies/Profiles/"+s.audio+".asset"));profile.name=s.prefix;profile.pitch=s.elite?.8f:s.prefix=="FlameGolem"?.75f:1;AssetDatabase.CreateAsset(profile,path);return profile;
    }
    static void Balance(Spec s)
    {
        var profiles=new[]{"01_AncientTrail","02_FairyForest","03_WildForest","04_Deadlands"};
        for(int i=0;i<profiles.Length;i++)
        {
            var profile=AssetDatabase.LoadAssetAtPath<EnemyDamageProfile>("Assets/Combat/Balance/"+profiles[i]+".asset");if(profile==null||profile.Find(s.id)!=null)continue;
            profile.enemies.Add(new EnemyDamageProfile.Enemy{archetypeId=s.id,attack=s.attackByArea[i],trueDamage=s.elite?2:i>1?1:0,criticalChance=0,skills=s.skills.Select(skill=>new EnemyDamageProfile.Skill{skillName=skill.skillName,damageMultiplier=skill.damageMultiplier}).ToList()});EditorUtility.SetDirty(profile);AssetDatabase.SaveAssetIfDirty(profile);
        }
    }
    public static void CreateSeedItem()
    {
        const string path="Assets/Resources/Items/LifeSeed.asset";
        if(GameItemDefinition.FindItem(LifeSeedItem.Id)!=null)return;
        var seed=ScriptableObject.CreateInstance<ItemDefinition>();seed.itemId=LifeSeedItem.Id;seed.displayName="Hạt sinh mệnh";seed.description="Tự nhận khi hạ Elite hoặc Boss. Hai lần tăng đầu cần 1 hạt/lần, các lần sau cần 2 hạt/lần tại Tượng thần → Hiến tế vật phẩm để tăng vĩnh viễn sức chứa bình máu thêm 1, từ 3 lên tối đa 10. Không dùng trực tiếp hoặc gắn vào ô nhanh.";seed.icon=Resources.Load<Sprite>("LifeSeedIcon");seed.category=ItemCategory.Support;seed.rarity=ItemRarity.Rare;seed.consumable=false;seed.maxStack=int.MaxValue;seed.buyable=seed.sellable=false;AssetDatabase.CreateAsset(seed,path);
    }
}
