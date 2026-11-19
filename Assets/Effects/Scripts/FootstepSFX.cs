using UnityEngine;

/// <summary>Separate bounded footstep voices; footsteps never occupy combat voices.</summary>
public sealed class FootstepSFX : MonoBehaviour
{
    static FootstepSFX instance;
    static FootstepAudioSettings settings;
    readonly AudioSource[] voices=new AudioSource[12];
    bool paused;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){instance=null;settings=null;}
    void Awake()
    {
        for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;}
    }
    public static bool Play(Vector3 position,FootstepAudioSettings.Surface surface,bool running,bool enemy,int variation)
    {
        if(!Application.isPlaying || Time.timeScale<=0)return false;
        if(settings==null)settings=Resources.Load<FootstepAudioSettings>("DefaultFootstepAudio");
        if(settings==null)return false;
        float gain=1;var camera=Camera.main;
        if(camera!=null)gain=1-Mathf.Clamp01(((Vector2)(position-camera.transform.position)).magnitude/settings.audibleDistance);
        if(gain<=0)return false;
        var clip=settings.Find(surface,running,variation);if(clip==null)return false;
        if(instance==null){var go=new GameObject("Footstep SFX");instance=go.AddComponent<FootstepSFX>();DontDestroyOnLoad(go);}
        int active=0;foreach(var voice in instance.voices)if(voice.isPlaying)active++;
        if(enemy && active>=6)return false;
        float volume=settings.volume*(enemy?settings.enemyVolume:1)*gain;
        float pitch=(running?1.1f:1f)+(variation%3-1)*.025f;
        if(surface==FootstepAudioSettings.Surface.Poison)
        {
            float blend=Mathf.Clamp01(settings.poisonWaterBlend);
            bool played=instance.Emit(clip,volume*(1-blend),pitch);
            var splash=settings.poisonSplash!=null && settings.poisonSplash.Length>0?
                settings.poisonSplash[variation%settings.poisonSplash.Length]:settings.Find(FootstepAudioSettings.Surface.Water,running,variation);
            if(splash!=null)played=instance.Emit(splash,volume*blend,pitch)||played;
            return played;
        }
        return instance.Emit(clip,volume,pitch);
    }
    bool Emit(AudioClip clip,float volume,float pitch)
    {
        if(volume<=0)return false;
        foreach(var voice in voices)
        {
            if(voice.isPlaying)continue;
            voice.clip=clip;voice.volume=volume;voice.pitch=pitch;voice.Play();return true;
        }
        return false;
    }
    void Update()
    {
        bool pause=Time.timeScale<=0;if(paused==pause)return;paused=pause;
        foreach(var voice in voices){if(pause)voice.Pause();else voice.UnPause();}
    }
}
