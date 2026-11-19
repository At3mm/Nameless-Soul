using UnityEngine;
using UnityEngine.UI;

/// <summary>Read-only HP/Stamina display bound to the player's live CharacterStats.</summary>
public class PlayerVitalsHUD : MonoBehaviour
{
    [Header("Player binding")]
    public CharacterStats target;
    public bool autoFindPlayer = true;
    [Tooltip("Hiển thị số gọn, ví dụ 70/100.")]
    public bool compactValues = true;
    [Header("HP")]
    public Image healthFill;
    public Text healthValue;
    public Color healthColor = new Color(0.96f, 0.12f, 0.17f);
    [Header("Stamina")]
    public Image staminaFill;
    public Text staminaValue;
    public Color staminaColor = new Color(0.25f, 0.91f, 0.34f);
    [Header("Currency row below stamina")]
    public Text goldValue;
    public Text experienceValue;
    [Header("POS")]
    public PixelPOSOrb postureOrb;
    public Text postureValue;
    [Header("Animation (unscaled seconds)")]
    [Min(0f)] public float decreaseDuration = 0.15f;
    [Min(0f)] public float increaseDuration = 0.25f;
    [Header("Health recovery animation")]
    [Min(0f), Tooltip("Duration of each health gain animation, independent of amount healed.")]
    public float healthRecoveryDuration = 0.65f;
    public Color healthRecoveryColor = new Color(1f, 0.65f, 0.55f);
    [Min(0f)] public float healthRecoveryFlashDuration = 0.5f;
    [Range(0f, 1f)] public float healthRecoveryFlashStrength = 0.55f;
    [Header("Resource loss effect")]
    public Image healthLossFill;
    public Image staminaLossFill;
    public Color healthLossColor = new Color(1f, 0.65f, 0.23f);
    public Color staminaLossColor = new Color(0.78f, 1f, 0.48f);
    [Min(0f)] public float lossHoldDuration = 0.3f;
    [Min(0f)] public float lossCatchupDuration = 0.65f;
    [Min(0f)] public float lossFlashDuration = 0.2f;
    [Range(0f, 1f)] public float lossFlashStrength = 0.6f;
    [Range(1f, 30f)] public float effectFPS = 8f;
    private PixelEffectClock effectClock;
    private bool sampleVisuals;

    private float displayedHealth;
    private float displayedStamina;
    private float displayedPosture;
    private float nextSearch;
    private bool initialized;
    private CharacterStats displayedTarget;
    private float healthTrail, staminaTrail, previousHealth, previousStamina;
    private float healthLostAt = -100f, staminaLostAt = -100f;
    private float healthGainStart, healthGainElapsed;
    private float healthGainedAt = -100f;
    private bool animatingHealthGain;

    private void OnEnable()
    {
        initialized = false; nextSearch = 0f; effectClock = default;
        GameEconomy.Changed += RefreshCurrency;
        RefreshCurrency();
        // Retain the legacy Text references (also used as a font source by interaction prompts).
        if (healthValue != null) { healthValue.text = ""; healthValue.enabled = false; }
        if (staminaValue != null) { staminaValue.text = ""; staminaValue.enabled = false; }
    }
    private void OnDisable() { GameEconomy.Changed -= RefreshCurrency; }
    private void RefreshCurrency()
    {
        if (goldValue != null) goldValue.text = GameEconomy.Gold.ToString();
        if (experienceValue != null) experienceValue.text = GameEconomy.Experience.ToString();
    }
    private void Start() { RefreshHUD(true); if(GetComponent<PlayerStatusHUD>()==null)gameObject.AddComponent<PlayerStatusHUD>(); }
    private void LateUpdate() { RefreshHUD(); }

    public void Bind(CharacterStats stats)
    {
        target = stats;
        initialized = false;
        RefreshHUD(true);
    }

    public void RefreshHUD(bool immediate = false)
    {
        sampleVisuals = effectClock.Tick(Time.unscaledDeltaTime, effectFPS) || immediate || !initialized || displayedTarget != target;
        if (target == null && autoFindPlayer && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.5f;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.GetComponent<CharacterStats>();
        }
        if (target == null)
        {
            initialized = false;
            if (healthFill != null) healthFill.fillAmount = 0;
            if (staminaFill != null) staminaFill.fillAmount = 0;
            if (healthLossFill != null) healthLossFill.fillAmount = 0;
            if (staminaLossFill != null) staminaLossFill.fillAmount = 0;
            if (postureOrb != null) postureOrb.SetLevel(0f);
            if (postureValue != null) postureValue.text = "POS --/--";
            return;
        }
        float hpMaximum = Mathf.Max(0f, target.maxHP);
        float staminaMaximum = Mathf.Max(0f, target.maxStamina);
        float hp = Mathf.Clamp(target.currentHP, 0f, hpMaximum);
        float stamina = Mathf.Clamp(target.currentStamina, 0f, staminaMaximum);
        float hpRatio = hpMaximum > 0f ? hp / hpMaximum : 0f;
        float staminaRatio = staminaMaximum > 0f ? stamina / staminaMaximum : 0f;
        float posMaximum = Mathf.Max(0f, target.maxPOS);
        float pos = Mathf.Clamp(target.currentPOS, 0f, posMaximum);
        float posRatio = posMaximum > 0f ? pos / posMaximum : 0f;
        if (immediate || !initialized || displayedTarget != target)
        {
            displayedHealth = hpRatio;
            displayedStamina = staminaRatio;
            displayedPosture = posRatio;
            healthTrail = previousHealth = hpRatio;
            staminaTrail = previousStamina = staminaRatio;
            healthLostAt = staminaLostAt = -100f;
            healthGainedAt = -100f;
            animatingHealthGain = false;
            initialized = true;
            displayedTarget = target;
        }
        else
        {
            if (hpRatio > previousHealth + 0.00001f)
            {
                healthGainStart = displayedHealth;
                healthGainElapsed = 0f;
                healthGainedAt = Time.unscaledTime;
                animatingHealthGain = true;
            }
            else if (hpRatio < previousHealth - 0.00001f)
            {
                animatingHealthGain = false;
                healthGainedAt = -100f;
            }
            TrackLoss(hpRatio, ref previousHealth, displayedHealth, ref healthTrail, ref healthLostAt);
            TrackLoss(staminaRatio, ref previousStamina, displayedStamina, ref staminaTrail, ref staminaLostAt);
            displayedHealth = AnimateHealth(hpRatio, Mathf.Min(Time.unscaledDeltaTime, 0.1f));
            displayedStamina = Animate(displayedStamina, staminaRatio);
            displayedPosture = Animate(displayedPosture, posRatio);
        }
        DrawLoss(healthLossFill, displayedHealth, ref healthTrail, healthLostAt, healthLossColor);
        DrawLoss(staminaLossFill, displayedStamina, ref staminaTrail, staminaLostAt, staminaLossColor);
        if (sampleVisuals && postureOrb != null) postureOrb.SetLevel(displayedPosture);
        if (postureValue != null) postureValue.text = "POS " + Mathf.CeilToInt(pos) + "/" + Mathf.CeilToInt(posMaximum);
        if (sampleVisuals && healthFill != null)
        {
            healthFill.fillAmount = displayedHealth;
            float glow = healthRecoveryFlashDuration > 0f
                ? 1f - Mathf.Clamp01((Time.unscaledTime - healthGainedAt) / healthRecoveryFlashDuration) : 0f;
            healthFill.color = Color.Lerp(healthColor, healthRecoveryColor, glow * healthRecoveryFlashStrength);
        }
        if (sampleVisuals && staminaFill != null)
        {
            staminaFill.fillAmount = displayedStamina;
            staminaFill.color = staminaColor;
        }
    }

    private float AnimateHealth(float goal, float deltaTime)
    {
        if (!animatingHealthGain) return Animate(displayedHealth, goal);
        healthGainElapsed += Mathf.Max(0f, deltaTime);
        float t = healthRecoveryDuration <= 0f ? 1f : Mathf.Clamp01(healthGainElapsed / healthRecoveryDuration);
        if (t >= 1f) animatingHealthGain = false;
        return Mathf.Lerp(healthGainStart, goal, Mathf.SmoothStep(0f, 1f, t));
    }

    private float Animate(float displayed, float goal)
    {
        float duration = goal < displayed ? decreaseDuration : increaseDuration;
        return duration <= 0f ? goal : Mathf.MoveTowards(displayed, goal, Time.unscaledDeltaTime / duration);
    }

    private void TrackLoss(float goal, ref float previous, float displayed, ref float trail, ref float lostAt)
    {
        if (goal < previous - 0.00001f)
        {
            trail = Mathf.Max(trail, previous, displayed);
            lostAt = Time.unscaledTime;
        }
        previous = goal;
    }

    private void DrawLoss(Image image, float displayed, ref float trail, float lostAt, Color tint)
    {
        float elapsed = Time.unscaledTime - lostAt;
        if (elapsed >= lossHoldDuration)
            trail = lossCatchupDuration <= 0f ? displayed : Mathf.MoveTowards(trail, displayed, Time.unscaledDeltaTime / lossCatchupDuration);
        trail = Mathf.Max(trail, displayed);
        if (image == null || !sampleVisuals) return;
        image.fillAmount = trail;
        float flash = lossFlashDuration > 0f ? 1f - Mathf.Clamp01(elapsed / lossFlashDuration) : 0f;
        image.color = Color.Lerp(tint, Color.white, flash * lossFlashStrength);
    }
}
