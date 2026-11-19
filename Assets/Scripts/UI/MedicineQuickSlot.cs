using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;

// Reserved slot: the equipped medicine supplies its use callback and availability.
[DisallowMultipleComponent]
public sealed class MedicineQuickSlot : MonoBehaviour
{
    [HideInInspector] public Key useKey = Key.C; // Legacy serialized value; GameControls owns the binding.
    public bool hasItem;
    public Button button;
    public Text keyText;
    public UnityEvent onUse = new UnityEvent();
    public Image itemIcon;
    public Text countText;
    public Text itemNameText;
    private PlayerConsumables consumables;
    private float forwardHeld, backwardHeld;
    private bool forwardReset, backwardReset;
    private int shownKind=-2;
    private float swapTime=1;
    private Vector3 iconScale;
    private bool iconScaleReady;
    private void OnEnable() { if (button != null) button.onClick.AddListener(Use); HideItemName(); AlignCount(countText,keyText); }
    private void OnDisable() { if (button != null) button.onClick.RemoveListener(Use); }
    private void Update()
    {
        if(GetComponent<MedicineQuickSlot>()!=this)return;
        var target=GetComponentInParent<PlayerVitalsHUD>()?.target;
        consumables=target!=null?target.GetComponent<PlayerConsumables>():null;
        if(consumables!=null)
        {
            var catalog=ConsumableCatalog.Default;var item=catalog!=null?catalog.Find(consumables.Selected):null;
            var custom=consumables.SelectedCustom;
            int kind=custom!=null?custom.GetInstanceID():(int)consumables.Selected;
            if(itemIcon!=null && !iconScaleReady){iconScale=itemIcon.rectTransform.localScale;iconScaleReady=true;}
            if(shownKind!=kind){if(shownKind!=-2)swapTime=0;shownKind=kind;}
            int count=custom!=null?ItemInventory.Count(custom):GameEconomy.ConsumableCount(consumables.Selected);hasItem=count>0;
            if(itemIcon!=null){itemIcon.sprite=custom!=null?custom.icon:item!=null?item.icon:null;itemIcon.enabled=itemIcon.sprite!=null;itemIcon.color=hasItem?Color.white:new Color(.45f,.45f,.45f);}
            if(itemIcon!=null && iconScaleReady)
            {
                swapTime+=Time.unscaledDeltaTime;
                int frame=Mathf.FloorToInt(swapTime/.05f);
                float scale=frame==0?.75f:frame==1?1.16f:frame==2?1.04f:1f;
                itemIcon.rectTransform.localScale=iconScale*scale;
            }
            if(countText!=null)countText.text=custom!=null&&!custom.consumable?"∞":custom!=null||item!=null?count.ToString():"";
            HideItemName();
            if(!GameControls.Blocked)
            {
                HandleCycleKey(GameControls.Button(GameControls.Action.NextItem),1,ref forwardHeld,ref forwardReset);
                HandleCycleKey(GameControls.Button(GameControls.Action.PreviousItem),-1,ref backwardHeld,ref backwardReset);
            }
        }
        if (keyText != null) keyText.text = GameControls.Display(GameControls.Action.Item);
        if (button != null) button.interactable = hasItem && (consumables==null || consumables.CanAct);
        if (GameControls.Pressed(GameControls.Action.Item)) Use();
    }
    public static void AlignCount(Text count,Text key)
    {
        if(count==null || key==null)return;
        var rect=count.rectTransform;var keyRect=key.rectTransform;
        if(rect.parent!=keyRect.parent)rect.SetParent(keyRect.parent,false);
        keyRect.sizeDelta=new Vector2(32,20);
        keyRect.anchoredPosition=new Vector2(36,-1);
        key.fontSize=18;key.color=Color.white;key.alignment=TextAnchor.UpperLeft;
        rect.anchorMin=keyRect.anchorMin;rect.anchorMax=keyRect.anchorMax;rect.pivot=keyRect.pivot;
        rect.sizeDelta=new Vector2(32,22);
        rect.anchoredPosition=keyRect.anchoredPosition+new Vector2(0,-18);
        count.alignment=TextAnchor.UpperLeft;count.font=GameFont.Current;count.fontSize=20;count.fontStyle=FontStyle.Bold;
        count.color=Color.white;count.raycastTarget=false;
        foreach(var text in new[]{count,key})
        {
            text.verticalOverflow=VerticalWrapMode.Overflow;text.horizontalOverflow=HorizontalWrapMode.Overflow;
            var outline=text.GetComponent<Outline>();if(outline==null)outline=text.gameObject.AddComponent<Outline>();
            outline.effectColor=new Color(0,0,0,.95f);outline.effectDistance=new Vector2(1,-1);outline.useGraphicAlpha=true;
        }
    }
    private void HideItemName()
    {
        if(itemNameText!=null){itemNameText.text="";itemNameText.enabled=false;}
        // Older HUD revisions left duplicate labels outside the serialized reference.
        if(transform.parent!=null)foreach(var label in transform.parent.GetComponentsInChildren<Text>(true))
        {
            if(label.name=="ConsumableName")label.gameObject.SetActive(false);
            if(label.name=="ConsumableCount" && label!=countText)label.gameObject.SetActive(false);
        }
    }
    private void HandleCycleKey(UnityEngine.InputSystem.Controls.ButtonControl key,int direction,ref float held,ref bool reset)
    {
        if(GameControls.Blocked || key==null || !consumables.CanAct || !key.isPressed){held=0;reset=false;return;}
        if(key.wasPressedThisFrame){consumables.Cycle(direction);held=0;reset=false;}
        held+=Time.unscaledDeltaTime;
        if(!reset && held>=.45f){consumables.SelectFirst();reset=true;}
    }
    public void Use()
    {
        if(GetComponent<MedicineQuickSlot>()!=this)return;
        if (!isActiveAndEnabled || Time.timeScale <= 0f) return;
        var stats = GetComponentInParent<PlayerVitalsHUD>()?.target;
        var health = stats != null ? stats.GetComponent<CharacterHealth>() : null;
        var controller = stats != null ? stats.GetComponent<PlayerController>() : null;
        if (health == null || health.isDead || (controller != null && !controller.isActiveAndEnabled)) return;
        consumables=stats!=null?stats.GetComponent<PlayerConsumables>():null;
        if(consumables!=null){consumables.TryUseSelected();return;}
        if(!hasItem)return;
        onUse.Invoke();
    }
}


