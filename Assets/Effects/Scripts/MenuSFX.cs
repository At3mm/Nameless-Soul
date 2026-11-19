using UnityEngine;

/// <summary>Menu audio uses unscaled cooldowns and independent voices while gameplay is paused.</summary>
public sealed class MenuSFX : MonoBehaviour
{
    public enum Kind { Open, Close, Move, Confirm, Back, Error, Page, Purchase }
    static MenuSFX instance;
    static MenuAudioSettings settings;
    static bool shuttingDown;
    readonly AudioSource[] voices = new AudioSource[6];
    readonly float[] last = new float[8];
    float suppressMoveUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; settings = null; shuttingDown=false; }

    public static void Play(Kind kind)
    {
        if (!Application.isPlaying || shuttingDown || (int)kind < 0 || (int)kind >= 8) return;
        if (settings == null) settings = Resources.Load<MenuAudioSettings>("DefaultMenuAudio");
        if (settings == null || settings.volume <= 0) return;
        if (instance == null)
        {
            var go = new GameObject("Menu SFX");
            instance = go.AddComponent<MenuSFX>();
            DontDestroyOnLoad(go);
        }
        instance.Emit(kind);
    }

    void Awake()
    {
        for (int i=0;i<last.Length;i++) last[i]=-100;
        for (int i=0;i<voices.Length;i++)
        {
            voices[i]=gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake=false;
            voices[i].spatialBlend=0;
            voices[i].ignoreListenerPause=true;
        }
    }
    void OnApplicationQuit(){shuttingDown=true;}

    void Emit(Kind kind)
    {
        float now=Time.unscaledTime;
        if (now-last[(int)kind]<.07f || (kind==Kind.Move && now<suppressMoveUntil)) return;
        AudioClip[] clips={settings.open,settings.close,settings.move,settings.confirm,settings.back,settings.error,settings.page,settings.purchase};
        var clip=clips[(int)kind];
        if(clip==null)return;
        foreach(var voice in voices)
        {
            if(voice.isPlaying)continue;
            voice.clip=clip;
            voice.pitch=kind==Kind.Error?.8f:kind==Kind.Back?.9f:1f;
            voice.volume=settings.volume*(kind==Kind.Move?.35f:.65f);
            voice.Play();last[(int)kind]=now;
            if(kind==Kind.Open || kind==Kind.Close)suppressMoveUntil=now+.12f;
            return;
        }
    }
}
