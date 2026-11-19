using UnityEngine;

[CreateAssetMenu(menuName = "Game/Boss Reward Item")]
public sealed class BossItemDefinition : GameItemDefinition
{
    [Tooltip("Hiển thị cổ vật này ở tab Vật phẩm thay vì Cổ vật cường địch. Vai trò xác nhận nhiệm vụ không phụ thuộc tab.")]
    public bool showAsQuestItem;

    public static BossItemDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var item in Resources.LoadAll<BossItemDefinition>("BossRewards"))
            if (item != null && item.itemId == id) return item;
        return null;
    }
}
