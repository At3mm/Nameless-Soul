using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>Displays a painted tilemap layout as screen-space UI.</summary>
public class SettingsMenuToggle : MonoBehaviour
{
    public GameObject menuPanel;
    public RectTransform paintedUI;
    public Grid layoutGrid;
    public Tilemap[] layoutLayers;
    [Min(0.01f)] public float pixelsPerUnit = 100f;
    public bool pauseGame = true;
    [Header("Book animation")]
    public Image animatedBook;
    public GameObject menuContent;
    public Sprite[] openingFrames;
    public Sprite[] closingFrames;
    [Min(0.01f)] public float openingDuration = 0.45f;
    [Min(0.01f)] public float closingDuration = 0.35f;
    public bool IsOpen { get; private set; }
    private static readonly HashSet<SettingsMenuToggle> openMenus = new HashSet<SettingsMenuToggle>();
    public static bool AnyOpen => openMenus.Count>0;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOpenMenus(){openMenus.Clear();}
    public bool IsAnimating { get; private set; }
    public event System.Action<bool> OpenStateChanged;
    public event System.Action Opened;

    private float bookProgress;

    private InputAction toggleAction;
    private float previousTimeScale;
    private bool pausedByMenu;
    private bool suppressEscapeToggleUntilReleased;
    private readonly List<Behaviour> suspendedControls = new List<Behaviour>();

    private void Awake()
    {
        toggleAction = GameControls.CreateButton(GameControls.Action.Pause);
        toggleAction.performed += OnToggle;
        // Legacy book artwork is no longer rendered; pause presentation is built by navigation.
        if (layoutLayers != null)
            foreach (var map in layoutLayers)
                if (map != null && map.TryGetComponent<TilemapRenderer>(out var renderer)) renderer.enabled = false;
        if (animatedBook != null) animatedBook.gameObject.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    private void OnEnable() { toggleAction?.Enable(); }
    private void OnDisable()
    {
        openMenus.Remove(this);
        toggleAction?.Disable();
        IsOpen = IsAnimating = false;
        bookProgress = 0f;
        if (menuPanel != null) menuPanel.SetActive(false);
        RestoreGameplay();
    }
    private void OnDestroy() { GameControls.Release(toggleAction); }
    internal void SuppressEscapeToggleUntilReleased()
    {
        suppressEscapeToggleUntilReleased = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
    }

    private void OnToggle(InputAction.CallbackContext context)
    {
        if (suppressEscapeToggleUntilReleased && context.control != null && context.control.path == "<Keyboard>/escape") return;
        if(GameControls.Blocked)return;
        if (World.InGameCutscene.BlocksPauseMenu || World.CheckpointMenu.BlocksPauseMenu || World.NpcInteractable.BlocksPauseMenu) return;
        var navigation=GetComponent<PauseBookNavigation>();
        if(IsOpen && navigation!=null && navigation.TryBack())return;
        SetOpen(!IsOpen);
    }
    public void Close() { SetOpen(false); }

    public void SetOpen(bool open)
    {
        if (open && (World.InGameCutscene.BlocksPauseMenu || World.CheckpointMenu.BlocksPauseMenu || World.NpcInteractable.BlocksPauseMenu)) return;
        if (menuPanel == null || IsOpen == open) return;
        MenuSFX.Play(open ? MenuSFX.Kind.Open : MenuSFX.Kind.Close);
        IsOpen = open;
        if(open){openMenus.Add(this);AreaNotice.Dismiss();}else openMenus.Remove(this);
        menuPanel.SetActive(true);
        if (open && pauseGame && !pausedByMenu)
        {
            previousTimeScale = Time.timeScale;
            pausedByMenu = true;
            Time.timeScale = 0f;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Suspend(player.GetComponent<PlayerController>());
                Suspend(player.GetComponent<PlayerDash>());
                Suspend(player.GetComponent<PlayerCombat>());
            }
        }
        IsAnimating = true;
        UpdateBookVisual();
        OpenStateChanged?.Invoke(open);
    }

    private void Update()
    {
        if (suppressEscapeToggleUntilReleased && (Keyboard.current == null || !Keyboard.current.escapeKey.isPressed))
            suppressEscapeToggleUntilReleased = false;
        if (!IsAnimating) return;
        AdvanceBookAnimation(Time.unscaledDeltaTime);
    }

    private void AdvanceBookAnimation(float deltaTime)
    {
        bookProgress=Mathf.MoveTowards(bookProgress,IsOpen?1:0,deltaTime/.18f);
        UpdateBookVisual();
        if((IsOpen && bookProgress>=1) || (!IsOpen && bookProgress<=0))
        {
            IsAnimating=false;
            if(IsOpen)Opened?.Invoke();
            else{menuPanel.SetActive(false);RestoreGameplay();}
        }
    }
    private void UpdateBookVisual()
    {
        if(paintedUI!=null)paintedUI.gameObject.SetActive(false);
        if(menuContent!=null)menuContent.SetActive(false);
        if(animatedBook!=null)animatedBook.gameObject.SetActive(false);
        var group=menuPanel.GetComponent<CanvasGroup>();if(group==null)group=menuPanel.AddComponent<CanvasGroup>();
        group.alpha=PixelAnimationSettings.Progress(bookProgress*.18f,.18f);group.interactable=IsOpen;group.blocksRaycasts=IsOpen;
    }

    private void RestoreGameplay()
    {
        if (pausedByMenu)
        {
            Time.timeScale = previousTimeScale;
            pausedByMenu = false;
            foreach (var control in suspendedControls)
            {
                if (control == null) continue;
                var health = control.GetComponent<CharacterHealth>();
                if (health == null || !health.isDead) control.enabled = true;
            }
            suspendedControls.Clear();
        }
    }

    private void Suspend(Behaviour control)
    {
        if (control == null || !control.enabled) return;
        suspendedControls.Add(control);
        control.enabled = false;
    }

    [ContextMenu("Rebuild Painted UI")]
    public void RebuildPaintedUI()
    {
        if (paintedUI == null || layoutGrid == null || layoutLayers == null) return;
        for (int i = paintedUI.childCount - 1; i >= 0; i--)
        {
            var child = paintedUI.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        foreach (var map in layoutLayers)
        {
            if (map == null) continue;
            foreach (var cell in map.cellBounds.allPositionsWithin)
            {
                if (!map.HasTile(cell)) continue;
                var sprite = map.GetSprite(cell);
                var animated = map.GetTile(cell) as AnimatedTile;
                if (animated != null && animated.m_AnimatedSprites != null && animated.m_AnimatedSprites.Length > 0)
                    sprite = animated.m_AnimatedSprites[0];
                if (sprite == null) continue;
                var item = new GameObject(map.name + "_" + cell, typeof(RectTransform), typeof(Image));
                item.layer = 5;
                item.transform.SetParent(paintedUI, false);
                var image = item.GetComponent<Image>();
                image.sprite = sprite;
                image.color = map.GetColor(cell) * map.color;
                image.raycastTarget = false;
                var rect = (RectTransform)item.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = sprite.pivot / sprite.rect.size;
                var matrix = map.GetTransformMatrix(cell);
                var position = map.CellToLocalInterpolated((Vector3)cell + map.tileAnchor)
                    + (Vector3)matrix.GetColumn(3);
                rect.anchoredPosition = (Vector2)layoutGrid.transform.InverseTransformPoint(
                    map.transform.TransformPoint(position)) * pixelsPerUnit;
                rect.sizeDelta = sprite.rect.size * (pixelsPerUnit / sprite.pixelsPerUnit);
                rect.localRotation = matrix.rotation;
                rect.localScale = matrix.lossyScale;
                if (animated != null && animated.m_AnimatedSprites != null && animated.m_AnimatedSprites.Length > 0)
                {
                    var animation = item.AddComponent<UIFrameAnimation>();
                    bool loop = (animated.m_TileAnimationFlags & TileAnimationFlags.LoopOnce) == 0;
                    animation.Configure(animated.m_AnimatedSprites,
                        animated.m_MinSpeed * map.animationFrameRate, loop,
                        (animated.m_TileAnimationFlags & TileAnimationFlags.UnscaledTime) != 0);
                }
            }
        }
    }
}
