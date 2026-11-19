using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterHealth))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [HideInInspector] public Key interactKey = Key.E; // Legacy serialized value; GameControls owns the binding.
        private CharacterHealth health;
        private PlayerController controller;
        private AnimationEventHandler actions;
        private PlayerDash dash;
        private Text prompt;
        private Text noticeText;
        private GameObject noticeCanvas;
        private string notice;
        private float noticeUntil, nextSearch;
        public WorldInteractable Target { get; private set; }
        public bool IsInCombat
        {
            get
            {
                if (health == null || health.isDead) return false;
                foreach (var enemy in EnemyClassification.Active)
                    if (enemy != null && enemy.isActiveAndEnabled && enemy.gameObject.scene == gameObject.scene &&
                        enemy.Health != null && !enemy.Health.isDead && enemy.AI != null && enemy.AI.IsInCombatWith(transform)) return true;
                return false;
            }
        }
        public bool CanUseCheckpoint => CanInteract && !IsInCombat;
        public bool CanInteract => isActiveAndEnabled && health != null && !health.isDead && Time.timeScale > 0f && !NpcInteractable.BlocksPauseMenu &&
            (controller == null || controller.isActiveAndEnabled) && (actions == null || !actions.isActionLocked) &&
            (dash == null || !dash.isDashing);
        private void Awake()
        {
            health = GetComponent<CharacterHealth>(); controller = GetComponent<PlayerController>();
            actions = GetComponent<AnimationEventHandler>(); dash = GetComponent<PlayerDash>();
        }
        private void Update()
        {
            FindTarget();
            if (CanInteract && GameControls.Pressed(GameControls.Action.Interact)) TryInteract();
            RefreshPrompt();
        }
        private void FindTarget()
        {
            Target = null;
            if (!CanInteract) return;
            float best = float.PositiveInfinity;
            foreach (var item in WorldInteractable.Active)
            {
                if (item == null || !item.IsAvailable || item.gameObject.scene != gameObject.scene) continue;
                float distance = ((Vector2)(item.InteractionPosition - transform.position)).sqrMagnitude;
                if (distance <= item.interactionRadius * item.interactionRadius && distance < best)
                { best = distance; Target = item; }
            }
        }
        public bool TryInteract() { FindTarget(); return Target != null && Target.TryInteract(this); }
        public void ShowNotice(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (notice == text && Time.unscaledTime < noticeUntil) return;
            notice = text; noticeUntil = Time.unscaledTime + 2.5f;
            if (noticeText == null)
            {
                noticeCanvas = new GameObject("Player action notice", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                var canvas = noticeCanvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 31000;
                var scaler = noticeCanvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
                var label = new GameObject("Notice", typeof(RectTransform), typeof(Text), typeof(Outline));
                label.transform.SetParent(noticeCanvas.transform,false); noticeText = label.GetComponent<Text>();
                noticeText.font = GameFont.Current; noticeText.fontSize = 22; noticeText.alignment = TextAnchor.MiddleCenter;
                noticeText.raycastTarget = false; noticeText.supportRichText = false;
                var rect = label.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f,.22f);
                rect.sizeDelta = new Vector2(1000,80); rect.anchoredPosition = Vector2.zero;
                label.GetComponent<Outline>().effectColor = Color.black;
            }
            noticeText.text = text; noticeText.color = new Color(1f,.94f,.72f);
        }
        private void RefreshPrompt()
        {
            if (noticeText != null)
            {
                float alpha = health != null && health.isDead ? 0f : Mathf.Clamp01((noticeUntil-Time.unscaledTime)/.25f);
                noticeText.color = new Color(1f,.94f,.72f,alpha);
                if (alpha <= 0f) noticeText.text = "";
            }
            if (prompt == null && Time.unscaledTime >= nextSearch)
            {
                nextSearch = Time.unscaledTime + 0.5f;
                var hud = FindFirstObjectByType<PlayerVitalsHUD>();
                if (hud != null)
                {
                    var existing = hud.transform.Find("InteractionPrompt");
                    var go = existing != null ? existing.gameObject : new GameObject("InteractionPrompt", typeof(RectTransform), typeof(Text), typeof(Outline));
                    go.transform.SetParent(hud.transform, false);
                    prompt = go.GetComponent<Text>();
                    prompt.font = hud.healthValue.font; prompt.fontSize = 22; prompt.alignment = TextAnchor.MiddleCenter;
                    prompt.color = new Color(1f, 0.94f, 0.72f); prompt.raycastTarget = false;
                    var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0, 115); rect.sizeDelta = new Vector2(620, 56);
                    go.GetComponent<Outline>().effectColor = Color.black;
                }
            }
            if (prompt != null) prompt.text = !CanInteract ? "" :
                Target is DoorInteractable && IsInCombat ? "Không thể mở cửa khi đang giao tranh." :
                Target is CheckpointInteractable && IsInCombat ? "Không thể dùng tượng khi đang giao tranh." :
                Target != null ? "[" + GameControls.Display(GameControls.Action.Interact) + "] " + Target.interactionLabel : "";
        }
        private void OnDisable() { Target = null; if (prompt != null) prompt.text = ""; if(noticeCanvas!=null)Destroy(noticeCanvas); noticeCanvas=null; noticeText=null; notice=null; }
    }
}
