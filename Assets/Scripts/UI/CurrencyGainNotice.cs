using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Noninteractive right-side receipt. Rapid gains accumulate; menu time does not consume its lifetime.</summary>
public sealed class CurrencyGainNotice : MonoBehaviour
{
    private static CurrencyGainNotice instance;
    private CurrencyGainNoticeSettings style;
    private CanvasGroup group;
    private RectTransform panel;
    private GameObject goldRow,experienceRow;
    private Text goldText,experienceText;
    private PlayerController player;
    private CharacterHealth health;
    private long gold,experience;
    private float elapsed;
    private bool pending;
    public long DisplayedGold=>gold;
    public long DisplayedExperience=>experience;
    public bool HasPendingNotice=>pending;
    private float Duration=>style.enterDuration+style.holdDuration+style.exitDuration;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if(instance!=null)return;
        var go=new GameObject("Currency Gain Notice");DontDestroyOnLoad(go);instance=go.AddComponent<CurrencyGainNotice>();
    }
    private void OnEnable(){GameEconomy.CurrencyReceived+=OnReceived;SceneManager.sceneLoaded+=OnSceneLoaded;}
    private void OnDisable(){GameEconomy.CurrencyReceived-=OnReceived;SceneManager.sceneLoaded-=OnSceneLoaded;if(group!=null)group.alpha=0;}
    private void OnDestroy(){if(instance==this)instance=null;}
    private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        player=null;health=null;
        if(mode==LoadSceneMode.Single){pending=false;gold=experience=0;if(group!=null)group.alpha=0;}
    }
    private bool HasPlayer()
    {
        if(player==null){player=FindFirstObjectByType<PlayerController>();health=player!=null?player.GetComponent<CharacterHealth>():null;}
        return player!=null&&player.gameObject.activeInHierarchy&&health!=null&&!health.isDead;
    }
    private void OnReceived(int coins,int exp)
    {
        if(!Application.isPlaying||coins<=0&&exp<=0||!HasPlayer())return;
        if(panel==null)Build();
        if(!pending){gold=experience=0;}
        gold+=Mathf.Max(0,coins);experience+=Mathf.Max(0,exp);elapsed=0;pending=true;
        goldRow.SetActive(gold>0);experienceRow.SetActive(experience>0);
        var format=CultureInfo.GetCultureInfo("vi-VN");
        goldText.text="Vàng     +"+gold.ToString("N0",format);
        experienceText.text="EXP      +"+experience.ToString("N0",format);
        panel.sizeDelta=new Vector2(286,gold>0&&experience>0?126:90);
        ((RectTransform)goldRow.transform).anchoredPosition=new Vector2(16,-44);
        ((RectTransform)experienceRow.transform).anchoredPosition=new Vector2(16,gold>0?-80:-44);
    }
    private void Update()
    {
        if(!pending||group==null)return;
        bool blocked=Time.timeScale<=0||SettingsMenuToggle.AnyOpen||ControlBindingsMenu.IsOpen||World.CheckpointMenu.AnyOpen||World.NpcInteractable.BlocksPauseMenu||World.InGameCutscene.AnyPlaying||!HasPlayer();
        if(blocked){group.alpha=0;return;}
        elapsed+=Time.unscaledDeltaTime;
        if(elapsed>=Duration){pending=false;gold=experience=0;group.alpha=0;return;}
        float enter=PixelAnimationSettings.Progress(elapsed,style.enterDuration);
        float exit=PixelAnimationSettings.Progress(elapsed-style.enterDuration-style.holdDuration,style.exitDuration);
        group.alpha=enter*(1-exit);
        panel.anchoredPosition=style.position+new Vector2(Mathf.Round((1-enter)*48+exit*24),0);
    }
    private void Build()
    {
        style=Resources.Load<CurrencyGainNoticeSettings>("DefaultCurrencyGainNotice");
        if(style==null){style=ScriptableObject.CreateInstance<CurrencyGainNoticeSettings>();style.hideFlags=HideFlags.HideAndDontSave;}
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=160;canvas.pixelPerfect=true;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var container=new GameObject("Receipt",typeof(RectTransform));
        container.transform.SetParent(transform,false);
        panel=(RectTransform)container.transform;
        panel.sizeDelta=new Vector2(286,126);
        panel.anchorMin=panel.anchorMax=new Vector2(1,.5f);panel.pivot=new Vector2(1,.5f);panel.anchoredPosition=style.position;
        group=panel.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.interactable=false;group.blocksRaycasts=false;
        goldRow=Row("Gold",style.goldIcon,style.goldColor,out goldText);
        experienceRow=Row("Experience",style.experienceIcon,style.experienceColor,out experienceText);
    }
    private GameObject Row(string name,Sprite icon,Color color,out Text text)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(panel,false);Place((RectTransform)go.transform,Vector2.zero,new Vector2(256,34));
        if(icon!=null){var image=Box(go.transform,"Icon",new Vector2(0,-3),new Vector2(26,26),Color.white);image.sprite=icon;image.preserveAspect=true;}
        else Box(go.transform,"Pixel",new Vector2(8,-12),new Vector2(8,8),color);
        text=Label(go.transform,"Amount","",new Vector2(36,0),new Vector2(218,34),27,color);
        text.horizontalOverflow=HorizontalWrapMode.Wrap;text.resizeTextForBestFit=true;text.resizeTextMinSize=16;text.resizeTextMaxSize=27;return go;
    }
    private static void Place(RectTransform rect,Vector2 pos,Vector2 size){rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;}
    private static Image Box(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),pos,size);var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    private static Text Label(Transform parent,string name,string value,Vector2 pos,Vector2 size,int fontSize,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),pos,size);
        var text=go.GetComponent<Text>();text.font=GameFont.Current;text.fontSize=fontSize;text.text=value;text.color=color;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;
        text.horizontalOverflow=HorizontalWrapMode.Overflow;return text;
    }
}
