using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Cài đặt Di chuyển")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private PlayerDash playerDash;
    private AnimationEventHandler animHandler;

    private InputAction moveAction;
    private InputAction dashAction;

    private Vector2 currentInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        playerDash = GetComponent<PlayerDash>();
        animHandler = GetComponent<AnimationEventHandler>();

        // Thiết lập phím di chuyển
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Thiết lập phím lướt Dash
        dashAction = new InputAction("Dash", binding: "<Keyboard>/leftShift");
        dashAction.AddBinding("<Gamepad>/buttonEast");

        dashAction.performed += OnDashPerformed;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        dashAction.Disable();
    }

    private void Update()
    {
        currentInput = moveAction.ReadValue<Vector2>();

        // KHÓA LẬT MẶT: Nếu đang bị khóa hành động hoặc đang Dash -> không xoay mặt
        if (animHandler != null && animHandler.isActionLocked) return;
        if (playerDash != null && playerDash.isDashing) return;

        if (currentInput.x > 0) transform.localScale = Vector3.one;
        else if (currentInput.x < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    private void FixedUpdate()
    {
        // 1. Nhường quyền điều khiển vận tốc cho PlayerDash nếu đang Lướt
        if (playerDash != null && playerDash.isDashing) return;

        // 2. KHÓA DI CHUYỂN: Nếu đang Tấn công / Bị đánh / Choáng
        if (animHandler != null && animHandler.isActionLocked)
        {
            // Tắt animation đi bộ
            animator.SetBool("IsWalking", false);

            // Bỏ "rb.linearVelocity = Vector2.zero" ở đây để không dập tắt lực đẩy lùi (Knockback)
            return;
        }

        // 3. Di chuyển vật lý khi ở trạng thái tự do bình thường
        rb.linearVelocity = currentInput * moveSpeed;

        bool isWalking = currentInput.sqrMagnitude > 0.01f;
        animator.SetBool("IsWalking", isWalking);
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (playerDash != null)
        {
            playerDash.TryDash(currentInput);
        }
    }
}