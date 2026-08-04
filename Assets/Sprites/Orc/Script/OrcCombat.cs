using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý logic chiến đấu, chọn kỹ năng dựa trên Cooldown chuẩn của SkillDamageManager,
/// và xử lý hiệu ứng lao tới (Lunge) khi Orc tấn công.
/// </summary>
[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class OrcCombat : MonoBehaviour
{
    [Header("Cài đặt Khoảng cách lùi")]
    public float minRetreatDistance = 1.5f;
    public float maxRetreatDistance = 3f;

    [Header("Cài đặt Lunge (Lao tới)")]
    public float lungeSpeed = 10f;
    public float lungeDuration = 0.15f;

    [Header("Quản lý Kỹ năng & Cooldown chuẩn")]
    public SkillDamageManager skillManager;

    private Animator animator;
    private Rigidbody2D rb;

    public Vector2 RetreatTarget { get; private set; }
    public bool IsLunging { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Tự động lấy SkillDamageManager nếu chưa gán trong Inspector
        if (skillManager == null) skillManager = GetComponent<SkillDamageManager>();
    }

    /// <summary>
    /// Kiểm tra xem người chơi có nằm trong tầm đánh của kỹ năng mặc định (kỹ năng đầu tiên trong danh sách) không.
    /// </summary>
    public bool IsInAttackRange(Transform player)
    {
        if (player == null) return false;

        float currentAttackRange = 0.5f;
        Vector2 attackCenter = transform.position;

        if (skillManager != null && skillManager.skills != null && skillManager.skills.Count > 0)
        {
            var defaultSkill = skillManager.skills[0];
            currentAttackRange = defaultSkill.attackRange;
            attackCenter = skillManager.GetHitboxOrigin(0);
        }

        return Vector2.Distance(attackCenter, player.position) <= currentAttackRange;
    }

    /// <summary>
    /// Lệnh tấn công: Kiểm tra Cooldown chuẩn từ SkillDamageManager.
    /// Nếu có chiêu sẵn sàng, chọn chiêu đó, tính Cooldown và phát Animation tương ứng.
    /// </summary>
    public bool TriggerAttack(Transform player, Vector2 startPosition, float maxChaseDistance)
    {
        if (skillManager == null || skillManager.skills == null || skillManager.skills.Count == 0)
        {
            Debug.LogWarning($"[OrcCombat] {gameObject.name} chưa gắn SkillDamageManager hoặc danh sách Kỹ năng bị trống!");
            return false;
        }

        // 1. Lọc các kỹ năng đã hồi chiêu (Ready) từ SkillDamageManager
        List<SkillData> readySkills = new List<SkillData>();
        foreach (var skill in skillManager.skills)
        {
            if (skillManager.IsSkillReady(skill.skillName))
            {
                readySkills.Add(skill);
            }
        }

        // Nếu không có kỹ năng nào hồi xong -> Không thể tấn công
        if (readySkills.Count == 0)
        {
            return false;
        }

        // 2. Chọn ngẫu nhiên 1 kỹ năng sẵn sàng trong danh sách
        SkillData selectedSkill = readySkills[Random.Range(0, readySkills.Count)];

        // 3. Đánh dấu kỹ năng đã được dùng để tính Cooldown chuẩn trong SkillData
        skillManager.MarkSkillUsed(selectedSkill.skillName);

        // 4. Reset các Trigger cũ dựa trên danh sách SkillName trong Manager và Set Trigger mới
        foreach (var skill in skillManager.skills)
        {
            animator.ResetTrigger(skill.skillName);
        }

        // Kích hoạt Trigger đúng theo Skill Name được chọn ("NormalAttack" hoặc "HeavyAttack")
        animator.SetTrigger(selectedSkill.skillName);

        // 5. Thực hiện hiệu ứng lao tới (Lunge) về phía Player
        Vector2 attackDirection = (player.position - transform.position).normalized;
        StartCoroutine(LungeRoutine(attackDirection));

        return true;
    }

    /// <summary>
    /// Coroutine duy trì lực lao tới (Lunge) khi tấn công.
    /// </summary>
    private IEnumerator LungeRoutine(Vector2 direction)
    {
        IsLunging = true;
        float timer = 0f;

        CharacterHealth health = GetComponent<CharacterHealth>();

        while (timer < lungeDuration)
        {
            if (health != null && health.isDead) break;

            rb.linearVelocity = direction * lungeSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        IsLunging = false;
    }

    /// <summary>
    /// Kiểm tra xem Orc đã có thể ngừng rút lui và chuyển sang rượt đuổi / tấn công tiếp hay chưa.
    /// Trả về true nếu có ít nhất 1 kỹ năng trong SkillDamageManager đã hồi xong.
    /// </summary>
    public bool CanChaseAgain()
    {
        if (skillManager == null) return false;
        return skillManager.IsAnySkillReady();
    }
}