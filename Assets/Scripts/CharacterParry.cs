using UnityEngine;

public class CharacterParry : MonoBehaviour
{
    [Header("Trạng thái phòng thủ")]
    public bool isParrying = false;

    [Header("Cài đặt Hiệu ứng")]
    public GameObject parrySparkPrefab;
    public AudioClip parryClangSound;

    [Header("Cài đặt Phản chấn (Recoil)")]
    [Tooltip("Lực đẩy lùi kẻ tấn công khi bị Parry")]
    public float recoilKnockbackForce = 15f;

    private AnimationEventHandler animHandler;
    private Rigidbody2D rb;

    private void Awake()
    {
        animHandler = GetComponent<AnimationEventHandler>();
        rb = GetComponent<Rigidbody2D>();
    }

    // --- CÁC HÀM GỌI QUA ANIMATION EVENT ---

    [Tooltip("Gọi ở frame bắt đầu mở cửa sổ Parry (hoặc Frame 0 của Animation Parry)")]
    public void EnableParryWindow()
    {
        isParrying = true;

        // KHÓA DI CHUYỂN & LẬT MẶT khi đang trong thế thủ Parry
        if (animHandler != null)
        {
            animHandler.LockAction();
        }

        // Triệt tiêu ngay vận tốc di chuyển để không bị trượt chân
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    [Tooltip("Gọi ở frame đóng cửa sổ Parry (hoặc Frame cuối cùng của Animation Parry)")]
    public void DisableParryWindow()
    {
        isParrying = false;

        // MỞ KHÓA DI CHUYỂN & LẬT MẶT sau khi kết thúc Parry
        if (animHandler != null)
        {
            animHandler.UnlockAction();
        }
    }

    // --- LOGIC XỬ LÝ KHI PARRY TRÚNG ĐÍCH ---
    public void ExecuteParry(Transform attacker)
    {
        Debug.Log($"<color=cyan><b>{gameObject.name}</b></color> ĐÃ PARRY THÀNH CÔNG!");

        // 1. Sinh hiệu ứng tia lửa
        if (parrySparkPrefab != null && attacker != null)
        {
            Vector2 contactPoint = (transform.position + attacker.position) / 2f;
            Instantiate(parrySparkPrefab, contactPoint, Quaternion.identity);
        }

        // 2. Phát âm thanh
        if (parryClangSound != null)
        {
            AudioSource.PlayClipAtPoint(parryClangSound, transform.position);
        }

        // 3. NGẮT CHIÊU VÀ ÉP KẺ ĐỊCH LÙI LẠI (STAGGER)
        if (attacker != null)
        {
            CharacterHealth attackerHealth = attacker.GetComponent<CharacterHealth>();
            if (attackerHealth != null)
            {
                // Truyền 0 DMG, Lực đẩy lùi recoilKnockbackForce, canBeParried = false
                attackerHealth.TakeDamage(0f, 0f, recoilKnockbackForce, false, transform);
                Debug.Log($"Đã ngắt chiêu và đẩy lùi {attacker.name}!");
            }
        }

        // Reset trạng thái Parry nhưng giữ LockAction cho tới khi Animation hoàn tất
        isParrying = false;
    }
}