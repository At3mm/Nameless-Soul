using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyCurrencyReward : MonoBehaviour
{
    public bool rewardsEnabled = true;
    public bool useDefaultRewards = true;
    [Min(0)] public int gold = 10;
    [Min(0)] public int experience = 25;
    private bool claimed;
    public bool TryClaim()
    {
        var health = GetComponent<CharacterHealth>();
        var classification = GetComponent<EnemyClassification>();
        if (!rewardsEnabled || claimed || health == null || !health.isDead || classification == null) return false;
        int coins = gold, exp = experience;
        if (useDefaultRewards) GameEconomy.Settings.Rewards(classification.enemyType,out coins,out exp);
        coins=SoulConsumption.Reward(coins,false);exp=SoulConsumption.Reward(exp,true);
        claimed = true;
        if (!GameEconomy.AddRewards(coins,exp)) { claimed = false; return false; }
        return true;
    }
}
