using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName="Game/Default Liquid Surfaces")]
public sealed class LiquidSurfaceDefaults : ScriptableObject
{
    public Material waterMaterial;
    public Material poisonMaterial;
    private static LiquidSurfaceDefaults cached;
    public static LiquidSurfaceDefaults Current
    {
        get { if(cached==null) cached=Resources.Load<LiquidSurfaceDefaults>("DefaultLiquidSurfaces"); return cached; }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { cached=null; SceneManager.sceneLoaded-=OnSceneLoaded; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded+=OnSceneLoaded;
        for(int i=0;i<SceneManager.sceneCount;i++) ConfigureScene(SceneManager.GetSceneAt(i));
    }
    private static void OnSceneLoaded(Scene scene,LoadSceneMode mode) { ConfigureScene(scene); }
    private static void ConfigureScene(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())
            foreach(var map in root.GetComponentsInChildren<Tilemap>(true))
                if(map.GetComponent<PoisonZone>()!=null || map.GetComponent<WaterSlowZone>()!=null ||
                    map.name=="Poison" || map.name=="Water" || map.name=="Lakes" || map.name=="Map_Boundaries")
                    LiquidSurfaceEffect.Ensure(map.gameObject);
    }
}
