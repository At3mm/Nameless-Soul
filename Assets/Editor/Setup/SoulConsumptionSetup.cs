using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SoulConsumptionSetup
{
    public const string SettingsPath="Assets/Resources/DefaultSoulConsumption.asset";
    [MenuItem("Tools/Game Systems/Create Missing Soul Consumption Assets")]
    public static void CreateMissing()
    {
        var settings=AssetDatabase.LoadAssetAtPath<SoulConsumptionSettings>(SettingsPath);
        if(settings==null)
        {
            settings=ScriptableObject.CreateInstance<SoulConsumptionSettings>();
            settings.statusIcon=AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Items/Amulets/Pixel1992_Necklaces/64x64/Blue Outline/Necklace V2_11.png").OfType<Sprite>().FirstOrDefault();
            if(settings.statusIcon==null)settings.statusIcon=Resources.LoadAll<BossItemDefinition>("BossRewards").FirstOrDefault()?.icon;
            AssetDatabase.CreateAsset(settings,SettingsPath);
        }
        var catalog=ConsumableCatalog.Default;
        if(catalog!=null&&catalog.Find(ConsumableKind.SoulPurification)==null)
        {
            var items=catalog.items.ToList();items.Add(new ConsumableCatalog.Item{
                kind=ConsumableKind.SoulPurification,displayName="Hương thanh hồn",
                description="Dùng 1 tại tượng thần → Thanh tẩy để giải Ăn hồn và đặt lại tỉ lệ nhiễm về 6%. Không thể dùng nhanh ngoài chiến trường.",
                icon=catalog.Find(ConsumableKind.CalmingMedicine)?.icon,buyPrice=150,sellPrice=30});
            catalog.items=items.ToArray();EditorUtility.SetDirty(catalog);
        }
        AssetDatabase.SaveAssets();
    }
}
