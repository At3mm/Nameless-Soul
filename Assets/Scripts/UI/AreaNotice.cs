using UnityEngine;
using UnityEngine.UI;
using World;

/// <summary>Nonblocking pixel location title. A newer location replaces the old title.</summary>
public sealed class AreaNotice : MonoBehaviour
{
    private static AreaNotice instance;
    private CanvasGroup group;
    private RectTransform panel;
    private Text title, subtitle;
    private AudioSource voice;
    private AudioClip defaultChime;
    private float elapsed;
    public const float Duration = 3.2f;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() { instance = null; }
    private static bool IsBlocked => Time.timeScale<=0 || CheckpointMenu.AnyOpen || SettingsMenuToggle.AnyOpen || NpcInteractable.BlocksPauseMenu || InGameCutscene.AnyPlaying;
    public static void Dismiss()
    {
        if(instance==null)return;
        instance.elapsed=Duration;
        if(instance.group!=null)instance.group.alpha=0;
        if(instance.panel!=null)instance.panel.gameObject.SetActive(false);
        if(instance.voice!=null)instance.voice.Stop();
    }
    public static void Show(AreaDefinition area)
    {
        if (!Application.isPlaying || area == null) return;
        if(IsBlocked){Dismiss();return;}
        if (instance == null)
        {
            var go = new GameObject("Area Name Notice"); DontDestroyOnLoad(go);
            instance = go.AddComponent<AreaNotice>(); instance.Build();
        }
        instance.title.text = area.displayName; instance.subtitle.text = area.subtitle;
        instance.elapsed = 0; instance.group.alpha = 0; instance.panel.gameObject.SetActive(true);
        instance.voice.Stop(); instance.voice.clip = area.entrySound != null ? area.entrySound : instance.defaultChime;
        var audioSettings = Resources.Load<MenuAudioSettings>("DefaultMenuAudio");
        instance.voice.volume = .3f * (audioSettings != null ? audioSettings.volume : 1f);
        instance.voice.Play();
    }
    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 110; canvas.pixelPerfect = true;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
        group = gameObject.AddComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
        var root = new GameObject("Location banner",typeof(RectTransform)); root.transform.SetParent(transform,false);
        panel = root.GetComponent<RectTransform>(); panel.sizeDelta = new Vector2(840,155); panel.anchoredPosition = new Vector2(0,75);
        var style = Resources.Load<BossVictoryStyle>("DefaultBossVictory");
        var font = style != null && style.font != null ? style.font : GameFont.Current;
        for(int i=0;i<9;i++) Image("Ink "+i,new Vector2(0,(i-4)*14),new Vector2(800-Mathf.Abs(i-4)*38,14),new Color(.055f,.045f,.03f,.65f));
        Image("Upper rule",new Vector2(0,65),new Vector2(570,2),new Color(.76f,.65f,.43f,.85f));
        Image("Lower rule",new Vector2(0,-65),new Vector2(570,2),new Color(.76f,.65f,.43f,.85f));
        title = Label("Area title",font,54,new Vector2(0,8),new Vector2(780,70));
        subtitle = Label("Area subtitle",font,24,new Vector2(0,-37),new Vector2(760,30));
        voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0; voice.volume = .3f;
        const int rate=22050; var samples=new float[rate];
        for(int i=0;i<samples.Length;i++) { float t=(float)i/rate; samples[i]=(.22f*Mathf.Sin(2*Mathf.PI*220*t)+.10f*Mathf.Sin(2*Mathf.PI*330*t))*Mathf.Exp(-5*t)*Mathf.Clamp01(t*80)*Mathf.Clamp01((1-t)*20); }
        defaultChime = AudioClip.Create("Original area bell",rate,1,rate,false); defaultChime.SetData(samples,0);
    }
    private Image Image(string name,Vector2 pos,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(panel,false);
        var rect=go.GetComponent<RectTransform>(); rect.anchoredPosition=pos; rect.sizeDelta=size;
        var image=go.GetComponent<Image>(); image.color=color; image.raycastTarget=false; return image;
    }
    private Text Label(string name,Font font,int size,Vector2 pos,Vector2 dimensions)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Shadow)); go.transform.SetParent(panel,false);
        var rect=go.GetComponent<RectTransform>(); rect.anchoredPosition=pos; rect.sizeDelta=dimensions;
        var text=go.GetComponent<Text>(); text.font=font; text.fontSize=size; text.alignment=TextAnchor.MiddleCenter; text.color=new Color(1,.93f,.77f); text.raycastTarget=false;
        text.resizeTextForBestFit=true; text.resizeTextMinSize=20; text.resizeTextMaxSize=size; return text;
    }
    private void Update()
    {
        if(group==null || elapsed>=Duration)return;
        if(IsBlocked){Dismiss();return;}
        elapsed+=Time.unscaledDeltaTime;
        if(elapsed>=Duration){Dismiss();return;}
        float sampled=PixelAnimationSettings.Sample(elapsed);
        group.alpha=sampled<.5f?Mathf.SmoothStep(0,1,sampled/.5f):sampled>2.5f?1-Mathf.SmoothStep(0,1,(sampled-2.5f)/.7f):1;
        panel.anchoredPosition=new Vector2(0,75+Mathf.Round((1-Mathf.Clamp01(sampled/.5f))*8));
    }
    private void OnDestroy() { if(defaultChime!=null)Destroy(defaultChime); }
}
