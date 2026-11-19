using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace World
{
    [DisallowMultipleComponent]
    public sealed class InGameCutscene : MonoBehaviour
    {
        [Serializable]
        public sealed class Shot
        {
            public string caption;
            public Vector2 offset;
            [Min(0.5f)] public float cameraSize = 3f;
            [Range(-10, 10)] public float roll;
            [Min(0.1f)] public float travelTime = 1f;
            [Min(0f)] public float holdTime = 0.5f;
        }

        public Transform focus;
        public Shot[] shots = new Shot[0];
        [Min(0.1f)] public float returnTime = 1f;
        public Animator effectAnimator;
        public CheckpointRuneVisuals checkpointRunes;
        public bool allowSkip;
        public bool playOnlyOnce;
        public bool IsPlaying { get; private set; }
        public static bool AnyPlaying { get; private set; }
        // Keep the skip press exclusive even when the sequence finishes later in this frame.
        private static int pauseBlockedThroughFrame = -1;
        public static bool BlocksPauseMenu => AnyPlaying || Time.frameCount <= pauseBlockedThroughFrame;

        private bool played, skip;
        private Camera output;
        private CinemachineBrain brain;
        private bool brainEnabled;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float originalSize, originalTimeScale;
        private GameObject overlay;
        private CanvasGroup overlayGroup;
        private Text subtitle;
        private CanvasGroup hudGroup;
        private bool addedHudGroup;
        private float hudAlpha;
        private readonly List<Behaviour> suspended = new List<Behaviour>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { AnyPlaying = false; pauseBlockedThroughFrame = -1; }

        public bool Play(PlayerInteractor player)
        {
            if (!isActiveAndEnabled || player == null || !player.CanInteract || AnyPlaying ||
                (playOnlyOnce && played) || shots == null || shots.Length == 0 || Camera.main == null) return false;
            output = Camera.main;
            if (!output.orthographic) return false;
            originalPosition = output.transform.position; originalRotation = output.transform.rotation;
            originalSize = output.orthographicSize; originalTimeScale = Time.timeScale;
            brain = output.GetComponent<CinemachineBrain>();
            brainEnabled = brain != null && brain.enabled;
            if (brain != null) brain.enabled = false;
            IsPlaying = AnyPlaying = true; played = true; skip = false;
            // Freeze enemies, damage ticks and physics while the unscaled camera/FX keep playing.
            Time.timeScale = 0f;
            Suspend(player.GetComponent<PlayerController>());
            Suspend(player.GetComponent<PlayerCombat>());
            Suspend(player.GetComponent<PlayerDash>());
            Suspend(player.GetComponent<CharacterParry>());
            Suspend(player.GetComponent<PlayerHealthPotion>());
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = Vector2.zero;
            CreateOverlay();
            if (checkpointRunes != null) checkpointRunes.PrepareActivation();
            var hud = FindFirstObjectByType<PlayerVitalsHUD>();
            if (hud != null)
            {
                hudGroup = hud.GetComponent<CanvasGroup>();
                addedHudGroup = hudGroup == null;
                if (addedHudGroup) hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
                hudAlpha = hudGroup.alpha; hudGroup.alpha = 0f;
            }
            if (effectAnimator != null)
            {
                effectAnimator.gameObject.SetActive(true);
                effectAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                effectAnimator.speed = 1f;
                effectAnimator.Play(0, 0, 0f);
            }
            StartCoroutine(Sequence());
            return true;
        }

        private void Suspend(Behaviour component)
        {
            if (component == null || !component.enabled) return;
            suspended.Add(component); component.enabled = false;
        }

        private IEnumerator Sequence()
        {
            bool firstShot = true;
            foreach (var shot in shots)
            {
                if (skip || output == null) break;
                subtitle.text = shot.caption ?? "";
                var anchor = focus != null ? focus.position : transform.position;
                var destination = new Vector3(anchor.x + shot.offset.x, anchor.y + shot.offset.y, originalPosition.z);
                yield return MoveCamera(destination, originalRotation * Quaternion.Euler(0, 0, shot.roll), shot.cameraSize, shot.travelTime);
                if (firstShot && checkpointRunes != null) checkpointRunes.BeginReveal();
                firstShot = false;
                float held = 0f;
                while (held < shot.holdTime && !skip)
                { held += Mathf.Min(Time.unscaledDeltaTime, 0.1f); yield return null; }
            }
            if (!skip && output != null)
            {
                subtitle.text = "";
                yield return MoveCamera(originalPosition, originalRotation, originalSize, returnTime);
            }
            Restore();
        }

        private IEnumerator MoveCamera(Vector3 position, Quaternion rotation, float size, float duration)
        {
            Vector3 start = output.transform.position;
            Quaternion angle = output.transform.rotation;
            float zoom = output.orthographicSize, elapsed = 0f;
            while (elapsed < Mathf.Max(0.1f, duration) && !skip && output != null)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration)));
                output.transform.SetPositionAndRotation(Vector3.Lerp(start, position, t), Quaternion.Slerp(angle, rotation, t));
                output.orthographicSize = Mathf.Lerp(zoom, Mathf.Max(0.5f, size), t);
                overlayGroup.alpha = Mathf.Min(1f, overlayGroup.alpha + Time.unscaledDeltaTime * 4f);
                yield return null;
            }
        }

        public void Skip()
        {
            if (!IsPlaying || !allowSkip) return;
            pauseBlockedThroughFrame = Time.frameCount + 1;
            skip = true;
        }
        private void Update()
        {
            if (allowSkip && IsPlaying && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Skip();
        }

        private void CreateOverlay()
        {
            overlay = new GameObject("Cutscene letterbox", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            var canvas = overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 2000;
            var scaler = overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            overlayGroup = overlay.GetComponent<CanvasGroup>(); overlayGroup.alpha = 0f; overlayGroup.blocksRaycasts = false;
            MakeBar("Top", new Vector2(0, 0.90f), Vector2.one);
            var bottom = MakeBar("Bottom", Vector2.zero, new Vector2(1, 0.10f));
            var label = new GameObject("Caption", typeof(RectTransform), typeof(Text)); label.transform.SetParent(bottom, false);
            var rect = label.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            subtitle = label.GetComponent<Text>();
            var hud = FindFirstObjectByType<PlayerVitalsHUD>();
            subtitle.font = hud != null && hud.healthValue != null ? hud.healthValue.font : GameFont.Current;
            subtitle.fontSize = 26; subtitle.alignment = TextAnchor.MiddleCenter; subtitle.color = new Color(0.8f, 1f, 1f); subtitle.raycastTarget = false;
            if (!allowSkip) return;
            var hint = new GameObject("Skip hint", typeof(RectTransform), typeof(Text)); hint.transform.SetParent(overlay.transform, false);
            var hr = hint.GetComponent<RectTransform>(); hr.anchorMin = hr.anchorMax = Vector2.one; hr.pivot = Vector2.one; hr.anchoredPosition = new Vector2(-28, -22); hr.sizeDelta = new Vector2(350, 40);
            var text = hint.GetComponent<Text>(); text.font = subtitle.font; text.fontSize = 21; text.text = "ESC - Skip"; text.alignment = TextAnchor.MiddleRight; text.color = Color.white; text.raycastTarget = false;
        }

        private Transform MakeBar(string name, Vector2 min, Vector2 max)
        {
            var bar = new GameObject(name, typeof(RectTransform), typeof(Image)); bar.transform.SetParent(overlay.transform, false);
            var rect = bar.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.sizeDelta = Vector2.zero; rect.anchoredPosition = Vector2.zero;
            var image = bar.GetComponent<Image>(); image.color = Color.black; image.raycastTarget = false;
            return bar.transform;
        }

        private void Restore()
        {
            if (!IsPlaying) return;
            if (output != null) { output.transform.SetPositionAndRotation(originalPosition, originalRotation); output.orthographicSize = originalSize; }
            if (brain != null) brain.enabled = brainEnabled;
            if (effectAnimator != null) effectAnimator.gameObject.SetActive(false);
            if (checkpointRunes != null) checkpointRunes.CompleteActivation();
            foreach (var component in suspended) if (component != null) component.enabled = true;
            suspended.Clear();
            if (overlay != null) Destroy(overlay);
            if (hudGroup != null)
            {
                hudGroup.alpha = hudAlpha;
                if (addedHudGroup) Destroy(hudGroup);
                hudGroup = null;
            }
            Time.timeScale = originalTimeScale;
            IsPlaying = AnyPlaying = false;
        }

        private void OnDisable() { StopAllCoroutines(); Restore(); }
    }
}
