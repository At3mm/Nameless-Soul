using UnityEngine;

// Attach to a 2D trigger area. Does not change the player's base movement speed.
[DisallowMultipleComponent]
public sealed class WaterSlowZone : MonoBehaviour
{
    [Range(0.05f, 1f)]
    [Tooltip("Movement speed retained in water. 0.6 means 40% slower. Overlapping zones use the strongest slow.")]
    [SerializeField] private float speedMultiplier = 0.6f;

    public float SpeedMultiplier => Mathf.Clamp(speedMultiplier, 0.05f, 1f);
    private void OnEnable() { LiquidSurfaceEffect.Ensure(gameObject); }

    private void OnTriggerEnter2D(Collider2D other) { Register(other); }
    private void OnTriggerStay2D(Collider2D other) { Register(other); }
    private void Register(Collider2D other)
    {
        if (!isActiveAndEnabled || !Application.isPlaying) return;
        var body = other.attachedRigidbody;
        if (body == null || body.GetComponent<EnemyFlight>() != null || body.GetComponent<CharacterHealth>() == null || body.GetComponent<SpriteRenderer>() == null) return;
        var waterCollider = GetComponent<CompositeCollider2D>() as Collider2D;
        if (waterCollider == null) waterCollider = GetComponent<Collider2D>();
        if (waterCollider == null || !waterCollider.enabled || !waterCollider.isTrigger) return;
        var presence = body.GetComponent<WaterPresence>();
        if (presence == null) presence = body.gameObject.AddComponent<WaterPresence>();
        presence.TrackWaterContact(waterCollider);
    }
}
