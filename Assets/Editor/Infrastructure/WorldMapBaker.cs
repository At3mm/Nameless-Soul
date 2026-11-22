using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using World;

public static class WorldMapBaker
{
    const int Width = 1024;
    const int Height = 768;
    static readonly string[] Scenes =
    {
        "Assets/Scenes/WastelandScene.unity",
        "Assets/Scenes/CaveScene.unity"
    };

    [MenuItem("Tools/Build/Bake World Map Images")]
    public static void Bake()
    {
        Directory.CreateDirectory("Assets/Resources/WorldMaps");
        foreach (var path in Scenes) BakeScene(path);
        AssetDatabase.SaveAssets();
    }

    public static void BakeAndQuit()
    {
        try { Bake(); EditorApplication.delayCall += () => EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.delayCall += () => EditorApplication.Exit(1); }
    }

    [MenuItem("Tools/Build/Bake World Map Checkpoint Labels")]
    public static void BakeCheckpointLabels()
    {
        foreach (var path in Scenes) BakeCheckpointLabels(path);
        AssetDatabase.SaveAssets();
    }

    static void BakeCheckpointLabels(string path)
    {
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        var previousActive = SceneManager.GetActiveScene();
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var definition = AssetDatabase.LoadAssetAtPath<WorldMapDefinition>("Assets/Resources/WorldMaps/" + name + "Map.asset");
            if (definition == null) throw new InvalidOperationException("Missing world map definition for " + path);
            SetCheckpointLabels(scene, definition);
            EditorUtility.SetDirty(definition);
        }
        finally
        {
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
        }
    }

    static void BakeScene(string path)
    {
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        var previousActive = SceneManager.GetActiveScene();
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        var hidden = new Dictionary<GameObject, bool>();
        GameObject cameraObject = null;
        RenderTexture target = null;
        Texture2D image = null;
        var previousTarget = RenderTexture.active;
        try
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var actor in root.GetComponentsInChildren<EnemyClassification>(true))
                    Hide(actor.gameObject, hidden);
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.CompareTag("Player")) Hide(transform.gameObject, hidden);
                foreach (var particles in root.GetComponentsInChildren<ParticleSystem>(true))
                    Hide(particles.gameObject, hidden);
                foreach (var tilemap in root.GetComponentsInChildren<Tilemap>(true))
                    tilemap.CompressBounds();
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            var renderers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy &&
                    (r is SpriteRenderer || r is TilemapRenderer) && (uiLayer < 0 || r.gameObject.layer != uiLayer))
                .ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible map renderers in " + path);

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            float padding = Mathf.Max(1f, Mathf.Max(bounds.size.x, bounds.size.y) * .04f);
            float aspect = (float)Width / Height;
            float halfHeight = Mathf.Max(bounds.extents.y + padding, (bounds.extents.x + padding) / aspect);
            var worldBounds = new Rect(bounds.center.x - halfHeight * aspect, bounds.center.y - halfHeight,
                halfHeight * 2f * aspect, halfHeight * 2f);

            cameraObject = new GameObject("World map bake camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = halfHeight;
            camera.aspect = aspect;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -100f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .045f, .04f, 1f);
            camera.cullingMask = uiLayer >= 0 ? ~(1 << uiLayer) : ~0;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.antiAliasing = 1;
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            string name = Path.GetFileNameWithoutExtension(path);
            string imagePath = "Assets/Resources/WorldMaps/" + name + "Map.png";
            File.WriteAllBytes(imagePath, image.EncodeToPNG());
            AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var definitionPath = "Assets/Resources/WorldMaps/" + name + "Map.asset";
            var definition = AssetDatabase.LoadAssetAtPath<WorldMapDefinition>(definitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WorldMapDefinition>();
                AssetDatabase.CreateAsset(definition, definitionPath);
            }
            definition.scenePath = path;
            definition.displayName = name == "WastelandScene" ? "RỪNG HOANG" : "HANG ĐỘNG";
            definition.image = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
            definition.worldBounds = worldBounds;
            SetCheckpointLabels(scene, definition);
            EditorUtility.SetDirty(definition);
            Debug.Log("Baked world map " + name + " with bounds " + worldBounds);
        }
        finally
        {
            RenderTexture.active = previousTarget;
            foreach (var entry in hidden) if (entry.Key != null) entry.Key.SetActive(entry.Value);
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
            if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
        }
    }

    static void SetCheckpointLabels(Scene scene, WorldMapDefinition definition)
    {
        string portraitPath = "Assets/UI/Packs/FantasyUI/Sprites/PlayerHUD/CompactHUD_Portrait.png";
        definition.playerMarker = AssetDatabase.LoadAllAssetsAtPath(portraitPath).OfType<Sprite>().FirstOrDefault();
        if (definition.playerMarker == null) throw new InvalidOperationException("Missing player map portrait at " + portraitPath);
        definition.checkpoints = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<CheckpointMenu>(true))
            .Where(menu => menu != null && !string.IsNullOrWhiteSpace(menu.checkpointName))
            .Select(menu => new WorldMapDefinition.CheckpointLocation
            {
                displayName = menu.checkpointName,
                worldPosition = menu.transform.position
            })
            .OrderBy(location => location.displayName, StringComparer.Ordinal)
            .ToArray();
        if (definition.checkpoints.Length == 0) throw new InvalidOperationException("No checkpoint locations found in " + scene.path);
    }

    static void Hide(GameObject go, Dictionary<GameObject, bool> hidden)
    {
        if (go == null || hidden.ContainsKey(go)) return;
        hidden.Add(go, go.activeSelf);
        go.SetActive(false);
    }
}
