using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class DarkCastleMapAnimationSetup
{
    public const string Root="Assets/Art/DarkCastle";
    public const string PalettePath=Root+"/Palettes/Cave_DarkCastle_Animated.prefab";
    private sealed class Sheet
    {
        public string name;public int width,height,columns,rows;public Sprite[] sprites;public Color32[] pixels;public int textureWidth;
        public Sprite Full(int row,int frame)=>sprites.First(s=>s.name==name+"_Frame_r"+row+"_f"+frame);
        public Sprite Cell(int row,int frame,int x,int y)=>width==16&&height==16?Full(row,frame):sprites.First(s=>s.name==name+"_Cell_r"+row+"_f"+frame+"_x"+x+"_y"+y);
    }
    [MenuItem("Tools/World/Create Missing Dark Castle Map Animations")]
    public static void CreateMissing()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi tạo animation.");
        Folder(Root+"/AnimationSources");Folder(Root+"/AnimatedTiles");Folder(Root+"/Prefabs/MapAnimations");
        var palette=new Dictionary<Vector3Int,TileBase>();int group=0;
        foreach(string variant in new[]{"","_Purple"})
        {
            var pool=Import("FG_Dark_Castle"+variant+"_Pool",48,48);
            Group(pool,1,Range(8),"EmptyPool"+variant,1,false,palette,group++);
            Group(pool,0,Range(8),"LavaPool"+variant,1,false,palette,group++);
            foreach(string number in new[]{"01","02"})
            {
                var torch=Import("FG_Dark_Castle"+variant+"_Torch"+number,16,32);
                Group(torch,0,Range(8),"Torch"+variant+number,1,false,palette,group++);
                var door=Import("FG_Dark_Castle"+variant+"_Door"+number,48,32);
                Group(door,1,Range(8),"Door"+variant+number,1,true,palette,group++,FullFrames(door,0,Range(8),1));
            }
        }
        foreach(string color in new[]{"Blue","Gold","Green","Red"})foreach(string number in new[]{"1","2"})
        {
            var crystal=Import("FG_Crystal_"+color+"_"+number,16,16);
            Group(crystal,0,Range(8),"Crystal_"+color+number,1,false,palette,group++);
        }
        foreach(string season in new[]{"Spring","Summer","Fall","Winter"})
        {
            var grass=Import("FG_Grass_"+season,16,16);
            Group(grass,0,Range(8),"Grass_"+season,1,false,palette,group++);
            var tree=Import("FG_Tree_"+season,64,80);
            // Alternating columns are mature/small variants, not successive growth frames.
            Group(tree,0,new[]{0,2},"Tree_"+season+"_Large",6,false,palette,group++);
            Group(tree,0,new[]{1,3},"Tree_"+season+"_Small",6,false,palette,group++);
        }
        var destroy=Import("FG_Grass_Destroy",32,32);
        Prop("Grass_Destroy",FullFrames(destroy,0,Range(6),1),32,null,false);
        CreatePalette(palette);
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("Dark Castle map animations ready: "+group+" groups, "+palette.Count+" animated cells. Characters and items untouched.");
    }
    private static int[] Range(int count)=>Enumerable.Range(0,count).ToArray();
    private static Sprite[] FullFrames(Sheet sheet,int row,int[] frames,int hold)=>frames.SelectMany(f=>Enumerable.Repeat(sheet.Full(row,f),hold)).ToArray();
    private static Sheet Import(string name,int width,int height)
    {
        string path=Root+"/AnimationSources/"+name+".png";
        if(!File.Exists(path)){File.Copy(Root+"/Source/Objects/"+name+".png",path);AssetDatabase.ImportAsset(path);}
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));
        var sheet=new Sheet{name=name,width=width,height=height,columns=texture.width/width,rows=texture.height/height,pixels=texture.GetPixels32(),textureWidth=texture.width};
        UnityEngine.Object.DestroyImmediate(texture);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        var existing=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        if(!existing.Any(s=>s.name==name+"_Frame_r0_f0"))
        {
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.spritePixelsPerUnit=16;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var rects=new List<SpriteRect>();
            for(int r=0;r<sheet.rows;r++)for(int f=0;f<sheet.columns;f++)
            {
                rects.Add(Rect(name+"_Frame_r"+r+"_f"+f,new Rect(f*width,r*height,width,height)));
                if(width==16&&height==16)continue;
                for(int y=0;y<height/16;y++)for(int x=0;x<width/16;x++)rects.Add(Rect(name+"_Cell_r"+r+"_f"+f+"_x"+x+"_y"+y,new Rect(f*width+x*16,r*height+y*16,16,16)));
            }
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));provider.Apply();importer.SaveAndReimport();
        }
        sheet.sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();return sheet;
    }
    private static SpriteRect Rect(string name,Rect rect)=>new SpriteRect{name=name,rect=rect,pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=GUID.Generate()};
    private static void Group(Sheet sheet,int row,int[] frames,string name,int hold,bool door,Dictionary<Vector3Int,TileBase> palette,int group,Sprite[] opening=null)
    {
        string folder=Root+"/AnimatedTiles/"+name;Folder(folder);
        AssetDatabase.StartAssetEditing();
        try
        {
            for(int y=0;y<sheet.height/16;y++)for(int x=0;x<sheet.width/16;x++)
            {
                bool visible=false;
                foreach(int f in frames)
                {
                    for(int dy=0;dy<16&&!visible;dy++)for(int dx=0;dx<16;dx++)if(sheet.pixels[(row*sheet.height+y*16+dy)*sheet.textureWidth+f*sheet.width+x*16+dx].a>0){visible=true;break;}
                    if(visible)break;
                }
                if(!visible)continue;
                string path=folder+"/"+name+"_x"+x+"_y"+y+".asset";
                var tile=AssetDatabase.LoadAssetAtPath<PixelMapAnimatedTile>(path);
                if(tile==null)
                {
                    int cellX=x,cellY=y;
                    tile=ScriptableObject.CreateInstance<PixelMapAnimatedTile>();tile.frames=frames.SelectMany(f=>Enumerable.Repeat(sheet.Cell(row,f,cellX,cellY),hold)).ToArray();
                    tile.colliderType=door?Tile.ColliderType.Grid:Tile.ColliderType.None;AssetDatabase.CreateAsset(tile,path);
                }
                palette[new Vector3Int(group%4*8+x,-group/4*9+y,0)]=tile;
            }
        }
        finally{AssetDatabase.StopAssetEditing();}
        Prop(name,FullFrames(sheet,row,frames,hold),sheet.height,opening,true);
    }
    private static void Prop(string name,Sprite[] frames,int height,Sprite[] opening,bool loop)
    {
        string path=Root+"/Prefabs/MapAnimations/"+name+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;
        var root=new GameObject(name);
        try
        {
            var child=new GameObject("Visual",typeof(SpriteRenderer));child.transform.SetParent(root.transform,false);child.transform.localPosition=new Vector3(0,height/32f,0);
            var renderer=child.GetComponent<SpriteRenderer>();renderer.sprite=frames[0];renderer.sortingLayerName="Characters";
            renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/PalettePreview_Unlit.mat");
            var animation=root.AddComponent<MapPropAnimation>();animation.visual=renderer;animation.frames=frames;animation.openingFrames=opening;animation.loop=loop;animation.deactivateOnComplete=!loop;
            if(opening!=null)
            {
                var collider=root.AddComponent<BoxCollider2D>();collider.size=new Vector2(1,2);collider.offset=new Vector2(0,1);animation.doorBlocker=collider;
                var interact=root.AddComponent<World.DoorInteractable>();interact.door=animation;interact.interactionPoint=renderer.transform;interact.interactionRadius=1.8f;interact.interactionLabel="Mở cửa";
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
    }
    private static void CreatePalette(Dictionary<Vector3Int,TileBase> cells)
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath)!=null)return;
        var root=new GameObject("Cave_DarkCastle_Animated",typeof(Grid));
        try
        {
            var child=new GameObject("Animated Map Tiles",typeof(Tilemap),typeof(TilemapRenderer));child.transform.SetParent(root.transform,false);
            var map=child.GetComponent<Tilemap>();map.animationFrameRate=PixelAnimationSettings.FPS;
            child.GetComponent<TilemapRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/PalettePreview_Unlit.mat");
            foreach(var pair in cells)map.SetTile(pair.Key,pair.Value);map.CompressBounds();PrefabUtility.SaveAsPrefabAsset(root,PalettePath);
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        var settings=ScriptableObject.CreateInstance("GridPalette");settings.name="Palette Settings";var field=settings.GetType().GetField("cellSizing");field.SetValue(settings,Enum.Parse(field.FieldType,"Manual"));AssetDatabase.AddObjectToAsset(settings,PalettePath);AssetDatabase.SaveAssetIfDirty(settings);AssetDatabase.ImportAsset(PalettePath);
    }
    private static void Folder(string path)
    {if(AssetDatabase.IsValidFolder(path))return;int index=path.LastIndexOf('/');Folder(path.Substring(0,index));AssetDatabase.CreateFolder(path.Substring(0,index),path.Substring(index+1));}
}
