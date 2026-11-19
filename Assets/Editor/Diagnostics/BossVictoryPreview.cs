using UnityEditor;
using UnityEngine;

public static class BossVictoryPreview
{
    private const string Menu = "Tools/Diagnostics/Preview Boss Victory (Play Mode)";
    [MenuItem(Menu)]
    public static void Show() => BossRewardNotice.Preview("Kỵ sĩ Orc", Resources.Load<BossItemDefinition>("BossRewards/OrcRiderMemory"));
    [MenuItem(Menu, true)]
    private static bool CanShow() => Application.isPlaying;
}
