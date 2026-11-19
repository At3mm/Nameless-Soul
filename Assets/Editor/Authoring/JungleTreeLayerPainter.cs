using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Keep the original whole-tree palette tiles as the painting interface.
// Split sprites are an internal rendering detail of the two scene tilemaps.
[InitializeOnLoad]
public static class JungleTreeLayerPainter
{
    private const string Folder = "Assets/ElvGames/Jungle - Rogue Adventure/Animations/";
    private static readonly Dictionary<TileBase, int> TreeIds = new();
    private static readonly AnimatedTile[] Roots = new AnimatedTile[18];
    private static readonly AnimatedTile[] Canopies = new AnimatedTile[18];
    private static bool applying;
    private static bool loaded;

    static JungleTreeLayerPainter()
    {
        Tilemap.tilemapTileChanged += OnTilesChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        Undo.undoRedoPerformed += OnUndoRedo;
        EditorApplication.delayCall += RefreshOpenScene;
    }

    private static void LoadTiles()
    {
        if (loaded) return;
        TreeIds.Clear();
        for (int i = 0; i < 18; i++)
        {
            var original = AssetDatabase.LoadAssetAtPath<AnimatedTile>(Folder + $"Tree {i + 1}.asset");
            Roots[i] = AssetDatabase.LoadAssetAtPath<AnimatedTile>(Folder + $"TreeLayers/Tree {i + 1:D2} Root.asset");
            Canopies[i] = AssetDatabase.LoadAssetAtPath<AnimatedTile>(Folder + $"TreeLayers/Tree {i + 1:D2} Canopy.asset");
            if (original == null || Roots[i] == null || Canopies[i] == null) continue;
            TreeIds[original] = i;
            TreeIds[Roots[i]] = i;
        }
        loaded = true;
    }

    private static Tilemap FindCanopy(Tilemap roots)
    {
        if (roots == null || roots.name != "Trees_Roots" || roots.transform.parent == null)
            return null;
        foreach (var map in roots.transform.parent.GetComponentsInChildren<Tilemap>(true))
            if (map.name == "Trees_Canopy") return map;
        return null;
    }

    private static bool IsManagedCanopy(TileBase tile)
    {
        if (tile == null) return false;
        foreach (var canopy in Canopies)
            if (tile == canopy) return true;
        return false;
    }

    private static void OnTilesChanged(Tilemap roots, Tilemap.SyncTile[] changes)
    {
        if (applying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var canopy = FindCanopy(roots);
        if (canopy == null) return;
        var positions = new List<Vector3Int>(changes.Length);
        foreach (var change in changes) positions.Add(change.position);
        Apply(roots, canopy, positions, true);
    }

    private static void Apply(Tilemap roots, Tilemap canopy, IEnumerable<Vector3Int> positions, bool recordUndo)
    {
        LoadTiles();
        applying = true;
        bool changed = false;
        try
        {
            foreach (var pos in positions)
            {
                var current = roots.GetTile(pos);
                TileBase nextRoot = current;
                TileBase nextCanopy = canopy.GetTile(pos);
                if (current != null && TreeIds.TryGetValue(current, out int id))
                {
                    // Leave unrelated artwork alone rather than overwrite it.
                    if (nextCanopy != null && !IsManagedCanopy(nextCanopy)) continue;
                    nextRoot = Roots[id];
                    nextCanopy = Canopies[id];
                }
                else if (IsManagedCanopy(nextCanopy))
                {
                    nextCanopy = null;
                }
                if (nextRoot == current && nextCanopy == canopy.GetTile(pos)) continue;
                if (!changed && recordUndo)
                    Undo.RegisterCompleteObjectUndo(new Object[] { roots, canopy }, "Paint Jungle tree");
                if (nextRoot != current) roots.SetTile(pos, nextRoot);
                if (nextCanopy != canopy.GetTile(pos)) canopy.SetTile(pos, nextCanopy);
                changed = true;
            }
            if (changed)
            {
                EditorUtility.SetDirty(roots);
                EditorUtility.SetDirty(canopy);
                EditorSceneManager.MarkSceneDirty(roots.gameObject.scene);
                SceneView.RepaintAll();
            }
        }
        finally { applying = false; }
    }

    public static void RefreshOpenScene()
    {
        if (applying || EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var roots in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            if (!roots.gameObject.scene.IsValid() || !roots.gameObject.scene.isLoaded) continue;
            var canopy = FindCanopy(roots);
            if (canopy == null) continue;
            var positions = new HashSet<Vector3Int>();
            foreach (var pos in roots.cellBounds.allPositionsWithin)
                if (roots.HasTile(pos)) positions.Add(pos);
            LoadTiles();
            foreach (var pos in canopy.cellBounds.allPositionsWithin)
                if (IsManagedCanopy(canopy.GetTile(pos))) positions.Add(pos);
            Apply(roots, canopy, positions, false);
        }
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => RefreshOpenScene();
    private static void OnUndoRedo() => EditorApplication.delayCall += RefreshOpenScene;
}
