using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class CharacterHealth : MonoBehaviour
{
    public event System.Action<float> Healed;
    public static event System.Action<CharacterHealth> AnyDied;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDeathEvents() { AnyDied = null; }
    private CharacterStats stats;
    private Rigidbody2D rb;
    private Animator animator;
    private bool untargetable;
    private int untargetableState;
    private float untargetableStart;
    private CharacterInvulnerability invulnerability;
    private CharacterParry parryComponent;
    private AnimationEventHandler animHandler;
    private PlayerStun stun;
    private EnemyBreak enemyBreak;

    [HideInInspector] public bool isDead = false;
    [Header("Bảo vệ sau khi trúng đòn")]
    [Min(0f), Tooltip("Số giây miễn sát thương sau một đòn trúng. Player mặc định 1 giây.")]
    public float damageInvulnerabilityDuration = 0.3f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        invulnerability = GetComponent<CharacterInvulnerability>();
        parryComponent = GetComponent<CharacterParry>();
        animHandler = GetComponent<AnimationEventHandler>();
        stun = GetComponent<PlayerStun>();
        enemyBreak = GetComponent<EnemyBreak>();
        if(GetComponent<EnemyFlight>()==null && (GetComponent<PlayerController>()!=null || GetComponent<EnemyBehavior>()!=null) && GetComponent<CharacterFootsteps>()==null)gameObject.AddComponent<CharacterFootsteps>();
    }

    public void TakeDamage(float physDamage, float trueDamage, float knockbackForce, bool canBeParried, Transform attacker, float staminaDamage = -1f, bool heavyImpact = false)
    {
        if (isDead || IsUntargetable) return;

        // 1. KIỂM TRA PARRY
        if (canBeParried && parryComponent != null && parryComponent.isActiveAndEnabled && parryComponent.isParrying)
        {
            parryComponent.ExecuteParry(attacker);
            return;
        }

        // 2. KIỂM TRA VÔ ĐỊCH (I-FRAMES)
        if (invulnerability != null && invulnerability.isInvulnerable) return;

        // 4. TÍNH SÁT THƯƠNG
        var attackerBuffs = attacker != null ? attacker.GetComponentInParent<CharacterBuffs>() : null;
        float outgoingMultiplier = attackerBuffs != null ? attackerBuffs.DamageMultiplier : 1f;
        outgoingMultiplier*=SoulConsumption.OutgoingMultiplier(attacker);
        var buffs = GetComponent<CharacterBuffs>();
        float reduction = buffs != null ? buffs.IncomingDamageMultiplier : 1f;
        reduction*=SoulConsumption.IncomingMultiplier(transform);
        float actualPhysDamage = Mathf.Max(0, Mathf.Max(0f, physDamage) * outgoingMultiplier - stats.EffectiveDefense);
        float totalDamage = (actualPhysDamage + Mathf.Max(0f, trueDamage) * outgoingMultiplier) * reduction;
        totalDamage*=FinalBossDamageBonus.MultiplierFor(this,attacker);
        float beforeBudget=totalDamage;
        var attackManager=attacker!=null?attacker.GetComponentInParent<SkillDamageManager>():null;
        if(attackManager!=null)totalDamage=attackManager.LimitPlayerDamage(this,totalDamage);
        if(beforeBudget>0f&&totalDamage<=0f)return;
        // 3. KHÓA HÀNH ĐỘNG & ÉP ANIMATION
        if (animHandler != null)
        {
            animHandler.LockAction();
        }

        if (animator != null && (stun == null || !stun.IsStunned) && (enemyBreak == null || !enemyBreak.IsBroken))
        {
            animator.Play("TakeDamage", -1, 0f);
        }


        stats.currentHP -= totalDamage;
        if (totalDamage > 0f)
        {
            if(stats.currentHP>0)GetComponent<EnemyAudio>()?.Hurt();
            PixelCombatImpact.Blood(this, attacker);
            CombatSFX.Hit(transform.position, heavyImpact, attacker!=null && attacker.GetComponentInParent<EnemyBehavior>()!=null);
        }

        Debug.Log($"<color=red><b>{gameObject.name}</b></color> nhận {totalDamage} DMG. HP còn: {stats.currentHP}");

        if (stats.currentHP <= 0)
        {
            Die();
            return;
        }

        GetComponent<BossPhaseController>()?.EvaluatePhase();
        if(BossPhaseController.Protects(gameObject))return;

        if (stun != null) stun.TryStunOnDamage(totalDamage);
        if (enemyBreak != null && (physDamage > 0f || trueDamage > 0f || staminaDamage > 0f))
            enemyBreak.ReceiveHit((staminaDamage >= 0f ? staminaDamage : Mathf.Max(0f, physDamage) + Mathf.Max(0f, trueDamage)) * outgoingMultiplier);

        if (invulnerability != null && damageInvulnerabilityDuration > 0f)
            invulnerability.GrantInvulnerability(damageInvulnerabilityDuration);
        if (enemyBreak != null && enemyBreak.IsBroken) { CancelKnockback(); return; }

        // 5. TÍNH LỰC VÀ BẬT COROUTINE ĐẨY LÙI
        float resistanceMultiplier = 1f - stats.EffectiveResistance;
        float finalKnockback = knockbackForce * resistanceMultiplier;

        if (rb != null && attacker != null && finalKnockback > 0f)
        {
            Vector2 knockbackDir = (transform.position - attacker.position).normalized;

            // Dừng Coroutine đẩy lùi cũ (nếu có) và khởi chạy Coroutine đẩy lùi mới
            StopCoroutine("ApplyKnockbackRoutine");
            StartCoroutine(ApplyKnockbackRoutine(knockbackDir, finalKnockback));
        }
    }

    public bool IsUntargetable
    {
        get
        {
            if(World.QuestBossEncounter.Protects(gameObject))return true;
            if(BossPhaseController.Protects(gameObject))return true;
            if (!untargetable || isDead || !isActiveAndEnabled || animator == null || !animator.isActiveAndEnabled)
                return false;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            return state.fullPathHash == untargetableState && state.normalizedTime >= untargetableStart &&
                (!animator.IsInTransition(0) || animator.GetNextAnimatorStateInfo(0).fullPathHash == untargetableState);
        }
    }

    public void BeginUntargetable(AnimationEvent animationEvent)
    {
        if (isDead || !isActiveAndEnabled || !animationEvent.isFiredByAnimator) return;
        var state = animationEvent.animatorStateInfo;
        untargetableState = state.fullPathHash;
        untargetableStart = state.normalizedTime;
        untargetable = true;
    }

    public void EndUntargetable() => untargetable = false;

    private void LateUpdate()
    {
        if (untargetable && !IsUntargetable) EndUntargetable();
    }

    private void OnDisable() => EndUntargetable();

    public float Heal(float amount)
    {
        if (isDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return 0f;
        float before = stats.currentHP;
        stats.currentHP = Mathf.Clamp(before + amount, 0f, Mathf.Max(0f, stats.maxHP));
        float recovered = Mathf.Max(0f, stats.currentHP - before);
        if (recovered > 0f)
        {
            StatusEffectDefaults.EnsureVisuals(gameObject, true);
            Healed?.Invoke(recovered);
        }
        return recovered;
    }

    public void CancelKnockback()
    {
        StopCoroutine("ApplyKnockbackRoutine");
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    public void TakeDamageOverTime(float damage)
    {
        if (isDead || World.QuestBossEncounter.Protects(gameObject) || BossPhaseController.Protects(gameObject) || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage)) return;
        var buffs = GetComponent<CharacterBuffs>();
        damage *= buffs != null ? buffs.IncomingDamageMultiplier : 1f;
        damage *= SoulConsumption.IncomingMultiplier(transform);
        stats.currentHP = Mathf.Max(0f, stats.currentHP - damage);
        if (stats.currentHP <= 0f) Die();
        else GetComponent<BossPhaseController>()?.EvaluatePhase();
    }

    /// <summary>
    /// Coroutine cưỡng chế duy trì vận tốc trượt lùi trong thời gian ngắn (0.12s)
    /// Tránh việc bị FixedUpdate hoặc Linear Drag dập tắt lực tức thì.
    /// </summary>
    private IEnumerator ApplyKnockbackRoutine(Vector2 direction, float force)
    {
        float timer = 0f;
        float duration = 0.25f; // Thời gian duy trì lực đẩy trượt lùi

        while (timer < duration)
        {
            if (isDead) yield break;

            // Cưỡng chế gán vận tốc đẩy lùi trượt mượt mà
            rb.linearVelocity = direction * force;

            timer += Time.deltaTime;
            yield return null;
        }

        // Sau khi hết thời gian đẩy cưỡng chế, giảm tốc mượt về 0
        rb.linearVelocity = Vector2.zero;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        World.PlayerDeathDrops.OnDeath(this);
        GetComponent<EnemyAudio>()?.Death();
        World.EliteLifeSeedDrops.OnDeath(this);
        // Award before recording defeat, so the persisted encounter cannot hide an unclaimed item.
        bool receivedBossItem = BossItemReward.GrantForDeath(this);
        var defeatedBoss = GetComponent<EnemyClassification>();
        if (!receivedBossItem && defeatedBoss != null && defeatedBoss.enemyType == EnemyClassification.EnemyType.Boss)
            BossRewardNotice.ShowVictory(defeatedBoss.DisplayName, defeatedBoss.DefeatId);
        EnemyDefeatState.RecordDefeat(GetComponent<EnemyClassification>());

        StopAllCoroutines();

        if (animator != null) animator.Play("Die", -1, 0f);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Collider2D coll = GetComponent<Collider2D>();
        if (coll != null) coll.enabled = false;

        MonoBehaviour[] scriptsToDisable = GetComponents<MonoBehaviour>();
        foreach (var script in scriptsToDisable)
        {
            if (script != this && script != stats && script.GetType() != typeof(AnimationEventHandler))
            {
                script.enabled = false;
            }
        }
        AnyDied?.Invoke(this);
        // Flush only after death listeners have granted currency and updated quests.
        // Keep the prayed-at shrine as the continue/respawn point.
        if (defeatedBoss != null && defeatedBoss.enemyType != EnemyClassification.EnemyType.Normal)
            GameEconomy.SaveProgress();
    }
}
