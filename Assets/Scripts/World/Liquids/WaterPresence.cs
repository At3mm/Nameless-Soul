using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WaterPresence : MonoBehaviour
{
    private readonly HashSet<Collider2D> contacts = new HashSet<Collider2D>();
    public bool IsInWater { get { return MultiplierAndPresence(true) < 0f; } }
    public float EnemySpeedMultiplier { get { return MultiplierAndPresence(false); } }
    public bool IsInLava
    {
        get { return IsLavaAtPoint(transform.position); }
    }
    public bool IsLavaAtPoint(Vector2 point)
    {
        contacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        foreach (var contact in contacts)
        {
            var zone=contact.GetComponentInParent<LavaZone>();
            if(zone!=null&&zone.isActiveAndEnabled&&contact.OverlapPoint(point))return true;
        }
        return false;
    }

    public bool ContainsPoint(Vector2 point)
    {
        contacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        foreach (var contact in contacts)
        {
            var zone = contact.GetComponentInParent<WaterSlowZone>();
            if (zone != null && zone.isActiveAndEnabled && contact.OverlapPoint(point)) return true;
        }
        return false;
    }

    private float MultiplierAndPresence(bool presenceOnly)
    {
        contacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        float multiplier = 1f;
        foreach (var c in contacts)
        {
            var zone = c.GetComponentInParent<WaterSlowZone>();
            if (zone == null || !zone.isActiveAndEnabled) continue;
            if (presenceOnly) return -1f;
            multiplier = Mathf.Min(multiplier, 1f - (1f - zone.SpeedMultiplier) * 0.5f);
        }
        return multiplier;
    }
    private void OnTriggerEnter2D(Collider2D other) { Track(other); }
    private void OnTriggerStay2D(Collider2D other) { Track(other); }
    private void OnTriggerExit2D(Collider2D other) { contacts.Remove(other); }
    private void Track(Collider2D other)
    {
        if (!isActiveAndEnabled || GetComponent<EnemyFlight>() != null) return;
        if (other.GetComponentInParent<WaterSlowZone>() != null && contacts.Add(other))
            WaterEffectDefaults.EnsureVisuals(gameObject);
    }
    public void TrackWaterContact(Collider2D waterCollider) { Track(waterCollider); }
    private void OnDisable() { contacts.Clear(); }
}
