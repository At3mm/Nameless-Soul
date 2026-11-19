using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persistent, streamed music with separate exploration and boss-combat contexts.</summary>
public sealed class GameMusicController : MonoBehaviour
{
    public enum Context { Menu, Exploration, Boss }
    public static GameMusicController Instance { get; private set; }
    public Context CurrentContext { get; private set; }
    public AudioClip CurrentTrack { get; private set; }
    public GameMusicSettings Settings { get; private set; }
    private readonly AudioSource[] voices=new AudioSource[2];
    private readonly float[] weights=new float[2];
    private PlayerController player;
    private EnemyClassification currentBoss;
    private float nextCheck,engagedUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic(){Instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if(Instance!=null)return;
        var settings=Resources.Load<GameMusicSettings>("DefaultGameMusic");
        if(settings==null)return;
        var go=new GameObject("Game Music");DontDestroyOnLoad(go);
        Instance=go.AddComponent<GameMusicController>();Instance.Settings=settings;
        for(int i=0;i<2;i++)
        {
            var voice=go.AddComponent<AudioSource>();voice.playOnAwake=false;voice.loop=true;voice.spatialBlend=0;voice.volume=0;voice.priority=192;
            Instance.voices[i]=voice;
        }
        SceneManager.sceneLoaded+=Instance.OnSceneLoaded;
        Instance.Evaluate();
    }
    private void OnSceneLoaded(Scene scene,LoadSceneMode mode){player=null;currentBoss=null;engagedUntil=0;nextCheck=0;}
    private void Update()
    {
        if(Settings==null)return;
        if(Time.unscaledTime>=nextCheck){nextCheck=Time.unscaledTime+.15f;Evaluate();}
        float level=Settings.volume*(Time.timeScale<=0?Settings.pausedVolume:1);
        float step=Mathf.Max(.001f,Settings.volume)*Time.unscaledDeltaTime/Mathf.Max(.05f,Settings.crossfadeSeconds);
        for(int i=0;i<2;i++)
        {
            voices[i].volume=Mathf.MoveTowards(voices[i].volume,level*weights[i],step);
            if(weights[i]==0&&voices[i].volume<=.0001f&&voices[i].isPlaying)voices[i].Stop();
        }
    }
    public void Evaluate()
    {
        if(Settings==null)return;
        if(SceneManager.GetActiveScene().path==MainMenuController.ScenePath)
        {currentBoss=null;Switch(Context.Menu,Settings.menu!=null?Settings.menu:Settings.exploration);return;}
        if(player==null||!player.gameObject.activeInHierarchy)player=FindFirstObjectByType<PlayerController>();
        var health=player!=null?player.GetComponent<CharacterHealth>():null;
        var boss=health!=null&&!health.isDead?FindEngagedBoss(player.transform):null;
        if(boss!=null){currentBoss=boss;engagedUntil=Time.unscaledTime+Settings.disengageDelay;}
        else if(currentBoss!=null && currentBoss.isActiveAndEnabled && currentBoss.Health!=null && !currentBoss.Health.isDead && health!=null && !health.isDead && Time.unscaledTime<engagedUntil)boss=currentBoss;
        if(boss==null){currentBoss=null;Switch(Context.Exploration,Settings.exploration);return;}
        var custom=boss.GetComponent<BossMusicOverride>();
        Switch(Context.Boss,custom!=null&&custom.combatMusic!=null?custom.combatMusic:Settings.boss);
    }
    private EnemyClassification FindEngagedBoss(Transform target)
    {
        EnemyClassification closest=null;float best=Settings.maximumBossDistance*Settings.maximumBossDistance;
        foreach(var enemy in EnemyClassification.Active)
        {
            if(enemy==null||!enemy.isActiveAndEnabled||enemy.enemyType!=EnemyClassification.EnemyType.Boss||enemy.gameObject.scene!=target.gameObject.scene||enemy.Health==null||enemy.Health.isDead||enemy.AI==null||!enemy.AI.IsInCombatWith(target))continue;
            float distance=((Vector2)(enemy.transform.position-target.position)).sqrMagnitude;
            if(distance>best)continue;
            if(enemy==currentBoss)return enemy;
            best=distance;closest=enemy;
        }
        return closest;
    }
    private void Switch(Context context,AudioClip clip)
    {
        CurrentContext=context;
        if(CurrentTrack==clip)return;
        CurrentTrack=clip;
        weights[0]=weights[1]=0;
        if(clip==null)return;
        for(int i=0;i<2;i++)if(voices[i].clip==clip&&voices[i].isPlaying){weights[i]=1;return;}
        int next=voices[0].volume<=voices[1].volume?0:1;
        voices[next].Stop();voices[next].clip=clip;voices[next].volume=0;voices[next].Play();weights[next]=1;
    }
    private void OnDestroy(){SceneManager.sceneLoaded-=OnSceneLoaded;if(Instance==this)Instance=null;}
}
