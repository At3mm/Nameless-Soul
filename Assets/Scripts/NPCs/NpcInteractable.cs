using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace World
{
    [DisallowMultipleComponent]
    public sealed partial class NpcInteractable : WorldInteractable
    {
        public string displayName = "Pháp sư";
        [TextArea(2, 5)] public string[] dialogueLines = {
            "Chào người lữ hành. Ta là pháp sư của ngôi làng này.",
            "Nếu cần trò chuyện, hãy đến gần và gọi ta. Chúc ngươi bình an trên đường đi."
        };
        public Font dialogueFont;
        [Header("Existing menu artwork")]
        public Sprite panelSprite;
        public Sprite advanceArrow;
        [Min(.01f)] public float openFadeDuration = .2f;
        [Min(.01f)] public float closeFadeDuration = .22f;
        public bool IsClosing => closingRoutine != null;
        private CanvasGroup viewGroup;
        private Coroutine openingRoutine, closingRoutine;
        public bool IsOpen { get; private set; }
        public int CurrentLine { get; private set; }
        public bool IsShopping => shop != null && shop.IsOpen;
        private NpcShop shop;
        private static NpcInteractable active;
        private static int blockedThroughFrame = -1;
        public static bool BlocksPauseMenu => active != null || Time.frameCount <= blockedThroughFrame;
        private PlayerInteractor actor;
        private GameObject view;
        private Text body, hint;
        private int openedFrame;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        public override bool IsAvailable => base.IsAvailable && !IsOpen;
        private void Awake()
        {
            if(GetComponent<NpcStationary>()==null)gameObject.AddComponent<NpcStationary>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { active = null; blockedThroughFrame = -1; }
        private void Reset() { interactionLabel = "Nói chuyện"; interactionRadius = 1.5f; }
        protected override bool Interact(PlayerInteractor player) => Open(player);

        public bool Open(PlayerInteractor player)
        {
            if (!IsAvailable || active != null || player == null || !player.CanInteract ||
                CheckpointMenu.AnyOpen || InGameCutscene.AnyPlaying || !HasDialogueContent) return false;
            actor = player;
            IsOpen = true; active = this; CurrentLine = 0; openedFrame = Time.frameCount;
            AreaNotice.Dismiss();
            MenuSFX.Play(MenuSFX.Kind.Open);
            Suspend(player.GetComponent<PlayerController>());
            Suspend(player.GetComponent<PlayerCombat>());
            Suspend(player.GetComponent<PlayerDash>());
            Suspend(player.GetComponent<CharacterParry>());
            Suspend(player.GetComponent<PlayerHealthPotion>());
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            var animator = player.GetComponent<Animator>();
            if (animator != null) { animator.SetBool("IsWalking", false); animator.SetBool("IsRunning", false); }
            shop = GetComponent<NpcShop>();
            BuildView();if(dialogue!=null)BeginConversation();else RefreshLine();
            openingRoutine = StartCoroutine(FadeOpen());
            return true;
        }

        private void Suspend(Behaviour control)
        {
            if (control == null || !control.enabled) return;
            suspended.Add(control); control.enabled = false;
        }
        private void Update()
        {
            if (!IsOpen) return;
            if (actor == null || actor.GetComponent<CharacterHealth>().isDead) { Close(); return; }
            if (IsClosing || openingRoutine != null) return;
            if (Time.frameCount <= openedFrame || Keyboard.current == null) return;
            if (IsShopping) { shop.HandleInput(); return; }
            var kb = Keyboard.current;
            if (kb.escapeKey.wasPressedThisFrame) RequestClose();
            else if(dialogue!=null)HandleChoiceInput();
            else if (GameControls.Pressed(GameControls.Action.Interact) || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Advance();
        }
        public void Advance()
        {
            if (!IsOpen || IsClosing) return;
            if(dialogue!=null){AdvanceConversation();return;}
            if (CurrentLine + 1 >= dialogueLines.Length)
            {
                if (shop != null && shop.enabled) shop.Open(actor, view, this); else RequestClose();
            }
            else { CurrentLine++; RefreshLine(); MenuSFX.Play(MenuSFX.Kind.Confirm); }
        }
        private void RefreshLine()
        {
            body.text = dialogueLines[CurrentLine];
            hint.text = "[" + GameControls.Display(GameControls.Action.Interact) + "] " + (CurrentLine + 1 < dialogueLines.Length ? "Tiếp tục" : shop != null && shop.enabled ? "Cửa hàng" : "Kết thúc") + "     [Esc] Đóng";
        }
        public void Close()
        {
            if (openingRoutine != null) { StopCoroutine(openingRoutine); openingRoutine = null; }
            if (closingRoutine != null) { StopCoroutine(closingRoutine); closingRoutine = null; }
            if (!IsOpen) return;
            MenuSFX.Play(MenuSFX.Kind.Close);
            if (shop != null) shop.Close();
            CloseConversationView();
            IsOpen = false; if (active == this) active = null;
            blockedThroughFrame = Time.frameCount + 1;
            foreach (var control in suspended)
            {
                if (control == null) continue;
                var health = control.GetComponent<CharacterHealth>();
                if (health == null || !health.isDead) control.enabled = true;
            }
            suspended.Clear(); actor = null;
            if (view != null) { view.SetActive(false); Destroy(view); }
        }
        protected override void OnDisable() { Close(); base.OnDisable(); }

        public void RequestClose()
        {
            if (!IsOpen || IsClosing) return;
            if (!isActiveAndEnabled || viewGroup == null) { Close(); return; }
            if (openingRoutine != null) { StopCoroutine(openingRoutine); openingRoutine = null; }
            closingRoutine = StartCoroutine(FadeClosed());
        }
        private System.Collections.IEnumerator FadeOpen()
        {
            float elapsed = 0;
            while (elapsed < openFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                viewGroup.alpha = Mathf.SmoothStep(0,1,PixelAnimationSettings.Progress(elapsed,openFadeDuration));
                yield return null;
            }
            viewGroup.alpha = 1; openingRoutine = null;
        }
        private System.Collections.IEnumerator FadeClosed()
        {
            viewGroup.interactable = false;
            float start = viewGroup.alpha, elapsed = 0;
            while (elapsed < closeFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                viewGroup.alpha = Mathf.Lerp(start,0,Mathf.SmoothStep(0,1,PixelAnimationSettings.Progress(elapsed,closeFadeDuration)));
                yield return null;
            }
            closingRoutine = null; Close();
        }

        private void BuildView()
        {
            view = new GameObject("NPC Dialogue", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            viewGroup = view.GetComponent<CanvasGroup>(); viewGroup.alpha = 0;
            var canvas = view.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
            var scaler = view.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            canvas.pixelPerfect = true;
            var panel = new GameObject("Dialogue panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(view.transform, false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.15f, .18f); rect.anchorMax = new Vector2(.85f, .41f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            var background = panel.GetComponent<Image>(); background.sprite = panelSprite;
            background.type = Image.Type.Sliced; background.pixelsPerUnitMultiplier = 1.5f;
            background.color = panelSprite != null ? Color.white : new Color(.89f,.75f,.52f);
            background.raycastTarget = false;
            var title = MakeText(panel.transform, "Speaker", new Vector2(.06f,.70f), new Vector2(.94f,.9f), 22);
            title.text = displayName; title.color = new Color(.30f,.16f,.08f);
            body = MakeText(panel.transform,"Text",new Vector2(.06f,.30f),new Vector2(.94f,.70f),20);
            hint = MakeText(panel.transform,"Controls",new Vector2(.06f,.10f),new Vector2(.90f,.28f),15);
            hint.alignment = TextAnchor.MiddleRight; hint.color = new Color(.43f,.29f,.15f);
            if (advanceArrow != null)
            {
                var arrow = new GameObject("Continue arrow", typeof(RectTransform), typeof(Image)); arrow.transform.SetParent(panel.transform,false);
                var r = arrow.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(.93f,.19f); r.sizeDelta = new Vector2(14,14);
                var image = arrow.GetComponent<Image>(); image.sprite = advanceArrow; image.preserveAspect = true; image.raycastTarget = false;
            }
        }
        private Text MakeText(Transform parent, string name, Vector2 min, Vector2 max, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent,false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>(); text.font = dialogueFont != null ? dialogueFont : GameFont.Current;
            text.fontSize = size; text.color = new Color(.23f,.17f,.12f); text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            return text;
        }
    }
}
