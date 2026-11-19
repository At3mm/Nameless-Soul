using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace World
{
    /// <summary>Keeps initial scene-instance snapshots even after dead enemies are destroyed.</summary>
    public sealed class NormalEnemyRespawns : MonoBehaviour
    {
        private sealed class Spawn
        {
            public EnemyClassification current;
            public GameObject template;
            public Transform parent;
            public Vector3 position, localScale, worldScale;
            public Quaternion rotation;
            public string name;
            public EnemyClassification.EnemyType originalType;
            public string defeatId;
        }

        private static readonly Dictionary<int, NormalEnemyRespawns> scenes = new Dictionary<int, NormalEnemyRespawns>();
        private readonly List<Spawn> spawns = new List<Spawn>();
        private Transform templates;
        private int sceneHandle;
        private bool restoring;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            scenes.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) CaptureScene(SceneManager.GetSceneAt(i));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { CaptureScene(scene); }

        private static void CaptureScene(Scene scene)
        {
            var service = Ensure(scene);
            if (service == null) return;
            // Materialize before creating inactive snapshots under the service root.
            var enemies = new List<EnemyClassification>();
            foreach (var root in scene.GetRootGameObjects()) enemies.AddRange(root.GetComponentsInChildren<EnemyClassification>(true));
            foreach (var enemy in enemies) service.Capture(enemy);
        }

        private static NormalEnemyRespawns Ensure(Scene scene)
        {
            if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded) return null;
            if (scenes.TryGetValue(scene.handle, out var existing) && existing != null) return existing;
            var root = new GameObject("Normal Enemy Respawns");
            SceneManager.MoveGameObjectToScene(root, scene);
            var service = root.AddComponent<NormalEnemyRespawns>();
            service.sceneHandle = scene.handle;
            var storage = new GameObject("Initial Enemy Templates");
            storage.SetActive(false);
            storage.transform.SetParent(root.transform, false);
            service.templates = storage.transform;
            scenes[scene.handle] = service;
            return service;
        }

        // Called from Awake, before AI, runtime bars and combat can modify an instance.
        public static void Register(EnemyClassification enemy)
        {
            if (enemy == null) return;
            Ensure(enemy.gameObject.scene)?.Capture(enemy);
        }

        private void Capture(EnemyClassification enemy)
        {
            if (restoring || enemy == null || enemy.GetComponent<NecromancerSummonedMinion>() != null || enemy.transform.IsChildOf(transform)) return;
            var health = enemy.GetComponent<CharacterHealth>();
            bool savedDefeat=enemy.enemyType!=EnemyClassification.EnemyType.Normal && EnemyDefeatState.WasDefeated(enemy.DefeatId);
            if (health != null && health.isDead && !savedDefeat) return;
            foreach (var spawn in spawns) if (spawn.current == enemy) return;
            var source = enemy.transform;
            var template = Instantiate(enemy.gameObject, templates, false);
            template.name = enemy.name + " (initial snapshot)";
            if(savedDefeat)
            {
                // Saved defeats can hide an actor before the scene-load scan.
                // The inactive storage parent keeps this pristine copy dormant.
                template.GetComponent<CharacterHealth>().isDead=false;
                var initialStats=template.GetComponent<CharacterStats>();initialStats.currentHP=initialStats.maxHP;
                template.SetActive(true);
            }
            spawns.Add(new Spawn
            {
                current = enemy, template = template, parent = source.parent,
                position = source.position, rotation = source.rotation,
                localScale = source.localScale, worldScale = source.lossyScale, name = enemy.name,
                originalType=enemy.enemyType,defeatId=enemy.DefeatId
            });
        }

        /// <summary>Resets only normal enemies belonging to the checkpoint's scene.</summary>
        public static int ResetNormals(Scene scene)
        {
            var service = Ensure(scene);
            return service != null ? service.Restore() : 0;
        }

#if UNITY_EDITOR
        public static int ResetAllForTesting(Scene scene)
        {
            if(!Application.isPlaying)return 0;
            var service=Ensure(scene);if(service==null)return 0;
            var ids=new List<string>();foreach(var spawn in service.spawns)ids.Add(spawn.defeatId);
            EnemyDefeatState.ForgetForTesting(ids);
            return service.Restore(true);
        }
#endif
        private int Restore(bool includeSpecial=false)
        {
            if (restoring) return 0;
            restoring = true;
            int restored = 0;
            try
            {
                foreach (var spawn in spawns)
                {
                    if (spawn.template == null) continue;
                    if(!includeSpecial && spawn.originalType!=EnemyClassification.EnemyType.Normal)continue;
                    // A living instance promoted to Elite/Boss must also be left alone.
                    if (!includeSpecial && spawn.current != null && spawn.current.enemyType != EnemyClassification.EnemyType.Normal) continue;
                    if (spawn.current != null)
                    {
                        spawn.current.gameObject.SetActive(false);
                        Destroy(spawn.current.gameObject);
                    }
                    // Keep inactive until position, scene and ownership have been restored.
                    var instance = Instantiate(spawn.template, templates, false);
                    bool active = instance.activeSelf;
                    instance.SetActive(false);
                    instance.name = spawn.name;
                    instance.transform.SetParent(spawn.parent, false);
                    if (instance.scene != gameObject.scene) SceneManager.MoveGameObjectToScene(instance, gameObject.scene);
                    instance.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
                    instance.transform.localScale = spawn.parent != null ? spawn.localScale : spawn.worldScale;
                    spawn.current = instance.GetComponent<EnemyClassification>();
                    instance.SetActive(active);
                    restored++;
                }
                Physics2D.SyncTransforms();
            }
            finally { restoring = false; }
            return restored;
        }

        private void OnDestroy()
        {
            if (scenes.TryGetValue(sceneHandle, out var service) && service == this) scenes.Remove(sceneHandle);
        }
    }
}
