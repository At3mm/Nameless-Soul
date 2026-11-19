using UnityEngine;
using UnityEngine.SceneManagement;

namespace World
{
    /// <summary>Automatic Elite/Boss seed rewards. Retains old save IDs for compatibility.</summary>
    public static class EliteLifeSeedDrops
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            for (int i = 0; i < SceneManager.sceneCount; i++) Restore(SceneManager.GetSceneAt(i));
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Restore(scene); }
        public static void OnDeath(CharacterHealth health)
        {
            if (!Application.isPlaying || health == null) return;
            if(GameEconomy.RecordEliteSeedDrop(health.GetComponent<EnemyClassification>())!=null)Notify(1);
        }
        public static void Restore(Scene scene)
        {
            if (!Application.isPlaying || !scene.IsValid() || !scene.isLoaded) return;
            int received=0;
            // Recover uncollected drops from older saves, including encounters on other maps.
            foreach (var record in GameEconomy.EliteSeedDrops)
                if(GameEconomy.CollectLifeSeed(record.scenePath+"."+record.pickupId,1))received++;
            if(received>0)Notify(received);
            foreach(var root in scene.GetRootGameObjects())
                foreach(var pickup in root.GetComponentsInChildren<LifeSeedPickup>(true))
                    if(GameEconomy.HasCollectedLifeSeed(scene.path+"."+pickup.pickupId))pickup.gameObject.SetActive(false);
        }
        private static void Notify(int count)
        {
            Object.FindFirstObjectByType<PlayerInteractor>()?.ShowNotice("Đã nhận "+count+" "+LifeSeedItem.Name+".");
        }
    }
}
