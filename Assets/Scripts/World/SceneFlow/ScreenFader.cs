using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace World
{
    public class ScreenFader : MonoBehaviour
    {
        private static ScreenFader _instance;

        public static ScreenFader Instance
        {
            get
            {
                if (_instance == null)
                {
                    var existing = FindFirstObjectByType<ScreenFader>();
                    if (existing != null)
                    {
                        _instance = existing;
                    }
                    else
                    {
                        var go = new GameObject("ScreenFader");
                        _instance = go.AddComponent<ScreenFader>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Fade Settings")]
        [SerializeField] private float defaultFadeDuration = 0.5f;
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private bool fadeInOnStart = true;

        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Image _fadeImage;
        private Coroutine _currentFadeRoutine;
        private bool _isTransitioning;
        private SceneLoadingView loadingView;
        [Min(0f)] public float minimumLoadingDuration = .7f;

        public bool IsTransitioning => _isTransitioning;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureUIElements();

            if (fadeInOnStart)
            {
                _canvasGroup.alpha = 1f;
                FadeIn(defaultFadeDuration);
            }
        }

        private void EnsureUIElements()
        {
            if (_canvasGroup != null) return;

            _canvas = GetComponentInChildren<Canvas>();
            if (_canvas == null)
            {
                var canvasGO = new GameObject("FaderCanvas");
                canvasGO.transform.SetParent(transform, false);
                _canvas = canvasGO.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 32767;

                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                canvasGO.AddComponent<GraphicRaycaster>();
            }

            _canvasGroup = _canvas.GetComponentInChildren<CanvasGroup>();
            if (_canvasGroup == null)
            {
                var imageGO = new GameObject("FadeOverlay");
                imageGO.transform.SetParent(_canvas.transform, false);

                var rect = imageGO.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;

                _fadeImage = imageGO.AddComponent<Image>();
                _fadeImage.color = fadeColor;

                _canvasGroup = imageGO.AddComponent<CanvasGroup>();
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        public void FadeIn(float duration, Action onComplete = null)
        {
            EnsureUIElements();
            if (_currentFadeRoutine != null) StopCoroutine(_currentFadeRoutine);
            _currentFadeRoutine = StartCoroutine(FadeRoutine(1f, 0f, duration, onComplete));
        }

        public void FadeOut(float duration, Action onComplete = null)
        {
            EnsureUIElements();
            if (_currentFadeRoutine != null) StopCoroutine(_currentFadeRoutine);
            _currentFadeRoutine = StartCoroutine(FadeRoutine(0f, 1f, duration, onComplete));
        }

        public void TransitionToScene(string sceneName, float fadeDuration = -1f, Action onComplete = null)
        {
            if (_isTransitioning) return;
            if (_currentFadeRoutine != null) { StopCoroutine(_currentFadeRoutine); _currentFadeRoutine = null; }
            float duration = fadeDuration > 0f ? fadeDuration : defaultFadeDuration;
            StartCoroutine(SceneTransitionRoutine(sceneName, duration, onComplete));
        }

        private IEnumerator SceneTransitionRoutine(string sceneName, float duration, Action onComplete)
        {
            _isTransitioning = true;
            EnsureUIElements();

            // 1. Fade Out to black
            yield return StartCoroutine(FadeRoutine(0f, 1f, duration, null));

            if (loadingView == null)
            {
                var root = new GameObject("Scene loading", typeof(RectTransform));
                root.transform.SetParent(_canvas.transform, false);
                loadingView = root.AddComponent<SceneLoadingView>(); loadingView.Build();
            }
            loadingView.Show();
            yield return null;
            float loadingStarted = Time.realtimeSinceStartup;
            // Hold activation until assets are ready and the short loading presentation has completed.
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp != null)
            {
                asyncOp.allowSceneActivation = false;
                while (asyncOp.progress < .9f || Time.realtimeSinceStartup - loadingStarted < minimumLoadingDuration)
                {
                    loadingView.SetProgress(Mathf.Min(.9f, asyncOp.progress));
                    yield return null;
                }
                asyncOp.allowSceneActivation = true;
                while (!asyncOp.isDone)
                {
                    yield return null;
                }
            }

            // Brief pause for scene initialization
            loadingView.SetProgress(1);
            yield return null;
            loadingView.Hide();

            // 3. Fade In from black
            yield return StartCoroutine(FadeRoutine(1f, 0f, duration, null));

            _isTransitioning = false;
            onComplete?.Invoke();
        }

        private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration, Action onComplete)
        {
            EnsureUIElements();
            _canvasGroup.blocksRaycasts = targetAlpha > 0.01f;

            if (duration <= 0.001f)
            {
                _canvasGroup.alpha = targetAlpha;
                _canvasGroup.blocksRaycasts = targetAlpha > 0.01f;
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            _canvasGroup.alpha = startAlpha;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = PixelAnimationSettings.Progress(elapsed,duration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.blocksRaycasts = targetAlpha > 0.01f;
            onComplete?.Invoke();
            _currentFadeRoutine = null;
        }
    }
}
