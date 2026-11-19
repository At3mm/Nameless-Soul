using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class PixelMenuButtonSetup
{
    [MenuItem("Tools/UI/Create Shared Pixel Menu Button Assets")]
    public static void Apply()
    {
        const string imagePath="Assets/Art/UI/Buttons/PixelMenuButton.png";
        const string stylePath="Assets/Resources/UI/PixelMenuButtonStyle.asset";
        const string prefabPath="Assets/Prefabs/UI/PixelMenuButton.prefab";
        Directory.CreateDirectory("Assets/Art/UI/Buttons");Directory.CreateDirectory("Assets/Resources/UI");Directory.CreateDirectory("Assets/Prefabs/UI");
        if(!File.Exists(imagePath))
        {
            const int w=64,h=24;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                int edge=Mathf.Min(y,h-1-y),cut=Mathf.Max(0,4-edge);
                if(x<cut||x>=w-cut){pixels[y*w+x]=Color.clear;continue;}
                bool border=edge<2||x<=cut+1||x>=w-2-cut;
                pixels[y*w+x]=border?new Color(.88f,.73f,.43f):(y==2||y==h-3)?new Color(.15f,.17f,.11f):new Color(.34f,.37f,.24f);
                if((x==7||x==w-8)&&Mathf.Abs(y-(h-1)*.5f)<2)pixels[y*w+x]=new Color(.75f,.64f,.38f);
            }
            texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(imagePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(imagePath,ImportAssetOptions.ForceUpdate);
        var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;importer.spriteBorder=new Vector4(10,7,10,7);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
        var style=AssetDatabase.LoadAssetAtPath<PixelMenuButtonStyle>(stylePath);
        if(style==null){style=ScriptableObject.CreateInstance<PixelMenuButtonStyle>();style.background=AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);AssetDatabase.CreateAsset(style,stylePath);}
        if(!File.Exists(prefabPath))
        {
            var root=new GameObject("Pixel Menu Button",typeof(RectTransform),typeof(Image),typeof(Button));
            try
            {
                root.GetComponent<RectTransform>().sizeDelta=new Vector2(360,52);
                var label=new GameObject("Label",typeof(RectTransform),typeof(Text));label.transform.SetParent(root.transform,false);
                var rect=label.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(14,0);rect.offsetMax=new Vector2(-14,0);
                var text=label.GetComponent<Text>();text.font=GameFont.Current;text.text="Xác nhận";text.fontSize=28;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
                var skin=root.AddComponent<PixelMenuButtonSkin>();skin.style=style;skin.Refresh();
                PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            }
            finally{Object.DestroyImmediate(root);}
        }
        AssetDatabase.SaveAssets();Debug.Log("Shared pixel menu button sprite, style and prefab ready.");
    }
}
