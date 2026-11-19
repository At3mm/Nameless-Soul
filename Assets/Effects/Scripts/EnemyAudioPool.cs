using UnityEngine;

/// <summary>Eight bounded creature voices, attenuated by distance and paused with gameplay.</summary>
public sealed class EnemyAudioPool : MonoBehaviour
{
    private static EnemyAudioPool instance;
    private static CombatAudioSettings settings;
    private readonly AudioSource[] voices=new AudioSource[8];
    private readonly float[] started=new float[8];
    private bool paused;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){instance=null;settings=null;}
    private void Awake()
    {
        for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;}
    }
    public static bool Play(AudioClip clip,Vector3 position,float volume,float pitch,bool critical)
    {
        if(!Application.isPlaying||Time.timeScale<=0||clip==null)return false;
        if(settings==null)settings=Resources.Load<CombatAudioSettings>("DefaultCombatAudio");
        if(settings==null)return false;
        float gain=settings.volume*settings.enemyVolume*Mathf.Clamp01(volume);
        var camera=Camera.main;
        if(camera!=null)gain*=1-Mathf.Clamp01((Vector2.Distance(position,camera.transform.position)-5)/Mathf.Max(1,settings.audibleDistance-5));
        if(gain<=0)return false;
        if(instance==null){var go=new GameObject("Enemy Creature Audio");instance=go.AddComponent<EnemyAudioPool>();DontDestroyOnLoad(go);}
        int index=-1,oldest=0;
        for(int i=0;i<instance.voices.Length;i++)
        {
            if(!instance.voices[i].isPlaying){index=i;break;}
            if(instance.started[i]<instance.started[oldest])oldest=i;
        }
        if(index<0){if(!critical)return false;index=oldest;}
        var voice=instance.voices[index];voice.Stop();voice.clip=clip;voice.volume=gain;voice.pitch=Mathf.Clamp(pitch,.5f,1.5f);voice.Play();instance.started[index]=Time.time;return true;
    }
    private void Update()
    {
        bool pause=Time.timeScale<=0;if(pause==paused)return;paused=pause;
        foreach(var source in voices){if(pause)source.Pause();else source.UnPause();}
    }
}
