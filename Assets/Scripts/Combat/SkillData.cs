using UnityEngine;

[System.Serializable]
public class SkillData
{
    [Header("Thông tin cơ bản")]
    public string skillName;
    [Tooltip("Hệ số sát thương (1.5 = 150% ATK)")]
    public float damageMultiplier = 1.0f;

    [Header("Thuộc tính Chiến đấu")]
    public float knockbackForce = 5f;
    public bool canBeParried = true;
    public float cooldownTime = 1f;

    [Header("Chi phí Stamina")]
    [Min(0f), Tooltip("Stamina tiêu hao một lần khi bắt đầu dùng skill, kể cả đánh hụt. Đặt 0 để miễn phí.")]
    public float staminaCost = 0f;
    [Min(0f), Tooltip("Hệ số sát thương lên thanh Stamina của quái, độc lập với DEF. 0 không làm mất sức bền.")]
    public float staminaDamageMultiplier = 1f;

    [Header("Cài đặt Tích lũy POS theo Frame (Animation Event)")]
    [Tooltip("Tích chọn nếu mỗi lần Frame Animation chém trúng quái sẽ cộng điểm POS")]
    public bool gainsPostureOnHit = true;

    [Tooltip("Số điểm POS điều chỉnh cộng cho bản thân MỖI LẦN Frame chém trúng quái")]
    public float postureGainPerHit = 10f; // <--- ĐIỀU CHỈNH CON SỐ NÀY TRONG INSPECTOR

    [Tooltip("Nếu TÍCH CHỌN: Điểm POS sẽ nhân theo số quái trúng ở frame đó. Nếu BỎ TÍCH: Chỉ cộng 1 lần cố định cho frame đó bất kể trúng bao nhiêu quái.")]
    public bool multiplyByTargetCount = false;

    [Header("Cài đặt Yêu cầu Chiến ý (Tiêu tốn POS của bản thân)")]
    [Tooltip("Tích chọn nếu kỹ năng này yêu cầu & tiêu tốn điểm Chiến ý của bản thân để kích hoạt")]
    public bool usePostureToActivate = false;

    [Tooltip("Lượng điểm Chiến ý cần thiết và bị trừ khi kích hoạt chiêu")]
    public float postureCost = 20f;

    [HideInInspector]
    public float lastUsedTime = -100f;

    [Header("Status buildup on confirmed HP damage")]
    [Min(0)] public float burnBuildup, poisonBuildup, stressBuildup;

    [Header("Cài đặt Hitbox")]
    public float attackRange = 1f;
    public Vector2 hitboxOffset;
    public GameObject hitEffectPrefab;
    public AudioClip hitSound;
}
