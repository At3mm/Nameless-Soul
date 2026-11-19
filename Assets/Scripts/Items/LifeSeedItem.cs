/// <summary>Editable item metadata backed by the existing seed wallet, with no second item count.</summary>
public static class LifeSeedItem
{
    public const string Id = "life-seed";
    public static GameItemDefinition Definition => GameItemDefinition.FindItem(Id);
    public static string Name => Definition != null ? Definition.displayName : "Hạt sinh mệnh";
}
