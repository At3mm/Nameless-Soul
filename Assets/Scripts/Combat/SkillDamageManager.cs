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
    private PlayerController playerController;
    private readonly Dictionary<CharacterHealth,float> comboDamage=new Dictionary<CharacterHealth,float>();
    public void BeginDamageSequence(){comboDamage.Clear();}
    // Called only after parry, invulnerability and damage modifiers have been evaluated.
    public float LimitPlayerDamage(CharacterHealth target,float damage)
    {
        if(target==null||damage<=0||target.GetComponent<PlayerController>()==null||GetComponent<EnemyClassification>()==null)return damage;
        var area=GetComponent<EnemyAreaDamage>();
        var profile=area!=null?area.ResolveProfile():null;
        if(profile==null)return damage;
        var stats=target.GetComponent<CharacterStats>();if(stats==null)return damage;
        float spent;comboDamage.TryGetValue(target,out spent);
        float accepted=Mathf.Min(damage,Mathf.Max(0,stats.maxHP*Mathf.Clamp(profile.maxComboHPFraction,.1f,1f)-spent));
        comboDamage[target]=spent+accepted;
        return accepted;
    }

    private void Awake()
    {
        if (characterTransform == null) characterTransform = transform;
        myStats = GetComponent<CharacterStats>();
        playerController = GetComponent<PlayerController>();
    }

    /// <summary>
    /// Kiểm tra cooldown, Stamina và POS trước khi dùng kỹ năng.
    /// </summary>
    public bool IsSkillReady(string skillName) => IsSkillReady(skillName, out _);

    public bool IsSkillReady(string skillName, out string reason)
    {
        reason = "";
        var breakState = GetComponent<EnemyBreak>();
        if (breakState != null && breakState.IsBroken) return false;
        SkillData skill = skills?.Find(s => s != null && s.skillName == skillName);
        if (skill == null) { reason = "Chiêu thức chưa được thiết lập."; return false; }

        bool isCooldownReady = Time.time >= (skill.lastUsedTime + skill.cooldownTime);
        if (!isCooldownReady) { reason = "Chiêu thức chưa hồi."; return false; }

        float staminaCost = Mathf.Max(0f, skill.staminaCost) * (myStats != null ? myStats.StaminaCostMultiplier : 1f);
        if (staminaCost > 0f && (myStats == null || myStats.currentStamina < staminaCost)) { reason = "Không đủ Stamina."; return false; }

        if (skill.usePostureToActivate)
        {
            if (myStats == null || !myStats.usePostureSystem || !myStats.HasEnoughPosture(skill.postureCost))
            { reason = "Không đủ chiến ý (POS)."; return false; }
        }

        return true;
    }

    public void ResetCooldowns()
    {
        if (skills == null) return;
        foreach (var skill in skills)
            if (skill != null) skill.lastUsedTime = Time.time - Mathf.Max(0f,skill.cooldownTime);
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
        // Validate again so every caller spends resources and starts cooldown atomically.
        if (!IsSkillReady(skillName)) return false;
        SkillData skill = skills.Find(s => s.skillName == skillName);
        if (skill == null) return false;

        if (skill.usePostureToActivate && myStats != null && myStats.usePostureSystem)
        {
            myStats.ConsumePosture(skill.postureCost);
        }

        float staminaCost = Mathf.Max(0f, skill.staminaCost) * (myStats != null ? myStats.StaminaCostMultiplier : 1f);
        if (staminaCost > 0f)
        {
            myStats.currentStamina = Mathf.Max(0f, myStats.currentStamina - staminaCost);
            if (playerController != null) playerController.DelayStaminaRecovery();
        }
        var director=GetComponent<BossMovesetDirector>();
        if(director==null||!director.IsControlling)BeginDamageSequence();
        skill.lastUsedTime = Time.time;

        return true;
    }

    public Vector2 GetHitboxOrigin(SkillData skill)
    {
        if (skill == null) return characterTransform.position;
        float faceDirection = Mathf.Sign(characterTransform.localScale.x);
        return (Vector2)characterTransform.position + new Vector2(skill.hitboxOffset.x * faceDirection, skill.hitboxOffset.y);
    }

    /// <summary>
    /// Tính tâm Hitbox xoay theo hướng mục tiêu/hướng tấn công (Top-Down 360 độ).
    /// Giúp đòn đánh quét chuẩn xác cả khi đứng dọc (trên/dưới) lẫn đứng ngang.
    /// </summary>
    public Vector2 GetHitboxOrigin(SkillData skill, Vector2 attackDirection)
    {
        if (skill == null) return characterTransform.position;
        if (attackDirection.sqrMagnitude < 0.001f)
        {
            return GetHitboxOrigin(skill);
        }

        Vector2 dir = attackDirection.normalized;
        // Chiều dài offset tính từ tâm nhân vật
        float forwardDistance = skill.hitboxOffset.x;
        // Bù trừ độ lệch trục dọc (nếu có)
        float verticalOffset = skill.hitboxOffset.y;

        return (Vector2)characterTransform.position + dir * forwardDistance + new Vector2(0f, verticalOffset);
    }

    public Vector2 GetHitboxOrigin(int skillIndex)
    {
        if (skills == null || skills.Count <= skillIndex) return characterTransform.position;
        return GetHitboxOrigin(skills[skillIndex]);
    }

    public Vector2 GetHitboxOrigin(int skillIndex, Vector2 attackDirection)
    {
        if (skills == null || skills.Count <= skillIndex) return characterTransform.position;
        return GetHitboxOrigin(skills[skillIndex], attackDirection);
    }

    /// <summary>
    /// Hướng tấn công hiện tại (được cập nhật khi tung chiêu lao tới hoặc nhắm vào mục tiêu).
    /// </summary>
    [HideInInspector] public Vector2 currentAttackDirection = Vector2.zero;

    /// <summary>
    /// Hàm được gọi trực tiếp từ Animation Event trên từng Frame của Animation Clip.
    /// Mỗi khi Frame này chạy và chém trúng quái ➔ Cộng điểm POS ngay lập tức.
    /// </summary>
    public void ExecuteHit(string currentSkillName)
    {
        ExecuteHitInternal(currentSkillName,null);
    }
    public void ExecuteHitAtPosition(string currentSkillName,Vector2 position)
    {
        ExecuteHitInternal(currentSkillName,position);
    }
    // Geometry (beam, ray, etc.) is selected by the caller; damage stays in this pipeline.
    public void ExecuteHitOnTargets(string currentSkillName, IReadOnlyList<CharacterHealth> targets)
    {
        if (targets == null || targets.Count == 0) return;
        var unique = new List<CharacterHealth>(targets.Count);
        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            if (target != null && target.transform != transform && !target.transform.IsChildOf(transform) &&
                target.isActiveAndEnabled && !unique.Contains(target)) unique.Add(target);
        }
        if (unique.Count > 0) ExecuteHitInternal(currentSkillName, null, unique);
    }
    private void ExecuteHitInternal(string currentSkillName,Vector2? externalPosition, IReadOnlyList<CharacterHealth> explicitTargets = null)
    {
        if(BossPhaseController.Protects(gameObject))return;
        var breakState = GetComponent<EnemyBreak>();
        if (breakState != null && breakState.IsBroken) return;
        var health = GetComponent<CharacterHealth>();
        if (health != null && health.isDead) return;
        SkillData activeSkill = skills.Find(s => s.skillName == currentSkillName);
        if (activeSkill == null) return;

        // Swing sounds follow the existing damage-frame animation events, even on a miss.
        // Actual HP impact audio remains in CharacterHealth, so it is not duplicated here.
        var playerCombat = GetComponent<PlayerCombat>();
        if (playerCombat != null && playerCombat.isActiveAndEnabled)
        {
            if (currentSkillName == playerCombat.skill2SkillName) CombatSFX.Skill2Swing(transform.position);
            else if (currentSkillName == playerCombat.normalAttackSkillName) CombatSFX.AttackSwing(transform.position);
        }

        else if(!externalPosition.HasValue && GetComponent<EnemyBehavior>()!=null)
        {
            var enemyAudio = GetComponent<EnemyAudio>();
            if (enemyAudio == null || !enemyAudio.ReplacesDefaultWeaponSwing) CombatSFX.EnemyAttackSwing(transform.position);
            enemyAudio?.Attack(currentSkillName);
        }

        Vector2 hitOrigin = currentAttackDirection.sqrMagnitude > 0.001f
            ? GetHitboxOrigin(activeSkill, currentAttackDirection)
            : GetHitboxOrigin(activeSkill);
        if(externalPosition.HasValue)hitOrigin=externalPosition.Value;

        Collider2D[] hitTargets = explicitTargets == null
            ? Physics2D.OverlapCircleAll(hitOrigin, activeSkill.attackRange, targetLayer)
            : null;

        float basePhysicalDamage = myStats != null ? myStats.ATK * activeSkill.damageMultiplier : 10f * activeSkill.damageMultiplier;
        float trueDamage = myStats != null ? myStats.STR : 0f;
        bool isCrit = myStats != null && (Random.value <= myStats.CRIT_Rate);
        float finalPhysicalDamage = isCrit ? basePhysicalDamage * myStats.CRIT_Damage : basePhysicalDamage;

        int validHitsCount = 0; // Đếm số quái bị chém trúng ở Frame này

        int targetCount = explicitTargets != null ? explicitTargets.Count : hitTargets.Length;
        for (int targetIndex = 0; targetIndex < targetCount; targetIndex++)
        {
            Collider2D target = explicitTargets != null ? explicitTargets[targetIndex].GetComponent<Collider2D>() : hitTargets[targetIndex];
            CharacterHealth targetHealth = explicitTargets != null ? explicitTargets[targetIndex] : target.GetComponent<CharacterHealth>();

            if (targetHealth != null && !targetHealth.isDead && !targetHealth.IsUntargetable)
            {
                validHitsCount++; // Phát hiện 1 mục tiêu trúng đòn ở frame này

                // 1. Áp dụng Sát thương Máu (HP) & Knockback
                float hpBefore=targetHealth.GetComponent<CharacterStats>().currentHP;
                targetHealth.TakeDamage(finalPhysicalDamage, trueDamage, activeSkill.knockbackForce, activeSkill.canBeParried, characterTransform,
                    (Mathf.Max(0f, finalPhysicalDamage) + Mathf.Max(0f, trueDamage)) * Mathf.Max(0f, activeSkill.staminaDamageMultiplier),
                    playerCombat != null && currentSkillName == playerCombat.skill2SkillName);

                if(!targetHealth.isDead && targetHealth.GetComponent<CharacterStats>().currentHP<hpBefore &&
                    (activeSkill.burnBuildup>0 || activeSkill.poisonBuildup>0 || activeSkill.stressBuildup>0))
                {
                    var status=targetHealth.GetComponent<CharacterDebuffs>();if(status==null)status=targetHealth.gameObject.AddComponent<CharacterDebuffs>();
                    status.AddBuildup(CharacterDebuffs.Kind.Burning,activeSkill.burnBuildup,0,status.burnMaxHPPercentPerSecond*SoulConsumption.OutgoingMultiplier(characterTransform));
                    status.AddBuildup(CharacterDebuffs.Kind.Poison,activeSkill.poisonBuildup,0,status.poisonMaxHPPercentPerSecond*SoulConsumption.OutgoingMultiplier(characterTransform));
                    status.AddBuildup(CharacterDebuffs.Kind.Stress,activeSkill.stressBuildup);
                }
                // 2. Hiệu ứng Visual & Sound
                if (activeSkill.hitEffectPrefab != null)
                    Instantiate(activeSkill.hitEffectPrefab, target != null ? target.bounds.center : targetHealth.transform.position, Quaternion.identity);

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
