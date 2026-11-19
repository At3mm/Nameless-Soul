using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Procedural pixel sprites, authored for this project. No third-party pixels copied.
public static class BuildDebuffEffectSprites
{
    private const int Cell = 100, Frames = 8;
    private const string Folder = "Assets/Effects/Sprites/Debuffs";
    [MenuItem("Tools/Status Effects/Rebuild Pixel Sprites")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        for (int kind = 0; kind < 3; kind++)
        {
            var texture = new Texture2D(Cell * Frames, Cell, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[Cell * Frames * Cell]);
            for (int frame = 0; frame < Frames; frame++)
            {
                if (kind == 0) Fire(texture, frame);
                else if (kind == 1) Poison(texture, frame);
                else Stress(texture, frame);
            }
            texture.Apply();
            string name = new[] { "Burning", "Poison", "Stress" }[kind];
            string path = Folder + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 25;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects();
            var metadata = new SpriteRect[Frames];
            var pairs = new System.Collections.Generic.List<SpriteNameFileIdPair>();
            for (int i = 0; i < Frames; i++)
            {
                string spriteName = name + "_" + i;
                var old = System.Array.Find(existing, r => r.name == spriteName);
                metadata[i] = new SpriteRect
                {
                    name = spriteName, rect = new Rect(i * Cell, 0, Cell, Cell),
                    alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f, 0.5f),
                    spriteID = old != null ? old.spriteID : GUID.Generate()
                };
                pairs.Add(new SpriteNameFileIdPair(spriteName, metadata[i].spriteID));
            }
            provider.SetSpriteRects(metadata);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
            provider.Apply();
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }
    // Logical coordinates are relative to the feet; preserve the 100x100 frame grid.
    private static void Pixel(Texture2D t, int frame, int x, int y, Color color)
    {
        x += frame * Cell + 50; y += 50;
        if (x >= frame * Cell && x < (frame + 1) * Cell && y >= 0 && y < Cell) t.SetPixel(x, y, color);
    }
    private static Color Hex(string hex) { Color c; ColorUtility.TryParseHtmlString(hex, out c); return c; }
    private static void Fire(Texture2D t, int f)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            int x = side * 13;
            int height = 4 + (f + (side + 1)) % 3;
            for (int y = 0; y < height; y++)
            {
                int lean = y > 3 ? side : 0;
                Pixel(t, f, x + lean, y - 2, Hex("#FFB83E"));
                if (y < height - 1) Pixel(t, f, x + lean + 1, y - 2, Hex("#F26932"));
                if (y < 2) Pixel(t, f, x + lean, y - 2, Hex("#FFF2A1"));
            }
        }
    }
    private static void Poison(Texture2D t, int f)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            int x = side * 13;
            int y = (f + (side + 1) * 2) % 7 - 3;
            Pixel(t, f, x, y + 1, Hex("#D3F398"));
            Pixel(t, f, x - 1, y, Hex("#A1C95D"));
            Pixel(t, f, x + 1, y, Hex("#9361CE"));
            Pixel(t, f, x, y - 1, Hex("#9361CE"));
        }
    }
    private static void Stress(Texture2D t, int f)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            int x = side * 13, y = f % 4 < 2 ? 0 : 1;
            for (int k = 0; k < 4; k++) Pixel(t, f, x, y + k, Hex("#A8CDF5"));
            Pixel(t, f, x + side, y, Hex("#8793DC"));
            Pixel(t, f, x, y + 1, Hex("#E8F5FF"));
        }
    }
}
