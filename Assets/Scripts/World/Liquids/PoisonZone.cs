using System.Collections.Generic;
using UnityEngine;

/// <summary>Continuous poison buildup. Active poison does not refresh until it expires.</summary>
[DisallowMultipleComponent]
public sealed class PoisonZone : MonoBehaviour
{
    [Min(0)] public float buildupPerSecond=30;
    [Min(.1f)] public float duration = 8f;
    [Min(0f), Tooltip("Damage per second as a percentage of maximum HP.")]
    public float maxHPPercentPerSecond = 2f;
    [Min(.1f)] public float refreshInterval = .5f;
    private readonly Dictionary<CharacterHealth,float> nextApplication = new Dictionary<CharacterHealth,float>();
    private readonly HashSet<Collider2D> contacts = new HashSet<Collider2D>();
    public bool IsExposing(CharacterHealth actor)
    {
        if(!isActiveAndEnabled || actor==null)return false;
        foreach(var contact in contacts)
            if(contact!=null && contact.enabled && contact.gameObject.activeInHierarchy && FindActor(contact)==actor)return true;
        return false;
    }
    private void OnEnable() { LiquidSurfaceEffect.Ensure(gameObject,LiquidSurfaceEffect.SurfaceKind.Poison); }

    private void OnTriggerEnter2D(Collider2D other) { contacts.Add(other); Apply(other); }
    private void OnTriggerStay2D(Collider2D other) { contacts.Add(other); Apply(other); }
    private void Update()
    {
        // Physics does not send stay callbacks for sleeping bodies: standing still must remain poisonous.
        if (Time.timeScale <= 0f) return;
        contacts.RemoveWhere(c=>c==null || !c.enabled || !c.gameObject.activeInHierarchy);
        foreach (var contact in contacts) Apply(contact);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        contacts.Remove(other);
        var health = FindActor(other);
        if (health != null)
        {
            foreach(var contact in contacts)if(contact!=null && FindActor(contact)==health)return;
            nextApplication.Remove(health);
            health.GetComponent<CharacterDebuffs>()?.SetExposure(CharacterDebuffs.Kind.Poison,this,false);
        }
        // Poison keeps its remaining duration after the actor leaves the pool.
    }
    private static CharacterHealth FindActor(Collider2D other)
    {
        return other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<CharacterHealth>() : other.GetComponentInParent<CharacterHealth>();
    }
    private void Apply(Collider2D other)
    {
        if (!Application.isPlaying || !isActiveAndEnabled || Time.timeScale <= 0f) return;
        var health = FindActor(other);
        if (health == null || health.isDead) return;
        var debuffs = health.GetComponent<CharacterDebuffs>() ?? health.gameObject.AddComponent<CharacterDebuffs>();
        debuffs.SetExposure(CharacterDebuffs.Kind.Poison,this,true);
        if (nextApplication.TryGetValue(health,out float readyAt) && Time.time < readyAt) return;
        debuffs.AddBuildup(CharacterDebuffs.Kind.Poison,buildupPerSecond*Mathf.Max(.1f,refreshInterval),duration,maxHPPercentPerSecond);
        nextApplication[health] = Time.time + Mathf.Max(.1f,refreshInterval);
    }
    private void OnDisable()
    {
        foreach(var contact in contacts)
        {
            if(contact==null)continue;var health=FindActor(contact);
            if(health!=null)health.GetComponent<CharacterDebuffs>()?.SetExposure(CharacterDebuffs.Kind.Poison,this,false);
        }
        contacts.Clear();nextApplication.Clear();
    }
}
