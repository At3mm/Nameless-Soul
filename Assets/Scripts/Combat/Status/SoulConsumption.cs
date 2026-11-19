using System;
using UnityEngine;

/// <summary>Persistent player curse. Only shrine purification removes it and resets the chance.</summary>
public static class SoulConsumption
{
    private static SoulConsumptionSettings settings;
    public static SoulConsumptionSettings Settings
    {
        get
        {
            if(settings==null){settings=Resources.Load<SoulConsumptionSettings>("DefaultSoulConsumption");if(settings==null){settings=ScriptableObject.CreateInstance<SoulConsumptionSettings>();settings.hideFlags=HideFlags.HideAndDontSave;}}
            return settings;
        }
    }
    public static bool Active=>GameEconomy.HasSoulConsumption;
    public static float NextChancePercent=>Mathf.Clamp(Settings.initialChancePercent+GameEconomy.SoulConsumptionDeaths*Settings.chanceIncreasePerDeath,0,100);
    public static bool Affects(Transform actor)=>Active&&actor!=null&&actor.GetComponentInParent<PlayerController>()!=null;
    public static float OutgoingMultiplier(Transform actor)=>Affects(actor)?Mathf.Clamp(Settings.outgoingDamageMultiplier,.01f,1):1;
    public static float IncomingMultiplier(Transform actor)=>Affects(actor)?Mathf.Max(1,Settings.incomingDamageMultiplier):1;
    public static int Reward(int amount,bool soul)
    {
        if(amount<=0)return Mathf.Max(0,amount);
        double multiplier=Active?Math.Max(1,soul?Settings.soulMultiplier:Settings.goldMultiplier):1;
        return (int)Math.Min(int.MaxValue,Math.Round(amount*multiplier,MidpointRounding.AwayFromZero));
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){settings=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap(){CharacterHealth.AnyDied-=OnDied;CharacterHealth.AnyDied+=OnDied;}
    private static void OnDied(CharacterHealth health)
    {
        if(health==null||health.GetComponent<PlayerController>()==null)return;
        GameEconomy.RecordSoulConsumptionDeath(UnityEngine.Random.value);
    }
}
