using UnityEngine;

/// <summary>Reconciles action flags when an animation is interrupted or skips its final event.</summary>
public class PlayerActionStateGuard : StateMachineBehaviour
{
    public bool restoreControlOnEntry = true;
    [Tooltip("Bật khi cần chẩn đoán khóa hành động. Phục hồi vẫn hoạt động khi tắt log.")]
    public bool logRecoveries = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var health = animator.GetComponent<CharacterHealth>();
        if (health != null && health.isDead) return;
        var stun = animator.GetComponent<PlayerStun>();
        if (stun != null && stun.IsStunned) return;
        var parry = animator.GetComponent<CharacterParry>();
        if (parry != null) parry.isParrying = false;
        if (!restoreControlOnEntry) return;
        var handler = animator.GetComponent<AnimationEventHandler>();
        if (handler == null || !handler.isActionLocked) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (logRecoveries)
            Debug.LogWarning("[PlayerActionStateGuard] Recovered action lock on " + animator.name +
                ". State hash=" + stateInfo.shortNameHash + ", frame=" + Time.frameCount +
                ". Character control has been restored.", animator);
#endif
        handler.UnlockAction();
    }
}
