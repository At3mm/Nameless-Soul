using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using World;

/// <summary>Converts complete painted door stamps only; validates every unrelated tile remains unchanged.</summary>
public static class CaveDoorSetup
{
    private const string PrefabFolder="Assets/Art/DarkCastle/Prefabs/MapAnimations/";
    private sealed class Stamp { public Tilemap map;public Vector3Int origin;public string variant; }
    private sealed class Cell { public Tilemap map;public Vector3Int pos;public TileBase tile;public Color color;public Matrix4x4 matrix; }
    [MenuItem("Tools/World/Enable Painted Cave Doors")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi cấu hình cửa.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/CaveScene.unity")throw new InvalidOperationException("Mở CaveScene trước khi chạy tool.");
        bool initiallyDirty=scene.isDirty;
        string backup="Library/DeveloperTools/CaveDoors/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
        File.Copy(scene.path,backup+"/CaveScene.before.unity");
        foreach(string variant in new[]{"Door01","Door02","Door_Purple01","Door_Purple02"})
        {
            string path=PrefabFolder+variant+".prefab";File.Copy(path,backup+"/"+variant+".before.prefab");
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var animation=root.GetComponent<MapPropAnimation>();
                var interaction=root.GetComponent<DoorInteractable>();if(interaction==null)interaction=root.AddComponent<DoorInteractable>();
                interaction.door=animation;interaction.interactionLabel="Mở cửa";interaction.interactionRadius=1.8f;interaction.interactionPoint=animation.visual.transform;
                var box=animation.doorBlocker as BoxCollider2D;
                if(box!=null){box.size=new Vector2(1,2);box.offset=new Vector2(0,1);box.isTrigger=false;}
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var maps=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)).ToArray();
        var stamps=new Dictionary<string,Stamp>();var pattern=new Regex(@"^(Door(?:_Purple)?0[12])_x([0-2])_y([01])$");
        foreach(var map in maps)foreach(var pos in map.cellBounds.allPositionsWithin)
        {
            var tile=map.GetTile(pos);if(tile==null)continue;var match=pattern.Match(tile.name);if(!match.Success)continue;
            var origin=pos-new Vector3Int(int.Parse(match.Groups[2].Value),int.Parse(match.Groups[3].Value),0);
            string key=map.GetInstanceID()+":"+origin;
            if(!stamps.ContainsKey(key))stamps.Add(key,new Stamp{map=map,origin=origin,variant=match.Groups[1].Value});
        }
        if(stamps.Count==0){MakeDoorHeadersPassable();Debug.Log("Door prefabs configured; no unconverted painted door stamps remain.");return;}
        var changes=new HashSet<string>();
        foreach(var stamp in stamps.Values)
        {
            for(int y=0;y<2;y++)for(int x=0;x<3;x++)
            {
                var pos=stamp.origin+new Vector3Int(x,y,0);var tile=stamp.map.GetTile(pos);
                if(tile==null||tile.name!=stamp.variant+"_x"+x+"_y"+y||stamp.map.GetTransformMatrix(pos)!=Matrix4x4.identity)
                    throw new InvalidOperationException("Cụm cửa chưa đủ 3×2 hoặc có xoay/lật riêng từng ô: "+stamp.origin);
                changes.Add(Key(stamp.map,pos));
            }
            var walls=maps.FirstOrDefault(m=>m.name=="Walls_Cliffs");
            if(walls!=null)for(int y=0;y<2;y++)
            {
                var pos=walls.WorldToCell(stamp.map.GetCellCenterWorld(stamp.origin+new Vector3Int(1,y,0)));
                if(walls.HasTile(pos))changes.Add(Key(walls,pos));
            }
        }
        var before=new List<Cell>();
        foreach(var map in maps)foreach(var pos in map.cellBounds.allPositionsWithin)if(map.HasTile(pos))before.Add(new Cell{map=map,pos=pos,tile=map.GetTile(pos),color=map.GetColor(pos),matrix=map.GetTransformMatrix(pos)});
        var created=new List<GameObject>();Undo.IncrementCurrentGroup();int undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Enable Cave doors");
        try
        {
            foreach(var map in maps.Where(m=>before.Any(c=>c.map==m&&changes.Contains(Key(m,c.pos)))))Undo.RegisterCompleteObjectUndo(map,"Replace painted doors");
            var parent=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Interactive Doors");
            if(parent==null){parent=new GameObject("Interactive Doors");Undo.RegisterCreatedObjectUndo(parent,"Door container");}
            int index=parent.transform.childCount;
            foreach(var stamp in stamps.Values.OrderBy(s=>s.origin.y).ThenBy(s=>s.origin.x))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder+stamp.variant+".prefab");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);Undo.RegisterCreatedObjectUndo(go,"Interactive Cave door");created.Add(go);
                go.name="CaveDoor_"+(++index).ToString("00");go.transform.SetParent(parent.transform,false);
                var x=stamp.map.CellToWorld(stamp.origin+Vector3Int.right)-stamp.map.CellToWorld(stamp.origin);
                var y=stamp.map.CellToWorld(stamp.origin+Vector3Int.up)-stamp.map.CellToWorld(stamp.origin);
                go.transform.localScale=new Vector3(x.magnitude,y.magnitude,1);go.transform.rotation=stamp.map.transform.rotation;
                var animation=go.GetComponent<MapPropAnimation>();
                Vector3 center=Vector3.zero;for(int cy=0;cy<2;cy++)for(int cx=0;cx<3;cx++)center+=stamp.map.GetCellCenterWorld(stamp.origin+new Vector3Int(cx,cy,0));center/=6;
                go.transform.position=center-go.transform.TransformVector(animation.visual.transform.localPosition);
                animation.visual.sortingLayerName="Characters";animation.visual.sortingOrder=0;animation.visual.spriteSortPoint=SpriteSortPoint.Center;animation.visual.color=stamp.map.color*stamp.map.GetColor(stamp.origin);
                PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(animation.visual);
                for(int cy=0;cy<2;cy++)for(int cx=0;cx<3;cx++)stamp.map.SetTile(stamp.origin+new Vector3Int(cx,cy,0),null);
                var walls=maps.FirstOrDefault(m=>m.name=="Walls_Cliffs");
                if(walls!=null)for(int cy=0;cy<2;cy++)walls.SetTile(walls.WorldToCell(stamp.map.GetCellCenterWorld(stamp.origin+new Vector3Int(1,cy,0))),null);
            }
            foreach(var cell in before)
            {
                bool removed=changes.Contains(Key(cell.map,cell.pos));
                if(removed?cell.map.HasTile(cell.pos):cell.map.GetTile(cell.pos)!=cell.tile||cell.map.GetColor(cell.pos)!=cell.color||cell.map.GetTransformMatrix(cell.pos)!=cell.matrix)
                    throw new InvalidOperationException("Unexpected tile change: "+cell.map.name+" "+cell.pos);
            }
            int after=maps.Sum(m=>m.GetTilesBlock(m.cellBounds).Count(t=>t!=null));
            if(after!=before.Count-changes.Count)throw new InvalidOperationException("Unexpected tile count after migration.");
            foreach(var map in maps){var collider=map.GetComponent<TilemapCollider2D>();if(collider!=null)collider.ProcessTilemapChanges();}
            Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);
            if(!initiallyDirty)EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);
            File.WriteAllText(backup+"/Report.txt","Converted doors: "+created.Count+"\nRemoved door/wall cells: "+changes.Count+"\nAll other "+(before.Count-changes.Count)+" tiles preserved (tile, color, transform).\nScene saved: "+!initiallyDirty,Encoding.UTF8);
            Selection.activeGameObject=created[0];EditorGUIUtility.PingObject(created[0]);
            Debug.Log("Cave doors ready: "+created.Count+" interactive doors; "+changes.Count+" door/passage cells changed; other tiles preserved. Backup: "+backup);
            MakeDoorHeadersPassable();
        }
        catch{Undo.RevertAllDownToGroup(undoGroup);throw;}
    }
    private static string Key(Tilemap map,Vector3Int pos)=>map.GetInstanceID()+":"+pos;

    [MenuItem("Tools/World/Make Cave Door Headers Passable")]
    public static void MakeDoorHeadersPassable()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi sửa collider cửa.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/CaveScene.unity")throw new InvalidOperationException("Mở CaveScene trước.");
        var maps=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Tilemap>(true)).ToArray();
        var walls=maps.First(m=>m.name=="Walls_Cliffs");
        var before=new List<Cell>();foreach(var map in maps)foreach(var pos in map.cellBounds.allPositionsWithin)if(map.HasTile(pos))before.Add(new Cell{map=map,pos=pos,tile=map.GetTile(pos),color=map.GetColor(pos),matrix=map.GetTransformMatrix(pos)});
        var positions=new HashSet<Vector3Int>();
        foreach(var door in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DoorInteractable>(true)))
        {
            var pos=walls.WorldToCell(door.transform.TransformPoint(new Vector3(0,2.5f,0)));
            var tile=walls.GetTile(pos) as Tile;
            if(tile==null||walls.GetColliderType(pos)==Tile.ColliderType.None)continue;
            if(!Regex.IsMatch(tile.name,@"^FG_Dark_Castle(?:_Purple)?_r13_c01$"))throw new InvalidOperationException("Header khác cấu trúc mặc định: "+pos);
            positions.Add(pos);
        }
        if(positions.Count==0)return;
        bool dirty=scene.isDirty;string backup="Library/DeveloperTools/CaveDoors/Header-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);File.Copy(scene.path,backup+"/CaveScene.before.unity");
        string folder="Assets/Art/DarkCastle/Tiles/DoorPassages";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Art/DarkCastle/Tiles","DoorPassages");
        Undo.RegisterCompleteObjectUndo(walls,"Preserve door header art without blocking passage");
        foreach(var pos in positions)
        {
            var original=(Tile)walls.GetTile(pos);string path=folder+"/Passable_"+original.name+".asset";var replacement=AssetDatabase.LoadAssetAtPath<Tile>(path);
            if(replacement==null){replacement=UnityEngine.Object.Instantiate(original);replacement.colliderType=Tile.ColliderType.None;AssetDatabase.CreateAsset(replacement,path);}
            var color=walls.GetColor(pos);var matrix=walls.GetTransformMatrix(pos);var flags=walls.GetTileFlags(pos);
            walls.SetTile(pos,replacement);walls.SetTileFlags(pos,TileFlags.None);walls.SetColor(pos,color);walls.SetTransformMatrix(pos,matrix);walls.SetTileFlags(pos,flags);
        }
        foreach(var cell in before)
        {
            bool header=cell.map==walls&&positions.Contains(cell.pos);
            if(!header&&cell.map.GetTile(cell.pos)!=cell.tile||cell.map.GetColor(cell.pos)!=cell.color||cell.map.GetTransformMatrix(cell.pos)!=cell.matrix)throw new InvalidOperationException("Unexpected header repair change: "+cell.pos);
            if(header&&((Tile)cell.map.GetTile(cell.pos)).sprite!=((Tile)cell.tile).sprite)throw new InvalidOperationException("Header art changed.");
        }
        walls.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);if(!dirty)EditorSceneManager.SaveScene(scene);
        File.WriteAllText(backup+"/Report.txt","Made "+positions.Count+" door header cells nonblocking. All tile visuals, colors and transforms preserved; other cells unchanged.",Encoding.UTF8);
        Debug.Log("Door headers passable: "+positions.Count+"; header artwork preserved. Backup: "+backup);
    }
}
