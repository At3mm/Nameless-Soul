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
        private static WorldMapOverlay instance;
        private readonly List<Behaviour> suspendedControls = new List<Behaviour>();
        private WorldMapDefinition[] maps = Array.Empty<WorldMapDefinition>();
        private GameObject canvasRoot;
        private RawImage mapImage;
        private RectTransform mapRect, markerRect;
        private Text title;
        private Sprite markerSprite;
        private Texture2D markerTexture;
        private int currentMap;
        private float previousTimeScale;
        private bool isOpen;

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
            BuildUI();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (isOpen) Close();
            if (markerSprite != null) Destroy(markerSprite);
            if (markerTexture != null) Destroy(markerTexture);
            if (instance == this) instance = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (isOpen)
            {
                if (keyboard.eKey.wasPressedThisFrame)
                {
                    if (maps.Length > 1)
                    {
                        currentMap = (currentMap + 1) % maps.Length;
                        RefreshMap();
                    }
                    GameControls.SuppressThisFrame();
                }
                if (keyboard.mKey.wasPressedThisFrame)
                {
                    GameControls.SuppressThisFrame();
                    Close();
                }
                RefreshMarker();
            }
            else if (keyboard.mKey.wasPressedThisFrame && CanOpen())
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
            isOpen = true;
            if (canvasRoot != null) canvasRoot.SetActive(true);
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                foreach (var control in new Behaviour[]
                {
                    player.GetComponent<PlayerController>(), player.GetComponent<PlayerCombat>(),
                    player.GetComponent<PlayerDash>(), player.GetComponent<CharacterParry>(),
                    player.GetComponent<PlayerHealthPotion>(), player.GetComponent<PlayerInteractor>(),
                    player.GetComponent<SettingsMenuToggle>()
                })
                {
                    if (control == null || !control.enabled) continue;
                    suspendedControls.Add(control);
                    control.enabled = false;
                }
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null) body.linearVelocity = Vector2.zero;
            }
            Time.timeScale = 0f;
            RefreshMap();
        }

        private void Close()
        {
            if (!isOpen) return;
            isOpen = false;
            Time.timeScale = previousTimeScale;
            foreach (var control in suspendedControls)
                if (control != null) control.enabled = true;
            suspendedControls.Clear();
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
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;

            var shade = MakeImage(canvasRoot.transform, "Shade", new Color(.015f, .02f, .025f, .94f));
            Stretch(shade.rectTransform, Vector2.zero, Vector2.one);

            var panel = new GameObject("Map Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasRoot.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
            panelRect.sizeDelta = new Vector2(1120, 680);

            title = MakeText(panel.transform, "Map Title", new Vector2(.05f, .91f), new Vector2(.95f, .99f), 30, TextAnchor.MiddleCenter);
            var view = new GameObject("Map View", typeof(RectTransform));
            view.transform.SetParent(panel.transform, false);
            var viewRect = view.GetComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(.04f, .10f);
            viewRect.anchorMax = new Vector2(.96f, .89f);
            viewRect.offsetMin = viewRect.offsetMax = Vector2.zero;

            var imageObject = new GameObject("Map Image", typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(view.transform, false);
            mapRect = imageObject.GetComponent<RectTransform>();
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(.5f, .5f);
            mapImage = imageObject.GetComponent<RawImage>();
            mapImage.raycastTarget = false;

            var marker = MakeImage(imageObject.transform, "Player Marker", new Color(1f, .24f, .06f, 1f));
            markerRect = marker.rectTransform;
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(.5f, .5f);
            markerRect.sizeDelta = new Vector2(20, 20);
            markerSprite = CreateMarkerSprite();
            marker.sprite = markerSprite;
            marker.preserveAspect = true;

            MakeText(panel.transform, "Map Help", new Vector2(.05f, .015f), new Vector2(.95f, .085f), 20, TextAnchor.MiddleCenter)
                .text = "M  Đóng bản đồ                 E  Đổi bản đồ";
            canvasRoot.SetActive(false);
        }

        private void RefreshMap()
        {
            if (maps.Length == 0 || currentMap < 0 || currentMap >= maps.Length) return;
            var definition = maps[currentMap];
            mapImage.texture = definition.image;
            float scale = Mathf.Min(mapRect.parent.GetComponent<RectTransform>().rect.width / definition.image.width,
                mapRect.parent.GetComponent<RectTransform>().rect.height / definition.image.height);
            mapRect.sizeDelta = new Vector2(definition.image.width * scale, definition.image.height * scale);
            title.text = definition.displayName;
            RefreshMarker();
        }

        private void RefreshMarker()
        {
            if (maps.Length == 0 || currentMap < 0 || currentMap >= maps.Length || markerRect == null) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            bool belongsToMap = player != null && player.scene.path == maps[currentMap].scenePath;
            markerRect.gameObject.SetActive(belongsToMap);
            if (!belongsToMap) return;
            markerRect.anchoredPosition = WorldToMapPosition(player.transform.position, maps[currentMap].worldBounds, mapRect.sizeDelta);
        }

        public static Vector2 WorldToMapPosition(Vector3 worldPosition, Rect worldBounds, Vector2 imageSize)
        {
            float x = Mathf.Clamp01((worldPosition.x - worldBounds.xMin) / worldBounds.width);
            float y = Mathf.Clamp01((worldPosition.y - worldBounds.yMin) / worldBounds.height);
            return new Vector2((x - .5f) * imageSize.x, (y - .5f) * imageSize.y);
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

        private static Text MakeText(Transform parent, string name, Vector2 min, Vector2 max, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = GameFont.Current;
            text.fontSize = size;
            text.color = new Color(.94f, .9f, .78f);
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private Sprite CreateMarkerSprite()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2((size - 1) * .5f, (size - 1) * .5f));
                    pixels[y * size + x] = distance <= 14 ? Color.white : Color.clear;
                }
            texture.SetPixels(pixels);
            texture.Apply();
            markerTexture = texture;
            return Sprite.Create(markerTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        }
    }
}
