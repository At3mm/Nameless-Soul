using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(SettingsMenuToggle))]
public class PauseBookNavigation : MonoBehaviour
{
    [System.Serializable]
    public class Section
    {
        public string title;
        public GameObject page;
        public Button button;
        public Image background;
        public CanvasGroup caption;
        [Range(0, 5)] public int sourceSlot;
    }

    [Header("Pages: Status, Inventory, Quest, Settings")]
    public Section[] sections;
    public Text pageTitle;
    public Text statusValues;
    public RectTransform tabsRoot;
    public Image pageAnimation;
    public Image contentReveal;
    public Slider masterVolume;
    public Text volumeValue;
    [Header("Original PaperBook animation frames")]
    public Sprite[] tabsAppearFrames;
    public Sprite[] tabsDisappearFrames;
    public Sprite[] selectedTabSheets;
    public Sprite[] flipLeftFrames;
    public Sprite[] flipRightFrames;
    public Sprite[] contentRevealFrames;
    [Header("Animation timing (unscaled seconds)")]
    [Min(0.01f)] public float tabsDuration = 0.45f;
    [Min(0.01f)] public float tabsHideDuration = 0.25f;
    [Min(0.01f)] public float pageTurnDuration = 0.4f;
    [Min(0.01f)] public float contentRevealDuration = 0.3f;
    public int CurrentSection { get; private set; }
    public bool IsTurning { get; private set; }

    private SettingsMenuToggle menu;
    private GameObject shrineView;
    private Font font;
    private Sprite arrowSprite;
    private Sprite selectionSprite;
    private readonly List<Button> buttons=new List<Button>();
    private readonly List<Image> arrows=new List<Image>();
    private Text wallet;
    private CanvasGroup hudGroup;
    private float hudAlpha;
    private bool contentFocus;
    private int inputBlockedFrame=-1;
    private bool savedNavigation;
    private EventSystem menuEvents;
    private bool cursorVisible;
    private CursorLockMode cursorLock;
    private Text controls;
    private Button bindingsButton;
    private Button mainMenuButton;
    private GameObject returnDialog;
    private Button returnCancel, returnConfirm;
    private bool confirmReturn, returning;
    private int settingsFocus;
    public bool IsInSection => contentFocus;
    private Sprite veilSprite;
    private Texture2D veilTexture;
    private readonly Color ink=new Color(.9f,.88f,.80f);

    private void Awake()
    {
        menu=GetComponent<SettingsMenuToggle>();
        var valid=new List<Section>();
        foreach(var section in sections)
        {
            section.page.SetActive(false);section.button.gameObject.SetActive(false);
            if(!string.Equals(section.title,"Status",System.StringComparison.OrdinalIgnoreCase) && section.page.name!="StatusPage")valid.Add(section);
        }
        sections=valid.ToArray();
        foreach(Transform child in menu.menuPanel.transform)child.gameObject.SetActive(false);
        var checkpoint=UnityEngine.Object.FindFirstObjectByType<World.CheckpointMenu>(FindObjectsInactive.Include);
        font=checkpoint!=null && checkpoint.menuFont!=null?checkpoint.menuFont:GameFont.Current;
        arrowSprite=checkpoint!=null?checkpoint.selectionArrow:null;
        selectionSprite=checkpoint!=null?checkpoint.selectionSprite:null;
        BuildView(checkpoint!=null?checkpoint.contentSprite:null);
        menu.OpenStateChanged+=OnOpenStateChanged;
        if(masterVolume!=null)
        {
            var hit=masterVolume.GetComponent<Image>();if(hit==null)hit=masterVolume.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var trigger=masterVolume.GetComponent<EventTrigger>();if(trigger==null)trigger=masterVolume.gameObject.AddComponent<EventTrigger>();
            var pointer=new EventTrigger.Entry{eventID=EventTriggerType.PointerDown};pointer.callback.AddListener(_=>FocusContentFromPointer());trigger.triggers.Add(pointer);
            if(masterVolume.fillRect!=null&&masterVolume.fillRect.TryGetComponent<Image>(out var fill))fill.color=new Color(.72f,.66f,.48f);
            if(masterVolume.handleRect!=null&&masterVolume.handleRect.TryGetComponent<Image>(out var handle))handle.color=new Color(.94f,.84f,.61f);
            masterVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat("Noname.MasterVolume",AudioListener.volume));masterVolume.onValueChanged.AddListener(SetMasterVolume);SetMasterVolume(masterVolume.value);
        }
        shrineView.SetActive(false);
    }
    private void BuildView(Sprite artwork)
    {
        shrineView=new GameObject("Shrine style pause",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        shrineView.transform.SetParent(menu.menuPanel.transform,false);
        var screen=shrineView.GetComponent<RectTransform>();screen.anchorMin=screen.anchorMax=new Vector2(.5f,.5f);screen.pivot=new Vector2(.5f,.5f);screen.anchoredPosition=Vector2.zero;screen.sizeDelta=new Vector2(1280,720);
        var canvas=shrineView.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2400;
        var scaler=shrineView.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var dim=Panel(shrineView.transform,"Dim",Vector2.zero,new Vector2(1280,720),new Color(0,0,0,.45f));
        veilTexture=new Texture2D(128,1,TextureFormat.RGBA32,false);veilTexture.wrapMode=TextureWrapMode.Clamp;
        for(int i=0;i<128;i++)veilTexture.SetPixel(i,0,new Color(.025f,.027f,.025f,Mathf.Lerp(.97f,0,Mathf.SmoothStep(0,1,i/127f))));
        veilTexture.Apply();veilSprite=Sprite.Create(veilTexture,new Rect(0,0,128,1),new Vector2(.5f,.5f),100);
        Panel(shrineView.transform,"Left veil",Vector2.zero,new Vector2(650,720),Color.white).sprite=veilSprite;
        Label(shrineView.transform,"Title","TẠM DỪNG",new Vector2(64,-64),new Vector2(310,46),30);
        Rule(new Vector2(64,-120),new Vector2(285,1));
        Label(shrineView.transform,"Subtitle","HÀNH TRANG & THIẾT LẬP",new Vector2(66,-136),new Vector2(300,28),14).color=new Color(.60f,.73f,.70f);
        var sheet=Panel(shrineView.transform,"Information sheet",new Vector2(390,-145),new Vector2(830,470),new Color(.10f,.11f,.12f,.92f));
        if(artwork!=null){sheet.sprite=artwork;sheet.type=Image.Type.Sliced;}
        wallet=Label(shrineView.transform,"Wallet","",new Vector2(800,-65),new Vector2(410,30),18);wallet.alignment=TextAnchor.MiddleRight;
        for(int i=0;i<sections.Length;i++)
        {
            int index=i;string title=sections[i].title=="Inventory"?"Túi đồ":sections[i].title=="Quest"?"Nhiệm vụ":"Thiết lập";
            var background=Panel(shrineView.transform,"Section "+i,new Vector2(54,-190-i*66),new Vector2(300,48),Color.clear);
            background.sprite=selectionSprite;background.type=Image.Type.Sliced;
            var button=background.gameObject.AddComponent<Button>();button.targetGraphic=background;background.raycastTarget=true;button.transition=Selectable.Transition.None;button.onClick.AddListener(()=>{SelectSection(index);EnterSection();});buttons.Add(button);PixelMenuButtonStyle.Apply(button);
            var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;
            var trigger=button.gameObject.AddComponent<EventTrigger>();var entry=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};
            entry.callback.AddListener(_=>{contentFocus=false;SelectSection(index);RefreshControls();});trigger.triggers.Add(entry);
            var text=Label(button.transform,"Label",title,new Vector2(38,0),new Vector2(250,48),23);text.alignment=TextAnchor.MiddleLeft;
            var arrow=Panel(button.transform,"Selection arrow",new Vector2(10,-16),new Vector2(16,16),Color.white);arrow.sprite=arrowSprite;arrow.preserveAspect=true;arrow.raycastTarget=false;arrow.transform.localScale=new Vector3(-1,1,1);arrows.Add(arrow);
            Rule(new Vector2(70,-245-i*66),new Vector2(270,1));
            var page=sections[i].page;page.transform.SetParent(shrineView.transform,false);
            var rect=page.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(805,-360);rect.sizeDelta=new Vector2(800,550);rect.localScale=Vector3.one;
            foreach(var label in page.GetComponentsInChildren<Text>(true)){label.font=font;label.color=ink;if(font.name.StartsWith("VT323"))label.fontSize=Mathf.RoundToInt(label.fontSize*1.25f);
                if(label.name=="BagHeading")label.text="VẬT PHẨM";
                if(label.name=="ItemDetailsHeading")label.text="THÔNG TIN VẬT PHẨM";
            }
            var inventory=page.GetComponentInChildren<InventoryConsumablesView>(true);if(inventory!=null)inventory.darkTheme=true;
            if(sections[i].title=="Settings")
            {
                var old=page.transform.Find("Controls");if(old!=null)old.gameObject.SetActive(false);
                foreach(var settingLabel in page.GetComponentsInChildren<Text>(true))
                {
                    if(settingLabel.name=="ControlsHeading")settingLabel.text="ĐIỀU KHIỂN";
                    if(settingLabel.name=="AudioHeading")settingLabel.text="ÂM THANH";
                    if(settingLabel.name=="MasterVolumeLabel")settingLabel.text="Âm lượng";
                }
                var bindingPanel=Panel(page.transform,"Gán phím",new Vector2(425,-235),new Vector2(310,48),new Color(.38f,.31f,.15f,.9f));
                bindingsButton=bindingPanel.gameObject.AddComponent<Button>();bindingsButton.onClick.AddListener(OpenBindings);PixelMenuButtonStyle.Apply(bindingsButton);
                var bindingNavigation=bindingsButton.navigation;bindingNavigation.mode=Navigation.Mode.None;bindingsButton.navigation=bindingNavigation;
                Label(bindingPanel.transform,"Label","Gán phím",Vector2.zero,new Vector2(310,48),24).alignment=TextAnchor.MiddleCenter;
                var exitPanel=Panel(page.transform,"Về menu chính",new Vector2(425,-305),new Vector2(310,48),Color.white);
                mainMenuButton=exitPanel.gameObject.AddComponent<Button>();mainMenuButton.onClick.AddListener(RequestMainMenu);PixelMenuButtonStyle.Apply(mainMenuButton);
                var exitNav=mainMenuButton.navigation;exitNav.mode=Navigation.Mode.None;mainMenuButton.navigation=exitNav;
                Label(exitPanel.transform,"Label","Về menu chính",Vector2.zero,new Vector2(310,48),24).alignment=TextAnchor.MiddleCenter;
            }
            page.SetActive(false);
        }
        controls=Label(shrineView.transform,"Controls","W/S  Chọn mục   •   E/Enter  Mở   •   Esc  Tiếp tục",new Vector2(66,-660),new Vector2(650,30),16);
        var close=Panel(shrineView.transform,"Resume",new Vector2(1010,-645),new Vector2(200,38),new Color(.12f,.12f,.11f,.85f));var resume=close.gameObject.AddComponent<Button>();resume.onClick.AddListener(menu.Close);PixelMenuButtonStyle.Apply(resume);
        Label(close.transform,"Label","Tiếp tục [Esc]",Vector2.zero,new Vector2(200,38),17).alignment=TextAnchor.MiddleCenter;
    }
    private void OnOpenStateChanged(bool open)
    {
        if(!open){contentFocus=false;Cursor.visible=cursorVisible;Cursor.lockState=cursorLock;if(menuEvents!=null)menuEvents.sendNavigationEvents=savedNavigation;if(hudGroup!=null){hudGroup.alpha=hudAlpha;hudGroup=null;}return;}
        shrineView.SetActive(true);
        cursorVisible=Cursor.visible;cursorLock=Cursor.lockState;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        var hud=UnityEngine.Object.FindFirstObjectByType<PlayerVitalsHUD>();
        if(hud!=null && hud.TryGetComponent<CanvasGroup>(out hudGroup)){hudAlpha=hudGroup.alpha;hudGroup.alpha=0;}
        if(EventSystem.current==null)new GameObject("Pause EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        menuEvents=EventSystem.current;savedNavigation=menuEvents.sendNavigationEvents;menuEvents.sendNavigationEvents=false;menuEvents.SetSelectedGameObject(null);
        contentFocus=false;CurrentSection=Mathf.Clamp(CurrentSection,0,sections.Length-1);ShowSection();
    }
    public void SelectSection(int index)
    {
        if(!menu.IsOpen || index<0 || index>=sections.Length || index==CurrentSection)return;
        contentFocus=false;CurrentSection=index;MenuSFX.Play(MenuSFX.Kind.Move);ShowSection();
    }
    private void ShowSection()
    {
        for(int i=0;i<sections.Length;i++)
        {
            sections[i].page.SetActive(i==CurrentSection);
            PixelMenuButtonStyle.Select(buttons[i],i==CurrentSection);
            arrows[i].gameObject.SetActive(i==CurrentSection && arrowSprite!=null);
        }
        RefreshControls();
        wallet.text="Gold  "+GameEconomy.Gold+"     •     EXP  "+GameEconomy.Experience;
    }
    public void EnterSection()
    {
        if(menu==null || !menu.IsOpen || menu.IsAnimating || contentFocus)return;
        contentFocus=true;MenuSFX.Play(MenuSFX.Kind.Confirm);RefreshControls();
    }
    public void FocusContentFromPointer(){EnterSection();}
    public bool TryBack()
    {
        if(returning)return true;
        if(returnDialog!=null){CancelMainMenu();return true;}
        if(!contentFocus)return false;
        contentFocus=false;inputBlockedFrame=Time.frameCount;MenuSFX.Play(MenuSFX.Kind.Back);RefreshControls();return true;
    }
    private void RefreshControls()
    {
        if(controls==null)return;
        controls.text=!contentFocus?"W/S  Chọn mục   •   E/Enter  Mở   •   Esc  Tiếp tục":
            sections[CurrentSection].title=="Inventory"?"Q/R  Đổi tab   •   WASD  Chọn đồ   •   E  Trang bị/tháo   •   Esc  Quay lại":
            sections[CurrentSection].title=="Settings"?"W/S  Chọn   •   A/D  Âm lượng   •   E  Xác nhận   •   Esc  Quay lại":"Esc  Quay lại";
        foreach(var arrow in arrows)arrow.color=contentFocus?new Color(.65f,.60f,.48f):Color.white;
    }
    private void Update()
    {
        if(menu==null || !menu.IsOpen || menu.IsAnimating || returning || Keyboard.current==null || Time.frameCount<=inputBlockedFrame || GameControls.Blocked)return;
        var kb=Keyboard.current;
        bool up=kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
        bool down=kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame;
        bool left=kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame;
        bool right=kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;
        bool confirm=kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
        if(returnDialog!=null)
        {
            if(up||down||left||right){confirmReturn=!confirmReturn;RefreshReturnChoice();}
            if(confirm){if(confirmReturn)ConfirmMainMenu();else CancelMainMenu();}
            return;
        }
        if(!contentFocus)
        {
            if(up)SelectSection((CurrentSection+sections.Length-1)%sections.Length);
            else if(down)SelectSection((CurrentSection+1)%sections.Length);
            if(confirm)EnterSection();
        }
        else if(sections[CurrentSection].title=="Inventory")
        {
            var inventory=sections[CurrentSection].page.GetComponentInChildren<InventoryConsumablesView>();
            if(inventory!=null)
            {
                if(kb.qKey.wasPressedThisFrame)inventory.CycleCategory(-1);
                else if(kb.rKey.wasPressedThisFrame)inventory.CycleCategory(1);
                else if(up||down||left||right)inventory.MoveSelection(left?-1:right?1:0,up?-1:down?1:0);
                if(confirm)inventory.ActivateInspected();
            }
        }
        else if(sections[CurrentSection].title=="Settings")
        {
            if(up||down){settingsFocus=(settingsFocus+(up?2:1))%3;MenuSFX.Play(MenuSFX.Kind.Move);}
            if(bindingsButton!=null)PixelMenuButtonStyle.Select(bindingsButton,settingsFocus==1);
            if(mainMenuButton!=null)PixelMenuButtonStyle.Select(mainMenuButton,settingsFocus==2);
            if(settingsFocus==0 && masterVolume!=null && (left||right)){masterVolume.value=Mathf.Clamp01(masterVolume.value+(left?-.05f:.05f));MenuSFX.Play(MenuSFX.Kind.Move);}
            if(settingsFocus==1 && confirm)OpenBindings();
            if(settingsFocus==2 && confirm)RequestMainMenu();
        }
        RefreshControls();
    }
    private void OpenBindings(){FocusContentFromPointer();ControlBindingsMenu.Open(shrineView.transform,()=>{inputBlockedFrame=Time.frameCount;});}
    public void RequestMainMenu()
    {
        if(returnDialog!=null||returning||menu==null||!menu.IsOpen||menu.IsAnimating)return;
        FocusContentFromPointer();settingsFocus=2;confirmReturn=false;inputBlockedFrame=Time.frameCount;
        var dim=Panel(shrineView.transform,"Confirm return to main menu",Vector2.zero,new Vector2(1280,720),new Color(0,0,0,.88f));
        returnDialog=dim.gameObject;dim.raycastTarget=true;
        Label(dim.transform,"Title","VỀ MENU CHÍNH?",new Vector2(340,-235),new Vector2(600,45),30).alignment=TextAnchor.MiddleCenter;
        var saved=World.CheckpointService.ReadSaved();
        Label(dim.transform,"Save explanation",saved!=null?"Tiếp tục sẽ trở lại tượng thần đã lưu.\nVàng, vật phẩm và nhiệm vụ vẫn được giữ.":"Bạn chưa cầu nguyện để lưu điểm tiếp tục.\nTiếp tục sẽ bắt đầu từ đầu bản đồ.",new Vector2(330,-300),new Vector2(620,85),22).alignment=TextAnchor.MiddleCenter;
        returnCancel=ReturnButton(dim.transform,"Quay lại",new Vector2(365,-425),CancelMainMenu);
        returnConfirm=ReturnButton(dim.transform,"Về menu chính",new Vector2(655,-425),ConfirmMainMenu);
        RefreshReturnChoice();
    }
    private Button ReturnButton(Transform parent,string title,Vector2 pos,UnityEngine.Events.UnityAction action)
    {
        var panel=Panel(parent,title,pos,new Vector2(260,48),Color.white);var button=panel.gameObject.AddComponent<Button>();
        button.onClick.AddListener(action);var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
        PixelMenuButtonStyle.Apply(button);Label(panel.transform,"Label",title,Vector2.zero,new Vector2(260,48),23).alignment=TextAnchor.MiddleCenter;return button;
    }
    private void RefreshReturnChoice(){PixelMenuButtonStyle.Select(returnCancel,!confirmReturn);PixelMenuButtonStyle.Select(returnConfirm,confirmReturn);}
    public void CancelMainMenu(){if(returning)return;if(returnDialog!=null)Destroy(returnDialog);returnDialog=null;inputBlockedFrame=Time.frameCount;}
    public void ConfirmMainMenu()
    {
        if(returnDialog==null||returning)return;
        returning=MainMenuController.ReturnToMainMenu();
        if(returning){returnCancel.interactable=false;returnConfirm.interactable=false;}
        else MenuSFX.Play(MenuSFX.Kind.Error);
    }
    private void SetMasterVolume(float value){AudioListener.volume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("Noname.MasterVolume",AudioListener.volume);PlayerPrefs.Save();if(volumeValue!=null)volumeValue.text=Mathf.RoundToInt(AudioListener.volume*100)+"%";}
    private void OnDisable()
    {
        contentFocus=false;
        if(menuEvents!=null)menuEvents.sendNavigationEvents=savedNavigation;
        if(hudGroup!=null){hudGroup.alpha=hudAlpha;hudGroup=null;}
    }
    private void OnDestroy(){if(menuEvents!=null)menuEvents.sendNavigationEvents=savedNavigation;if(hudGroup!=null)hudGroup.alpha=hudAlpha;if(menu!=null)menu.OpenStateChanged-=OnOpenStateChanged;if(masterVolume!=null)masterVolume.onValueChanged.RemoveListener(SetMasterVolume);if(veilSprite!=null)Destroy(veilSprite);if(veilTexture!=null)Destroy(veilTexture);}
    private Image Panel(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),pos,size);var image=go.GetComponent<Image>();image.color=color;return image;}
    private Text Label(Transform parent,string name,string text,Vector2 pos,Vector2 size,int fontSize)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),pos,size);var label=go.GetComponent<Text>();label.font=font;label.fontSize=font.name.StartsWith("VT323")?Mathf.RoundToInt(fontSize*1.25f):fontSize;label.text=text;label.color=ink;label.raycastTarget=false;return label;}
    private static void Place(RectTransform rect,Vector2 pos,Vector2 size){rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;}
    private void Rule(Vector2 pos,Vector2 size){Panel(shrineView.transform,"Rule",pos,size,new Color(.58f,.54f,.43f,.45f)).raycastTarget=false;}
}
