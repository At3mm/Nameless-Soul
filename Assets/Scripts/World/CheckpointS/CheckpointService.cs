using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace World
{
    public sealed class CheckpointService : MonoBehaviour
    {
        public const string SaveKey = "Noname.Checkpoint.v1";
        public const string UnlockSaveKey = "Noname.Checkpoint.Unlocked.v1";
        [Serializable] private sealed class UnlockList { public List<UnlockEntry> entries = new List<UnlockEntry>(); }
        [Serializable] private sealed class UnlockEntry { public string scenePath, checkpointId, displayName; }
        public sealed class Destination
        {
            public string scenePath, checkpointId, displayName;
            public bool isCurrent;
            public string areaName;
            public string Area => !string.IsNullOrEmpty(areaName) ? areaName : System.IO.Path.GetFileNameWithoutExtension(scenePath).Replace("Scene", "");
        }
        public static event Action SavedChanged;
        [Serializable] public sealed class Record
        {
            public int version = 1;
            public string scenePath, checkpointId;
            public string returnScenePath, returnCheckpointId;
            public float x, y, z;
            public Vector3 Position => new Vector3(x, y, z);
        }
        private static CheckpointService instance;
        private Record fallback, pending;
        private bool respawning;
        private bool traveling;
        private int travelPotionCapacity;
        private float travelTimeScale;
        private readonly List<Behaviour> travelControls = new List<Behaviour>();
        public static bool IsRespawning => instance != null && instance.respawning;
        public static bool IsTraveling => instance != null && instance.traveling;
        [Min(0f)] public float deathDelay = 1.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { instance = null; SavedChanged = null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            new GameObject("CheckpointService").AddComponent<CheckpointService>();
            instance.PlaceOrBind(SceneManager.GetActiveScene());
            // Keep Play-in-Editor scene selection intact. Standalone resumes the save.
            if (!Application.isEditor && SceneManager.GetActiveScene().path != MainMenuController.ScenePath)
            {
                var saved = ReadSaved();
                if (saved != null) { instance.pending = saved; instance.StartCoroutine(instance.Reload(0f)); }
            }
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            CharacterHealth.AnyDied += OnDied;
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            CharacterHealth.AnyDied -= OnDied;
            if (instance == this) instance = null;
        }
        private static bool Valid(Record r)
        {
            return r != null && r.version == 1 && !string.IsNullOrEmpty(r.scenePath) &&
                SceneUtility.GetBuildIndexByScenePath(r.scenePath) >= 0 &&
                !float.IsNaN(r.x) && !float.IsInfinity(r.x) && !float.IsNaN(r.y) && !float.IsInfinity(r.y) &&
                !float.IsNaN(r.z) && !float.IsInfinity(r.z);
        }
        public static Record ReadSaved()
        {
            try { var r = JsonUtility.FromJson<Record>(PlayerPrefs.GetString(SaveKey, "")); return Valid(r) ? r : null; }
            catch (ArgumentException) { return null; }
        }
        public static bool Save(string scenePath, string id, Vector3 position, string displayName = null, bool rememberForReturn = false)
        {
            var r = new Record { scenePath = scenePath, checkpointId = id, x = position.x, y = position.y, z = position.z };
            if (!Valid(r) || string.IsNullOrWhiteSpace(id)) return false;
            var previous = ReadSaved();
            r.returnScenePath = rememberForReturn ? scenePath : previous != null ? previous.returnScenePath : null;
            r.returnCheckpointId = rememberForReturn ? id : previous != null ? previous.returnCheckpointId : null;
            var unlocked = ReadUnlocked();
            var entry = AddUnlocked(unlocked, scenePath, id);
            if (!string.IsNullOrWhiteSpace(displayName)) entry.displayName = displayName;
            PlayerPrefs.SetString(UnlockSaveKey, JsonUtility.ToJson(unlocked));
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(r)); PlayerPrefs.Save();
            if (rememberForReturn) GameplayTutorial.Report(GameObject.FindGameObjectWithTag("Player"), GameplayTutorial.Lesson.Pray);
            SavedChanged?.Invoke(); return true;
        }
        public static bool Unlock(string scenePath, string id, Vector3 position, string displayName)
        {
            if (!Valid(new Record { scenePath = scenePath, x = position.x, y = position.y, z = position.z }) || string.IsNullOrWhiteSpace(id)) return false;
            var unlocked = ReadUnlocked();
            var entry = AddUnlocked(unlocked, scenePath, id);
            if (!string.IsNullOrWhiteSpace(displayName)) entry.displayName = displayName;
            PlayerPrefs.SetString(UnlockSaveKey, JsonUtility.ToJson(unlocked));
            PlayerPrefs.Save(); SavedChanged?.Invoke(); return true;
        }
        private static UnlockList ReadUnlocked()
        {
            UnlockList list = null;
            try { list = JsonUtility.FromJson<UnlockList>(PlayerPrefs.GetString(UnlockSaveKey, "")); }
            catch (ArgumentException) { }
            if (list == null) list = new UnlockList();
            if (list.entries == null) list.entries = new List<UnlockEntry>();
            // Existing saves already count as unlocked when upgrading from the single-checkpoint format.
            var saved = ReadSaved();
            if (saved != null) AddUnlocked(list, saved.scenePath, saved.checkpointId);
            return list;
        }
        private static UnlockEntry AddUnlocked(UnlockList list, string scenePath, string id)
        {
            var entry = list.entries.Find(e => e != null && e.scenePath == scenePath && e.checkpointId == id);
            if (entry == null) { entry = new UnlockEntry { scenePath = scenePath, checkpointId = id }; list.entries.Add(entry); }
            return entry;
        }
        public static List<Destination> GetTravelDestinations(string currentScene, string currentId, bool includeCurrent = false)
        {
            var result = new List<Destination>();
            var sourceScene = SceneManager.GetSceneByPath(currentScene);
            var source = sourceScene.IsValid() && sourceScene.isLoaded ? FindCheckpoint(sourceScene, currentId) : null;
            var sourceArea = source != null ? AreaManager.FindArea(source.transform.position, sourceScene.handle) : null;
            foreach (var entry in ReadUnlocked().entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.checkpointId) || string.IsNullOrEmpty(entry.scenePath) ||
                    SceneUtility.GetBuildIndexByScenePath(entry.scenePath) < 0 ||
                    (!includeCurrent && entry.scenePath == currentScene && entry.checkpointId == currentId) ||
                    result.Exists(d => d.scenePath == entry.scenePath && d.checkpointId == entry.checkpointId)) continue;
                string title = entry.displayName;
                var loaded = SceneManager.GetSceneByPath(entry.scenePath);
                var statue = loaded.IsValid() && loaded.isLoaded ? FindCheckpoint(loaded, entry.checkpointId) : null;
                // Old save entries can outlive a renamed ID or a removed statue.
                if (loaded.IsValid() && loaded.isLoaded && statue == null) continue;
                if (statue != null && statue.TryGetComponent<CheckpointMenu>(out var menu)) title = menu.checkpointName;
                var targetArea = statue != null ? AreaManager.FindArea(statue.transform.position, loaded.handle) : null;
                result.Add(new Destination { scenePath = entry.scenePath, checkpointId = entry.checkpointId,
                    isCurrent = entry.scenePath == currentScene && (entry.checkpointId == currentId ||
                        includeCurrent && sourceArea != null && targetArea != null && sourceArea.area.areaId == targetArea.area.areaId),
                    areaName = targetArea != null ? targetArea.area.displayName : null,
                    displayName = string.IsNullOrWhiteSpace(title) ? entry.checkpointId : title });
            }
            result.Sort((a,b) => { int area = string.CompareOrdinal(a.scenePath,b.scenePath); return area != 0 ? area : string.CompareOrdinal(a.checkpointId,b.checkpointId); });
            return result;
        }

        private static CheckpointInteractable FindCheckpoint(Scene scene, string id)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var checkpoint in root.GetComponentsInChildren<CheckpointInteractable>(true))
                    if (checkpoint.isActiveAndEnabled && checkpoint.checkpointId == id) return checkpoint;
            return null;
        }

        public static bool CanTravelTo(string scenePath, string id)
        {
            var loaded = SceneManager.GetSceneByPath(scenePath);
            if (loaded.IsValid() && loaded.isLoaded && FindCheckpoint(loaded,id) == null) return false;
            return instance != null && !IsTraveling && !IsRespawning && !InGameCutscene.AnyPlaying &&
                !ScreenFader.Instance.IsTransitioning && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrEmpty(scenePath) &&
                SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0 && IsUnlocked(scenePath, id);
        }

        public static bool TravelTo(string scenePath, string id, PlayerInteractor actor)
        {
            if (!CanTravelTo(scenePath,id) || actor == null || !actor.CanUseCheckpoint || CheckpointMenu.AnyOpen) return false;
            var loaded = SceneManager.GetSceneByPath(scenePath);
            var target = loaded.IsValid() && loaded.isLoaded ? FindCheckpoint(loaded,id) : null;
            if (loaded.IsValid() && loaded.isLoaded && target == null) return false;
            instance.travelTimeScale = Time.timeScale;
            var potion = actor.GetComponent<PlayerHealthPotion>();
            instance.travelPotionCapacity = potion != null ? potion.capacity : 0;
            instance.traveling = true;
            instance.HoldTravelControls(actor.gameObject);
            Time.timeScale = 0f;
            SceneSpawnPoint.Prepare("", null);
            if (target != null && actor.gameObject.scene.path == scenePath) instance.StartCoroutine(instance.TravelWithinScene(actor,target));
            else
            {
                instance.pending = new Record { scenePath = scenePath, checkpointId = id };
                ScreenFader.Instance.TransitionToScene(scenePath,.3f,instance.FinishTravel);
            }
            return true;
        }

        public static bool ReturnToLastShrine(PlayerInteractor actor)
        {
            if(actor!=null && actor.IsInCombat){actor.ShowNotice("Không thể hồi quy khi đang giao tranh.");return false;}
            if (actor == null || !actor.CanUseCheckpoint) return false;
            var saved = ReadSaved();
            if (saved == null || string.IsNullOrEmpty(saved.returnCheckpointId))
            { actor.ShowNotice("Hãy cầu nguyện hoặc du hành đến một tượng thần trước."); return false; }
            if (!CanTravelTo(saved.returnScenePath, saved.returnCheckpointId))
            { actor.ShowNotice("Chưa thể trở về tượng thần đã ghi nhớ."); return false; }
            return TravelTo(saved.returnScenePath, saved.returnCheckpointId, actor);
        }

        private void HoldTravelControls(GameObject actor)
        {
            foreach (var control in new Behaviour[] { actor.GetComponent<PlayerController>(), actor.GetComponent<PlayerCombat>(),
                actor.GetComponent<PlayerDash>(), actor.GetComponent<CharacterParry>(), actor.GetComponent<PlayerHealthPotion>() })
                if (control != null && control.enabled) { travelControls.Add(control); control.enabled = false; }
            var body = actor.GetComponent<Rigidbody2D>(); if (body != null) body.linearVelocity = Vector2.zero;
        }

        private IEnumerator TravelWithinScene(PlayerInteractor actor, CheckpointInteractable target)
        {
            bool faded = false;
            ScreenFader.Instance.FadeOut(.25f,()=>faded = true);
            while (!faded) yield return null;
            if (actor != null && target != null)
            {
                var r = RecordFor(target);
                PlacePlayer(actor.gameObject,r);
                NormalEnemyRespawns.ResetNormals(target.gameObject.scene);
                Save(r.scenePath,r.checkpointId,r.Position,CheckpointName(target),true);
            }
            ScreenFader.Instance.FadeIn(.25f,FinishTravel);
        }

        private void FinishTravel()
        {
            foreach (var control in travelControls)
                if (control != null && !control.GetComponent<CharacterHealth>().isDead) control.enabled = true;
            travelControls.Clear();
            Time.timeScale = travelTimeScale;
            traveling = false;
            travelPotionCapacity = 0;
        }

        private static string CheckpointName(CheckpointInteractable checkpoint)
        {
            var menu = checkpoint.GetComponent<CheckpointMenu>(); return menu != null ? menu.checkpointName : checkpoint.checkpointId;
        }
        private static Record RecordFor(CheckpointInteractable checkpoint)
        {
            Vector3 position = checkpoint.RespawnPosition;
            return new Record { scenePath = checkpoint.gameObject.scene.path, checkpointId = checkpoint.checkpointId, x = position.x, y = position.y, z = position.z };
        }
        public static bool IsUnlocked(string scenePath, string id)
        {
            return ReadUnlocked().entries.Exists(e => e != null && e.scenePath == scenePath && e.checkpointId == id);
        }
        public static void ClearSaved()
        {
            if (instance != null) { instance.pending = null; instance.fallback = null; }
            PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.DeleteKey(UnlockSaveKey);
            PlayerPrefs.Save(); SavedChanged?.Invoke();
        }
        private void OnDied(CharacterHealth health)
        {
            if (!health.CompareTag("Player") || respawning || traveling) return;
            pending = ReadSaved() ?? fallback;
            if (!Valid(pending)) { Debug.LogError("No loadable respawn scene configured."); return; }
            StartCoroutine(Reload(deathDelay));
        }
        private IEnumerator Reload(float delay)
        {
            respawning = true;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
            Time.timeScale = 1f;
            SceneSpawnPoint.Prepare("", null); // Prevent a stale doorway spawn overriding the checkpoint.
            ScreenFader.Instance.TransitionToScene(pending.scenePath, 0.3f);
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { PlaceOrBind(scene); }
        public static bool ResumeSavedGame()
        {
            var saved = ReadSaved();
            if (instance == null || saved == null || SceneUtility.GetBuildIndexByScenePath(saved.scenePath) < 0 || IsTraveling || IsRespawning) return false;
            instance.pending = saved;
            Time.timeScale = 1;
            ScreenFader.Instance.TransitionToScene(saved.scenePath, .45f);
            return true;
        }
        private void PlaceOrBind(Scene scene)
        {
            GameObject player = null;
            foreach (var root in scene.GetRootGameObjects()) foreach (var t in root.GetComponentsInChildren<Transform>())
                if (t.CompareTag("Player")) { player = t.gameObject; break; }
            if (player == null) return;
            if (player.GetComponent<PlayerInteractor>() == null) player.AddComponent<PlayerInteractor>();
            if (fallback == null) fallback = new Record { scenePath = scene.path, checkpointId = "initial", x = player.transform.position.x, y = player.transform.position.y, z = player.transform.position.z };
            if (pending == null || pending.scenePath != scene.path) return;
            if (traveling)
            {
                HoldTravelControls(player);
                var target = FindCheckpoint(scene,pending.checkpointId);
                if (target == null)
                {
                    player.GetComponent<PlayerInteractor>().ShowNotice("Tượng đích không còn tồn tại trong khu vực này.");
                    pending = null;
                    return;
                }
                pending = RecordFor(target);
                Save(pending.scenePath,pending.checkpointId,pending.Position,CheckpointName(target),true);
            }
            PlacePlayer(player,pending);
            pending = null; respawning = false;
        }
        private void PlacePlayer(GameObject player, Record record)
        {
            Vector3 delta = record.Position - player.transform.position;
            player.transform.position = record.Position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) { body.position = record.Position; body.linearVelocity = Vector2.zero; body.angularVelocity = 0f; }
            Physics2D.SyncTransforms();
            foreach (var camera in FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsSortMode.None))
                camera.OnTargetObjectWarped(player.transform, delta);
            var stats = player.GetComponent<CharacterStats>();
            stats.currentHP = stats.maxHP; stats.currentStamina = stats.maxStamina;
            var debuffs = player.GetComponent<CharacterDebuffs>();
            if (debuffs != null) debuffs.ClearAll();
            var potion = player.GetComponent<PlayerHealthPotion>();
            if (potion != null)
            {
                if (traveling && travelPotionCapacity > 0) potion.capacity = travelPotionCapacity;
                potion.ResetForRest();
            }
            if (traveling)
            {
                stats.ResetPosture();
                player.GetComponent<CharacterBuffs>()?.ClearAll();
                player.GetComponent<SkillDamageManager>()?.ResetCooldowns();
                player.GetComponent<PlayerDash>()?.ResetCooldown();
            }
            var invulnerability = player.GetComponent<CharacterInvulnerability>();
            if (invulnerability != null) invulnerability.GrantInvulnerability(2f);
        }
    }
}
