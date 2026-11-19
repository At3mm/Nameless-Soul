using UnityEngine;

/// <summary>Visual-only movement accents, sampled from actual displacement.</summary>
[DisallowMultipleComponent]
public sealed class PlayerMovementVFX : MonoBehaviour
{
    public DashEffectAsset dashEffect;
    PlayerController controller;
    PlayerDash dash;
    CharacterHealth health;
    AnimationEventHandler actions;
    Vector3 previous, lastEmission;
    float wait;
    bool wasDashing;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        dash = GetComponent<PlayerDash>();
        health = GetComponent<CharacterHealth>();
        actions = GetComponent<AnimationEventHandler>();
        if(dashEffect==null)dashEffect=DashEffectAsset.Default;
    }

    void OnEnable() { previous = lastEmission = transform.position; wait = 0f; wasDashing = false; }

    void LateUpdate()
    {
        Vector3 position = transform.position;
        Vector2 displacement = position - previous;
        previous = position;
        bool dashing = dash != null && dash.isActiveAndEnabled && dash.isDashing;
        bool running = controller != null && controller.isActiveAndEnabled && controller.IsSprinting;
        if (Time.deltaTime <= 0f) return;
        wait = Mathf.Max(0f, wait - Time.deltaTime);
        // Suppress dust in water, menus, death, stationary movement and checkpoint teleports.
        bool blocked = controller == null || !controller.isActiveAndEnabled || controller.IsInWater ||
            (health != null && health.isDead) || displacement.sqrMagnitude > 4f ||
            (!dashing && actions != null && actions.isActionLocked);
        if (blocked || (!dashing && !running) || displacement.sqrMagnitude < .000001f)
        {
            lastEmission = position;
            wasDashing = false;
            return;
        }
        float spacing = dashing ? (dashEffect!=null?dashEffect.emissionSpacing:.28f) : .55f;
        if ((dashing && !wasDashing) || (wait <= 0f && (position - lastEmission).sqrMagnitude >= spacing * spacing))
        {
            PixelCombatImpact.Motion(transform, displacement, dashing, dashEffect);
            lastEmission = position;
            wait = dashing ? (dashEffect!=null?dashEffect.emissionInterval:.055f) : .16f;
        }
        wasDashing = dashing;
    }
}
