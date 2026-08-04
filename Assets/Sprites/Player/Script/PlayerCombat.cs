using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý Input tấn công của Người chơi.
/// Tự động kiểm tra Cooldown & Điểm Chiến ý (POS) từ SkillDamageManager trước khi cho phép tung chiêu.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    private Animator animator;
    private AnimationEventHandler animHandler;
    private SkillDamageManager skillManager;

    [Header("Tên Kỹ năng tương ứng trong SkillDamageManager")]
    [Tooltip("Tên skill đánh thường trong SkillDamageManager")]
    public string normalAttackSkillName = "NormalAttack";

    [Tooltip("Tên skill 2/tuyệt kỹ trong SkillDamageManager")]
    public string skill2SkillName = "Skill2";

    private InputAction normalAttackAction;
    private InputAction parryAction;
    private InputAction skill2Action;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animHandler = GetComponent<AnimationEventHandler>();
        skillManager = GetComponent<SkillDamageManager>();

        normalAttackAction = new InputAction("NormalAttack", binding: "<Mouse>/leftButton");
        parryAction = new InputAction("Parry", binding: "<Mouse>/rightButton");
        skill2Action = new InputAction("Skill2", binding: "<Keyboard>/space");
    }

    private void OnEnable()
    {
        normalAttackAction.Enable();
        parryAction.Enable();
        skill2Action.Enable();

        normalAttackAction.performed += OnNormalAttack;
        parryAction.performed += OnParry;
        skill2Action.performed += OnSkill2;
    }

    private void OnDisable()
    {
        normalAttackAction.Disable();
        parryAction.Disable();
        skill2Action.Disable();

        normalAttackAction.performed -= OnNormalAttack;
        parryAction.performed -= OnParry;
        skill2Action.performed -= OnSkill2;
    }

    private bool CanAct()
    {
        if (animHandler != null) return !animHandler.isActionLocked;
        return true;
    }

    private void OnNormalAttack(InputAction.CallbackContext context)
    {
        if (!CanAct()) return;

        // 1. KIỂM TRA ĐỦ POS & COOLDOWN (Nếu Player có gắn SkillDamageManager)
        if (skillManager != null && !string.IsNullOrEmpty(normalAttackSkillName))
        {
            if (!skillManager.IsSkillReady(normalAttackSkillName))
            {
                Debug.Log($"<color=yellow>[PlayerCombat]</color> Không thể Đánh thường: Đang Cooldown hoặc KHÔNG ĐỦ POS!");
                return; // KHÓA: Hủy lệnh, không cho tung chiêu
            }

            // Trừ điểm POS & Bắt đầu tính Cooldown
            skillManager.MarkSkillUsed(normalAttackSkillName);
        }

        // 2. Cho phép tung chiêu
        if (animHandler != null) animHandler.LockAction();
        animator.SetTrigger("PlayerNormalAttack");
    }

    private void OnParry(InputAction.CallbackContext context)
    {
        if (!CanAct()) return;

        if (animHandler != null) animHandler.LockAction();
        animator.SetTrigger("PlayerParry");
    }

    private void OnSkill2(InputAction.CallbackContext context)
    {
        if (!CanAct()) return;

        // 1. KIỂM TRA ĐỦ POS & COOLDOWN
        if (skillManager != null && !string.IsNullOrEmpty(skill2SkillName))
        {
            if (!skillManager.IsSkillReady(skill2SkillName))
            {
                Debug.Log($"<color=red>[PlayerCombat]</color> Không thể dùng Skill 2: Đang Cooldown hoặc KHÔNG ĐỦ POS!");
                return; // KHÓA: Hủy lệnh, không cho tung chiêu khi thiếu POS
            }

            // Trừ điểm POS & Bắt đầu tính Cooldown
            skillManager.MarkSkillUsed(skill2SkillName);
        }

        // 2. Cho phép tung chiêu
        if (animHandler != null) animHandler.LockAction();
        animator.SetTrigger("PlayerSkill2");
    }
}