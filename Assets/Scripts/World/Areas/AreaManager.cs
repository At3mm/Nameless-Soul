using System;
using System.Collections.Generic;
using UnityEngine;

namespace World
{
    /// <summary>Checks actual player position, so teleports and overlapping regions work without trigger event ordering.</summary>
    public sealed class AreaManager : MonoBehaviour
    {
        public static AreaManager Instance { get; private set; }
        public AreaDefinition CurrentArea { get; private set; }
        public event Action<AreaDefinition> AreaChanged;
        [Min(0)] public float boundaryDelay = .25f;
        [Min(0)] public float repeatCooldown = 3f;
        private PlayerController player;
        private string candidateId;
        private float candidateSince, nextCheck;
        private float outsideSince = -1;
        private bool noticePending;
        private int currentScene;
        private readonly Dictionary<string, float> shown = new Dictionary<string, float>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { Instance = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Instance != null) return;
            var go = new GameObject("World Area Manager"); DontDestroyOnLoad(go);
            Instance = go.AddComponent<AreaManager>();
        }
        private void Update()
        {
            if (Time.timeScale <= 0 || Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + .1f;
            if (player == null || !player.gameObject.activeInHierarchy) player = FindFirstObjectByType<PlayerController>();
            if (player == null || player.GetComponent<CharacterHealth>().isDead) return;
            Evaluate(player.transform.position, player.gameObject.scene.handle);
        }
        public static AreaTrigger FindArea(Vector2 position, int sceneHandle)
        {
            AreaTrigger best = null;
            foreach (var zone in AreaTrigger.Active)
            {
                if (zone == null || !zone.isActiveAndEnabled || zone.gameObject.scene.handle != sceneHandle || zone.area == null || string.IsNullOrWhiteSpace(zone.area.areaId) || !zone.Contains(position)) continue;
                if (best == null || zone.priority > best.priority || zone.priority == best.priority && string.CompareOrdinal(zone.area.areaId, best.area.areaId) < 0) best = zone;
            }
            return best;
        }
        public void Evaluate(Vector2 position, int sceneHandle)
        {
            if (currentScene != sceneHandle) { currentScene = sceneHandle; CurrentArea = null; candidateId = null; outsideSince = -1; noticePending = false; }
            var zone = FindArea(position, sceneHandle);
            if (zone == null)
            {
                candidateId = null;
                if (outsideSince < 0) outsideSince = Time.unscaledTime;
                if (CurrentArea != null && Time.unscaledTime - outsideSince >= boundaryDelay)
                {
                    CurrentArea = null; noticePending = false;
                    AreaChanged?.Invoke(null);
                }
                return;
            }
            outsideSince = -1;
            var next = zone.area;
            if (CurrentArea != null && CurrentArea.areaId == next.areaId) { candidateId = null; PresentPending(); return; }
            if (candidateId != next.areaId) { candidateId = next.areaId; candidateSince = Time.unscaledTime; }
            if (Time.unscaledTime - candidateSince < boundaryDelay) return;
            CurrentArea = next; candidateId = null;
            noticePending = true;
            AreaChanged?.Invoke(next);
            PresentPending();
        }
        private void PresentPending()
        {
            if (!noticePending || CurrentArea == null) return;
            string key = currentScene + ":" + CurrentArea.areaId;
            if (shown.TryGetValue(key, out var last) && Time.unscaledTime - last < repeatCooldown) return;
            shown[key] = Time.unscaledTime;
            noticePending = false;
            AreaNotice.Show(CurrentArea);
        }
    }
}
