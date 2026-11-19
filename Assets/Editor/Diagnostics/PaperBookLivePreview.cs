using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

/// <summary>Renders a disposable copy; never activates or modifies the authored menu.</summary>
public class PaperBookLivePreview : EditorWindow
{
    private enum PreviewMode { Page, OpenBook, CloseBook, FlipLeft, FlipRight }

    [SerializeField] private SettingsMenuToggle source;
    [SerializeField] private int page;
    [SerializeField] private bool live = true;
    [SerializeField] private PreviewMode mode;
    [SerializeField] private float progress;
    private bool playing;
    private bool dirty = true;
    private int sourceHash;
    private double nextPoll;
    private double lastTime;
    private double nextIconFrame;
    private double iconPreviewStarted;
    private PreviewRenderUtility renderer;
    private SettingsMenuToggle copy;
    private PauseBookNavigation navigation;
    private readonly List<Sprite> temporarySprites = new List<Sprite>();
    private string error;
    private static readonly string[] PageNames = { "Status", "Inventory", "Quest", "Settings" };

    [MenuItem("Tools/PaperBook/Live Menu Preview")]
    public static void ShowWindow() { Open(null); }

    public static PaperBookLivePreview Open(SettingsMenuToggle menu)
    {
        var window = GetWindow<PaperBookLivePreview>("PaperBook Preview");
        window.minSize = new Vector2(480, 360);
        if (menu != null) window.source = menu;
        window.FindSource();
        window.dirty = true;
        window.Show();
        return window;
    }

    public void SetPage(int index)
    {
        page = Mathf.Clamp(index, 0, 3);
        mode = PreviewMode.Page;
        playing = false;
        dirty = true;
        Repaint();
    }

    private void OnEnable()
    {
        EditorApplication.update += Tick;
        Selection.selectionChanged += SelectionChanged;
        AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
        Tilemap.tilemapTileChanged += TilesChanged;
        lastTime = EditorApplication.timeSinceStartup;
        dirty = true;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Tick;
        Selection.selectionChanged -= SelectionChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= Cleanup;
        Tilemap.tilemapTileChanged -= TilesChanged;
        Cleanup();
    }

    private void SelectionChanged()
    {
        FindSource();
        Repaint();
    }

    private void TilesChanged(Tilemap map, Tilemap.SyncTile[] changes)
    {
        if (!live || source == null || !map.transform.IsChildOf(source.transform)) return;
        dirty = true;
        Repaint();
    }

    private void FindSource()
    {
        SettingsMenuToggle candidate = null;
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null) candidate = stage.prefabContentsRoot.GetComponentInChildren<SettingsMenuToggle>(true);
        if (candidate == null && Selection.activeGameObject != null)
            candidate = Selection.activeGameObject.GetComponentInParent<SettingsMenuToggle>(true);
        if (candidate == null && source == null)
            candidate = Object.FindFirstObjectByType<SettingsMenuToggle>();
        if (candidate != null && candidate != source)
        {
            source = candidate;
            dirty = true;
        }
    }

    private void Tick()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (renderer != null) Cleanup();
            dirty = true;
            return;
        }
        double now = EditorApplication.timeSinceStartup;
        if (playing)
        {
            progress = Mathf.Repeat(progress + (float)(now - lastTime) / AnimationDuration(), 1f);
            Repaint();
        }
        lastTime = now;
        if (copy != null && now >= nextIconFrame && copy.GetComponentInChildren<UIFrameAnimation>(true) != null)
        {
            nextIconFrame = now + 1.0 / 20.0;
            Repaint();
        }
        if (now < nextPoll) return;
        nextPoll = now + 0.2;
        if (source == null) FindSource();
        if (!live || source == null) return;
        int hash = ComputeSourceHash();
        if (hash == sourceHash && !dirty) return;
        sourceHash = hash;
        dirty = true;
        Repaint();
    }

    private int ComputeSourceHash()
    {
        unchecked
        {
            int hash = source.GetInstanceID();
            foreach (var component in source.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                hash = hash * 31 + component.GetInstanceID();
                hash = hash * 31 + EditorUtility.GetDirtyCount(component);
                hash = hash * 31 + (component.gameObject.activeSelf ? 1 : 0);
            }
            return hash;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Live preview: edit the original prefab or Tilemap; this window renders a separate copy.", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        source = (SettingsMenuToggle)EditorGUILayout.ObjectField("Menu", source, typeof(SettingsMenuToggle), true);
        if (EditorGUI.EndChangeCheck()) dirty = true;
        using (new EditorGUILayout.HorizontalScope())
        {
            live = GUILayout.Toggle(live, "Live update", GUILayout.Width(100));
            if (GUILayout.Button("Refresh", GUILayout.Width(80))) dirty = true;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save PNG", GUILayout.Width(85)))
            {
                string path = EditorUtility.SaveFilePanel("Save menu preview", "", "paperbook-preview.png", "png");
                if (!string.IsNullOrEmpty(path)) SavePreviewPng(path);
            }
        }
        EditorGUI.BeginChangeCheck();
        page = GUILayout.Toolbar(page, PageNames);
        if (EditorGUI.EndChangeCheck()) dirty = true;
        using (new EditorGUILayout.HorizontalScope())
        {
            mode = (PreviewMode)EditorGUILayout.EnumPopup("View", mode);
            using (new EditorGUI.DisabledScope(mode == PreviewMode.Page))
                playing = GUILayout.Toggle(playing, "Play", "Button", GUILayout.Width(60));
        }
        if (mode != PreviewMode.Page) progress = EditorGUILayout.Slider("Animation", progress, 0f, 1f);
        if (source == null)
        {
            EditorGUILayout.HelpBox("Select SettingsMenu in the Hierarchy, or open its prefab.", MessageType.Warning);
            return;
        }
        Rect area = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(area, new Color(0.08f, 0.08f, 0.10f));
        if (Event.current.type == EventType.Repaint)
        {
            var texture = RenderPreview();
            if (texture != null) EditorGUI.DrawPreviewTexture(area, texture, null, ScaleMode.ScaleToFit);
        }
        if (!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error, MessageType.Error);
        EditorGUILayout.LabelField("Tilemap changes refresh automatically. Page content and animation timing remain editable in the original Inspector.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("Status numbers are sample values in this editor preview.", EditorStyles.wordWrappedMiniLabel);
    }

    private float AnimationDuration()
    {
        if (source == null) return 0.45f;
        if (mode == PreviewMode.OpenBook) return Mathf.Max(0.01f, source.openingDuration);
        if (mode == PreviewMode.CloseBook) return Mathf.Max(0.01f, source.closingDuration);
        var nav = source.GetComponent<PauseBookNavigation>();
        return nav != null ? Mathf.Max(0.01f, nav.pageTurnDuration) : 0.4f;
    }

    private void BuildCopy()
    {
        Cleanup();
        renderer = new PreviewRenderUtility();
        var root = Instantiate(source.gameObject);
        root.name = "PaperBook_PreviewOnly";
        root.hideFlags = HideFlags.HideAndDontSave;
        root.transform.position = Vector3.zero;
        renderer.AddSingleGO(root);
        copy = root.GetComponent<SettingsMenuToggle>();
        navigation = root.GetComponent<PauseBookNavigation>();
        copy.enabled = false;
        if (navigation != null) navigation.enabled = false;
        copy.RebuildPaintedUI();
        if (copy.layoutLayers != null)
            foreach (var map in copy.layoutLayers)
                if (map != null && map.TryGetComponent<UnityEngine.Tilemaps.TilemapRenderer>(out var tileRenderer))
                    tileRenderer.enabled = false;
        copy.menuPanel.SetActive(true);
        foreach (var inventory in root.GetComponentsInChildren<InventoryGridView>(true)) inventory.Refresh();
        var camera = renderer.camera;
        // Unity excludes Canvas graphics from cameras classified as Preview.
        camera.cameraType = CameraType.Game;
        camera.cullingMask = -1;
        camera.orthographic = true;
        camera.orthographicSize = 3.6f;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.transform.rotation = Quaternion.identity;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.17f, 0.18f, 0.21f);
        var canvas = copy.menuPanel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        if (navigation != null)
        {
            for (int i = 0; i < navigation.sections.Length; i++)
            {
                var section = navigation.sections[i];
                int sheet = i == page ? section.sourceSlot + 1 : 0;
                var fullSprite = navigation.selectedTabSheets[sheet];
                var crop = new Rect(fullSprite.rect.x + 770, fullSprite.rect.y + 720 - (245 + section.sourceSlot * 38 + 30), 48, 30);
                var sprite = Sprite.Create(fullSprite.texture, crop, new Vector2(0.5f, 0.5f), 100);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                temporarySprites.Add(sprite);
                section.background.sprite = sprite;
                section.caption.alpha = 1;
                section.button.interactable = true;
                var label = section.caption.GetComponentInChildren<Text>(true);
                if (label != null) label.color = i == page
                    ? new Color(1f, 0.88f, 0.57f) : new Color(0.83f, 0.77f, 0.65f);
            }
            if (navigation.statusValues != null)
                navigation.statusValues.text = "HP       100 / 100\n\nStamina  100 / 100\n\nATK      20\n\nDEF      10\n\nSTR      5\n\nCRIT     10%";
        }
        dirty = false;
        iconPreviewStarted = EditorApplication.timeSinceStartup;
        sourceHash = ComputeSourceHash();
    }

    private void ApplyView()
    {
        bool showPage = mode == PreviewMode.Page;
        copy.paintedUI.gameObject.SetActive(showPage);
        copy.menuContent.SetActive(showPage);
        copy.animatedBook.gameObject.SetActive(!showPage);
        if (navigation != null)
        {
            navigation.tabsRoot.gameObject.SetActive(showPage);
            navigation.pageAnimation.gameObject.SetActive(false);
            navigation.contentReveal.gameObject.SetActive(false);
            int selected = Mathf.Clamp(page, 0, navigation.sections.Length - 1);
            for (int i = 0; i < navigation.sections.Length; i++)
                navigation.sections[i].page.SetActive(i == selected);
            navigation.pageTitle.gameObject.SetActive(true);
            navigation.pageTitle.text = navigation.sections[selected].title.ToUpperInvariant();
        }
        Sprite[] frames = null;
        if (mode == PreviewMode.OpenBook) frames = copy.openingFrames;
        if (mode == PreviewMode.CloseBook) frames = copy.closingFrames;
        if (navigation != null && mode == PreviewMode.FlipLeft) frames = navigation.flipLeftFrames;
        if (navigation != null && mode == PreviewMode.FlipRight) frames = navigation.flipRightFrames;
        if (frames != null && frames.Length > 0)
            copy.animatedBook.sprite = frames[Mathf.Clamp(Mathf.FloorToInt(progress * frames.Length), 0, frames.Length - 1)];
    }

    private Texture RenderPreview()
    {
        if (source == null) return null;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            error = "Live Preview is available in Edit Mode. Use the Game view while playing.";
            return null;
        }
        try
        {
            if (dirty || copy == null || renderer == null) BuildCopy();
            ApplyView();
            foreach (var animation in copy.menuPanel.GetComponentsInChildren<UIFrameAnimation>())
                animation.Sample((float)(EditorApplication.timeSinceStartup - iconPreviewStarted));
            renderer.BeginPreview(new Rect(0, 0, 1280, 720), GUIStyle.none);
            // Populate dynamic font atlases before rebuilding all text geometry.
            foreach (var text in copy.menuPanel.GetComponentsInChildren<Text>())
                if (text.font != null) text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);
            foreach (var text in copy.menuPanel.GetComponentsInChildren<Text>()) text.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            renderer.Render(true, false);
            var texture = renderer.EndPreview();
            error = null;
            return texture;
        }
        catch (System.Exception exception)
        {
            error = exception.Message;
            return null;
        }
    }

    public void SavePreviewPng(string path)
    {
        var texture = RenderPreview() as RenderTexture;
        if (texture == null) throw new System.InvalidOperationException(error ?? "Preview is not ready.");
        var previous = RenderTexture.active;
        var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            if (!texture.sRGB && QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                var pixels = image.GetPixels();
                for (int i = 0; i < pixels.Length; i++) pixels[i] = pixels[i].gamma;
                image.SetPixels(pixels);
            }
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            DestroyImmediate(image);
        }
    }

    private void Cleanup()
    {
        if (renderer != null) renderer.Cleanup();
        renderer = null;
        copy = null;
        navigation = null;
        foreach (var sprite in temporarySprites)
            if (sprite != null) DestroyImmediate(sprite);
        temporarySprites.Clear();
    }
}

[CustomEditor(typeof(SettingsMenuToggle))]
public class SettingsMenuToggleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Live Preview", GUILayout.Height(30)))
            PaperBookLivePreview.Open((SettingsMenuToggle)target);
        DrawDefaultInspector();
    }
}

[CustomEditor(typeof(PauseBookNavigation))]
public class PauseBookNavigationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open Live Preview", GUILayout.Height(30)))
            PaperBookLivePreview.Open(((PauseBookNavigation)target).GetComponent<SettingsMenuToggle>());
        DrawDefaultInspector();
    }
}
