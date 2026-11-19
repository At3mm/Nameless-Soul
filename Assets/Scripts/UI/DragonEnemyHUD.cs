using UnityEngine;
using UnityEngine.UI;

public class DragonEnemyHUD : MonoBehaviour
{
    public CanvasGroup group;
    public Image healthFill;
    public Image damageTrail;
    public Text enemyName;
    public Text healthValue;
    [Min(0f)] public float visibleDistance = 18f;
    [Min(0f)] public float trailDelay = .35f;
    [Min(.01f)] public float trailDuration = .6f;
    [Header("Fade (unscaled seconds)")]
    [Min(0f)] public float fadeInDuration = .25f;
    [Min(0f)] public float fadeOutDuration = .35f;
    [Range(1f, 30f)] public float effectFPS = 8f;
    public EnemyClassification Target { get; private set; }
    private Transform player;
    private float shown, delayed, previous, hold;
    private float fadeAlpha;
    private PixelEffectClock fadeClock, barClock;
    private Image staminaFill;
    private Text staminaText;
    private GameObject staminaTrack;
    private EnemyBreak displayedBreak;
    private bool wasBroken;

    private void EnsureStaminaBar()
    {
        if (staminaTrack != null || healthFill == null) return;
        PixelEnemyBarStyle.Boss(this);
        staminaTrack = new GameObject("EnemyStaminaTrack", typeof(RectTransform), typeof(Image));
        staminaTrack.transform.SetParent(healthFill.transform.parent, false);
        var rect = staminaTrack.GetComponent<RectTransform>(); var hp = healthFill.rectTransform;
        PixelEnemyBarStyle.Rect(rect,new Vector2(0,-66),new Vector2(600,4));
        PixelEnemyBarStyle.Frame(rect,"Pixel stagger rim",Vector2.zero,new Vector2(604,8));
        var track = staminaTrack.GetComponent<Image>(); PixelEnemyBarStyle.Paint(track,PixelEnemyBarStyle.Track);
        var fillGo = new GameObject("StaminaFill",typeof(RectTransform),typeof(Image)); fillGo.transform.SetParent(rect,false);
        staminaFill = fillGo.GetComponent<Image>(); PixelEnemyBarStyle.Paint(staminaFill,PixelEnemyBarStyle.Stagger);
        staminaFill.type=Image.Type.Filled;staminaFill.fillMethod=Image.FillMethod.Horizontal;staminaFill.fillOrigin=0;staminaFill.fillAmount=0;
        var fr = staminaFill.rectTransform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        var textGo = new GameObject("StaminaValue",typeof(RectTransform),typeof(Text)); textGo.transform.SetParent(rect,false);
        staminaText = textGo.GetComponent<Text>(); staminaText.font = healthValue != null ? healthValue.font : GameFont.Current;
        staminaText.fontSize = 13; staminaText.alignment = TextAnchor.MiddleCenter; staminaText.color = new Color(1,.82f,.4f); staminaText.raycastTarget = false;
        staminaText.rectTransform.anchoredPosition = new Vector2(0,-13); staminaText.rectTransform.sizeDelta = new Vector2(hp.rect.width,18);
        staminaText.color=PixelEnemyBarStyle.Stagger;
    }

    private void OnEnable()
    {
        Target = null;
        fadeAlpha = 0f;
        fadeClock = barClock = default;
        displayedBreak=null;wasBroken=false;
        if (group != null) group.alpha = 0f;
    }

    private void Fade(bool visible, float deltaTime)
    {
        float duration = visible ? fadeInDuration : fadeOutDuration;
        fadeAlpha = duration <= 0f ? (visible ? 1f : 0f) :
            Mathf.MoveTowards(fadeAlpha, visible ? 1f : 0f, Mathf.Max(0f, deltaTime) / duration);
        if (fadeClock.Tick(deltaTime, effectFPS) || fadeAlpha == 0f || fadeAlpha == 1f)
            group.alpha = fadeAlpha;
    }

    private void LateUpdate()
    {
        EnsureStaminaBar();
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        EnemyClassification selected = null;
        float best = float.MaxValue;
        foreach (var enemy in EnemyClassification.Active)
        {
            if (enemy == null || !enemy.UsesDragonBar || !enemy.IsEngaged || enemy.Health == null || enemy.Health.isDead || player == null) continue;
            float distance = Vector2.Distance(player.position, enemy.transform.position);
            if (distance > visibleDistance) continue;
            float score = distance - (enemy.enemyType == EnemyClassification.EnemyType.Boss ? visibleDistance : 0f);
            if (enemy == Target) score -= 1f; // Avoid flicker between nearby enemies.
            if (score < best) { best = score; selected = enemy; }
        }
        if (selected == null)
        {
            Target = null;
            Fade(false, Time.unscaledDeltaTime);
            return;
        }
        var stats = selected.Stats;
        float ratio = stats.maxHP > 0 ? Mathf.Clamp01(stats.currentHP / stats.maxHP) : 0f;
        bool changed = Target != selected;
        if (changed) { shown = delayed = previous = ratio; hold = 0; }
        Target = selected;
        if(BossPhaseController.Protects(selected.gameObject))
        {
            // Hide immediately, including the name and every bar/label; resume with current values.
            group.alpha=fadeAlpha=0;
            fadeClock=barClock=default;
            shown=delayed=previous=ratio;hold=0;
            displayedBreak=null;wasBroken=false;
            if(staminaText!=null)staminaText.text="";
            return;
        }
        Fade(true, Time.unscaledDeltaTime);
        if (ratio < previous) hold = trailDelay;
        if (ratio > previous) delayed = Mathf.Max(delayed, ratio);
        previous = ratio;
        shown = Mathf.MoveTowards(shown, ratio, Time.deltaTime / .15f);
        hold -= Time.deltaTime;
        if (hold <= 0) delayed = Mathf.MoveTowards(delayed, shown, Time.deltaTime / Mathf.Max(.01f, trailDuration));
        delayed = Mathf.Max(delayed, shown);
        bool sampleBars=barClock.Tick(Time.deltaTime, effectFPS) || changed;
        if (sampleBars || (shown == ratio && delayed == shown))
        {
            healthFill.fillAmount = Mathf.Round(shown*600)/600;
            damageTrail.fillAmount = Mathf.Round(delayed*600)/600;
        }
        enemyName.text = selected.DisplayName;
        healthValue.text = Mathf.CeilToInt(Mathf.Max(0, stats.currentHP)) + " / " + Mathf.CeilToInt(stats.maxHP);
        var enemyBreak = selected.GetComponent<EnemyBreak>();
        RefreshStagger(enemyBreak,sampleBars);
    }

    private void RefreshStagger(EnemyBreak enemyBreak,bool sample)
    {
        if (staminaTrack != null) staminaTrack.SetActive(enemyBreak != null);
        if (enemyBreak != null && staminaFill != null)
        {
            bool stateChanged=displayedBreak!=enemyBreak || wasBroken!=enemyBreak.IsBroken;
            if(sample || stateChanged || enemyBreak.Ratio<=0 || enemyBreak.Ratio>=1)
                staminaFill.fillAmount=Mathf.Round(enemyBreak.Ratio*600)/600;
            staminaFill.enabled = staminaFill.fillAmount > 0f;
            staminaText.text = enemyBreak.IsBroken ? "BREAK" : "";
            staminaTrack.GetComponent<Image>().color = PixelEnemyBarStyle.Track;
            wasBroken=enemyBreak.IsBroken;
        }
        displayedBreak=enemyBreak;
    }
}
