using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using World;
using Object = UnityEngine.Object;

public static class CheckpointIdentityRegression
{
    [MenuItem("Tools/Diagnostics/Test Automatic Checkpoint Identities")]
    public static void RunMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run outside Play Mode.");
        var checks = new List<string>();
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Editor/CheckpointIdentityRegressionScene.unity");
        try
        {
            var original = new GameObject("Existing checkpoint").AddComponent<CheckpointInteractable>();
            original.checkpointId = "existing-save-id";
            CheckpointIdentityEditor.EnsureScene(scene, true);
            Check(original.checkpointId == "existing-save-id", "Preserve existing save IDs", checks);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/CheckpointStatue.prefab");
            var first = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<CheckpointInteractable>();
            var second = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<CheckpointInteractable>();
            CheckpointIdentityEditor.EnsureScene(scene);
            Check(first.checkpointId != second.checkpointId && first.checkpointId.StartsWith("checkpoint-") &&
                second.checkpointId.StartsWith("checkpoint-"), "New prefab instances get unique IDs", checks);

            string retained = first.checkpointId;
            var duplicate = Object.Instantiate(first.gameObject).GetComponent<CheckpointInteractable>();
            duplicate.transform.SetAsFirstSibling();
            duplicate.gameObject.SetActive(false);
            CheckpointIdentityEditor.EnsureScene(scene);
            Check(first.checkpointId == retained && duplicate.checkpointId != retained,
                "Inactive duplicate gets a new ID even before its original in the hierarchy", checks);

            first.name = "Renamed checkpoint";
            first.transform.position = new Vector3(37, -42, 0);
            CheckpointIdentityEditor.EnsureScene(scene);
            Check(first.checkpointId == retained, "Rename and move preserve save identity", checks);

            duplicate.checkpointId = retained;
            Check(CheckpointIdentityEditor.ValidateScene(scene).Count == 1, "Detect a merged duplicate ID", checks);
            bool rejected = false;
            try { new CheckpointIdentityBuildValidator().OnProcessScene(scene, null); }
            catch (BuildFailedException) { rejected = true; }
            Check(rejected, "Build validation rejects duplicate identities", checks);
            CheckpointIdentityEditor.EnsureScene(scene);
            Check(first.checkpointId == retained && duplicate.checkpointId != retained,
                "Repair collision without changing the established owner", checks);

            var empty = new GameObject("Empty ID").AddComponent<CheckpointInteractable>();
            Check(CheckpointIdentityEditor.ValidateScene(scene).Count == 1, "Detect an unassigned checkpoint", checks);
            CheckpointIdentityEditor.EnsureScene(scene);
            Check(!string.IsNullOrWhiteSpace(empty.checkpointId), "Assign IDs to newly added components", checks);

            var manual = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<CheckpointInteractable>();
            manual.name = "Manual checkpoint";
            manual.manualCheckpointId = true;
            manual.checkpointId = "jungle-entrance";
            CheckpointIdentityEditor.EnsureScene(scene);
            Check(manual.checkpointId == "jungle-entrance", "Preserve manual ID on new prefab instance", checks);
            var manualDuplicate = Object.Instantiate(manual.gameObject).GetComponent<CheckpointInteractable>();
            CheckpointIdentityEditor.EnsureScene(scene);
            Check(manualDuplicate.checkpointId == "jungle-entrance" && CheckpointIdentityEditor.ValidateScene(scene).Count == 1,
                "Report duplicate manual ID without rewriting it", checks);
            manualDuplicate.checkpointId = "jungle-exit";
            manualDuplicate.name = "Second manual checkpoint";
            CheckpointIdentityEditor.EnsureScene(scene);
            Check(manualDuplicate.checkpointId == "jungle-exit" && CheckpointIdentityEditor.ValidateScene(scene).Count == 0,
                "Accept corrected manual ID", checks);
            manualDuplicate.checkpointId = "";
            CheckpointIdentityEditor.EnsureScene(scene);
            Check(manualDuplicate.checkpointId == "" && CheckpointIdentityEditor.ValidateScene(scene).Count == 1,
                "Report empty manual ID without generating a replacement", checks);
            manualDuplicate.checkpointId = "jungle-exit";

            var ids = new Dictionary<string, string>();
            foreach (var item in CheckpointIdentityEditor.GetCheckpoints(scene)) ids[item.name] = item.checkpointId;
            Check(EditorSceneManager.SaveScene(scene, path), "Save generated identities", checks);
            EditorSceneManager.CloseScene(scene, true);
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            CheckpointIdentityEditor.EnsureScene(scene, true);
            foreach (var item in CheckpointIdentityEditor.GetCheckpoints(scene))
                Check(ids[item.name] == item.checkpointId, "Reload preserves " + item.name, checks);
            Check(CheckpointIdentityEditor.ValidateScene(scene).Count == 0, "Saved scene has no identity collisions", checks);
            return "PASS\n" + string.Join("\n", checks);
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void Check(bool condition, string description, List<string> checks)
    {
        if (!condition) throw new InvalidOperationException(description);
        checks.Add("PASS: " + description);
    }
}
