using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    public sealed class PlayerDeathCutscene : MonoBehaviour
    {
        public const float Duration = 4.2f;
        private const string Headline = "BẠN ĐÃ GỤC NGÃ";
        private static PlayerDeathCutscene instance;

        private CanvasGroup canvasGroup;
        private Image shade;
        private Image[] strokes;
        private Image[] embers;
        private RectTransform headlineRect;
        private Text headline;
        private CanvasGroup captionGroup;
        private AudioSource deathVoice;
        private BossVictoryStyle audioStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => instance = null;

        public static void Play()
        {
            if (instance == null)
            {
                var root = new GameObject("Player Death Cutscene");
                instance = root.AddComponent<PlayerDeathCutscene>();
            }

            instance.gameObject.SetActive(true);
            instance.StopAllCoroutines();
            instance.StartCoroutine(instance.Present());
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Build();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Build()
        {
            audioStyle = Resources.Load<BossVictoryStyle>("DefaultBossVictory");
            if (audioStyle == null || audioStyle.impact == null)
                Debug.LogError("Player death cutscene audio is missing from DefaultBossVictory.");
            deathVoice = gameObject.AddComponent<AudioSource>();
            deathVoice.playOnAwake = false;
            deathVoice.spatialBlend = 0;
            deathVoice.ignoreListenerPause = true;
            deathVoice.priority = 16;

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32766;
            canvas.pixelPerfect = true;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            shade = MakeImage(canvas.transform, "Ashen veil", Vector2.zero, Vector2.zero,
                new Color(.035f, .012f, .018f, 0));
            var shadeRect = shade.rectTransform;
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.sizeDelta = Vector2.zero;

            var ink = new Color(.09f, .025f, .03f, 0);
            strokes = new Image[13];
            for (int i = 0; i < strokes.Length; i++)
            {
                float width = 690 + (i % 4) * 28 - Mathf.Abs(i - 6) * 8;
                strokes[i] = MakeImage(canvas.transform, "Pixel ink " + i,
                    new Vector2((i % 3 - 1) * 8, (i - 6) * 15), new Vector2(width, i % 3 == 0 ? 8 : 10), ink);
            }

            var gold = new Color(.91f, .74f, .43f);
            MakeImage(canvas.transform, "Upper pixel rule", new Vector2(0, 101), new Vector2(540, 2),
                new Color(gold.r, gold.g, gold.b, .7f));
            MakeImage(canvas.transform, "Lower pixel rule", new Vector2(0, -68), new Vector2(540, 2),
                new Color(gold.r, gold.g, gold.b, .7f));

            var font = GameFont.Current;
            var title = MakeLabel(canvas.transform, "Death title", "", new Vector2(0, 16), new Vector2(930, 90), 52,
                new Color(.78f, .22f, .19f));
            title.font = font;
            title.fontStyle = FontStyle.Bold;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.025f, .006f, .01f, 1);
            outline.effectDistance = new Vector2(3, -3);
            headlineRect = title.rectTransform;
            headline = title;

            var caption = MakeLabel(canvas.transform, "Death caption", "HÃY ĐỨNG LÊN MỘT LẦN NỮA",
                new Vector2(0, -42), new Vector2(680, 32), 20, new Color(.82f, .72f, .58f, 0));
            captionGroup = caption.gameObject.AddComponent<CanvasGroup>();
            captionGroup.alpha = 0;
            captionGroup.interactable = false;
            captionGroup.blocksRaycasts = false;

            embers = new Image[18];
            for (int i = 0; i < embers.Length; i++)
            {
                var ember = MakeImage(canvas.transform, "Falling pixel " + i, Vector2.zero,
                    new Vector2(i % 4 == 0 ? 5 : 3, i % 3 == 0 ? 5 : 3), gold);
                embers[i] = ember;
            }
        }

        private IEnumerator Present()
        {
            float elapsed = 0;
            deathVoice.Stop();
            if (audioStyle != null && audioStyle.impact != null)
            {
                var menuAudio = Resources.Load<MenuAudioSettings>("DefaultMenuAudio");
                deathVoice.clip = audioStyle.impact;
                deathVoice.volume = audioStyle.volume * (menuAudio != null ? menuAudio.volume : 1f);
                deathVoice.Play();
            }
            canvasGroup.alpha = 1;
            headline.text = "";
            captionGroup.alpha = 0;

            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float frameTime = PixelAnimationSettings.Sample(elapsed);
                float fadeIn = Mathf.Clamp01(frameTime / .55f);
                float fadeOut = 1 - Mathf.Clamp01((frameTime - (Duration - .5f)) / .5f);
                shade.color = new Color(.035f, .012f, .018f, .9f * fadeIn * fadeOut);

                float reveal = Mathf.Clamp01((frameTime - .45f) / .95f);
                int visible = Mathf.Clamp(Mathf.FloorToInt(reveal * Headline.Length), 0, Headline.Length);
                headline.text = Headline.Substring(0, visible);
                headlineRect.anchoredPosition = new Vector2(0, 16 + Mathf.Round((1 - reveal) * 8 / 2) * 2);
                headline.color = new Color(.78f, .22f, .19f, reveal * fadeOut);
                captionGroup.alpha = Mathf.Clamp01((frameTime - 1.35f) / .3f) * fadeOut;

                for (int i = 0; i < strokes.Length; i++)
                {
                    var color = strokes[i].color;
                    color.a = (i == 0 || i == strokes.Length - 1 ? .24f : .68f) * fadeIn * fadeOut;
                    strokes[i].color = color;
                    strokes[i].rectTransform.localScale = new Vector3(Mathf.Max(.01f, Mathf.Floor(reveal * 16) / 16), 1, 1);
                }

                for (int i = 0; i < embers.Length; i++)
                {
                    float phase = Mathf.Repeat(frameTime * .28f + i * .173f, 1f);
                    float x = ((i * 83 % 540) - 270) + Mathf.Sin((frameTime + i) * 1.7f) * 12;
                    float y = -210 + phase * 410;
                    embers[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(x / 2) * 2, Mathf.Round(y / 2) * 2);
                    var color = embers[i].color;
                    color.a = fadeIn * fadeOut * (1 - phase) * .75f;
                    embers[i].color = color;
                }

                yield return null;
            }

            canvasGroup.alpha = 0;
        }

        private static Image MakeImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), position, size);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text MakeLabel(Transform parent, string name, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), position, size);
            var text = go.GetComponent<Text>();
            text.font = GameFont.Current;
            text.fontSize = fontSize;
            text.text = value;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
