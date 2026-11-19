using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class DarkCastleTilesetSetup
{
    public const string Root="Assets/Art/DarkCastle";
    private static readonly string[] Sheets={"FG_Dark_Castle","FG_Dark_Castle_Purple","FG_Extras","FG_Grounds"};
    [MenuItem("Tools/World/Create Missing Dark Castle Palettes")]
    public static void CreateMissing()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi import tileset.");
        AssetDatabase.Refresh();
        Folder(Root+"/Tiles");Folder(Root+"/Palettes");Folder(Root+"/Materials");
        var resliced=new HashSet<string>();
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Source"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            float ppu=path.Contains("(48x48)")||path.Contains("RPG Maker MV/")?48:path.Contains("(32x32)")||path.Contains("RPG Maker VX Ace/")?32:16;
            bool slice=path.StartsWith(Root+"/Source/Tilesets/");
            var existing=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            bool fresh=importer.spriteImportMode!=SpriteImportMode.Multiple || existing.Length==0 || existing.Any(s=>s.rect.width!=16 || s.rect.height!=16 || s.rect.x%16!=0 || s.rect.y%16!=0);
            bool changed=importer.textureType!=TextureImporterType.Sprite||importer.filterMode!=FilterMode.Point||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.spritePixelsPerUnit!=ppu;
            if(changed){importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=ppu;}
            if(slice&&fresh)
            {
                var texture=new Texture2D(2,2);
                try
                {
                    texture.LoadImage(File.ReadAllBytes(path));var pixels=texture.GetPixels32();var rects=new List<SpriteRect>();
                    for(int y=0;y+16<=texture.height;y+=16)for(int x=0;x+16<=texture.width;x+=16)
                    {
                        bool opaque=false;
                        for(int dy=0;dy<16&&!opaque;dy++)for(int dx=0;dx<16;dx++)if(pixels[(y+dy)*texture.width+x+dx].a>0){opaque=true;break;}
                        if(!opaque)continue;
                        rects.Add(new SpriteRect{name=Path.GetFileNameWithoutExtension(path)+"_r"+((texture.height-y)/16-1).ToString("00")+"_c"+(x/16).ToString("00"),rect=new Rect(x,y,16,16),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=GUID.Generate()});
                    }
                    importer.spriteImportMode=SpriteImportMode.Multiple;
                    var factories=new SpriteDataProviderFactories();factories.Init();
                    var provider=factories.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();provider.SetSpriteRects(rects.ToArray());
                    var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                    if(names!=null)names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
                    provider.Apply();
                    changed=true;
                    resliced.Add(Path.GetFileNameWithoutExtension(path));
                }
                finally{UnityEngine.Object.DestroyImmediate(texture);}
            }
            if(changed)importer.SaveAndReimport();
        }
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/PalettePreview_Unlit.mat");
        if(material==null)
        {
            var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if(shader!=null){material=new Material(shader);AssetDatabase.CreateAsset(material,Root+"/Materials/PalettePreview_Unlit.mat");}
        }
        foreach(var sheet in Sheets)Palette(sheet,material,resliced.Contains(sheet));
        Debug.Log("Dark Castle tiles ready: native 16x16 cells / 16 PPU, four palettes. CaveScene tilemaps preserved.");
    }
    private static void Palette(string sheet,Material material,bool refreshLayout)
    {
        var sprites=AssetDatabase.LoadAllAssetsAtPath(Root+"/Source/Tilesets/"+sheet+".png").OfType<Sprite>().ToArray();
        if(sprites.Length==0)throw new InvalidOperationException("Thiếu sprite tileset "+sheet);
        string tileFolder=Root+"/Tiles/"+sheet;Folder(tileFolder);
        var pairs=new List<KeyValuePair<Sprite,Tile>>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach(var sprite in sprites)
            {
                string path=tileFolder+"/"+sprite.name+".asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
                if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();tile.sprite=sprite;tile.colliderType=Tile.ColliderType.Grid;AssetDatabase.CreateAsset(tile,path);}
                pairs.Add(new KeyValuePair<Sprite,Tile>(sprite,tile));
            }
        }
        finally{AssetDatabase.StopAssetEditing();}
        string prefabPath=Root+"/Palettes/Cave_"+sheet+".prefab";
        bool exists=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)!=null;
        if(exists&&!refreshLayout)return;
        var root=exists?PrefabUtility.LoadPrefabContents(prefabPath):new GameObject("Cave_"+sheet,typeof(Grid));
        try
        {
            root.GetComponent<Grid>().cellSize=Vector3.one;
            var child=root.GetComponentInChildren<Tilemap>()!=null?root.GetComponentInChildren<Tilemap>().gameObject:new GameObject("Tiles",typeof(Tilemap),typeof(TilemapRenderer));child.transform.SetParent(root.transform,false);
            var map=child.GetComponent<Tilemap>();var renderer=child.GetComponent<TilemapRenderer>();if(material!=null)renderer.sharedMaterial=material;
            map.ClearAllTiles();
            foreach(var pair in pairs)map.SetTile(new Vector3Int(Mathf.RoundToInt(pair.Key.rect.x/16),Mathf.RoundToInt(pair.Key.rect.y/16),0),pair.Value);
            map.CompressBounds();PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
        }
        finally{if(exists)PrefabUtility.UnloadPrefabContents(root);else UnityEngine.Object.DestroyImmediate(root);}
        if(AssetDatabase.LoadAllAssetsAtPath(prefabPath).Any(a=>a.GetType().Name=="GridPalette"))return;
        var palette=ScriptableObject.CreateInstance("GridPalette");palette.name="Palette Settings";
        var field=palette.GetType().GetField("cellSizing");if(field!=null)field.SetValue(palette,Enum.Parse(field.FieldType,"Manual"));
        AssetDatabase.AddObjectToAsset(palette,prefabPath);AssetDatabase.SaveAssetIfDirty(palette);AssetDatabase.ImportAsset(prefabPath);
    }
    private static void Folder(string path)
    {if(AssetDatabase.IsValidFolder(path))return;int index=path.LastIndexOf('/');Folder(path.Substring(0,index));AssetDatabase.CreateFolder(path.Substring(0,index),path.Substring(index+1));}
}
