using UnityEngine;

/// <summary>Applies persisted upgrade bonuses once per player instance, including respawn/scene changes.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats))]
public sealed class PlayerProgression : MonoBehaviour
{
    private CharacterStats stats;
    private readonly float[] applied = new float[4];
    public static void Attach(CharacterStats actor)
    {
        if (actor.GetComponent<PlayerProgression>() == null) actor.gameObject.AddComponent<PlayerProgression>();
    }
    private void Awake() { stats = GetComponent<CharacterStats>(); }
    private void OnEnable() { GameEconomy.Changed += Refresh; Refresh(); }
    private void OnDisable() { GameEconomy.Changed -= Refresh; }
    private void Refresh()
    {
        if (stats == null) return;
        for (int i=0;i<4;i++)
        {
            var stat = (GameEconomy.Stat)i;
            float gain = GameEconomy.Settings.Gain(stat);
            if (gain < 0 || float.IsNaN(gain) || float.IsInfinity(gain)) gain = 0;
            float bonus = GameEconomy.Purchases(stat)*gain;
            float delta = bonus-applied[i]; applied[i] = bonus;
            switch (stat)
            {
                case GameEconomy.Stat.Health:
                    stats.maxHP += delta; stats.currentHP = Mathf.Clamp(stats.currentHP+delta,0,stats.maxHP); break;
                case GameEconomy.Stat.Stamina:
                    stats.maxStamina += delta; stats.currentStamina = Mathf.Clamp(stats.currentStamina+delta,0,stats.maxStamina); break;
                case GameEconomy.Stat.Attack: stats.ATK += delta; break;
                case GameEconomy.Stat.Defense: stats.DEF += delta; break;
            }
        }
    }
    // UnityEvent hooks for quests, pickups and other reward sources.
    public void GrantGold(int amount) { GameEconomy.AddGold(amount); }
    public void GrantExperience(int amount) { GameEconomy.AddExperience(amount); }
}
