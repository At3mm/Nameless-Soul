using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace World
{
    public sealed class WorldMapOverlay : MonoBehaviour
    {
        private sealed class CheckpointLabelView
        {
            public RectTransform root;
            public RectTransform label;
            public string displayName;
            public Vector2 mapPosition;
        }

        private sealed class AreaLabelView
        {
            public RectTransform root;
            public string displayName;
            public Vector2 mapPosition;
            public Vector2 screenOffset;
        }

        private const float MinZoom = 1f;
        private const float MaxZoom = 4f;
        private const float WheelZoomFactor = 1.25f;
        private const float WheelZoomSpeedMultiplier = 5f;
        private const float CheckpointLabelZoom = 1.65f;
        private const float AreaLabelZoom = 1.25f;
        private static WorldMapOverlay instance;
        private readonly List<Behaviour> suspendedControls = new List<Behaviour>();
        private readonly List<CheckpointLabelView> checkpointLabels = new List<CheckpointLabelView>();
        private readonly List<AreaLabelView> areaLabels = new List<AreaLabelView>();
        private WorldMapDefinition[] maps = Array.Empty<WorldMapDefinition>();
        private GameObject canvasRoot;
        private RectTransform mapViewRect, mapRect, markerRect;
        private RawImage mapImage;
        private Image markerImage;
        private Text title, areaTitle, help, zoomLabel;
        private Sprite panelSprite;
        private Font mapFont;
        private int currentMap;
        private float previousTimeScale;
        private float zoom = MinZoom;
        private float baseMapScale = 1f;
        private Vector2 panOffset;
        private Vector2 dragStartPointer, dragStartPan;
        private bool isOpen;
        private bool draggingMap;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLock;

        public static bool IsOpen => instance != null && instance.isOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            var manager = new GameObject("World Map Overlay");
            DontDestroyOnLoad(manager);
            instance = manager.AddComponent<WorldMapOverlay>();
            instance.OnSceneChanged(SceneManager.GetActiveScene());
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            maps = Resources.LoadAll<WorldMapDefinition>("WorldMaps")
                .Where(map => map != null && map.image != null && map.worldBounds.width > 0 && map.worldBounds.height > 0)
                .OrderBy(map => map.scenePath, StringComparer.Ordinal)
                .ToArray();
            if (maps.Length == 0) Debug.LogError("World map definitions are missing or invalid in Resources/WorldMaps.");
            var checkpoint = FindFirstObjectByType<CheckpointMenu>(FindObjectsInactive.Include);
            panelSprite = checkpoint != null ? checkpoint.contentSprite : null;
            mapFont = checkpoint != null && checkpoint.menuFont != null ? checkpoint.menuFont : GameFont.Current;
            BuildUI();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (isOpen) Close();
            if (instance == this) instance = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (isOpen)
            {
                if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame))
                {
                    GameControls.SuppressThisFrame();
                    Close(keyboard.escapeKey.wasPressedThisFrame);
                    return;
                }
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                {
                    if (maps.Length > 1)
                    {
                        currentMap = (currentMap + 1) % maps.Length;
                        RefreshMap();
                    }
                    GameControls.SuppressThisFrame();
                }
                UpdateMapPointer();
                RefreshMarker();
            }
            else if (keyboard != null && keyboard.mKey.wasPressedThisFrame && CanOpen())
            {
                GameControls.SuppressThisFrame();
                Open();
            }
        }

        private bool CanOpen()
        {
            return currentMap >= 0 && Time.timeScale > 0f && !GameControls.Blocked &&
                !SettingsMenuToggle.AnyOpen && !CheckpointMenu.BlocksPauseMenu &&
                !NpcInteractable.BlocksPauseMenu && !InGameCutscene.BlocksPauseMenu;
        }

        private void Open()
        {
            previousTimeScale = Time.timeScale;
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            isOpen = true;
            draggingMap = false;
            zoom = MinZoom;
            panOffset = Vector2.zero;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (canvasRoot != null) canvasRoot.SetActive(true);
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                foreach (var control in new Behaviour[]
                {
                    player.GetComponent<PlayerController>(), player.GetComponent<PlayerCombat>(),
                    player.GetComponent<PlayerDash>(), player.GetComponent<CharacterParry>(),
                    player.GetComponent<PlayerHealthPotion>(), player.GetComponent<PlayerInteractor>()
                })
                {
                    if (control == null || !control.enabled) continue;
                    suspendedControls.Add(control);
                    control.enabled = false;
                }
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null) body.linearVelocity = Vector2.zero;
            }
            foreach (var pauseMenu in FindObjectsByType<SettingsMenuToggle>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (pauseMenu == null || !pauseMenu.enabled || suspendedControls.Contains(pauseMenu)) continue;
                suspendedControls.Add(pauseMenu);
                pauseMenu.enabled = false;
            }
            Time.timeScale = 0f;
            RefreshMap();
        }

        private void Close(bool suppressEscapePauseToggle = false)
        {
            if (!isOpen) return;
            isOpen = false;
            draggingMap = false;
            Time.timeScale = previousTimeScale;
            if (suppressEscapePauseToggle)
                suspendedControls.OfType<SettingsMenuToggle>().FirstOrDefault()?.SuppressEscapeToggleUntilReleased();
            foreach (var control in suspendedControls)
                if (control != null) control.enabled = true;
            suspendedControls.Clear();
            Cursor.visible = previousCursorVisible;
            Cursor.lockState = previousCursorLock;
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => OnSceneChanged(scene);

        private void OnSceneChanged(Scene scene)
        {
            if (isOpen) Close();
            currentMap = Array.FindIndex(maps, map => map.scenePath == scene.path);
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        private void BuildUI()
        {
            canvasRoot = new GameObject("World Map Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvas.pixelPerfect = true;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;

            var shade = MakeImage(canvasRoot.transform, "Shade", new Color(.015f, .02f, .025f, .78f));
            Stretch(shade.rectTransform, Vector2.zero, Vector2.one);

            var panelObject = new GameObject("Map Panel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(canvasRoot.transform, false);
            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.sizeDelta = new Vector2(1180, 680);
            var panelImage = panelObject.GetComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.type = panelSprite != null && panelSprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
            panelImage.color = panelSprite != null ? Color.white : new Color(.12f, .1f, .08f, .98f);
            panelImage.raycastTarget = false;

            title = MakeText(panelRect, "Map Title", new Vector2(.055f, .88f), new Vector2(.36f, .97f), 30, TextAnchor.MiddleLeft);
            title.text = "BẢN ĐỒ";
            SetTextOutline(title, new Color(.98f, .88f, .65f, .95f), new Vector2(1f, -1f));
            areaTitle = MakeText(panelRect, "Area Name", new Vector2(.57f, .88f), new Vector2(.94f, .97f), 30, TextAnchor.MiddleRight);
            areaTitle.color = new Color(.99f, .82f, .47f);
            SetTextOutline(areaTitle, new Color(.08f, .055f, .03f, .95f), new Vector2(1.5f, -1.5f));
            var rule = MakeImage(panelRect, "Header Rule", new Color(.39f, .25f, .12f));
            rule.rectTransform.anchorMin = new Vector2(.055f, .865f);
            rule.rectTransform.anchorMax = new Vector2(.945f, .87f);
            rule.rectTransform.offsetMin = rule.rectTransform.offsetMax = Vector2.zero;

            var frame = MakeImage(panelRect, "Map Frame", new Color(.19f, .12f, .065f));
            frame.rectTransform.anchorMin = new Vector2(.042f, .12f);
            frame.rectTransform.anchorMax = new Vector2(.958f, .855f);
            frame.rectTransform.offsetMin = frame.rectTransform.offsetMax = Vector2.zero;

            var view = new GameObject("Map View", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            view.transform.SetParent(panelRect, false);
            mapViewRect = view.GetComponent<RectTransform>();
            mapViewRect.anchorMin = new Vector2(.05f, .13f);
            mapViewRect.anchorMax = new Vector2(.95f, .845f);
            mapViewRect.offsetMin = mapViewRect.offsetMax = Vector2.zero;
            var viewImage = view.GetComponent<Image>();
            viewImage.color = new Color(.055f, .07f, .065f);
            viewImage.raycastTarget = false;

            var imageObject = new GameObject("Map Image", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(view.transform, false);
            mapRect = imageObject.GetComponent<RectTransform>();
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(.5f, .5f);
            mapImage = imageObject.GetComponent<RawImage>();
            mapImage.raycastTarget = false;

            var markerFrame = MakeImage(view.transform, "Player Marker Frame", new Color(.88f, .64f, .26f));
            markerRect = markerFrame.rectTransform;
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(.5f, .5f);
            markerRect.sizeDelta = new Vector2(42, 42);
            var frameOutline = markerFrame.gameObject.AddComponent<Outline>();
            frameOutline.effectColor = new Color(.08f, .045f, .02f, 1f);
            frameOutline.effectDistance = new Vector2(2, -2);
            var markerBacking = MakeImage(markerFrame.transform, "Player Marker Backing", new Color(.1f, .065f, .035f));
            markerBacking.rectTransform.anchorMin = markerBacking.rectTransform.anchorMax = new Vector2(.5f, .5f);
            markerBacking.rectTransform.sizeDelta = new Vector2(36, 36);
            var marker = MakeImage(markerBacking.transform, "Player Head Marker", Color.white);
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(.5f, .5f);
            marker.rectTransform.sizeDelta = new Vector2(30, 30);
            markerImage = marker;
            markerImage.preserveAspect = true;

            var footer = MakeImage(panelRect, "Map Help Backing", new Color(.06f, .045f, .03f, .92f));
            footer.rectTransform.anchorMin = new Vector2(.04f, .02f);
            footer.rectTransform.anchorMax = new Vector2(.96f, .11f);
            footer.rectTransform.offsetMin = footer.rectTransform.offsetMax = Vector2.zero;

            help = MakeText(panelRect, "Map Help", new Vector2(.055f, .025f), new Vector2(.77f, .105f), 19, TextAnchor.MiddleLeft);
            help.text = "ESC / M  Đóng   •   Con lăn  Phóng to / thu nhỏ   •   Kéo chuột trái  Di chuyển   •   E  Đổi bản đồ";
            help.color = new Color(.98f, .91f, .76f);
            SetTextOutline(help, new Color(.035f, .025f, .015f, 1f), new Vector2(1.5f, -1.5f));
            zoomLabel = MakeText(panelRect, "Zoom Level", new Vector2(.78f, .025f), new Vector2(.945f, .105f), 20, TextAnchor.MiddleRight);
            zoomLabel.color = new Color(.99f, .82f, .47f);
            SetTextOutline(zoomLabel, new Color(.035f, .025f, .015f, 1f), new Vector2(1.5f, -1.5f));
            canvasRoot.SetActive(false);
        }

        private static void SetTextOutline(Text text, Color color, Vector2 distance)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private Text MakeText(Transform parent, string name, Vector2 min, Vector2 max, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = mapFont != null ? mapFont : GameFont.Current;
            text.fontSize = size;
            text.color = new Color(.24f, .16f, .085f);
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void RefreshMap()
        {
            if (maps.Length == 0 || currentMap < 0 || currentMap >= maps.Length) return;
            var definition = maps[currentMap];
            mapImage.texture = definition.image;
            float width = mapViewRect.rect.width;
            float height = mapViewRect.rect.height;
            baseMapScale = Mathf.Min(width / definition.image.width, height / definition.image.height);
            var baseSize = new Vector2(definition.image.width * baseMapScale, definition.image.height * baseMapScale);
            mapRect.sizeDelta = baseSize;
            areaTitle.text = definition.displayName;
            markerImage.sprite = definition.playerMarker;
            zoom = MinZoom;
            panOffset = Vector2.zero;
            BuildCheckpointLabels(definition, baseSize);
            BuildAreaLabels(definition, baseSize);
            ApplyMapTransform();
            RefreshMarker();
            markerRect.SetAsLastSibling();
            RefreshZoomLabel();
        }

        private void BuildCheckpointLabels(WorldMapDefinition definition, Vector2 baseSize)
        {
            foreach (var view in checkpointLabels)
                if (view.root != null) Destroy(view.root.gameObject);
            checkpointLabels.Clear();
            if (definition.checkpoints == null) return;

            foreach (var checkpoint in definition.checkpoints)
            {
                if (checkpoint == null || string.IsNullOrWhiteSpace(checkpoint.displayName)) continue;
                var rootObject = new GameObject("Checkpoint Location " + checkpoint.displayName, typeof(RectTransform));
                rootObject.transform.SetParent(mapViewRect, false);
                var root = rootObject.GetComponent<RectTransform>();
                root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
                root.sizeDelta = Vector2.zero;

                var pin = MakeImage(root, "Shrine Pin", new Color(1f, .77f, .31f));
                pin.rectTransform.anchorMin = pin.rectTransform.anchorMax = new Vector2(.5f, .5f);
                pin.rectTransform.sizeDelta = new Vector2(9, 9);
                pin.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);

                var labelObject = new GameObject("Name Plate", typeof(RectTransform), typeof(Image));
                labelObject.transform.SetParent(root, false);
                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(.5f, .5f);
                labelRect.sizeDelta = new Vector2(220, 38);
                var side = checkpointLabels.Count % 2 == 0 ? 1f : -1f;
                labelRect.pivot = new Vector2(side > 0 ? 0f : 1f, .5f);
                labelRect.anchoredPosition = new Vector2(side * 9f, 20f);
                var plate = labelObject.GetComponent<Image>();
                plate.color = new Color(.07f, .05f, .03f, .94f);
                plate.raycastTarget = false;
                var plateOutline = labelObject.AddComponent<Outline>();
                plateOutline.effectColor = new Color(.83f, .61f, .29f, .95f);
                plateOutline.effectDistance = new Vector2(1f, -1f);
                var text = MakeText(labelObject.transform, "Name", Vector2.zero, Vector2.one, 18, TextAnchor.MiddleCenter);
                text.text = checkpoint.displayName;
                text.color = new Color(.98f, .9f, .72f);
                SetTextOutline(text, new Color(.035f, .025f, .015f, 1f), new Vector2(1f, -1f));

                var locationView = new CheckpointLabelView
                {
                    root = root,
                    label = labelRect,
                    displayName = checkpoint.displayName,
                    mapPosition = WorldToMapPosition(checkpoint.worldPosition, definition.worldBounds, baseSize)
                };
                checkpointLabels.Add(locationView);
            }
        }

        private void BuildAreaLabels(WorldMapDefinition definition, Vector2 baseSize)
        {
            foreach (var view in areaLabels)
                if (view.root != null) Destroy(view.root.gameObject);
            areaLabels.Clear();
            if (definition.areas == null) return;

            foreach (var area in definition.areas)
            {
                if (area == null || string.IsNullOrWhiteSpace(area.displayName)) continue;
                var labelObject = new GameObject("Area Name " + area.displayName, typeof(RectTransform), typeof(Image));
                labelObject.transform.SetParent(mapViewRect, false);
                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(.5f, .5f);
                labelRect.sizeDelta = new Vector2(200, 40);
                var plate = labelObject.GetComponent<Image>();
                plate.color = new Color(.055f, .04f, .025f, .9f);
                plate.raycastTarget = false;
                var plateOutline = labelObject.AddComponent<Outline>();
                plateOutline.effectColor = new Color(.88f, .66f, .32f, .95f);
                plateOutline.effectDistance = new Vector2(1.5f, -1.5f);
                var text = MakeText(labelObject.transform, "Name", Vector2.zero, Vector2.one, 19, TextAnchor.MiddleCenter);
                text.text = area.displayName;
                text.color = new Color(1f, .91f, .72f);
                SetTextOutline(text, new Color(.025f, .018f, .01f, 1f), new Vector2(1f, -1f));
                var mapPosition = WorldToMapPosition(area.worldPosition, definition.worldBounds, baseSize);
                var screenOffset = FindAreaLabelOffset(mapPosition);
                areaLabels.Add(new AreaLabelView
                {
                    root = labelRect,
                    displayName = area.displayName,
                    mapPosition = mapPosition,
                    screenOffset = screenOffset
                });
            }
        }

        private Vector2 FindAreaLabelOffset(Vector2 mapPosition)
        {
            var candidates = new[] { 0f, 48f, -48f, 96f, -96f };
            foreach (float offset in candidates)
            {
                bool overlaps = areaLabels.Any(existing =>
                    Mathf.Abs((mapPosition.x - existing.mapPosition.x) * AreaLabelZoom) < 200f &&
                    Mathf.Abs((mapPosition.y - existing.mapPosition.y) * AreaLabelZoom +
                        offset - existing.screenOffset.y) < 42f);
                if (!overlaps) return new Vector2(0f, offset);
            }
            return new Vector2(0f, candidates[candidates.Length - 1]);
        }

        private void ApplyMapTransform()
        {
            if (mapViewRect == null || mapRect == null) return;
            ClampPan();
            var baseSize = new Vector2(mapImage.texture.width * baseMapScale, mapImage.texture.height * baseMapScale);
            mapRect.sizeDelta = baseSize * zoom;
            mapRect.anchoredPosition = panOffset;
            bool showAreaNames = zoom >= AreaLabelZoom;
            foreach (var area in areaLabels)
            {
                if (area.root == null) continue;
                area.root.anchoredPosition = panOffset + area.mapPosition * zoom + area.screenOffset;
                area.root.gameObject.SetActive(showAreaNames);
            }
            bool showCheckpointNames = zoom >= CheckpointLabelZoom;
            foreach (var checkpoint in checkpointLabels)
            {
                if (checkpoint.root == null) continue;
                checkpoint.root.anchoredPosition = panOffset + checkpoint.mapPosition * zoom;
                bool showLabel = showCheckpointNames;
                if (showCheckpointNames)
                {
                    bool placeRight = checkpoint.root.anchoredPosition.x < mapViewRect.rect.width * .25f;
                    checkpoint.label.pivot = new Vector2(placeRight ? 0f : 1f, .5f);
                    checkpoint.label.anchoredPosition = new Vector2(placeRight ? 9f : -9f,
                        checkpoint.root.anchoredPosition.y > mapViewRect.rect.height * .3f ? -20f : 20f);
                    showLabel = !areaLabels.Any(area =>
                        string.Equals(area.displayName, checkpoint.displayName, StringComparison.Ordinal) ||
                        LabelsOverlap(area.root, checkpoint.label));
                }
                checkpoint.root.gameObject.SetActive(showCheckpointNames);
                checkpoint.label.gameObject.SetActive(showLabel);
            }
            RefreshZoomLabel();
        }

        private static bool LabelsOverlap(RectTransform first, RectTransform second)
        {
            if (first == null || second == null) return false;
            Vector2 firstCenter = first.anchoredPosition;
            Vector2 secondCenter = second.anchoredPosition + second.parent.GetComponent<RectTransform>().anchoredPosition;
            secondCenter += new Vector2(second.pivot.x == 0f ? second.rect.width * .5f : -second.rect.width * .5f, 0f);
            Vector2 halfSize = (first.rect.size + second.rect.size) * .5f;
            Vector2 distance = firstCenter - secondCenter;
            return Mathf.Abs(distance.x) < halfSize.x && Mathf.Abs(distance.y) < halfSize.y;
        }

        private void ClampPan()
        {
            if (mapViewRect == null || mapImage == null || mapImage.texture == null) return;
            var viewSize = mapViewRect.rect.size;
            var mapSize = new Vector2(mapImage.texture.width * baseMapScale, mapImage.texture.height * baseMapScale) * zoom;
            float maxX = Mathf.Max(0f, (mapSize.x - viewSize.x) * .5f);
            float maxY = Mathf.Max(0f, (mapSize.y - viewSize.y) * .5f);
            panOffset = new Vector2(Mathf.Clamp(panOffset.x, -maxX, maxX), Mathf.Clamp(panOffset.y, -maxY, maxY));
        }

        private void UpdateMapPointer()
        {
            var mouse = Mouse.current;
            if (mouse == null || mapViewRect == null) return;
            var screenPosition = mouse.position.ReadValue();
            bool overMap = RectTransformUtility.RectangleContainsScreenPoint(mapViewRect, screenPosition, null);
            if (mouse.leftButton.wasPressedThisFrame && overMap &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewRect, screenPosition, null, out dragStartPointer))
            {
                draggingMap = true;
                dragStartPan = panOffset;
            }
            if (draggingMap && mouse.leftButton.isPressed &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewRect, screenPosition, null, out var currentPointer))
            {
                panOffset = dragStartPan + currentPointer - dragStartPointer;
                ApplyMapTransform();
            }
            if (mouse.leftButton.wasReleasedThisFrame) draggingMap = false;

            float scroll = mouse.scroll.ReadValue().y;
            if (overMap && !Mathf.Approximately(scroll, 0f)) ZoomAt(screenPosition, scroll);
        }

        private void ZoomAt(Vector2 screenPosition, float scroll)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewRect, screenPosition, null, out var pointer))
                return;
            float steps = Mathf.Clamp(scroll / 120f, -1f, 1f);
            if (Mathf.Abs(steps) < .1f) steps = Mathf.Clamp(scroll * .25f, -1f, 1f);
            float nextZoom = Mathf.Clamp(zoom * Mathf.Pow(WheelZoomFactor, steps * WheelZoomSpeedMultiplier), MinZoom, MaxZoom);
            if (Mathf.Approximately(nextZoom, zoom)) return;
            var mapPoint = (pointer - panOffset) / zoom;
            zoom = nextZoom;
            panOffset = pointer - mapPoint * zoom;
            ApplyMapTransform();
        }

        private void RefreshMarker()
        {
            if (maps.Length == 0 || currentMap < 0 || currentMap >= maps.Length || markerRect == null) return;
            var definition = maps[currentMap];
            var player = GameObject.FindGameObjectWithTag("Player");
            bool belongsToMap = player != null && player.scene.path == definition.scenePath;
            markerRect.gameObject.SetActive(belongsToMap && definition.playerMarker != null);
            if (!belongsToMap || definition.playerMarker == null) return;
            var baseSize = new Vector2(definition.image.width * baseMapScale, definition.image.height * baseMapScale);
            var point = WorldToMapPosition(player.transform.position, definition.worldBounds, baseSize);
            markerRect.anchoredPosition = panOffset + point * zoom;
        }

        private void RefreshZoomLabel()
        {
            if (zoomLabel != null) zoomLabel.text = Mathf.RoundToInt(zoom * 100f) + "%";
        }

        public static Vector2 WorldToMapPosition(Vector3 worldPosition, Rect worldBounds, Vector2 imageSize)
        {
            float x = Mathf.Clamp01((worldPosition.x - worldBounds.xMin) / worldBounds.width);
            float y = Mathf.Clamp01((worldPosition.y - worldBounds.yMin) / worldBounds.height);
            return new Vector2((x - .5f) * imageSize.x, (y - .5f) * imageSize.y);
        }
    }
}
