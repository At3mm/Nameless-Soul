using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Shared appearance only: never changes listeners, navigation or interactability.</summary>
[DisallowMultipleComponent,RequireComponent(typeof(Button))]
[DefaultExecutionOrder(1000)]
public sealed class PixelMenuButtonSkin : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler,ISubmitHandler
{
    public PixelMenuButtonStyle style;
    public bool selected;
    public bool useMenuSelection;
    private Button button;
    private Image image;
    private Text[] labels;
    private int labelChildCount=-1;
    private bool hovering,pressing;
    private float pressedUntil;
    private void Awake(){button=GetComponent<Button>();image=GetComponent<Image>();}
    private void OnEnable(){Refresh();}
    private void OnDisable(){hovering=pressing=false;pressedUntil=0;}
    private void LateUpdate(){Refresh();}
    public void Refresh()
    {
        if(style==null)style=PixelMenuButtonStyle.Current;
        if(style==null || style.background==null)return;
        if(button==null)button=GetComponent<Button>();
        if(image==null)image=GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        button.transition=Selectable.Transition.None;button.targetGraphic=image;
        image.sprite=style.background;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=1;
        image.preserveAspect=false;image.enabled=true;image.raycastTarget=true;
        image.canvasRenderer.SetColor(Color.white);
        bool enabled=button.isActiveAndEnabled && button.IsInteractable();
        bool focused=selected || hovering || (!useMenuSelection && EventSystem.current!=null && EventSystem.current.currentSelectedGameObject==gameObject);
        image.color=!enabled?style.disabled:(pressing||Time.unscaledTime<pressedUntil)?style.pressed:focused?style.selected:style.normal;
        // Text children may be populated after AddComponent, or rebuilt by a menu refresh.
        if(labels==null || labelChildCount!=transform.childCount)
        {
            labelChildCount=transform.childCount;
            labels=GetComponentsInChildren<Text>(true);
        }
        foreach(var label in labels)
        {
            if(label==null)continue;
            label.color=enabled?style.text:new Color(style.text.r,style.text.g,style.text.b,.45f);
            var shadow=label.GetComponent<Shadow>();if(shadow==null)shadow=label.gameObject.AddComponent<Shadow>();
            shadow.effectColor=style.shadow;shadow.effectDistance=style.shadowOffset;
        }
    }
    public void OnPointerEnter(PointerEventData e){hovering=true;}
    public void OnPointerExit(PointerEventData e){hovering=pressing=false;}
    public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)pressing=true;}
    public void OnPointerUp(PointerEventData e){pressing=false;}
    public void OnSubmit(BaseEventData e){pressedUntil=Time.unscaledTime+1f/PixelAnimationSettings.FPS;}
}
