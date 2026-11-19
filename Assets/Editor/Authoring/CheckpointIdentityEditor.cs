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
public static class CheckpointIdentityEditor
{
    private static readonly Dictionary<CheckpointInteractable, string> Known = new();
    private static readonly HashSet<int> AdoptScenes = new();
    private static bool scheduled;

    static CheckpointIdentityEditor()
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
        var dead = new List<CheckpointInteractable>();
        foreach (var pair in Known) if (pair.Key == null) dead.Add(pair.Key);
        foreach (var item in dead) Known.Remove(item);
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            EnsureScene(scene, AdoptScenes.Remove(scene.handle));
        }
    }

    public static List<CheckpointInteractable> GetCheckpoints(Scene scene)
    {
        var result = new List<CheckpointInteractable>();
        if (!scene.IsValid() || !scene.isLoaded) return result;
        foreach (var root in scene.GetRootGameObjects())
            result.AddRange(root.GetComponentsInChildren<CheckpointInteractable>(true));
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
            int manualOrder = b.manualCheckpointId.CompareTo(a.manualCheckpointId);
            if (manualOrder != 0) return manualOrder;
            bool aOwnsId = Known.TryGetValue(a, out var aId) && aId == a.checkpointId;
            bool bOwnsId = Known.TryGetValue(b, out var bId) && bId == b.checkpointId;
            return bOwnsId.CompareTo(aOwnsId);
        });
        var used = new HashSet<string>(StringComparer.Ordinal);
        bool changed = false;
        foreach (var checkpoint in checkpoints)
        {
            string id = checkpoint.checkpointId;
            if (checkpoint.manualCheckpointId)
            {
                if (!string.IsNullOrWhiteSpace(id)) used.Add(id);
                Known[checkpoint] = id;
                continue;
            }
            bool newPrefab = !adoptExisting && !Known.ContainsKey(checkpoint) &&
                PrefabUtility.IsPartOfPrefabInstance(checkpoint);
            if (string.IsNullOrWhiteSpace(id) || id == "checkpoint" || newPrefab || !used.Add(id))
            {
                Undo.RecordObject(checkpoint, "Assign checkpoint identity");
                do { id = "checkpoint-" + Guid.NewGuid().ToString("N"); } while (!used.Add(id));
                checkpoint.checkpointId = id;
                EditorUtility.SetDirty(checkpoint);
                PrefabUtility.RecordPrefabInstancePropertyModifications(checkpoint);
                changed = true;
            }
            Known[checkpoint] = id;
        }
        if (changed) EditorSceneManager.MarkSceneDirty(scene);
    }

    public static List<string> ValidateScene(Scene scene)
    {
        var issues = new List<string>();
        var owners = new Dictionary<string, CheckpointInteractable>(StringComparer.Ordinal);
        foreach (var checkpoint in GetCheckpoints(scene))
        {
            string location = GetPath(checkpoint.transform);
            string id = checkpoint.checkpointId;
            if (string.IsNullOrWhiteSpace(id) || id == "checkpoint")
                issues.Add($"{scene.path}: '{location}' has no unique checkpoint ID.");
            else if (owners.TryGetValue(id, out var owner))
                issues.Add($"{scene.path}: '{location}' and '{GetPath(owner.transform)}' share checkpoint ID '{id}'.");
            else owners.Add(id, checkpoint);
        }
        return issues;
    }

    private static string GetPath(Transform item) => item.parent == null ? item.name : GetPath(item.parent) + "/" + item.name;
}

[CustomEditor(typeof(CheckpointInteractable)), CanEditMultipleObjects]
public sealed class CheckpointInteractableInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "checkpointId", "manualCheckpointId");
        var manual = serializedObject.FindProperty("manualCheckpointId");
        EditorGUILayout.PropertyField(manual, new GUIContent("ID thủ công"));
        using (new EditorGUI.DisabledScope(!manual.boolValue || manual.hasMultipleDifferentValues))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("checkpointId"));
        if (serializedObject.ApplyModifiedProperties()) CheckpointIdentityEditor.Schedule();
        if (manual.boolValue && !manual.hasMultipleDifferentValues)
        {
            EditorGUILayout.HelpBox("ID phải duy nhất trong scene. Đổi ID của tượng đã dùng sẽ khiến save cũ không nhận ra tượng này.", MessageType.Info);
            foreach (var selected in targets)
            {
                var checkpoint = (CheckpointInteractable)selected;
                if (string.IsNullOrWhiteSpace(checkpoint.checkpointId) || checkpoint.checkpointId == "checkpoint")
                    EditorGUILayout.HelpBox("Hãy nhập ID checkpoint hợp lệ.", MessageType.Error);
                else
                {
                    foreach (var other in CheckpointIdentityEditor.GetCheckpoints(checkpoint.gameObject.scene))
                        if (other != checkpoint && other.checkpointId == checkpoint.checkpointId)
                        { EditorGUILayout.HelpBox("ID đang trùng với checkpoint: " + other.name, MessageType.Error); break; }
                }
            }
        }
    }
}

public sealed class CheckpointIdentityBuildValidator : IProcessSceneWithReport
{
    public int callbackOrder => 10;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // This callback also runs when entering Play Mode. Validate there as well, without rewriting IDs.
        var issues = CheckpointIdentityEditor.ValidateScene(scene);
        if (issues.Count > 0) throw new BuildFailedException(string.Join("\n", issues));
    }
}
