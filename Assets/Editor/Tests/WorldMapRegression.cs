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
            if (map == null || map.image == null || map.playerMarker == null ||
                string.IsNullOrWhiteSpace(map.displayName) || map.worldBounds.width <= 0 ||
                map.worldBounds.height <= 0 || map.checkpoints == null || map.checkpoints.Length == 0 ||
                map.areas == null || map.areas.Length == 0)
                throw new InvalidOperationException("Missing map image, bounds, marker, region name, checkpoint labels, or area labels for " + scenePath);
            foreach (var checkpoint in map.checkpoints)
                if (checkpoint == null || string.IsNullOrWhiteSpace(checkpoint.displayName) ||
                    !map.worldBounds.Contains(checkpoint.worldPosition))
                    throw new InvalidOperationException("Invalid checkpoint label data for " + scenePath);
            foreach (var area in map.areas)
                if (area == null || string.IsNullOrWhiteSpace(area.areaId) ||
                    string.IsNullOrWhiteSpace(area.displayName) || !map.worldBounds.Contains(area.worldPosition))
                    throw new InvalidOperationException("Invalid named area label data for " + scenePath);
            if (map.areas.Select(area => area.areaId).Distinct(StringComparer.Ordinal).Count() != map.areas.Length)
                throw new InvalidOperationException("Duplicate named area labels for " + scenePath);
            var size = new Vector2(100, 60);
            var lowerLeft = WorldMapOverlay.WorldToMapPosition(new Vector3(map.worldBounds.xMin, map.worldBounds.yMin), map.worldBounds, size);
            var center = WorldMapOverlay.WorldToMapPosition(map.worldBounds.center, map.worldBounds, size);
            var upperRight = WorldMapOverlay.WorldToMapPosition(new Vector3(map.worldBounds.xMax, map.worldBounds.yMax), map.worldBounds, size);
            if (Vector2.Distance(lowerLeft, new Vector2(-50, -30)) > .01f ||
                Vector2.Distance(center, Vector2.zero) > .01f ||
                Vector2.Distance(upperRight, new Vector2(50, 30)) > .01f)
                throw new InvalidOperationException("Player marker projection is incorrect for " + scenePath);
        }
        Debug.Log("PASS: Two scene maps include checkpoint and named area labels, the player portrait, valid bounds, and correct marker projection.");
    }
}
