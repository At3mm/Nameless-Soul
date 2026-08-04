using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý danh sách kỹ năng, Cooldown chuẩn, Tích lũy/Tiêu tốn Chiến ý, Sát thương và Hitbox.
/// </summary>
public class SkillDamageManager : MonoBehaviour
{
    public List<SkillData> skills = new List<SkillData>();
    public Transform characterTransform;
    public LayerMask targetLayer;
    private CharacterStats myStats;

    private void Awake()
    {
        if (characterTransform == null) characterTransform = transform;
        myStats = GetComponent<CharacterStats>();
    }

    /// <summary>
    /// Kiểm tra xem một kỹ năng đã SẴN SÀNG chưa (Kiểm tra Cooldown & Điểm POS).
    /// </summary>
    public bool IsSkillReady(string skillName)
    {
        SkillData skill = skills.Find(s => s.skillName == skillName);
        if (skill == null) return false;

        bool isCooldownReady = Time.time >= (skill.lastUsedTime + skill.cooldownTime);
        if (!isCooldownReady) return false;

        if (skill.usePostureToActivate)
        {
            if (myStats == null || !myStats.usePostureSystem) return false;
            if (!myStats.HasEnoughPosture(skill.postureCost)) return false;
        }

        return true;
    }

    public bool IsAnySkillReady()
    {
        if (skills == null || skills.Count == 0) return false;

        foreach (var skill in skills)
        {
            if (IsSkillReady(skill.skillName)) return true;
        }
        return false;
    }

    public bool MarkSkillUsed(string skillName)
    {
        SkillData skill = skills.Find(s => s.skillName == skillName);
        if (skill == null) return false;

        skill.lastUsedTime = Time.time;

        if (skill.usePostureToActivate && myStats != null && myStats.usePostureSystem)
        {
            myStats.ConsumePosture(skill.postureCost);
        }

        return true;
    }

    public Vector2 GetHitboxOrigin(SkillData skill)
    {
        if (skill == null) return characterTransform.position;
        float faceDirection = Mathf.Sign(characterTransform.localScale.x);
        return (Vector2)characterTransform.position + new Vector2(skill.hitboxOffset.x * faceDirection, skill.hitboxOffset.y);
    }

    public Vector2 GetHitboxOrigin(int skillIndex)
    {
        if (skills == null || skills.Count <= skillIndex) return characterTransform.position;
        return GetHitboxOrigin(skills[skillIndex]);
    }

    /// <summary>
    /// Hàm được gọi trực tiếp từ Animation Event trên từng Frame của Animation Clip.
    /// Mỗi khi Frame này chạy và chém trúng quái ➔ Cộng điểm POS ngay lập tức.
    /// </summary>
    public void ExecuteHit(string currentSkillName)
    {
        SkillData activeSkill = skills.Find(s => s.skillName == currentSkillName);
        if (activeSkill == null) return;

        Vector2 hitOrigin = GetHitboxOrigin(activeSkill);
        Collider2D[] hitTargets = Physics2D.OverlapCircleAll(hitOrigin, activeSkill.attackRange, targetLayer);

        float basePhysicalDamage = myStats != null ? myStats.ATK * activeSkill.damageMultiplier : 10f * activeSkill.damageMultiplier;
        float trueDamage = myStats != null ? myStats.STR : 0f;
        bool isCrit = myStats != null && (Random.value <= myStats.CRIT_Rate);
        float finalPhysicalDamage = isCrit ? basePhysicalDamage * myStats.CRIT_Damage : basePhysicalDamage;

        int validHitsCount = 0; // Đếm số quái bị chém trúng ở Frame này

        foreach (Collider2D target in hitTargets)
        {
            CharacterHealth targetHealth = target.GetComponent<CharacterHealth>();

            if (targetHealth != null && !targetHealth.isDead)
            {
                validHitsCount++; // Phát hiện 1 mục tiêu trúng đòn ở frame này

                // 1. Áp dụng Sát thương Máu (HP) & Knockback
                targetHealth.TakeDamage(finalPhysicalDamage, trueDamage, activeSkill.knockbackForce, activeSkill.canBeParried, characterTransform);

                // 2. Hiệu ứng Visual & Sound
                if (activeSkill.hitEffectPrefab != null)
                    Instantiate(activeSkill.hitEffectPrefab, target.bounds.center, Quaternion.identity);

                if (activeSkill.hitSound != null)
                    AudioSource.PlayClipAtPoint(activeSkill.hitSound, target.transform.position);
            }
        }

        // 3. CỘNG ĐIỂM POS TRỰC TIẾP CHO MỖI LẦN FRAME ANIMATION CHÉM TRÚNG QUÁI
        if (validHitsCount > 0 && activeSkill.gainsPostureOnHit && myStats != null && myStats.usePostureSystem)
        {
            // Tính số điểm POS được cộng ở Frame này:
            // - Nếu MultiplyByTargetCount = true: + (postureGainPerHit * số quái trúng)
            // - Nếu MultiplyByTargetCount = false: + (postureGainPerHit cố định cho nhát chém đó)
            float postureGainedThisFrame = activeSkill.multiplyByTargetCount
                ? activeSkill.postureGainPerHit * validHitsCount
                : activeSkill.postureGainPerHit;

            myStats.AddPosture(postureGainedThisFrame);

            Debug.Log($"<color=cyan>[Frame Hit POS]</color> Frame Animation '{currentSkillName}' vung trúng ➔ Bản thân được cộng +{postureGainedThisFrame} POS! (POS hiện tại: {myStats.currentPOS}/{myStats.maxPOS})");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (characterTransform == null) characterTransform = transform;
        foreach (var skill in skills)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
            Vector2 hitOrigin = GetHitboxOrigin(skill);
            Gizmos.DrawWireSphere(hitOrigin, skill.attackRange);
        }
    }
}