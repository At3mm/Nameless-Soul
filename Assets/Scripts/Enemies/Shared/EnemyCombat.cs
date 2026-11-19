using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quản lý logic chiến đấu, chọn kỹ năng dựa trên Cooldown chuẩn của SkillDamageManager,
/// và xử lý hiệu ứng lao tới (Lunge) khi Orc tấn công.
/// </summary>
[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class EnemyCombat : MonoBehaviour
{
    [Header("Cài đặt Khoảng cách lùi")]
    public float minRetreatDistance = 1.5f;
    public float maxRetreatDistance = 3f;

    [Header("Cài đặt Lunge (Lao tới)")]
    public float lungeSpeed = 10f;
    public float lungeDuration = 0.15f;
    [Tooltip("Chỉ lao nhẹ khi dùng các chiêu trong danh sách. Tắt để giữ hành vi cũ.")]
    public bool restrictLungeSkills;
    public List<string> lungeSkillNames = new List<string>();

    [Header("Cài đặt Vật cản")]
    public LayerMask obstacleLayer = 1 << 0;
    public float obstacleCheckRadius = 0.18f;

    [Header("Quản lý Kỹ năng & Cooldown chuẩn")]
    public SkillDamageManager skillManager;

    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D myCollider;

    public Vector2 RetreatTarget { get; private set; }
    public bool IsLunging { get; private set; }

    protected void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        // Tự động lấy SkillDamageManager nếu chưa gán trong Inspector
        if (skillManager == null) skillManager = GetComponent<SkillDamageManager>();
    }

    /// <summary>
    /// Kiểm tra xem người chơi có nằm trong tầm đánh của kỹ năng mặc định không.
    /// Hỗ trợ đa hướng 360 độ (bao gồm cả đứng thẳng hàng dọc theo trục Y).
    /// </summary>
    public virtual bool IsInAttackRange(Transform player)
    {
        if (player == null) return false;
        var awareness = GetComponent<EnemyAwareness>();
        if (awareness != null && !awareness.HasLineOfSight(player)) return false;

        // 0. Kiểm tra Line-of-Sight: Nếu bị vật cản (tường, đá...) chắn trực tiếp giữa Orc và Player thì không tính là trong tầm đánh
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float distToPlayer = toPlayer.magnitude;
        if (distToPlayer > 0.05f)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, distToPlayer, obstacleLayer);
            if (hit && hit.collider != null && hit.collider.gameObject != player.gameObject)
            {
                return false; // Bị tường/vật cản chắn giữa, không thể tấn công qua tường
            }
        }

        float currentAttackRange = 0.6f;
        float hitboxOffsetDist = 0.4f;

        if (skillManager != null && skillManager.skills != null && skillManager.skills.Count > 0)
        {
            var defaultSkill = skillManager.skills[0];
            currentAttackRange = defaultSkill.attackRange;
            hitboxOffsetDist = Mathf.Abs(defaultSkill.hitboxOffset.x);
        }

        // 1. Kiểm tra theo khoảng cách mép Collider giữa Orc và Player (chuẩn vật lý khi áp sát)
        var playerCol = player.GetComponent<Collider2D>();
        if (myCollider != null && playerCol != null)
        {
            ColliderDistance2D colDist = myCollider.Distance(playerCol);
            // Nếu 2 collider chạm nhau hoặc cách nhau trong tầm với của vũ khí
            if (colDist.isOverlapped || colDist.distance <= (currentAttackRange * 0.7f))
            {
                return true;
            }
        }

        // 2. Kiểm tra theo khoảng cách tâm định hướng (directional origin)
        Vector2 dirToPlayer = toPlayer.normalized;
        Vector2 directionalHitOrigin = (Vector2)transform.position + dirToPlayer * hitboxOffsetDist;

        return Vector2.Distance(directionalHitOrigin, player.position) <= currentAttackRange;
    }

    /// <summary>
    /// Lệnh tấn công: Kiểm tra Cooldown chuẩn từ SkillDamageManager.
    /// Nếu có chiêu sẵn sàng, chọn chiêu đó, tính Cooldown và phát Animation tương ứng.
    /// </summary>
    public bool TriggerAttack(Transform player, Vector2 startPosition, float maxChaseDistance)
    {
        var director = GetComponent<BossMovesetDirector>();
        if (director != null && director.IsBossActive && !director.CanStartAttack) return false;
        if (!isActiveAndEnabled || (GetComponent<EnemyBreak>() != null && GetComponent<EnemyBreak>().IsBroken)) return false;
        var awareness = GetComponent<EnemyAwareness>();
        if (player == null || (awareness != null && !awareness.HasLineOfSight(player))) return false;
        if (skillManager == null || skillManager.skills == null || skillManager.skills.Count == 0)
        {
            Debug.LogWarning($"[EnemyCombat] {gameObject.name} chưa gắn SkillDamageManager hoặc danh sách Kỹ năng bị trống!");
            return false;
        }

        // 1. Lọc các kỹ năng đã hồi chiêu (Ready) từ SkillDamageManager
        List<SkillData> readySkills = new List<SkillData>();
        foreach (var skill in skillManager.skills)
        {
            if (IsSkillAllowed(skill.skillName) && skillManager.IsSkillReady(skill.skillName) && CanSelectSkill(skill,player))
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
        SkillData selectedSkill = director != null && director.IsBossActive
            ? director.SelectOpening(readySkills, player)
            : readySkills[Random.Range(0, readySkills.Count)];

        return StartSkill(selectedSkill, player, startPosition, maxChaseDistance);
    }

    // Combo steps use the same resource spending, aiming and creature-specific hooks.
    public bool TriggerSkill(string skillName, Transform player, Vector2 home, float chaseLimit)
    {
        if (!isActiveAndEnabled || player == null || skillManager == null) return false;
        var health = GetComponent<CharacterHealth>();
        var targetHealth = player.GetComponent<CharacterHealth>();
        var broken = GetComponent<EnemyBreak>();
        var sight = GetComponent<EnemyAwareness>();
        if ((health != null && health.isDead) || (targetHealth != null && targetHealth.isDead) ||
            (broken != null && broken.IsBroken) || (sight != null && !sight.HasLineOfSight(player))) return false;
        return StartSkill(skillManager.skills.Find(s => s.skillName == skillName), player, home, chaseLimit);
    }

    private bool StartSkill(SkillData selectedSkill, Transform player, Vector2 startPosition, float maxChaseDistance)
    {
        if (selectedSkill == null || !IsSkillAllowed(selectedSkill.skillName) || !CanSelectSkill(selectedSkill,player)) return false;

        // 3. Đánh dấu kỹ năng đã được dùng để tính Cooldown chuẩn trong SkillData
        if (!skillManager.MarkSkillUsed(selectedSkill.skillName)) return false;

        // 4. Reset các Trigger cũ dựa trên danh sách SkillName trong Manager và Set Trigger mới
        foreach (var skill in skillManager.skills)
        {
            animator.ResetTrigger(skill.skillName);
        }

        // Kích hoạt Trigger đúng theo Skill Name được chọn ("NormalAttack" hoặc "HeavyAttack")
        animator.SetTrigger(selectedSkill.skillName);

        // 5. Cập nhật hướng tấn công vào SkillDamageManager để Hitbox quét đúng về phía Player (kể cả trục dọc)
        Vector2 attackDirection = (player.position - transform.position).normalized;
        skillManager.currentAttackDirection = attackDirection;
        OnAttackStarted(selectedSkill, player, startPosition, maxChaseDistance);
        var director = GetComponent<BossMovesetDirector>();
        if (director != null && director.IsBossActive)
            director.OnSkillStarted(selectedSkill.skillName, player, startPosition, maxChaseDistance);

        var leap = GetComponent<EliteOrcTargetedLeap>();
        if (leap != null && leap.TryBegin(selectedSkill.skillName, player, startPosition, maxChaseDistance))
            return true;

        // 6. Thực hiện hiệu ứng lao tới (Lunge) về phía Player (nếu phía trước không bị cản ngay sát mặt)
        RaycastHit2D frontObstacle = Physics2D.CircleCast(transform.position, obstacleCheckRadius, attackDirection, 0.35f, obstacleLayer);
        if (ShouldLunge(selectedSkill) &&
            (!frontObstacle || frontObstacle.collider == null || frontObstacle.collider.gameObject == player.gameObject))
        {
            StartCoroutine(LungeRoutine(attackDirection, player));
        }

        return true;
    }

    protected virtual bool ShouldLunge(SkillData skill) => !restrictLungeSkills || skill != null && lungeSkillNames.Contains(skill.skillName);
    public bool IsSkillAllowed(string skillName)
    {
        var phases=GetComponent<BossPhaseController>();
        return phases==null||phases.AllowsSkill(skillName);
    }
    protected virtual bool CanSelectSkill(SkillData skill,Transform target) => true;

    protected virtual void OnAttackStarted(SkillData skill, Transform target, Vector2 home, float chaseLimit) { }

    /// <summary>
    /// Coroutine duy trì lực lao tới (Lunge) khi tấn công.
    /// </summary>
    private IEnumerator LungeRoutine(Vector2 direction, Transform targetPlayer)
    {
        IsLunging = true;
        float timer = 0f;

        CharacterHealth health = GetComponent<CharacterHealth>();

        while (timer < lungeDuration)
        {
            if (health != null && health.isDead) break;

            // Kiểm tra vật cản phía trước khi đang lao tới -> Ngắt lunge ngay nếu sắp đâm vào tường/vật cản
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, obstacleCheckRadius, direction, 0.3f, obstacleLayer);
            if (hit && hit.collider != null && hit.collider.gameObject != gameObject && (targetPlayer == null || hit.collider.gameObject != targetPlayer.gameObject))
            {
                break;
            }

            rb.linearVelocity = direction * lungeSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        IsLunging = false;
    }

    public virtual void CancelAttack()
    {
        var director = GetComponent<BossMovesetDirector>();
        if (director != null) director.CancelCombo();
        StopAllCoroutines(); IsLunging = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null && skillManager != null)
            foreach (var skill in skillManager.skills)
                if (!string.IsNullOrEmpty(skill.skillName)) animator.ResetTrigger(skill.skillName);
    }

    /// <summary>
    /// Kiểm tra xem Orc đã có thể ngừng rút lui và chuyển sang rượt đuổi / tấn công tiếp hay chưa.
    /// Trả về true nếu có ít nhất 1 kỹ năng trong SkillDamageManager đã hồi xong.
    /// </summary>
    public bool CanChaseAgain()
    {
        if (skillManager == null) return false;
        foreach(var skill in skillManager.skills)
            if(IsSkillAllowed(skill.skillName)&&skillManager.IsSkillReady(skill.skillName))return true;
        return false;
    }
}
