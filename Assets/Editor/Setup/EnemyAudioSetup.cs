using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class EnemyAudioSetup
{
    const string Audio="Assets/Effects/Audio/Enemies/";
    const string Profiles="Assets/Effects/Audio/Enemies/Profiles/";
    [MenuItem("Tools/Enemies/Create Missing Enemy Audio Profiles")]
    public static void CreateMissing()
    {
        Directory.CreateDirectory(Profiles);AssetDatabase.Refresh();
        var orc=Profile("Orc",.8f,.95f,new[]{"Monster/Monster-1.wav","Monster/Monster-2.wav"},new[]{"Creature2/grunt_06.ogg","Creature2/grunt_07.ogg"},new[]{"Creature2/die_01.ogg"},new[]{"Creature2/roar_04.ogg"});
        var rider=Profile("OrcRider",.9f,.85f,new[]{"Monster/Monster-3.wav","Monster/Monster-4.wav"},new[]{"Creature2/grunt_08.ogg"},new[]{"Creature2/die_04.ogg"},new[]{"Creature2/roar_05.ogg"});
        var slime=Profile("Slime",.75f,1,new[]{"Creature2/slime_01.ogg","Creature2/slime_03.ogg"},new[]{"Creature2/slime_02.ogg","Creature2/slime_04.ogg"},new[]{"Creature2/slime_08.ogg"},new[]{"Creature2/slime_05.ogg"});
        var wolf=Profile("Werewolf",.85f,1.05f,new[]{"Creature2/attack_02.ogg","Creature2/attack_03.ogg"},new[]{"Creature2/grunt_09.ogg"},new[]{"Creature2/die_02.ogg"},new[]{"Creature2/roar_06.ogg"});
        var bear=Profile("Werebear",.9f,.75f,new[]{"Creature2/roar_04.ogg","Creature2/attack_04.ogg"},new[]{"Creature2/grunt_08.ogg"},new[]{"Creature2/die_03.ogg"},new[]{"Creature2/roar_05.ogg"});
        var necro=Profile("Necromancer",.8f,.95f,new[]{"Creature2/weird_07.ogg"},new[]{"Creature2/weird_06.ogg"},new[]{"Creature2/die_04.ogg"},new[]{"Magic/magical_7.ogg"});
        var bat=Profile("NecromancerBat",.45f,1.1f,new[]{"Bat/bat_01.ogg"},new[]{"Bat/bat_02.ogg"},new[]{"Bat/bat_03.ogg"},new string[0]);
        Skill(necro,"HeavyAttack","Magic/magical_2.ogg");Skill(necro,"Summon","Magic/magical_6.ogg");
        Skill(wolf,"TeleportAttack","Magic/magical_5.ogg");Skill(rider,"Skill3","Creature2/roar_04.ogg");
        Bind("Orc",orc);Bind("EliteOrc",orc);Bind("OrcRider",rider);Bind("Slime",slime);Bind("Werewolf",wolf);Bind("Werebear",bear);Bind("Necromancer",necro);Bind("NecromancerBat",bat);
        foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{Audio}))
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            importer.forceToMono=true;
            var sample=importer.defaultSampleSettings;sample.preloadAudioData=true;sample.loadType=AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=sample;importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }
    static AudioClip[] Clips(string[] paths)=>paths.Select(path=>AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+path)).ToArray();
    static EnemyAudioProfile Profile(string name,float volume,float pitch,string[] attack,string[] hurt,string[] death,string[] phase)
    {
        string path=Profiles+name+".asset";var p=AssetDatabase.LoadAssetAtPath<EnemyAudioProfile>(path);if(p!=null)return p;
        p=ScriptableObject.CreateInstance<EnemyAudioProfile>();p.volume=volume;p.pitch=pitch;p.attacks=Clips(attack);p.hurt=Clips(hurt);p.death=Clips(death);p.phaseTransition=Clips(phase);AssetDatabase.CreateAsset(p,path);return p;
    }
    static void Skill(EnemyAudioProfile profile,string name,string clip)
    {
        if(profile.skills.Exists(item=>item.skillName==name))return;
        profile.skills.Add(new EnemyAudioProfile.SkillCue{skillName=name,clips=Clips(new[]{clip})});EditorUtility.SetDirty(profile);
    }
    static void Bind(string name,EnemyAudioProfile profile)
    {
        string path="Assets/Prefabs/Characters/"+name+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
        try{var emitter=go.GetComponent<EnemyAudio>();if(emitter==null)emitter=go.AddComponent<EnemyAudio>();if(emitter.profile==null)emitter.profile=profile;PrefabUtility.SaveAsPrefabAsset(go,path);}
        finally{PrefabUtility.UnloadPrefabContents(go);}
    }
}
