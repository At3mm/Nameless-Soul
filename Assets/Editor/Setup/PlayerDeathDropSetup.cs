using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PlayerDeathDropSetup
{
    public const string Path="Assets/Resources/DefaultPlayerDeathDrop.asset";
    [MenuItem("Tools/Game Systems/Create Missing Player Death Drop Assets")]
    public static void CreateMissing()
    {
        if(AssetDatabase.LoadAssetAtPath<PlayerDeathDropSettings>(Path)!=null)return;
        var settings=ScriptableObject.CreateInstance<PlayerDeathDropSettings>();settings.iconWorldHeight=.3f;
        settings.icon=AssetDatabase.LoadAllAssetsAtPath("Assets/UI/Packs/PaperBook/SpriteSheets/Icons/PaperBook_Icons_Atlas.png").OfType<Sprite>().FirstOrDefault(sprite=>sprite.name=="PBIcon_Items_Items_03");
        AssetDatabase.CreateAsset(settings,Path);AssetDatabase.SaveAssets();
    }
}
