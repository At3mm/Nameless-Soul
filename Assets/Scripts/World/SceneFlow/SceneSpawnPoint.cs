using UnityEngine;
using UnityEngine.SceneManagement;

namespace World
{
    public class SceneSpawnPoint : MonoBehaviour
    {
        public string spawnPointId;
        private static string pendingScene, pendingPoint;
        private static bool pendingInitialGameSpawn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPending()
        {
            SceneManager.sceneLoaded -= PlacePlayer;
            pendingScene = pendingPoint = null;
            pendingInitialGameSpawn = false;
        }

        public static void Prepare(string sceneName, string pointId, bool initialGameSpawn = false)
        {
            ResetPending();
            if (string.IsNullOrWhiteSpace(pointId)) return;
            pendingScene = sceneName;
            pendingPoint = pointId;
            pendingInitialGameSpawn = initialGameSpawn;
            SceneManager.sceneLoaded += PlacePlayer;
        }

        private static void PlacePlayer(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != pendingScene) return;
            string id = pendingPoint;
            bool initialGameSpawn = pendingInitialGameSpawn;
            ResetPending(); // Consume once; direct scene loads retain their default spawn.
            SceneSpawnPoint point = null;
            GameObject player = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var candidate in root.GetComponentsInChildren<SceneSpawnPoint>(true))
                    if (candidate.isActiveAndEnabled && candidate.spawnPointId == id) point = candidate;
                foreach (var candidate in root.GetComponentsInChildren<Transform>())
                    if (candidate.CompareTag("Player")) player = candidate.gameObject;
            }
            if (point == null || player == null)
            {
                Debug.LogWarning("[SceneSpawnPoint] Could not find arrival point '" + id + "' or Player in " + scene.name);
                return;
            }
            player.transform.position = point.transform.position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = point.transform.position;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            Physics2D.SyncTransforms();
            if (initialGameSpawn) CheckpointService.SetInitialSpawn(scene.path, point.transform.position);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, .3f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * .6f);
        }
    }
}
