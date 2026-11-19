using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pixel-stamped victory and reward; never changes game time or captures input.</summary>
public sealed class BossRewardNotice : MonoBehaviour
{
    private sealed class Entry { public string boss; public BossItemInventory.Item item; }
    private static BossRewardNotice instance;
    private readonly Queue<Entry> queue = new Queue<Entry>();
    private readonly HashSet<string> seen = new HashSet<string>();
    private readonly List<Text> letters = new List<Text>();
    private readonly List<RectTransform> strokes = new List<RectTransform>();
    private readonly List<RectTransform> motes = new List<RectTransform>();
    private readonly List<Vector2> letterPositions = new List<Vector2>();
    private BossVictoryStyle style;
    private CanvasGroup hero, card;
    private RectTransform upperRule, lowerRule, cardRect;
    private Text bossLabel, itemTitle, itemDescription;
    private Image dim, itemIcon;
    private readonly AudioSource[] audioVoices = new AudioSource[3];
    private Entry current;
    private float elapsed;
    private bool resolvePlayed, rewardPlayed;
    public bool IsPresenting => current != null;
    public float RewardStart => style.leadDuration + style.revealDuration + style.holdDuration + style.fadeDuration;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { instance = null; }
    public static void Show(BossItemInventory.Item item, string bossName = null, string encounterId = null)
    { if (item != null) Enqueue(bossName ?? item.DisplayName, item, encounterId); }
    public static void ShowVictory(string bossName, string encounterId) => Enqueue(bossName, null, encounterId);
#if UNITY_EDITOR
    public static void Preview(string bossName, GameItemDefinition definition)
    {
        var item = definition != null ? new BossItemInventory.Item(definition.itemId, definition.displayName, definition.description, 1) : null;
        Enqueue(bossName, item, null);
    }
#endif
    private static void Enqueue(string name, BossItemInventory.Item item, string encounterId)
    {
        if (!Application.isPlaying) return;
        if (instance == null)
        {
            var root = new GameObject("Boss Reward Notice"); DontDestroyOnLoad(root);
            instance = root.AddComponent<BossRewardNotice>(); instance.Build();
        }
        if (!string.IsNullOrEmpty(encounterId) && !instance.seen.Add(encounterId)) return;
        instance.queue.Enqueue(new Entry { boss = name, item = item });
    }
    private void Build()
    {
        style = Resources.Load<BossVictoryStyle>("DefaultBossVictory");
        if (style == null) { style = ScriptableObject.CreateInstance<BossVictoryStyle>(); style.hideFlags = HideFlags.HideAndDontSave; }
        if (style.font == null) style.font = GameFont.Current;
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 130; canvas.pixelPerfect = true;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        dim = MakeImage(transform, "World dim", Vector2.zero, Vector2.zero, Color.clear);
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one; dim.rectTransform.sizeDelta = Vector2.zero;
        hero = Group("Victory banner", new Vector2(0, 35), new Vector2(1040, 250));
        for (int i = 0; i < 19; i++)
        {
            float width = 930 + ((i * 37) % 7) * 12 - Mathf.Abs(i - 9) * 13;
            var stroke = MakeImage(hero.transform, "Ink stroke " + i, new Vector2((i % 3 - 1) * 8, (i - 9) * 12), new Vector2(width, 12),
                new Color(style.ink.r, style.ink.g, style.ink.b, i == 0 || i == 18 ? .35f : .88f));
            strokes.Add(stroke.rectTransform);
        }
        upperRule = MakeImage(hero.transform, "Upper gold rule", new Vector2(0, 90), new Vector2(700, 2), style.gold).rectTransform;
        lowerRule = MakeImage(hero.transform, "Lower gold rule", new Vector2(0, -80), new Vector2(700, 2), style.gold).rectTransform;
        foreach (int side in new[] { -1, 1 }) for (int j = 0; j < 3; j++)
        {
            var seal = MakeImage(hero.transform, "Pixel seal", new Vector2(side * (370 + j * 13), 0), new Vector2(j == 0 ? 10 : 4, j == 0 ? 10 : 4), style.gold);
            seal.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        }
        BuildLetters();
        bossLabel = Label(hero.transform, "Boss name", "", new Vector2(0, -49), new Vector2(650, 30), 21, style.gold);
        for (int i = 0; i < 24; i++) motes.Add(MakeImage(hero.transform, "Gold pixel " + i, Vector2.zero, new Vector2(i % 3 == 0 ? 4 : 2, 2), style.gold).rectTransform);
        card = Group("Reward card", new Vector2(0, -160), new Vector2(540, 150)); cardRect = (RectTransform)card.transform;
        var paper = MakeImage(card.transform, "Menu parchment", Vector2.zero, new Vector2(540, 150), Color.white); paper.sprite = style.parchment;
        if (paper.sprite != null) paper.type = paper.sprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
        else paper.color = new Color(.76f, .64f, .44f);
        MakeImage(card.transform, "Top edge", new Vector2(0, 69), new Vector2(512, 2), style.parchmentInk);
        MakeImage(card.transform, "Bottom edge", new Vector2(0, -69), new Vector2(512, 2), style.parchmentInk);
        itemIcon = MakeImage(card.transform, "Reward icon", new Vector2(-209, -7), new Vector2(64, 64), Color.white); itemIcon.preserveAspect = true;
        Label(card.transform, "Reward heading", "KỶ VẬT NHẬN ĐƯỢC", new Vector2(25, 46), new Vector2(420, 25), 17, style.parchmentInk);
        itemTitle = Label(card.transform, "Item name", "", new Vector2(40, 12), new Vector2(390, 32), 21, style.parchmentInk);
        itemDescription = Label(card.transform, "Item description", "", new Vector2(40, -33), new Vector2(390, 52), 15, style.parchmentInk);
        itemDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
        for (int i = 0; i < audioVoices.Length; i++)
        {
            var voice = gameObject.AddComponent<AudioSource>(); audioVoices[i] = voice;
            voice.playOnAwake = false; voice.spatialBlend = 0; voice.ignoreListenerPause = true; voice.priority = 16;
        }
        Hide();
    }
    private void BuildLetters()
    {
        int size = FontSize(48); style.font.RequestCharactersInTexture(style.headline, size);
        float total = 0; var widths = new List<float>();
        foreach (char ch in style.headline)
        {
            CharacterInfo info; float advance = style.font.GetCharacterInfo(ch, out info, size) ? info.advance : 27;
            widths.Add(advance + 2); total += advance + 2;
        }
        float x = -total * .5f;
        for (int i = 0; i < style.headline.Length; i++)
        {
            var text = Label(hero.transform, "Title glyph " + i, style.headline[i].ToString(), new Vector2(x + widths[i] * .5f, 16), new Vector2(widths[i] + 8, 85), 48, style.gold);
            var outline = text.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.08f, .035f, .015f, 1); outline.effectDistance = new Vector2(2, -2);
            letterPositions.Add(text.rectTransform.anchoredPosition); letters.Add(text); x += widths[i];
        }
    }
    private CanvasGroup Group(string name, Vector2 position, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup)); root.transform.SetParent(transform, false);
        Place((RectTransform)root.transform, position, size); var group = root.GetComponent<CanvasGroup>();
        group.blocksRaycasts = group.interactable = false; return group;
    }
    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    { rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; }
    private static Image MakeImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, position, size); var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    private int FontSize(int size) => style.font.name.StartsWith("VT323") ? Mathf.RoundToInt(size * 1.25f) : size;
    private Text Label(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, position, size); var text = go.GetComponent<Text>(); text.font = style.font;
        text.fontSize = FontSize(fontSize); text.text = value; text.color = color; text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow; text.raycastTarget = false; return text;
    }
    private void PlayCue(int channel, AudioClip clip)
    {
        if (clip == null) return;
        var menu = Resources.Load<MenuAudioSettings>("DefaultMenuAudio");
        audioVoices[channel].clip = clip; audioVoices[channel].volume = style.volume * (menu != null ? menu.volume : 1f); audioVoices[channel].Play();
    }
    private void Update()
    {
        if (current == null)
        {
            if (queue.Count == 0) return;
            current = queue.Dequeue(); elapsed = 0; resolvePlayed = rewardPlayed = false;
            bossLabel.text = (current.boss ?? "BOSS").ToUpperInvariant();
            if (current.item != null) { itemTitle.text = current.item.DisplayName; itemDescription.text = current.item.Description; itemIcon.sprite = current.item.Icon; itemIcon.enabled = itemIcon.sprite != null; }
            PlayCue(0, style.impact);
        }
        elapsed += Time.unscaledDeltaTime; Present(elapsed);
        if (elapsed >= style.leadDuration + .2f && !resolvePlayed) { resolvePlayed = true; PlayCue(1, style.resolve); }
        if (elapsed >= RewardStart && current.item != null && !rewardPlayed) { rewardPlayed = true; PlayCue(2, style.reward); }
        if (elapsed >= RewardStart + (current.item != null ? style.rewardDuration : .2f)) { current = null; Hide(); }
    }
    private void Present(float time)
    {
        float t = PixelAnimationSettings.Sample(time);
        float enter = Mathf.Clamp01((t - style.leadDuration) / .35f);
        float exit = 1 - Mathf.Clamp01((t - (RewardStart - style.fadeDuration)) / style.fadeDuration);
        hero.alpha = enter * exit;
        dim.color = new Color(style.ink.r, style.ink.g, style.ink.b, style.worldDim * Mathf.Clamp01(t / .25f) * exit);
        for (int i = 0; i < strokes.Count; i++) strokes[i].localScale = new Vector3(Mathf.Max(.01f, Mathf.Floor(enter * 24) / 24), 1, 1);
        upperRule.localScale = lowerRule.localScale = new Vector3(enter, 1, 1);
        for (int i = 0; i < letters.Count; i++)
        {
            float stamp = Mathf.Clamp01((t - style.leadDuration - .12f - i * .027f) / .22f);
            letters[i].color = new Color(style.gold.r, style.gold.g, style.gold.b, stamp);
            letters[i].rectTransform.anchoredPosition = letterPositions[i] + new Vector2(0, Mathf.Round((1 - stamp) * 12 / 2) * 2);
        }
        for (int i = 0; i < motes.Count; i++)
        {
            float life = Mathf.Clamp01((t - .55f - (i % 5) * .06f) / 2.5f);
            motes[i].anchoredPosition = new Vector2(Mathf.Round(((i * 83 % 720) - 360 + (i % 2 == 0 ? 1 : -1) * life * 35) / 2) * 2, Mathf.Round((i % 3 * 30 - 30 + life * 55) / 2) * 2);
            var image = motes[i].GetComponent<Image>(); image.color = new Color(style.gold.r, style.gold.g, style.gold.b, life > 0 ? (1 - life) * .65f : 0);
        }
        float rewardTime = t - RewardStart;
        card.alpha = current.item != null ? Mathf.Clamp01(rewardTime / .25f) * Mathf.Clamp01((style.rewardDuration - rewardTime) / .5f) : 0;
        cardRect.anchoredPosition = new Vector2(0, -160 - Mathf.Round((1 - Mathf.Clamp01(rewardTime / .25f)) * 16 / 2) * 2);
    }
    private void Hide() { hero.alpha = card.alpha = 0; dim.color = Color.clear; }
}
