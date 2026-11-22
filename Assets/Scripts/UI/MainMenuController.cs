using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using World;

public sealed class MainMenuController : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/MainMenu.unity";
    public string gameTitle = "Nameless Soul";
    public Sprite titleLogo;
    public string firstScene = "Assets/Scenes/WastelandScene.unity";
    public string firstSpawnPointId = "NewGameArea1";
    private static readonly string[] SaveKeys = { CheckpointService.SaveKey, CheckpointService.UnlockSaveKey, GameEconomy.SaveKey, EnemyDefeatState.SaveKey, BossItemInventory.SaveKey, QuestService.SaveKey };
    private readonly List<Button> buttons = new List<Button>();
    private CanvasGroup content;
    private GameObject modal;
    private Text status;
    private int selected;
    private bool busy;
    private float intro;
    private Texture2D backdrop;
    private Sprite backdropSprite;
    private readonly Color gold = new Color(.87f,.74f,.48f);
    public static bool HasProgress
    {
        get { foreach(var key in SaveKeys) if(PlayerPrefs.HasKey(key)) return true; return false; }
    }
    public static bool ReturnToMainMenu()
    {
        if (SceneManager.GetActiveScene().path == ScenePath || SceneUtility.GetBuildIndexByScenePath(ScenePath) < 0 ||
            CheckpointService.IsTraveling || CheckpointService.IsRespawning || CheckpointMenu.AnyOpen ||
            InGameCutscene.AnyPlaying || ScreenFader.Instance.IsTransitioning) return false;
        var actor = GameObject.FindGameObjectWithTag("Player");
        var health = actor != null ? actor.GetComponent<CharacterHealth>() : null;
        if (health != null && health.isDead) return false;
        GameEconomy.SaveProgress();
        SceneSpawnPoint.Prepare("", null);
        AreaNotice.Dismiss();
        // Keep the pause menu open until scene unload so gameplay cannot run during the fade.
        ScreenFader.Instance.TransitionToScene(ScenePath, .35f);
        return true;
    }
    private void Start()
    {
        Time.timeScale=1; AudioListener.pause=false;
        AudioListener.volume=PlayerPrefs.GetFloat("Noname.MasterVolume",1);
        Cursor.visible=true; Cursor.lockState=CursorLockMode.None;
        if(EventSystem.current==null)
        {
            var events=new GameObject("Menu EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        Build();
        selected=HasProgress?0:1; Focus(selected);
    }
    private void Build()
    {
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;canvas.pixelPerfect=true;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        DrawBackdrop();
        var panel=new GameObject("Main menu content",typeof(RectTransform),typeof(CanvasGroup));panel.transform.SetParent(transform,false);
        var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        content=panel.GetComponent<CanvasGroup>();content.alpha=0;
        if(titleLogo!=null)
        {
            var logo=Image(panel.transform,"Nameless Soul Logo",new Vector2(0,155),new Vector2(240,160),Color.white);
            logo.sprite=titleLogo;logo.preserveAspect=true;logo.raycastTarget=false;
        }
        else Label(panel.transform,"Title",gameTitle,new Vector2(0,155),new Vector2(240,70),36,gold);
        string[] names={"Tiếp tục","Bắt đầu mới","Thiết lập","Thoát game"};
        Action[] actions={ContinueGame,RequestNewGame,OpenSettings,Quit};
        for(int i=0;i<names.Length;i++)
        {
            int index=i;
            var button=Button(panel.transform,names[i],new Vector2(0,50-i*66),new Vector2(360,52),()=>actions[index]());
            buttons.Add(button);
            var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
        }
        buttons[0].interactable=HasProgress;
        buttons[0].gameObject.AddComponent<CanvasGroup>().alpha=HasProgress?1:.35f;
        status=Label(panel.transform,"Status","",new Vector2(0,-215),new Vector2(470,40),20,new Color(.74f,.74f,.62f));
    }
    private void Update()
    {
        if(content==null || busy || GameControls.Blocked)return;
        intro+=Time.unscaledDeltaTime; content.alpha=Mathf.SmoothStep(0,1,PixelAnimationSettings.Progress(intro,.7f));
        var kb=Keyboard.current;if(kb==null)return;
        if(modal!=null)
        {
            if(kb.escapeKey.wasPressedThisFrame)CloseModal();
            else if(kb.eKey.wasPressedThisFrame)EventSystem.current.currentSelectedGameObject?.GetComponent<Button>()?.onClick.Invoke();
            return;
        }
        int direction=kb.upArrowKey.wasPressedThisFrame||kb.wKey.wasPressedThisFrame?-1:kb.downArrowKey.wasPressedThisFrame||kb.sKey.wasPressedThisFrame?1:0;
        if(direction!=0){int next=selected;do{next=(next+direction+buttons.Count)%buttons.Count;}while(!buttons[next].interactable);Focus(next);MenuSFX.Play(MenuSFX.Kind.Move);}
        if(kb.eKey.wasPressedThisFrame && buttons[selected].interactable)buttons[selected].onClick.Invoke();
        var focused=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
        for(int i=0;i<buttons.Count;i++)if(buttons[i].gameObject==focused)selected=i;
    }
    private void Focus(int index){selected=index;if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);}
    public void ContinueGame()
    {
        if(busy||!HasProgress)return;
        MenuSFX.Play(MenuSFX.Kind.Confirm);
        if(CheckpointService.ResumeSavedGame()){busy=true;content.interactable=false;return;}
        LoadFirstScene(false);
    }
    public void RequestNewGame()
    {
        if(busy)return;
        MenuSFX.Play(MenuSFX.Kind.Confirm);
        if(!HasProgress){LoadFirstScene(true);return;}
        var root=OpenModal("Bắt đầu hành trình mới?",new Vector2(620,280));
        Label(root,"Warning","Tiến trình hiện tại sẽ được thay thế.",new Vector2(0,25),new Vector2(560,45),24,gold);
        var cancel=Button(root,"Quay lại",new Vector2(-140,-65),new Vector2(230,44),CloseModal);
        Button(root,"Bắt đầu mới",new Vector2(140,-65),new Vector2(230,44),()=>LoadFirstScene(true));
        EventSystem.current.SetSelectedGameObject(cancel.gameObject);
    }
    private void LoadFirstScene(bool reset)
    {
        if(busy)return;
        if(SceneUtility.GetBuildIndexByScenePath(firstScene)<0){if(status!=null)status.text="Chưa cấu hình scene bắt đầu.";return;}
        if(reset)
        {
            // Only confirmed new runs reach this path. Keep a recoverable copy of the previous slot.
            var backup=new SaveBackup();foreach(var key in SaveKeys)backup.entries.Add(new SaveEntry{key=key,existed=PlayerPrefs.HasKey(key),value=PlayerPrefs.GetString(key,"")});
            PlayerPrefs.SetString("Noname.PreviousRun.v1",JsonUtility.ToJson(backup));
            GameplayTutorial.BeginNewRun();
            CheckpointService.ClearSaved();EnemyDefeatState.ClearSaved();PlayerPrefs.DeleteKey(QuestService.SaveKey);QuestService.ReloadFromSave();
            PlayerPrefs.DeleteKey(BossItemInventory.SaveKey);BossItemInventory.ReloadFromSave();
            PlayerPrefs.DeleteKey(GameEconomy.SaveKey);GameEconomy.ReloadFromSave();PlayerPrefs.Save();
        }
        busy=true;content.interactable=false;Time.timeScale=1;AudioListener.pause=false;
        SceneSpawnPoint.Prepare(System.IO.Path.GetFileNameWithoutExtension(firstScene),firstSpawnPointId,true);ScreenFader.Instance.TransitionToScene(firstScene,.45f);
    }
    [Serializable] private sealed class SaveEntry { public string key,value;public bool existed; }
    [Serializable] private sealed class SaveBackup { public List<SaveEntry> entries=new List<SaveEntry>(); }
    public void OpenSettings()
    {
        if(busy||modal!=null)return;
        MenuSFX.Play(MenuSFX.Kind.Confirm);
        var root=OpenModal("THIẾT LẬP",new Vector2(620,460));
        Label(root,"Volume label","Âm lượng",new Vector2(-170,30),new Vector2(200,35),28,gold);
        var value=Label(root,"Volume value",Mathf.RoundToInt(AudioListener.volume*100)+"%",new Vector2(220,30),new Vector2(100,35),26,gold);
        var track=Image(root,"Volume slider",new Vector2(0,-15),new Vector2(440,14),new Color(.24f,.28f,.19f));
        var slider=track.gameObject.AddComponent<Slider>();
        var handle=Image(track.transform,"Handle",Vector2.zero,new Vector2(14,28),gold);slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.direction=Slider.Direction.LeftToRight;slider.minValue=0;slider.maxValue=1;slider.value=AudioListener.volume;
        slider.onValueChanged.AddListener(v=>{AudioListener.volume=v;PlayerPrefs.SetFloat("Noname.MasterVolume",v);PlayerPrefs.Save();value.text=Mathf.RoundToInt(v*100)+"%";});
        Button(root,"Gán phím",new Vector2(0,-70),new Vector2(280,44),()=>ControlBindingsMenu.Open(root));
        Button(root,"Thông tin",new Vector2(0,-125),new Vector2(280,44),OpenCredits);
        var back=Button(root,"Quay lại",new Vector2(0,-180),new Vector2(280,44),CloseModal);
        EventSystem.current.SetSelectedGameObject(back.gameObject);
    }
    private void OpenCredits()
    {
        CloseModal();
        var root=OpenModal("THÔNG TIN",new Vector2(620,500));
        Label(root,"Music attribution","Âm nhạc: Witchcraft\nMakotoHiramatsu\nCC BY 4.0",new Vector2(0,20),new Vector2(540,120),26,gold);
        Button(root,"Nguồn nhạc",new Vector2(-145,-85),new Vector2(250,42),()=>Application.OpenURL("https://makotohiramatsu.itch.io/witchcraft"));
        Button(root,"Giấy phép",new Vector2(145,-85),new Vector2(250,42),()=>Application.OpenURL("https://creativecommons.org/licenses/by/4.0/"));
        Button(root,"Âm thanh quái",new Vector2(0,-145),new Vector2(280,44),OpenEnemyAudioCredits);
        var back=Button(root,"Quay lại",new Vector2(0,-205),new Vector2(280,44),()=>{CloseModal();OpenSettings();});
        EventSystem.current.SetSelectedGameObject(back.gameObject);
    }
    private void OpenEnemyAudioCredits()
    {
        CloseModal();
        var root=OpenModal("ÂM THANH QUÁI",new Vector2(860,650));
        var credits=Resources.Load<TextAsset>("Credits/EnemyAudio");
        Label(root,"Enemy audio attribution",credits!=null?credits.text:"Xem ThirdPartyNotices/EnemySFXExpansion.txt trong thư mục dữ liệu game.",new Vector2(0,15),new Vector2(800,460),19,gold);
        var back=Button(root,"Quay lại",new Vector2(-145,-270),new Vector2(250,42),()=>{CloseModal();OpenCredits();});
        Button(root,"CC BY 4.0",new Vector2(145,-270),new Vector2(250,42),()=>Application.OpenURL("https://creativecommons.org/licenses/by/4.0/"));
        EventSystem.current.SetSelectedGameObject(back.gameObject);
    }
    private Transform OpenModal(string heading,Vector2 size)
    {
        content.interactable=false;content.blocksRaycasts=false;
        var dim=Image(transform,"Menu dialog",Vector2.zero,new Vector2(1280,720),new Color(0,0,0,.8f));modal=dim.gameObject;
        var panel=Image(modal.transform,"Dialog",Vector2.zero,size,new Color(.075f,.09f,.065f));
        Image(panel.transform,"Rule",new Vector2(0,size.y/2-12),new Vector2(size.x-24,2),gold);
        Label(panel.transform,"Heading",heading,new Vector2(0,size.y/2-55),new Vector2(size.x-40,50),36,gold);
        return panel.transform;
    }
    public void CloseModal()
    {
        if(busy)return;
        if(modal!=null){modal.SetActive(false);Destroy(modal);modal=null;}
        content.interactable=true;content.blocksRaycasts=true;Focus(selected);MenuSFX.Play(MenuSFX.Kind.Back);
    }
    private void Quit()
    {
        if(busy)return;MenuSFX.Play(MenuSFX.Kind.Confirm);Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#endif
    }
    private Image Image(Transform parent,string name,Vector2 position,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=position;rect.sizeDelta=size;
        var image=go.GetComponent<Image>();image.color=color;return image;
    }
    private Text Label(Transform parent,string name,string value,Vector2 position,Vector2 size,int fontSize,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=position;rect.sizeDelta=size;
        var text=go.GetComponent<Text>();text.font=GameFont.Current;text.fontSize=fontSize;text.color=color;text.text=value;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
    }
    private Button Button(Transform parent,string caption,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
    {
        var image=Image(parent,caption,position,size,Color.white);
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        var colors=button.colors;colors.normalColor=new Color(.7f,.72f,.65f);colors.highlightedColor=new Color(1,1,.92f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.55f,.52f,.4f);colors.disabledColor=new Color(.45f,.45f,.4f,.4f);colors.fadeDuration=1f/12f;button.colors=colors;
        var label=Label(image.transform,"Label",caption,Vector2.zero,size,32,new Color(1f,.91f,.7f));
        var shadow=label.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.9f);shadow.effectDistance=new Vector2(1,-2);
        PixelMenuButtonStyle.Apply(button);
        return button;
    }
    private void DrawBackdrop()
    {
        const int w=320,h=180;backdrop=new Texture2D(w,h,TextureFormat.RGBA32,false);backdrop.filterMode=FilterMode.Point;
        var pixels=new Color[w*h];var rng=new System.Random(714);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)pixels[y*w+x]=Color.Lerp(new Color(.055f,.075f,.06f),new Color(.16f,.20f,.15f),(float)y/h);
        for(int layer=0;layer<3;layer++)for(int t=0;t<18;t++)
        {
            int cx=rng.Next(-15,w+15),cy=rng.Next(70,185),radius=rng.Next(12,32),trunk=rng.Next(2,5);
            var color=Color.Lerp(new Color(.10f,.135f,.10f),new Color(.025f,.04f,.03f),layer/2f);
            for(int y=0;y<h;y++)for(int x=Mathf.Max(0,cx-radius);x<Mathf.Min(w,cx+radius);x++)
                if(y<cy && Mathf.Abs(x-cx)<trunk || ((x-cx)*(x-cx)+(y-cy)*(y-cy)<radius*radius && y>cy-radius))pixels[y*w+x]=color;
        }
        for(int i=0;i<95;i++){int x=rng.Next(w),y=rng.Next(h);pixels[y*w+x]=new Color(.55f,.57f,.28f);}
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)pixels[y*w+x]=Color.Lerp(pixels[y*w+x],new Color(.025f,.03f,.025f),Mathf.Clamp01((190-x)/190f)*.7f);
        backdrop.SetPixels(pixels);backdrop.Apply();backdropSprite=Sprite.Create(backdrop,new Rect(0,0,w,h),new Vector2(.5f,.5f),1);
        var image=Image(transform,"Pixel forest",Vector2.zero,new Vector2(1280,720),Color.white);image.sprite=backdropSprite;image.raycastTarget=false;
        var rect=image.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
    private void OnDestroy(){if(backdropSprite!=null)Destroy(backdropSprite);if(backdrop!=null)Destroy(backdrop);}
}
