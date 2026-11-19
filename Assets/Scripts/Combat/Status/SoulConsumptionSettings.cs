using UnityEngine;

[CreateAssetMenu(menuName="Game/Soul Consumption Settings")]
public sealed class SoulConsumptionSettings : ScriptableObject
{
    [Range(0,100)] public float initialChancePercent=6;
    [Range(0,100)] public float chanceIncreasePerDeath=1;
    [Range(.01f,1)] public float outgoingDamageMultiplier=.8f;
    [Min(1)] public float incomingDamageMultiplier=1.25f;
    [Min(1)] public float goldMultiplier=1.5f,soulMultiplier=1.5f;
    public Sprite statusIcon;
}
