using UnityEngine;

/// <summary>Temporary summon, excluded from checkpoint respawns and currency rewards.</summary>
[DisallowMultipleComponent]
public sealed class NecromancerSummonedMinion : MonoBehaviour
{
    [Min(1)] public float lifetime=30;
    private CharacterHealth caster;
    private float expiresAt;
    private bool bound;
    public void Bind(CharacterHealth owner)
    {
        caster=owner;bound=true;expiresAt=Time.time+lifetime;
        var reward=GetComponent<EnemyCurrencyReward>();
        if(reward==null)reward=gameObject.AddComponent<EnemyCurrencyReward>();
        reward.rewardsEnabled=false;
    }
    private void Update()
    {
        if(bound&&(caster==null||caster.isDead||!caster.gameObject.activeInHierarchy||Time.time>=expiresAt))Destroy(gameObject);
    }
}
