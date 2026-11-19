using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(CharacterStats))]
public class PlayerController : MonoBehaviour
{
    [Header("Cài đặt Di chuyển")]
    public float moveSpeed = 5f;

    [Header("Chạy sau Dash")]
    [Tooltip("Chạy sau dash khi tiếp tục giữ Shift (hoặc nút dash trên gamepad) và di chuyển.")]
    public bool enableSprintAfterDash = true;
    [Min(0f), Tooltip("Phần trăm tăng tốc so với tốc độ di chuyển thường.")]
    public float sprintSpeedBonusPercent = 25f;
    [Min(0f), Tooltip("Stamina tiêu hao mỗi giây khi chạy.")]
    public float sprintStaminaPerSecond = 12f;
    public bool IsSprinting { get; private set; }

    [Header("Hồi Stamina")]
    [Min(0f), Tooltip("Stamina hồi mỗi giây khi không chạy hoặc dash. Đặt 0 để tắt hồi.")]
    public float staminaRecoveryPerSecond = 15f;
    [Min(0f), Tooltip("Thời gian chờ sau khi ngừng chạy hoặc dash trước khi hồi Stamina.")]
    public float staminaRecoveryDelay = 1f;
    private float staminaRecoveryWait;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerDash playerDash;
    private AnimationEventHandler animHandler;
    private CharacterStats stats;
    private CharacterHealth health;

    private InputAction moveAction;
    private InputAction dashAction;

    private Vector2 currentInput;
    private readonly HashSet<Collider2D> waterContacts = new HashSet<Collider2D>();

    public bool IsInWater
    {
        get
        {
            waterContacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            foreach (var contact in waterContacts)
            {
                var zone = contact.GetComponentInParent<WaterSlowZone>();
                if (zone != null && zone.isActiveAndEnabled) return true;
            }
            return false;
        }
    }

    public float WaterSpeedMultiplier
    {
        get
        {
            waterContacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
            float multiplier = 1f;
            foreach (var contact in waterContacts)
            {
                var zone = contact.GetComponentInParent<WaterSlowZone>();
                if (zone != null && zone.isActiveAndEnabled)
                    multiplier = Mathf.Min(multiplier, zone.SpeedMultiplier);
            }
            return multiplier;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) { TrackWater(other); }
    private void OnTriggerStay2D(Collider2D other) { TrackWater(other); }
    private void OnTriggerExit2D(Collider2D other) { waterContacts.Remove(other); }

    private void TrackWater(Collider2D other)
    {
        if (other.GetComponentInParent<WaterSlowZone>() != null) waterContacts.Add(other);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerDash = GetComponent<PlayerDash>();
        animHandler = GetComponent<AnimationEventHandler>();
        stats = GetComponent<CharacterStats>();
        health = GetComponent<CharacterHealth>();
        if (GetComponent<PlayerMovementVFX>() == null) gameObject.AddComponent<PlayerMovementVFX>();

        // Thiết lập phím di chuyển
        moveAction = GameControls.CreateMove();

        // Thiết lập phím lướt Dash
        dashAction = GameControls.CreateButton(GameControls.Action.Dash);

        dashAction.performed += OnDashPerformed;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();
    }

    private void OnDestroy(){GameControls.Release(moveAction);GameControls.Release(dashAction);}

    private void OnDisable()
    {
        waterContacts.Clear();
        StopSprinting();
        currentInput = Vector2.zero;
        moveAction.Disable();
        dashAction.Disable();
    }

    private void Update()
    {
        currentInput = moveAction.ReadValue<Vector2>();
        if (!dashAction.IsPressed() || currentInput.sqrMagnitude <= 0.01f ||
            (animHandler != null && animHandler.isActionLocked) ||
            (health != null && health.isDead)) StopSprinting();

        RecoverStamina(Time.deltaTime);

        // KHÓA LẬT MẶT: Nếu đang bị khóa hành động hoặc đang Dash -> không xoay mặt
        if (animHandler != null && animHandler.isActionLocked) return;
        if (playerDash != null && playerDash.isDashing) return;

        if (currentInput.x > 0) transform.localScale = Vector3.one;
        else if (currentInput.x < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    private void FixedUpdate()
    {
        if (health != null && health.isDead)
        {
            StopSprinting();
            return;
        }
        // 1. Nhường quyền điều khiển vận tốc cho PlayerDash nếu đang Lướt
        if (playerDash != null && playerDash.isDashing) return;

        // 2. KHÓA DI CHUYỂN: Nếu đang Tấn công / Bị đánh / Choáng
        if (animHandler != null && animHandler.isActionLocked)
        {
            StopSprinting();
            // Tắt animation đi bộ
            animator.SetBool("IsWalking", false);

            // Bỏ "rb.linearVelocity = Vector2.zero" ở đây để không dập tắt lực đẩy lùi (Knockback)
            return;
        }

        // 3. Di chuyển vật lý khi ở trạng thái tự do bình thường
        Vector2 movement = Vector2.ClampMagnitude(currentInput, 1f);
        float speedMultiplier = 1f;
        if (IsSprinting)
        {
            if (!enableSprintAfterDash || !dashAction.IsPressed() ||
                movement.sqrMagnitude <= 0.01f || stats.currentStamina <= 0f)
                StopSprinting();
            else
            {
                float drain = Mathf.Max(0f, sprintStaminaPerSecond) * (stats != null ? stats.StaminaCostMultiplier : 1f) * Time.fixedDeltaTime;
                // A partial final tick spends only the remaining stamina.
                float runningFraction = drain > 0f ? Mathf.Clamp01(stats.currentStamina / drain) : 1f;
                speedMultiplier += Mathf.Max(0f, sprintSpeedBonusPercent) * 0.01f * runningFraction;
                stats.currentStamina = Mathf.Max(0f, stats.currentStamina - drain);
                DelayStaminaRecovery();
                if (stats.currentStamina <= 0f) StopSprinting();
            }
        }
        rb.linearVelocity = movement * moveSpeed * speedMultiplier * WaterSpeedMultiplier *
            (stats != null ? stats.MovementEffectMultiplier : 1f);

        bool isWalking = currentInput.sqrMagnitude > 0.01f;
        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsRunning", IsSprinting);
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if(GameControls.Blocked || Time.timeScale<=0f)return;
        if (playerDash != null)
        {
            playerDash.TryDash(moveAction.ReadValue<Vector2>());
        }
    }

    public void BeginSprintAfterDash()
    {
        IsSprinting = isActiveAndEnabled && enableSprintAfterDash && dashAction.IsPressed() && stats.currentStamina > 0f &&
            moveAction.ReadValue<Vector2>().sqrMagnitude > 0.01f &&
            (animHandler == null || !animHandler.isActionLocked) &&
            (health == null || !health.isDead);
        animator.SetBool("IsRunning", IsSprinting);
    }

    public void StopSprinting()
    {
        IsSprinting = false;
        if (animator != null) animator.SetBool("IsRunning", false);
    }

    public void DelayStaminaRecovery()
    {
        staminaRecoveryWait = Mathf.Max(0f, staminaRecoveryDelay);
    }

    private void RecoverStamina(float deltaTime)
    {
        if (health != null && health.isDead) return;
        if (IsSprinting || (playerDash != null && playerDash.isDashing))
        {
            DelayStaminaRecovery();
            return;
        }

        // Only recover for the portion of this frame after the wait expires.
        float recoveryTime = Mathf.Max(0f, deltaTime - staminaRecoveryWait);
        staminaRecoveryWait = Mathf.Max(0f, staminaRecoveryWait - deltaTime);
        stats.currentStamina = Mathf.Clamp(stats.currentStamina +
            Mathf.Max(0f, staminaRecoveryPerSecond) * recoveryTime, 0f, Mathf.Max(0f, stats.maxStamina));
    }
}
