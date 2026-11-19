using UnityEngine;

/// <summary>
/// Quản lý chỉ số tổng hợp của nhân vật (HP, ATK, DEF, Stamina, CRIT, RES, POS)
/// </summary>
public class CharacterStats : MonoBehaviour
{
    public float EffectiveDefense => Mathf.Max(0f, DEF) * (GetComponent<EnemyBreak>() != null ? GetComponent<EnemyBreak>().DefenseMultiplier : 1f);
    public float EffectiveResistance
    {
        get { var buffs = GetComponent<CharacterBuffs>(); return Mathf.Clamp01(RES + (buffs != null ? buffs.ResistanceBonus : 0f)); }
    }
    public float MovementEffectMultiplier
    {
        get
        {
            var buffs = GetComponent<CharacterBuffs>();
            var debuffs = GetComponent<CharacterDebuffs>();
            return (buffs != null ? buffs.MovementMultiplier : 1f) * (debuffs != null ? debuffs.MovementMultiplier : 1f);
        }
    }
    public float StaminaCostMultiplier
    {
        get
        {
            var buffs = GetComponent<CharacterBuffs>();
            var debuffs = GetComponent<CharacterDebuffs>();
            return (buffs != null ? buffs.StaminaCostMultiplier : 1f) * (debuffs != null ? debuffs.StaminaCostMultiplier : 1f);
        }
    }
    [Header("Sinh tồn (Health)")]
    public float maxHP = 100f;      // Máu tối đa
    public float currentHP;         // Máu hiện tại

    [Header("Tấn công & Phòng thủ")]
    public float ATK = 20f;         // Tấn công cơ bản
    public float DEF = 10f;         // Phòng thủ (Giảm sát thương vật lý)
    public float STR = 5f;          // Sát thương chuẩn (Xuyên DEF)

    [Header("Sức bền (Stamina)")]
    [InspectorName("Max Stamina")] public float maxStamina = 100f;     // Sức bền tối đa
    [InspectorName("Current Stamina")] public float currentStamina;   // Sức bền hiện tại

    [Header("Chí mạng & Kháng")]
    [Range(0f, 1f)]
    public float CRIT_Rate = 0.1f;  // Tỷ lệ chí mạng (0.1 = 10%)
    public float CRIT_Damage = 1.5f;// Sát thương chí mạng (1.5 = 150%)

    [Tooltip("Kháng đẩy lùi/hiệu ứng (0.2 = Kháng 20%, 1.0 = Miễn nhiễm)")]
    [Range(0f, 1f)]
    public float RES = 0.2f;

    [Header("Chiến ý (Posture)")]
    [Tooltip("Tích chọn nếu nhân vật này sử dụng thanh Chiến ý (Posture)")]
    public bool usePostureSystem = false; // Checkbox Bật/Tắt hệ thống Chiến ý

    [Tooltip("Chỉ số Chiến ý Tối đa của nhân vật")]
    public float maxPOS = 100f;     // Giới hạn Chiến ý tối đa

    [Tooltip("Chỉ số Chiến ý Hiện tại của nhân vật")]
    public float currentPOS = 0f;   // Chiến ý hiện tại

    private void Awake()
    {
        currentHP = maxHP;
        currentStamina = maxStamina;

        if (usePostureSystem)
        {
            // Đảm bảo currentPOS nằm trong khoảng 0 đến maxPOS
            currentPOS = Mathf.Clamp(currentPOS, 0f, maxPOS);
        }
        if (CompareTag("Player"))
        {
            PlayerProgression.Attach(this);
            if(GetComponent<PlayerConsumables>()==null)gameObject.AddComponent<PlayerConsumables>();
        }
    }

    /// <summary>
    /// Thêm/trừ điểm Chiến ý.
    /// </summary>
    public void AddPosture(float amount)
    {
        if (!usePostureSystem) return;
        currentPOS = Mathf.Clamp(currentPOS + amount, 0f, maxPOS);
    }

    /// <summary>
    /// Kiểm tra xem nhân vật có đủ lượng Chiến ý yêu cầu để thi triển chiêu không.
    /// </summary>
    public bool HasEnoughPosture(float amount)
    {
        if (!usePostureSystem) return false;
        return currentPOS >= amount;
    }

    /// <summary>
    /// Tiêu tốn điểm Chiến ý của bản thân khi ra chiêu.
    /// </summary>
    public bool ConsumePosture(float amount)
    {
        if (!usePostureSystem || !HasEnoughPosture(amount)) return false;

        currentPOS -= amount;
        return true;
    }

    /// <summary>
    /// Kiểm tra xem nhân vật có bị Vỡ Chiến ý (Posture Break / Stagger) hay chưa.
    /// </summary>
    public bool IsPostureBroken()
    {
        if (!usePostureSystem) return false;
        return currentPOS >= maxPOS;
    }

    /// <summary>
    /// Đưa thanh Chiến ý về 0.
    /// </summary>
    public void ResetPosture()
    {
        if (!usePostureSystem) return;
        currentPOS = 0f;
    }
}
