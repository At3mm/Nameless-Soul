using UnityEngine;

[CreateAssetMenu(menuName = "Game/Economy Settings")]
public sealed class EconomySettings : ScriptableObject
{
    [Header("EXP cost = first + linear * purchases + quadratic * purchases squared")]
    [Min(1)] public int firstUpgradeCost = 100;
    [Min(0)] public int linearCostIncrease = 50;
    [Min(0)] public int quadraticCostIncrease = 10;
    [Header("Gain per purchase (existing character parameters)")]
    [Min(.01f)] public float healthPerPurchase = 10;
    [Min(.01f)] public float staminaPerPurchase = 5;
    [Min(.01f)] public float attackPerPurchase = 1;
    [Min(.01f)] public float defensePerPurchase = .5f;
    [Header("Enemy rewards: Gold / EXP")]
    [Min(0)] public int normalGold = 10;
    [Min(0)] public int normalExperience = 25;
    [Min(0)] public int eliteGold = 50;
    [Min(0)] public int eliteExperience = 150;
    [Min(0)] public int bossGold = 250;
    [Min(0)] public int bossExperience = 1000;

    [Header("Life seeds per capacity increase, starting at 3 flasks")]
    public int[] potionSeedCosts = {1,1,2,2,2,2,2};
    public int PotionSeedCost(int upgrades)
    {
        if(upgrades<0||upgrades>=PlayerHealthPotion.MaximumCapacity-PlayerHealthPotion.StartingCapacity)return 0;
        return potionSeedCosts!=null&&upgrades<potionSeedCosts.Length?Mathf.Max(1,potionSeedCosts[upgrades]):2;
    }

    public int UpgradeCost(int purchases)
    {
        if (purchases < 0 || purchases == int.MaxValue) return -1;
        double value = System.Math.Max(1,firstUpgradeCost) + (double)System.Math.Max(0,linearCostIncrease)*purchases +
            (double)System.Math.Max(0,quadraticCostIncrease)*purchases*purchases;
        return value <= int.MaxValue ? (int)value : -1;
    }
    public float Gain(GameEconomy.Stat stat)
    {
        switch (stat)
        {
            case GameEconomy.Stat.Health: return healthPerPurchase;
            case GameEconomy.Stat.Stamina: return staminaPerPurchase;
            case GameEconomy.Stat.Attack: return attackPerPurchase;
            case GameEconomy.Stat.Defense: return defensePerPurchase;
            default: return 0;
        }
    }
    public void Rewards(EnemyClassification.EnemyType type,out int gold,out int experience)
    {
        gold = type == EnemyClassification.EnemyType.Boss ? bossGold : type == EnemyClassification.EnemyType.Elite ? eliteGold : normalGold;
        experience = type == EnemyClassification.EnemyType.Boss ? bossExperience : type == EnemyClassification.EnemyType.Elite ? eliteExperience : normalExperience;
    }
}
