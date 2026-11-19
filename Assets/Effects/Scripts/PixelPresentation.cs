using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>Holds rendered poses at the shared cadence; restores authored values before the next gameplay update.</summary>
[DefaultExecutionOrder(-32000)]
public sealed class PixelPresentation : MonoBehaviour
{
    private sealed class SpritePose
    {
        public SpriteRenderer target; public Animator animator; public Sprite raw,shown; public Color rawColor,shownColor;
        public void Restore(){if(target!=null){target.sprite=raw;target.color=rawColor;}}
        public void Capture(bool tick){if(target==null)return;raw=target.sprite;rawColor=target.color;if(tick){shown=raw;shownColor=rawColor;}else{target.sprite=shown;target.color=shownColor;}}
    }
    private static PixelPresentation instance;
    private static readonly HashSet<SpriteRenderer> unscaledSprites=new HashSet<SpriteRenderer>();
    public static void SetUnscaled(SpriteRenderer target,bool enabled)
    {
        if(target==null)return;
        if(enabled)unscaledSprites.Add(target);else unscaledSprites.Remove(target);
    }
    // Persistent state changes must also update the held pose, otherwise Update restores the old color.
    public static void SetPersistentColor(SpriteRenderer target,Color color)
    {
        if(target==null)return;
        target.color=color;
        SpritePose pose;
        if(instance!=null && instance.sprites.TryGetValue(target,out pose))
        {pose.rawColor=color;pose.shownColor=color;}
    }
    private readonly Dictionary<SpriteRenderer,SpritePose> sprites=new Dictionary<SpriteRenderer,SpritePose>();
    private float nextDiscovery;
    private int worldFrame=-1,uiFrame=-1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic(){instance=null;unscaledSprites.Clear();}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if(instance!=null)return;
        var go=new GameObject("Pixel presentation clock");DontDestroyOnLoad(go);instance=go.AddComponent<PixelPresentation>();go.AddComponent<PixelPresentationLate>().owner=instance;
        SceneManager.sceneLoaded+=instance.OnSceneLoaded;
        instance.Discover();
    }
    private void OnSceneLoaded(Scene scene,LoadSceneMode mode){Discover();}
    private void Update()
    {
        foreach(var pose in sprites.Values)pose.Restore();
        if(Time.unscaledTime>=nextDiscovery){Discover();nextDiscovery=Time.unscaledTime+.25f;}
        Shader.SetGlobalFloat("_PixelAnimationFPS",PixelAnimationSettings.FPS);
    }
    private void Discover()
    {
        Prune(sprites);
        unscaledSprites.RemoveWhere(target=>target==null);
        foreach(var target in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(!sprites.ContainsKey(target))sprites[target]=new SpritePose{target=target,animator=target.GetComponentInParent<Animator>(),raw=target.sprite,shown=target.sprite,rawColor=target.color,shownColor=target.color};
    }
    private static void Prune<T,V>(Dictionary<T,V> poses) where T:UnityEngine.Object
    {
        var dead=new List<T>();foreach(var key in poses.Keys)if(key==null)dead.Add(key);foreach(var key in dead)poses.Remove(key);
    }
    public void Present()
    {
        int nextWorld=Mathf.FloorToInt(Time.time*PixelAnimationSettings.FPS),nextUI=Mathf.FloorToInt(Time.unscaledTime*PixelAnimationSettings.FPS);
        bool worldTick=nextWorld!=worldFrame,uiTick=nextUI!=uiFrame;worldFrame=nextWorld;uiFrame=nextUI;
        foreach(var pose in sprites.Values)pose.Capture(unscaledSprites.Contains(pose.target) || pose.animator!=null && pose.animator.updateMode==AnimatorUpdateMode.UnscaledTime?uiTick:worldTick);
    }
    private void OnDestroy(){SceneManager.sceneLoaded-=OnSceneLoaded;}
}
