using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>One-time import/configuration; rerunnable from the editor menu.</summary>
[InitializeOnLoad]
public static class WitchcraftMusicSetup
{
    private const string Config="Assets/Resources/DefaultGameMusic.asset";
    static WitchcraftMusicSetup(){EditorApplication.delayCall+=EnsureSetup;}
    [MenuItem("Tools/Audio/Configure Witchcraft Music")]
    public static void EnsureSetup()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=EnsureSetup;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode||AssetDatabase.LoadAssetAtPath<GameMusicSettings>(Config)!=null)return;
        try
        {
            var settings=ScriptableObject.CreateInstance<GameMusicSettings>();
            settings.menu=Import("Witchcraft");settings.exploration=Import("Hidden Path");settings.boss=Import("Ritual");
            Import("Forest");Import("Forgotten Words");
            if(settings.menu==null||settings.exploration==null||settings.boss==null){UnityEngine.Object.DestroyImmediate(settings);return;}
            AssetDatabase.CreateAsset(settings,Config);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Library/MusicSetup");File.WriteAllText("Library/MusicSetup/status.txt","PASS: Witchcraft music imported as stereo Vorbis streaming. Menu=Witchcraft; Exploration=Hidden Path; Boss=Ritual.");
            Debug.Log("[Music] Witchcraft configured: menu / exploration / boss.");
        }
        catch(Exception error){Debug.LogException(error);}
    }
    private static AudioClip Import(string name)
    {
        string path="Assets/Audio/Music/Witchcraft/"+name+".wav";
        var importer=AssetImporter.GetAtPath(path) as AudioImporter;
        if(importer==null)return null;
        var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.Streaming;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.75f;
        importer.defaultSampleSettings=sample;importer.forceToMono=false;importer.loadInBackground=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
