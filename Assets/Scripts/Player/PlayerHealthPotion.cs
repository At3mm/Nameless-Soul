using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterHealth), typeof(CharacterStats))]
public sealed class PlayerHealthPotion : MonoBehaviour
{
    public const int StartingCapacity = 3;
    public const int MaximumCapacity = 10;
    [Range(1, MaximumCapacity)] public int capacity = StartingCapacity;
    [SerializeField, Min(0)] private int charges = StartingCapacity;
    [Range(1f, 100f)] public float healMaxHPPercent = 40f;
    [Min(0f)] public float cooldown = 1.5f;
    [HideInInspector] public Key useKey = Key.R; // Legacy serialized value; GameControls owns the binding.
    private CharacterHealth health;
    private CharacterStats stats;
    private PlayerController controller;
    private AnimationEventHandler actions;
    private float readyAt;
    private int baseCapacity;

    private void ApplyCapacityUpgrade()
    {
        int bonus = GameEconomy.PotionCapacityUpgrades;
        capacity = (int)System.Math.Min(MaximumCapacity, (long)baseCapacity + bonus);
        charges = Mathf.Clamp(charges, 0, capacity);
    }
    private void OnDestroy() { GameEconomy.Changed -= ApplyCapacityUpgrade; }

    public int Charges => charges;
    public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
    public bool CanUse => isActiveAndEnabled && health != null && !health.isDead &&
        stats != null && stats.currentHP > 0f && stats.currentHP < stats.maxHP &&
        charges > 0 && CooldownRemaining <= 0f && Time.timeScale > 0f &&
        (controller == null || controller.isActiveAndEnabled) &&
        (actions == null || !actions.isActionLocked);

    private void Awake()
    {
        health = GetComponent<CharacterHealth>();
        stats = GetComponent<CharacterStats>();
        controller = GetComponent<PlayerController>();
        actions = GetComponent<AnimationEventHandler>();
        baseCapacity = Mathf.Clamp(capacity, 1, MaximumCapacity);
        ApplyCapacityUpgrade();
        GameEconomy.Changed += ApplyCapacityUpgrade;
        charges = Mathf.Clamp(charges, 0, Mathf.Max(1, capacity));
    }
    private void Update()
    {
        if (GameControls.Pressed(GameControls.Action.Heal)) TryUse();
    }
    public bool TryUse()
    {
        if (GameControls.Blocked || Time.timeScale <= 0f) return false;
        if (!CanUse)
        {
            if (isActiveAndEnabled && health != null && !health.isDead && stats != null && stats.currentHP > 0f &&
                (controller == null || controller.isActiveAndEnabled) && (actions == null || !actions.isActionLocked))
                GetComponent<World.PlayerInteractor>()?.ShowNotice(charges <= 0 ? "Đã hết bình máu." :
                    stats.currentHP >= stats.maxHP ? "Máu đã đầy." : "Bình máu chưa hồi.");
            return false;
        }
        float healed = health.Heal(stats.maxHP * Mathf.Clamp(healMaxHPPercent, 1f, 100f) * 0.01f);
        if (healed <= 0f) return false;
        CombatSFX.Heal(transform.position);
        charges--;
        readyAt = Time.time + Mathf.Max(0f, cooldown);
        return true;
    }
    public void Use() { TryUse(); }
    public void ResetForRest()
    {
        charges = Mathf.Max(1,capacity);
        readyAt = Time.time;
    }
    public void RestoreTravelState(int savedCharges,float remainingCooldown)
    {
        charges = Mathf.Clamp(savedCharges,0,Mathf.Max(1,capacity));
        readyAt = Time.time + (float.IsNaN(remainingCooldown) || float.IsInfinity(remainingCooldown) ? 0 : Mathf.Max(0,remainingCooldown));
    }
    public int AddCharges(int amount)
    {
        int added = Mathf.Min(Mathf.Max(0, amount), Mathf.Max(0, capacity - charges));
        charges += added;
        return added;
    }
}
