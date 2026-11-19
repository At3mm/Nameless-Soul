using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One noninteractive receipt for completed saves, using unscaled pixel timing.</summary>
public sealed class SaveGameNotice : MonoBehaviour
{
    private static SaveGameNotice instance;
    private CanvasGroup group;
    private float shownAt;
    private bool pending;
    public bool IsVisible => pending && group != null && group.alpha > 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { instance = null; }

    public static void Show()
    {
        if (!Application.isPlaying) return;
        if (instance == null)
        {
            var go = new GameObject("Save Game Notice", typeof(RectTransform));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SaveGameNotice>();
            instance.Build();
        }
        instance.shownAt = Time.unscaledTime;
        instance.pending = true;
        instance.group.alpha = 1f;
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvas.pixelPerfect = true;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        group = gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        var label = new GameObject("Saved", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(transform, false);
        var rect = (RectTransform)label.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-26, -22);
        rect.sizeDelta = new Vector2(210, 36);
        var text = label.GetComponent<Text>();
        text.font = GameFont.Current;
        text.fontSize = 26;
        text.text = "Đã lưu hành trình";
        text.color = new Color(.94f, .86f, .65f);
        text.alignment = TextAnchor.MiddleRight;
        text.raycastTarget = false;
        var shadow = label.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, .9f);
        shadow.effectDistance = new Vector2(2, -2);
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnDestroy() { if (instance == this) instance = null; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        pending = false;
        if (group != null) group.alpha = 0f;
    }
    private void Update()
    {
        if (!pending || group == null) return;
        float elapsed = Mathf.Max(0f, Time.unscaledTime - shownAt);
        group.alpha = 1f - PixelAnimationSettings.Progress(elapsed - 1.8f, .4f);
        if (elapsed >= 2.2f) { pending = false; group.alpha = 0f; }
    }
}
