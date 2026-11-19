using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using World;

/// <summary>Assign scene identities, keeping existing owners when a statue is duplicated.</summary>
[InitializeOnLoad]
public static class LifeSeedIdentityEditor
{
    private static readonly Dictionary<LifeSeedPickup, string> Known = new();
    private static readonly HashSet<int> AdoptScenes = new();
    private static bool scheduled;

    static LifeSeedIdentityEditor()
    {
        EditorApplication.hierarchyChanged += Schedule;
        Undo.undoRedoPerformed += Schedule;
        EditorSceneManager.sceneOpened += (scene, mode) => { AdoptScenes.Add(scene.handle); Schedule(); };
        EditorSceneManager.sceneSaving += (scene, path) => EnsureScene(scene, AdoptScenes.Remove(scene.handle));
        for (int i = 0; i < SceneManager.sceneCount; i++)
            AdoptScenes.Add(SceneManager.GetSceneAt(i).handle);
        Schedule();
    }

    public static void Schedule()
    {
        if (scheduled) return;
        scheduled = true;
        EditorApplication.delayCall += Process;
    }

    private static void Process()
    {
        scheduled = false;
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        var dead = new List<LifeSeedPickup>();
        foreach (var pair in Known) if (pair.Key == null) dead.Add(pair.Key);
        foreach (var item in dead) Known.Remove(item);
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            EnsureScene(scene, AdoptScenes.Remove(scene.handle));
        }
    }

    public static List<LifeSeedPickup> GetCheckpoints(Scene scene)
    {
        var result = new List<LifeSeedPickup>();
        if (!scene.IsValid() || !scene.isLoaded) return result;
        foreach (var root in scene.GetRootGameObjects())
            result.AddRange(root.GetComponentsInChildren<LifeSeedPickup>(true));
        return result;
    }

    public static void EnsureScene(Scene scene, bool adoptExisting = false)
    {
        if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene) ||
            PrefabStageUtility.GetCurrentPrefabStage()?.scene == scene ||
            EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        var checkpoints = GetCheckpoints(scene);
        // Prioritize an already tracked owner even if its duplicate is moved before it in the hierarchy.
        checkpoints.Sort((a, b) =>
        {
            bool aOwnsId = Known.TryGetValue(a, out var aId) && aId == a.pickupId;
            bool bOwnsId = Known.TryGetValue(b, out var bId) && bId == b.pickupId;
            return bOwnsId.CompareTo(aOwnsId);
        });
        var used = new HashSet<string>(StringComparer.Ordinal);
        bool changed = false;
        foreach (var checkpoint in checkpoints)
        {
            string id = checkpoint.pickupId;
            bool newPrefab = !adoptExisting && !Known.ContainsKey(checkpoint) &&
                PrefabUtility.IsPartOfPrefabInstance(checkpoint);
            if (string.IsNullOrWhiteSpace(id) || id == "checkpoint" || newPrefab || !used.Add(id))
            {
                Undo.RecordObject(checkpoint, "Assign life seed identity");
                do { id = "life-seed-" + Guid.NewGuid().ToString("N"); } while (!used.Add(id));
                checkpoint.pickupId = id;
                EditorUtility.SetDirty(checkpoint);
                PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint);
                changed = true;
            }
            Known[checkpoint] = id;
        }
        if (changed) EditorSceneManager.MarkSceneDirty(scene);
    }

}
