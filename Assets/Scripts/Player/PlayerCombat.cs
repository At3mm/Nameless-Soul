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

        normalAttackAction = GameControls.CreateButton(GameControls.Action.Attack);
        parryAction = GameControls.CreateButton(GameControls.Action.Parry);
        skill2Action = GameControls.CreateButton(GameControls.Action.Skill);
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
        if(GameControls.Blocked || Time.timeScale<=0f)return false;
        if (animHandler != null) return !animHandler.isActionLocked;
        return true;
    }

    private void OnDestroy(){GameControls.Release(normalAttackAction);GameControls.Release(parryAction);GameControls.Release(skill2Action);}

    private void OnNormalAttack(InputAction.CallbackContext context)
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && Mouse.current != null)
        {
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            pointer.position = Mouse.current.position.ReadValue();
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Exists(hit => hit.module is UnityEngine.UI.GraphicRaycaster)) return;
        }
        if (!CanAct()) return;

        // 1. KIỂM TRA ĐỦ POS & COOLDOWN (Nếu Player có gắn SkillDamageManager)
        if (skillManager != null && !string.IsNullOrEmpty(normalAttackSkillName))
        {
            if (!skillManager.IsSkillReady(normalAttackSkillName, out string reason))
            {
                GetComponent<World.PlayerInteractor>()?.ShowNotice(reason);
                return; // KHÓA: Hủy lệnh, không cho tung chiêu
            }

            // Trừ điểm POS & Bắt đầu tính Cooldown
            if (!skillManager.MarkSkillUsed(normalAttackSkillName)) return;
        }

        // 2. Cho phép tung chiêu
        if (animHandler != null) animHandler.LockAction();
        animator.SetTrigger("PlayerNormalAttack");
        GameplayTutorial.Report(gameObject, GameplayTutorial.Lesson.Attack);
    }

    private void OnParry(InputAction.CallbackContext context)
    {
        if (!CanAct()) return;

        if (animHandler != null) animHandler.LockAction();
        animator.SetTrigger("PlayerParry");
        GameplayTutorial.Report(gameObject, GameplayTutorial.Lesson.Parry);
        CombatSFX.ParryReady(transform.position);
    }

    private void OnSkill2(InputAction.CallbackContext context)
    {
        if (!CanAct()) return;

        // 1. KIỂM TRA ĐỦ POS & COOLDOWN
        if (skillManager != null && !string.IsNullOrEmpty(skill2SkillName))
        {
            if (!skillManager.IsSkillReady(skill2SkillName, out string reason))
            {
                GetComponent<World.PlayerInteractor>()?.ShowNotice(reason);
                return; // KHÓA: Hủy lệnh, không cho tung chiêu khi thiếu POS
            }

            // Trừ điểm POS & Bắt đầu tính Cooldown
            if (!skillManager.MarkSkillUsed(skill2SkillName)) return;
        }

        // 2. Cho phép tung chiêu
        if (animHandler != null) animHandler.LockAction();
        animator.SetTrigger("PlayerSkill2");
        GameplayTutorial.Report(gameObject, GameplayTutorial.Lesson.Skill);
        CombatSFX.Skill2Cast(transform.position);
    }
}
