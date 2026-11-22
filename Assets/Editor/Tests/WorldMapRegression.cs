using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using World;

public static class WorldMapRegression
{
    [MenuItem("Tools/Diagnostics/Test World Maps")]
    public static void Run()
    {
        var maps = Resources.LoadAll<WorldMapDefinition>("WorldMaps");
        if (maps.Length != 2) throw new InvalidOperationException("Expected one baked map for each gameplay scene.");
        var expectedScenes = new[] { "Assets/Scenes/CaveScene.unity", "Assets/Scenes/WastelandScene.unity" };
        foreach (var scenePath in expectedScenes)
        {
            var map = maps.SingleOrDefault(entry => entry.scenePath == scenePath);
            if (map == null || map.image == null || map.worldBounds.width <= 0 || map.worldBounds.height <= 0)
                throw new InvalidOperationException("Missing image or world bounds for " + scenePath);
            var size = new Vector2(100, 60);
            var lowerLeft = WorldMapOverlay.WorldToMapPosition(new Vector3(map.worldBounds.xMin, map.worldBounds.yMin), map.worldBounds, size);
            var center = WorldMapOverlay.WorldToMapPosition(map.worldBounds.center, map.worldBounds, size);
            var upperRight = WorldMapOverlay.WorldToMapPosition(new Vector3(map.worldBounds.xMax, map.worldBounds.yMax), map.worldBounds, size);
            if (Vector2.Distance(lowerLeft, new Vector2(-50, -30)) > .01f ||
                Vector2.Distance(center, Vector2.zero) > .01f ||
                Vector2.Distance(upperRight, new Vector2(50, 30)) > .01f)
                throw new InvalidOperationException("Player marker projection is incorrect for " + scenePath);
        }
        Debug.Log("PASS: Two scene maps are baked with valid bounds and player marker projection.");
    }
}
