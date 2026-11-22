using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WindowsReleaseBuild
{
    public const string OutputPath="Builds/Windows/NamelessSoul.exe";
    public const string PlaytestOutputPath="Builds/Windows-Playtest/NamelessSoul-Playtest.exe";
    const string PlaytestProductName="Noname-Playtest";
    public static readonly string[] Scenes={"Assets/Scenes/MainMenu.unity","Assets/Scenes/WastelandScene.unity","Assets/Scenes/CaveScene.unity"};
    [MenuItem("Tools/Build/Validate Windows Release")]
    public static void ValidateMenu(){Debug.Log(Validate());}
    public static string Validate()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before release checks.");
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new InvalidOperationException("Windows Build Support is not installed.");
        var enabled=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        if(!enabled.SequenceEqual(Scenes))throw new InvalidOperationException("Expected MainMenu, WastelandScene, CaveScene in build order.");
        int objects=0,prefabs=0;
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));prefabs++;
            foreach(var t in asset.GetComponentsInChildren<Transform>(true))
            {
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Missing script in prefab "+asset.name+"/"+t.name);
                objects++;
            }
        }
        foreach(string path in Scenes)
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new InvalidOperationException("Missing script in "+path+"/"+t.name);
                    objects++;
                }
                if(path=="Assets/Scenes/CaveScene.unity")
                {
                    var walls=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true)).FirstOrDefault(m=>m.name=="Walls_Cliffs");
                    var collider=walls!=null?walls.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>():null;
                    if(walls==null||collider==null||!collider.enabled||!collider.gameObject.activeInHierarchy||collider.isTrigger)
                        throw new InvalidOperationException("CaveScene Walls_Cliffs must have an active solid TilemapCollider2D.");
                    foreach(var cell in walls.cellBounds.allPositionsWithin)
                        if(walls.GetTile(cell)!=null&&walls.GetColliderType(cell)==UnityEngine.Tilemaps.Tile.ColliderType.None)
                            throw new InvalidOperationException("CaveScene Walls_Cliffs has a tile without collision at "+cell+".");
                }
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        var final=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/BlackKnightBoss.prefab");
        if(final.GetComponent<FinalBossDamageBonus>()==null)throw new InvalidOperationException("Final boss bonus missing.");
        if(Resources.Load<BossDefeatCatalog>("BossDefeatCatalog")==null)throw new InvalidOperationException("Boss catalog missing.");
        return "PASS: three startup scenes, "+prefabs+" prefabs and "+objects+" objects; no missing scripts.";
    }
    [MenuItem("Tools/Build/Build Windows Release")]
    public static void Build()
    {
        BuildPlayer(OutputPath,null);
    }
    [MenuItem("Tools/Build/Build Windows Playtest")]
    public static void BuildPlaytest()
    {
        BuildPlayer(PlaytestOutputPath,PlaytestProductName);
    }
    static void BuildPlayer(string outputPath,string productNameOverride)
    {
        Validate();Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        string previousProductName=PlayerSettings.productName;
        try
        {
            if(productNameOverride!=null)
            {
                if(string.Equals(previousProductName,productNameOverride,StringComparison.Ordinal))
                    throw new InvalidOperationException("Playtest product name must differ from the production product name.");
                PlayerSettings.productName=productNameOverride;
            }
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=Scenes,locationPathName=outputPath,target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.CompressWithLz4|BuildOptions.DetailedBuildReport
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Windows build failed: "+report.summary.result);
            Debug.Log((productNameOverride==null?"Windows release built: ":"Windows playtest built: ")+Path.GetFullPath(outputPath));
        }
        finally
        {
            if(productNameOverride!=null)PlayerSettings.productName=previousProductName;
        }
    }
}
