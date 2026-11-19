using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Shared controls dialog for the title screen and paused game. Uses unscaled input.</summary>
public sealed class ControlBindingsMenu : MonoBehaviour
{
    private static ControlBindingsMenu instance;
    public static bool IsOpen=>instance!=null&&instance.gameObject.activeInHierarchy;
    private readonly List<Button> rows=new List<Button>();
    private readonly List<Text> captions=new List<Text>();
    private Text status;
    private int selected,capturing=-1,blockedUntil;
    private float captureUntil;
    private GameObject previousSelection;
    private bool previousNavigation;
    private EventSystem events;
    private Action onClosed;
    public bool IsCapturing=>capturing>=0;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){instance=null;}
    public static void Open(Transform owner,Action closed=null)
    {
        if(IsOpen)return;
        var go=new GameObject("Control bindings",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        go.transform.SetParent(owner,false);instance=go.AddComponent<ControlBindingsMenu>();instance.onClosed=closed;instance.Build();
    }
    private void Build()
    {
        var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=3200;canvas.pixelPerfect=true;
        var scaler=GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        Panel(transform,"Dim",Vector2.zero,new Vector2(1280,720),new Color(0,0,0,.9f));
        var sheet=Panel(transform,"Bindings panel",Vector2.zero,new Vector2(1080,650),new Color(.075f,.085f,.065f,.99f));
        Label(sheet.transform,"Title","GÁN PHÍM",new Vector2(0,276),new Vector2(1000,45),38);
        Label(sheet.transform,"Help","Chọn thao tác rồi nhấn phím hoặc nút chuột mới. Esc để hủy / quay lại.",new Vector2(0,234),new Vector2(1020,32),22);
        for(int i=0;i<GameControls.Definitions.Count;i++)
        {
            int index=i;var def=GameControls.Definitions[i];
            var button=MakeButton(sheet.transform,"Binding "+def.action,new Vector2(i<7?-260:260,178-(i%7)*49),new Vector2(492,42),()=>BeginCapture(index));
            var caption=Label(button.transform,"Caption","",Vector2.zero,new Vector2(468,42),23);caption.alignment=TextAnchor.MiddleLeft;captions.Add(caption);rows.Add(button);
        }
        status=Label(sheet.transform,"Status","",new Vector2(0,-185),new Vector2(1020,58),22);
        rows.Add(MakeButton(sheet.transform,"Khôi phục mặc định",new Vector2(-250,-260),new Vector2(420,44),()=>{CancelCapture();GameControls.RestoreDefaults();status.text="Đã khôi phục phím mặc định.";Refresh();}));
        rows.Add(MakeButton(sheet.transform,"Quay lại [Esc]",new Vector2(250,-260),new Vector2(420,44),Close));
        Label(sheet.transform,"Navigation","↑ ↓ ← →  Chọn    E / Enter  Xác nhận    •    Thay đổi được lưu tự động",new Vector2(0,-305),new Vector2(1020,25),18);
        events=EventSystem.current;if(events!=null){previousSelection=events.currentSelectedGameObject;previousNavigation=events.sendNavigationEvents;events.sendNavigationEvents=false;events.SetSelectedGameObject(null);}
        blockedUntil=Time.frameCount;GameControls.Changed+=Refresh;Refresh();
        status.text="Phím điều hướng menu giữ nguyên; Esc luôn có thể mở / đóng menu tạm dừng.";
    }
    public void BeginCapture(int index)
    {
        if(index<0||index>=GameControls.Definitions.Count)return;
        selected=capturing=index;blockedUntil=Time.frameCount;captureUntil=Time.unscaledTime+10;
        status.text="Đang chờ: "+GameControls.Definitions[index].label+" — nhấn phím mới (Esc hủy).";Refresh();
    }
    private void CancelCapture(){capturing=-1;blockedUntil=Time.frameCount;Refresh();}
    private void Update()
    {
        if(Time.frameCount<=blockedUntil)return;
        var kb=Keyboard.current;
        if(capturing>=0)
        {
            if(kb!=null&&kb.escapeKey.wasPressedThisFrame){CancelCapture();status.text="Đã hủy đổi phím.";return;}
            if(Time.unscaledTime>=captureUntil){CancelCapture();status.text="Hết thời gian chờ; giữ phím cũ.";return;}
            if(kb!=null)foreach(var key in kb.allKeys)if(key.wasPressedThisFrame){Assign("<Keyboard>/"+key.name);return;}
            var mouse=Mouse.current;
            if(mouse!=null)foreach(var button in new[]{mouse.leftButton,mouse.rightButton,mouse.middleButton,mouse.backButton,mouse.forwardButton})
                if(button.wasPressedThisFrame){Assign("<Mouse>/"+button.name);return;}
            return;
        }
        if(kb==null)return;
        if(kb.escapeKey.wasPressedThisFrame){Close();return;}
        int next=selected;
        if(kb.upArrowKey.wasPressedThisFrame||kb.wKey.wasPressedThisFrame)next=(selected+rows.Count-1)%rows.Count;
        else if(kb.downArrowKey.wasPressedThisFrame||kb.sKey.wasPressedThisFrame)next=(selected+1)%rows.Count;
        else if(kb.leftArrowKey.wasPressedThisFrame||kb.aKey.wasPressedThisFrame)next=selected<14?(selected+7)%14:14;
        else if(kb.rightArrowKey.wasPressedThisFrame||kb.dKey.wasPressedThisFrame)next=selected<14?(selected+7)%14:15;
        if(next!=selected){selected=next;Refresh();MenuSFX.Play(MenuSFX.Kind.Move);}
        if(kb.enterKey.wasPressedThisFrame||kb.eKey.wasPressedThisFrame)rows[selected].onClick.Invoke();
    }
    private void Assign(string path)
    {
        var action=GameControls.Definitions[capturing].action;
        if(!GameControls.TrySet(action,path,out var message)){status.text=message;return;}
        capturing=-1;blockedUntil=Time.frameCount;status.text="Đã lưu: "+GameControls.Definitions[selected].label+" → "+GameControls.Display(action);Refresh();MenuSFX.Play(MenuSFX.Kind.Confirm);
    }
    private void Refresh()
    {
        for(int i=0;i<captions.Count;i++)captions[i].text=GameControls.Definitions[i].label+"   [ "+(capturing==i?"...":GameControls.Display(GameControls.Definitions[i].action))+" ]";
        for(int i=0;i<rows.Count;i++){rows[i].interactable=capturing<0;PixelMenuButtonStyle.Select(rows[i],i==selected);}
    }
    public void Close(){if(capturing>=0){CancelCapture();return;}gameObject.SetActive(false);Destroy(gameObject);}
    private void OnDisable()
    {
        GameControls.Changed-=Refresh;GameControls.SuppressThisFrame();
        if(events!=null){events.sendNavigationEvents=previousNavigation;events.SetSelectedGameObject(previousSelection);}
        if(instance==this)instance=null;onClosed?.Invoke();onClosed=null;
    }
    private static Image Panel(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=pos;rect.sizeDelta=size;var image=go.GetComponent<Image>();image.color=color;return image;
    }
    private static Text Label(Transform parent,string name,string value,Vector2 pos,Vector2 size,int fontSize)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=pos;rect.sizeDelta=size;
        var text=go.GetComponent<Text>();text.font=GameFont.Current;text.text=value;text.fontSize=fontSize;text.color=new Color(.90f,.84f,.65f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
    }
    private Button MakeButton(Transform parent,string name,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
    {
        var image=Panel(parent,name,pos,size,new Color(.16f,.18f,.12f));var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
        var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
        PixelMenuButtonStyle.Apply(button);
        if(name.StartsWith("Binding "))return button;
        Label(button.transform,"Caption",name,Vector2.zero,size,26);return button;
    }
}
