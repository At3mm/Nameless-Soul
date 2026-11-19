using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace World
{
    /// <summary>One grave across all maps. Wallet and grave are committed together by GameEconomy.</summary>
    public static class PlayerDeathDrops
    {
        private static readonly HashSet<PlayerDeathDropPickup> live=new HashSet<PlayerDeathDropPickup>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            live.Clear();SceneManager.sceneLoaded-=OnSceneLoaded;GameEconomy.Changed-=Refresh;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize(){SceneManager.sceneLoaded+=OnSceneLoaded;GameEconomy.Changed+=Refresh;Refresh();}
        private static void OnSceneLoaded(Scene scene,LoadSceneMode mode){Refresh();}
        internal static void Register(PlayerDeathDropPickup pickup){live.Add(pickup);}
        internal static void Unregister(PlayerDeathDropPickup pickup){live.Remove(pickup);}
        public static void OnDeath(CharacterHealth player){GameEconomy.RecordPlayerDeathDrop(player);}
        public static void Refresh()
        {
            if(!Application.isPlaying)return;
            var drop=GameEconomy.PendingDeathDrop;bool found=false;
            foreach(var pickup in new List<PlayerDeathDropPickup>(live))
            {
                if(pickup==null){live.Remove(pickup);continue;}
                if(drop==null||pickup.DropId!=drop.id||pickup.gameObject.scene.path!=drop.scenePath||found)
                {pickup.gameObject.SetActive(false);Object.Destroy(pickup.gameObject);}
                else found=true;
            }
            if(drop==null||found)return;
            var scene=SceneManager.GetSceneByPath(drop.scenePath);if(!scene.IsValid()||!scene.isLoaded)return;
            var go=new GameObject("Lost Soul and Gold");go.SetActive(false);SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=drop.position;
            var pickupComponent=go.AddComponent<PlayerDeathDropPickup>();pickupComponent.Configure(drop);go.SetActive(true);
        }
    }
}
