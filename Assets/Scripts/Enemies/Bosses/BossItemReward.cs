using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(EnemyClassification))]
public sealed class BossItemReward : MonoBehaviour
{
    public bool rewardsEnabled = true;
    [Tooltip("Place reward definitions in Resources/BossRewards so their icons resolve after loading a save.")]
    public GameItemDefinition item;

    public static bool GrantForDeath(CharacterHealth health)
    {
        if (health == null) return false;
        var reward = health.GetComponent<BossItemReward>();
        if (reward != null && (!reward.enabled || !reward.rewardsEnabled)) return false;
        return BossItemInventory.TryGrant(health.GetComponent<EnemyClassification>(), reward != null ? reward.item : null);
    }
}
