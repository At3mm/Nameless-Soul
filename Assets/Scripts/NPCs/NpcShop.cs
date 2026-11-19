using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace World
{
    [DisallowMultipleComponent, RequireComponent(typeof(NpcInteractable))]
    public sealed class NpcShop : MonoBehaviour
    {
        [Tooltip("Để trống nếu cửa hàng luôn mở. Có nhiệm vụ: chỉ mở sau khi đã hoàn tất/báo cáo nhiệm vụ.")]
        public QuestDefinition requiredQuest;
        public bool IsUnlocked => requiredQuest==null||QuestService.Status(requiredQuest)==QuestStatus.Completed;
        [Header("Shop stock")]
        [Tooltip("Bật để NPC chỉ bán các loại thuốc trong danh sách bên dưới.")]
        public bool useCustomStock;
        public System.Collections.Generic.List<ConsumableKind> consumableStock=new System.Collections.Generic.List<ConsumableKind>();
        public bool includeCustomItems=true;
        public ConsumableKind[] AvailableConsumables
        {
            get
            {
                var result=new System.Collections.Generic.List<ConsumableKind>();
                if(useCustomStock){foreach(var kind in consumableStock)if((int)kind>=0&&(int)kind<ConsumableCatalog.ShopCount&&!result.Contains(kind))result.Add(kind);}
                else for(int i=0;i<ConsumableCatalog.ShopCount;i++)result.Add((ConsumableKind)i);
                return result.ToArray();
            }
        }
        private ConsumableKind[] stockedConsumables=new ConsumableKind[0];
        [Range(0,1)] public float backgroundDim = .68f;
        [Min(.01f)] public float openFadeDuration = .18f;
        public bool IsOpen => view != null && view.activeSelf;
        public bool IsConfirming => confirmation != null && confirmation.activeSelf;
        public string Status { get; private set; }
        public int Quantity { get; private set; } = 1;
        private GameObject view, ownedEventSystem, previousSelection, confirmation;
        private PlayerInteractor actor;
        private NpcInteractable npc;
        private CanvasGroup group;
        private float openFadeElapsed;
        private Text wallet, detail, status, modeTitle, quantityLabel, totalLabel;
        private CanvasGroup hudGroup;
        private float hudAlpha;
        private bool hudInteractable, hudRaycasts;
        private Button confirmButton;
        private readonly Button[] tabs = new Button[2];
        private int selected;
        private int selectedItem;
        private const int RowsPerPage = 5;
        private readonly System.Collections.Generic.List<Button> rows = new System.Collections.Generic.List<Button>();
        private readonly System.Collections.Generic.List<Text> rowNames = new System.Collections.Generic.List<Text>();
        private readonly System.Collections.Generic.List<Text> rowPrices = new System.Collections.Generic.List<Text>();
        private readonly System.Collections.Generic.List<Image> rowIcons = new System.Collections.Generic.List<Image>();
        private Image largeIcon;
        private Text itemTitle, confirmationTitle, pageLabel;
        private readonly System.Collections.Generic.List<GameItemDefinition> customItems=new System.Collections.Generic.List<GameItemDefinition>();
        private int ShopItemCount => stockedConsumables.Length+customItems.Count;
        private GameItemDefinition CustomAt(int index)=>index>=stockedConsumables.Length&&index-stockedConsumables.Length<customItems.Count?customItems[index-stockedConsumables.Length]:null;
        private GameItemDefinition CustomDefinition=>CustomAt(selectedItem);
        private ConsumableKind SelectedKind => selectedItem>=0&&selectedItem<stockedConsumables.Length?stockedConsumables[selectedItem]:(ConsumableKind)(-1);
        private ConsumableCatalog.Item Definition => ConsumableCatalog.Default!=null ? ConsumableCatalog.Default.Find(SelectedKind) : null;
        private int OwnedCount => CustomDefinition!=null?ItemInventory.Count(CustomDefinition):GameEconomy.ConsumableCount(SelectedKind);
        private string ItemName => CustomDefinition!=null?CustomDefinition.displayName:Definition!=null?Definition.displayName:"Vật phẩm";
        private Sprite CurrentIcon => CustomDefinition!=null?CustomDefinition.icon:Definition!=null?Definition.icon:null;
        private float previousTimeScale;
        private bool pausedByShop;
        private readonly Color ink = new Color(.23f,.17f,.12f);
        private readonly Color lightInk = new Color(.96f,.90f,.76f);
        private int BuyPrice => CustomDefinition!=null?Mathf.Max(0,CustomDefinition.buyPrice):Mathf.Max(1,Definition!=null?Definition.buyPrice:1);
        private int SellPrice => CustomDefinition!=null?Mathf.Max(0,CustomDefinition.sellPrice):Mathf.Clamp(Definition!=null?Definition.sellPrice:1,1,BuyPrice);
        private string ModeName => selected == 0 ? "Mua vật phẩm" : "Bán vật phẩm";
        private int UnitPrice => selected == 0 ? BuyPrice : SellPrice;
        public int MaximumQuantity
        {
            get
            {
                if(CustomDefinition!=null)
                {
                    if(selected==0)return CustomDefinition.buyable?Mathf.Max(0,Mathf.Min(99,BuyPrice==0?99:GameEconomy.Gold/BuyPrice,CustomDefinition.maxStack-OwnedCount)):0;
                    return CustomDefinition.sellable?Mathf.Min(99,OwnedCount,SellPrice==0?99:(int.MaxValue-GameEconomy.Gold)/SellPrice):0;
                }
                if(selected==0)return Mathf.Min(99,GameEconomy.Gold/BuyPrice,int.MaxValue-OwnedCount);
                if(selected==1)return Mathf.Min(99,OwnedCount,(int.MaxValue-GameEconomy.Gold)/SellPrice);
                return 0;
            }
        }
        public void Open(PlayerInteractor player, GameObject canvas, NpcInteractable owner)
        {
            if(IsOpen || player==null || owner==null || !owner.IsOpen || owner.IsClosing)return;
            if(!IsUnlocked){player.ShowNotice("Hoàn thành nhiệm vụ của NPC trước để mở khóa cửa hàng.");return;}
            stockedConsumables=AvailableConsumables;
            customItems.Clear();if(includeCustomItems)foreach(var item in GameItemDefinition.All)if(item.buyable||item.sellable)customItems.Add(item);
            if(ShopItemCount==0){player.ShowNotice("Cửa hàng chưa có hàng hóa.");return;}
            customItems.Sort((a,b)=>string.Compare(a.displayName,b.displayName,System.StringComparison.Ordinal));
            MenuSFX.Play(MenuSFX.Kind.Open);
            actor=player;npc=owner;selected=0;selectedItem=0;rows.Clear();rowNames.Clear();rowPrices.Clear();rowIcons.Clear();Status="Chọn vật phẩm để xem và xác nhận giao dịch.";
            canvas.transform.Find("Dialogue panel").gameObject.SetActive(false);
            if(canvas.GetComponent<GraphicRaycaster>()==null)canvas.AddComponent<GraphicRaycaster>();
            if(EventSystem.current==null)
            {
                ownedEventSystem=new GameObject("Shop EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                ownedEventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            previousSelection=EventSystem.current.currentSelectedGameObject;EventSystem.current.SetSelectedGameObject(null);
            view=new GameObject("NPC Shop",typeof(RectTransform),typeof(CanvasGroup));view.transform.SetParent(canvas.transform,false);
            Stretch(view.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
            group=view.GetComponent<CanvasGroup>();group.alpha=0;openFadeElapsed=0;
            var dim=Panel(view.transform,"World dim",Vector2.zero,Vector2.one,false);dim.color=new Color(0,0,0,backgroundDim);
            Label(view.transform,"Title",npc.displayName+" — Cửa hàng",.06f,.88f,.65f,.96f,28,true);
            wallet=Label(view.transform,"Wallet","",.68f,.88f,.94f,.96f,23,true);wallet.alignment=TextAnchor.MiddleRight;
            for(int i=0;i<2;i++)
            {
                int index=i;tabs[i]=MakeButton(view.transform,"Category "+i,i==0?"Mua vật phẩm":"Bán vật phẩm",.06f+i*.20f,.79f,.25f+i*.20f,.85f,()=>SelectMode(index));
            }
            var left=Panel(view.transform,"Item list",new Vector2(.055f,.23f),new Vector2(.52f,.77f),true);
            var right=Panel(view.transform,"Item details",new Vector2(.55f,.23f),new Vector2(.945f,.77f),true);
            modeTitle=Label(left.transform,"List heading","",.07f,.84f,.93f,.94f,22);
            Label(left.transform,"Item column","Vật phẩm",.07f,.74f,.65f,.83f,15);
            var priceHeading=Label(left.transform,"Price column","Giá / Số lượng",.68f,.74f,.94f,.83f,15);priceHeading.alignment=TextAnchor.MiddleRight;
            for(int i=0;i<RowsPerPage;i++)
            {
                int row=i;
                var button=MakeButton(left.transform,"Item row "+i,"",.05f,.62f-i*.12f,.95f,.74f-i*.12f,()=>{SelectItem((selectedItem/RowsPerPage)*RowsPerPage+row);BeginTrade();});
                var icon=Icon(button.transform,"Item icon",new Vector2(.03f,.12f),new Vector2(.15f,.88f));
                var caption=button.GetComponentInChildren<Text>();caption.fontSize=16;Stretch(caption.rectTransform,new Vector2(.18f,0),new Vector2(.70f,1));caption.alignment=TextAnchor.MiddleLeft;
                var price=Label(button.transform,"Price","",.71f,0,.97f,1,15);price.alignment=TextAnchor.MiddleRight;
                rows.Add(button);rowNames.Add(caption);rowPrices.Add(price);rowIcons.Add(icon);
                var trigger=button.gameObject.AddComponent<EventTrigger>();var entry=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};entry.callback.AddListener(_=>SelectItem((selectedItem/RowsPerPage)*RowsPerPage+row));trigger.triggers.Add(entry);
            }
            pageLabel=Label(left.transform,"Page","",.07f,.04f,.93f,.12f,15);
            itemTitle=Label(right.transform,"Item title","",.08f,.79f,.92f,.92f,24);
            largeIcon=Icon(right.transform,"Large item icon",new Vector2(.39f,.54f),new Vector2(.61f,.78f));
            detail=Label(right.transform,"Description","",.08f,.10f,.92f,.53f,19);
            status=Label(view.transform,"Status","",.06f,.135f,.94f,.205f,19,true);
            Label(view.transform,"Controls","Q / R: Mua / Bán    ↑ ↓ / W S: Chọn hàng    E / Enter: Xác nhận    Esc: Thoát",.06f,.07f,.94f,.125f,16,true);
            BuildConfirmation();
            GameEconomy.Changed+=Refresh;SelectMode(0);
            var hud=FindFirstObjectByType<PlayerVitalsHUD>();
            if(hud!=null)
            {
                hudGroup=hud.GetComponent<CanvasGroup>();if(hudGroup==null)hudGroup=hud.gameObject.AddComponent<CanvasGroup>();
                hudAlpha=hudGroup.alpha;hudInteractable=hudGroup.interactable;hudRaycasts=hudGroup.blocksRaycasts;
                hudGroup.alpha=0;hudGroup.interactable=false;hudGroup.blocksRaycasts=false;
            }
            previousTimeScale=Time.timeScale;pausedByShop=true;Time.timeScale=0;
        }
        private void Update()
        {
            if(!IsOpen)return;
            openFadeElapsed+=Time.unscaledDeltaTime;group.alpha=PixelAnimationSettings.Progress(openFadeElapsed,openFadeDuration);
        }
        public void HandleInput()
        {
            var kb=Keyboard.current;if(!IsOpen || npc.IsClosing || kb==null)return;
            if(kb.escapeKey.wasPressedThisFrame){Exit();return;}
            if(IsConfirming)
            {
                if(kb.backspaceKey.wasPressedThisFrame){CancelConfirmation();return;}
                if(kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)ChangeQuantity(-1);
                if(kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)ChangeQuantity(1);
                if(GameControls.Pressed(GameControls.Action.Interact) || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)ConfirmTrade();
                return;
            }
            if(kb.qKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)SelectMode((selected+1)%2);
            if(kb.rKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)SelectMode((selected+1)%2);
            {
                if(kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)SelectItem((selectedItem+ShopItemCount-1)%ShopItemCount);
                if(kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)SelectItem((selectedItem+1)%ShopItemCount);
                if(Mouse.current!=null && Mouse.current.scroll.ReadValue().y!=0)SelectItem(Mathf.Clamp(selectedItem+(Mouse.current.scroll.ReadValue().y>0?-1:1),0,ShopItemCount-1));
            }
            if(GameControls.Pressed(GameControls.Action.Interact) || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)BeginTrade();
        }
        public void SelectMode(int mode)
        {
            if(!IsOpen || npc.IsClosing || IsConfirming)return;
            if(selected!=Mathf.Clamp(mode,0,1))MenuSFX.Play(MenuSFX.Kind.Move);
            selected=Mathf.Clamp(mode,0,1);
            for(int i=0;i<tabs.Length;i++)PixelMenuButtonStyle.Select(tabs[i],i==selected);
            Refresh();
        }
        public void SelectItem(int index)
        {
            if(!IsOpen || npc.IsClosing || IsConfirming)return;
            if(selectedItem!=Mathf.Clamp(index,0,ShopItemCount-1))MenuSFX.Play(MenuSFX.Kind.Move);
            selectedItem=Mathf.Clamp(index,0,ShopItemCount-1);Refresh();
        }
        public void BeginTrade()
        {
            if(!IsOpen || npc.IsClosing || IsConfirming)return;
            if(MaximumQuantity<=0)
            {
                Status=CustomDefinition!=null&&((selected==0&&!CustomDefinition.buyable)||(selected==1&&!CustomDefinition.sellable))?"Vật phẩm không cho phép giao dịch theo cách này.":selected==0?(GameEconomy.Gold<BuyPrice?"Không đủ vàng để mua.":"Kho vật phẩm đã đầy."):(OwnedCount<=0?"Không có vật phẩm để bán.":"Vàng đã đạt giới hạn.");
                actor?.ShowNotice(Status);
                MenuSFX.Play(MenuSFX.Kind.Error);Refresh();return;
            }
            MenuSFX.Play(MenuSFX.Kind.Confirm);Quantity=1;confirmation.SetActive(true);RefreshConfirmation();
        }
        public void ChangeQuantity(int delta)
        {
            if(!IsConfirming || npc.IsClosing)return;
            int oldQuantity=Quantity;
            Quantity=(int)System.Math.Max(1,System.Math.Min(System.Math.Max(1,MaximumQuantity),(long)Quantity+delta));if(oldQuantity!=Quantity)MenuSFX.Play(MenuSFX.Kind.Move);RefreshConfirmation();
        }
        public void ConfirmTrade()
        {
            if(!IsConfirming || npc.IsClosing)return;
            if(Trade(selected,Quantity))confirmation.SetActive(false);
        }
        public void CancelConfirmation(){if(IsConfirming)MenuSFX.Play(MenuSFX.Kind.Back);if(confirmation!=null)confirmation.SetActive(false);}
        public bool Trade(int action,int quantity=1)
        {
            if(!IsUnlocked){actor?.ShowNotice("Cửa hàng chưa được mở khóa.");return false;}
            if(!IsOpen || npc.IsClosing || actor==null || actor.GetComponent<CharacterHealth>().isDead || quantity<=0 || action<0 || action>1)return false;
            bool success;
            var custom=CustomDefinition;
            if(custom!=null)
            {
                long total=(long)(action==0?BuyPrice:SellPrice)*quantity;
                success=total<=int.MaxValue&&(action==0?custom.buyable&&GameEconomy.TryPurchase((int)total,()=>QuestService.GrantItem(custom,quantity)):
                    custom.sellable&&GameEconomy.TrySellItem((int)total,()=>ItemInventory.Remove(custom,quantity)));
            }
            else success=action==0?GameEconomy.BuyConsumable(SelectedKind,BuyPrice,quantity):GameEconomy.SellConsumable(SelectedKind,SellPrice,quantity);
            Status=success?(action==0?"Đã mua "+quantity+" × "+ItemName+".":"Đã bán "+quantity+" × "+ItemName+"."):
                action==0?((long)BuyPrice*quantity>GameEconomy.Gold?"Không đủ vàng để mua số lượng này.":"Không thể mua: kho vật phẩm đã đầy hoặc vật phẩm không khả dụng."):
                OwnedCount<quantity?"Không đủ vật phẩm để bán.":"Không thể bán: vàng đã đạt giới hạn.";
            actor.ShowNotice(Status);
            MenuSFX.Play(success?MenuSFX.Kind.Purchase:MenuSFX.Kind.Error);Refresh();return success;
        }
        private void Refresh()
        {
            if(!IsOpen)return;
            wallet.text="Gold: "+GameEconomy.Gold;
            modeTitle.text=ModeName;
            int page=selectedItem/RowsPerPage;
            for(int i=0;i<rows.Count;i++)
            {
                int index=page*RowsPerPage+i;bool visible=index<ShopItemCount;rows[i].gameObject.SetActive(visible);if(!visible)continue;
                var def=ConsumableCatalog.Default!=null?ConsumableCatalog.Default.Find(index<stockedConsumables.Length?stockedConsumables[index]:(ConsumableKind)(-1)):null;
                var custom=CustomAt(index);
                rowNames[i].text=custom!=null?custom.displayName:def!=null?def.displayName:"—";
                int price=def!=null?(selected==0?Mathf.Max(1,def.buyPrice):Mathf.Clamp(def.sellPrice,1,Mathf.Max(1,def.buyPrice))):1;
                if(custom!=null)price=Mathf.Max(0,selected==0?custom.buyPrice:custom.sellPrice);
                int owned=custom!=null?ItemInventory.Count(custom):GameEconomy.ConsumableCount(index<stockedConsumables.Length?stockedConsumables[index]:(ConsumableKind)(-1));
                rowPrices[i].text=(price+" Gold")+"\nCó: "+owned;
                if(rowIcons[i]!=null){rowIcons[i].sprite=custom!=null?custom.icon:def!=null?def.icon:null;rowIcons[i].enabled=rowIcons[i].sprite!=null;}
                PixelMenuButtonStyle.Select(rows[i],index==selectedItem);
            }
            pageLabel.text="Trang "+(page+1)+" / "+Mathf.CeilToInt(ShopItemCount/(float)RowsPerPage)+"    ↑ ↓ / cuộn chuột";
            itemTitle.text=ItemName;if(largeIcon!=null){largeIcon.sprite=CurrentIcon;largeIcon.enabled=CurrentIcon!=null;}
            detail.text=(CustomDefinition!=null?CustomDefinition.description:Definition!=null?Definition.description:"")+"\n\nTrong túi: "+OwnedCount+(CustomDefinition==null&&SelectedKind==ConsumableKind.SoulPurification?"\nDùng tại tượng thần → Thanh tẩy để chữa Ăn hồn.":"\nGắn vào danh sách dùng nhanh trong túi đồ.");
            status.text=Status;
            if(IsConfirming)RefreshConfirmation();
        }
        private void BuildConfirmation()
        {
            confirmation=new GameObject("Quantity confirmation",typeof(RectTransform));confirmation.transform.SetParent(view.transform,false);Stretch(confirmation.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);
            var dim=Panel(confirmation.transform,"Confirmation dim",Vector2.zero,Vector2.one,false);dim.color=new Color(0,0,0,.5f);
            var box=Panel(confirmation.transform,"Confirmation sheet",new Vector2(.30f,.28f),new Vector2(.70f,.72f),true);
            confirmationTitle=Label(box.transform,"Item","",.08f,.75f,.92f,.91f,23);
            Label(box.transform,"Quantity heading","Số lượng",.08f,.58f,.92f,.73f,18);
            MakeButton(box.transform,"Minus","−",.15f,.42f,.30f,.57f,()=>ChangeQuantity(-1));
            quantityLabel=Label(box.transform,"Quantity","1",.32f,.42f,.68f,.57f,24);quantityLabel.alignment=TextAnchor.MiddleCenter;
            MakeButton(box.transform,"Plus","+",.70f,.42f,.85f,.57f,()=>ChangeQuantity(1));
            totalLabel=Label(box.transform,"Total","",.08f,.28f,.92f,.41f,19);totalLabel.alignment=TextAnchor.MiddleCenter;
            confirmButton=MakeButton(box.transform,"Confirm","Xác nhận",.08f,.10f,.48f,.24f,ConfirmTrade);
            MakeButton(box.transform,"Cancel","Hủy",.52f,.10f,.92f,.24f,CancelConfirmation);
            confirmation.SetActive(false);
        }
        private void RefreshConfirmation()
        {
            confirmationTitle.text=ItemName;
            quantityLabel.text=Quantity+" / "+MaximumQuantity;
            totalLabel.text=(selected==0?"Thanh toán: ":"Nhận: ")+((long)UnitPrice*Quantity)+" Gold";
            confirmButton.interactable=Quantity>0 && Quantity<=MaximumQuantity;
        }
        public void Close()
        {
            GameEconomy.Changed-=Refresh;
            if(pausedByShop){Time.timeScale=previousTimeScale;pausedByShop=false;}
            if(hudGroup!=null){hudGroup.alpha=hudAlpha;hudGroup.interactable=hudInteractable;hudGroup.blocksRaycasts=hudRaycasts;hudGroup=null;}
            if(view!=null){view.SetActive(false);Destroy(view);view=null;}
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(previousSelection!=null && previousSelection.activeInHierarchy?previousSelection:null);
            if(ownedEventSystem!=null)Destroy(ownedEventSystem);
            actor=null;confirmation=null;
        }
        public void Exit(){if(npc!=null && npc.IsOpen)npc.RequestClose();else Close();}
        private void OnDisable(){if(npc!=null && npc.IsOpen)npc.Close();else Close();}
        private static void Stretch(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private Image Panel(Transform parent,string name,Vector2 min,Vector2 max,bool paper)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Stretch(go.GetComponent<RectTransform>(),min,max);
            var image=go.GetComponent<Image>();image.color=Color.white;
            if(paper){image.sprite=npc.panelSprite;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=1.5f;}
            return image;
        }
        private Image Icon(Transform parent,string name,Vector2 min,Vector2 max)
        {
            var image=Panel(parent,name,min,max,false);image.sprite=CurrentIcon;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        private Text Label(Transform parent,string name,string value,float x0,float y0,float x1,float y1,int size,bool light=false)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Stretch(go.GetComponent<RectTransform>(),new Vector2(x0,y0),new Vector2(x1,y1));
            var text=go.GetComponent<Text>();text.font=npc.dialogueFont!=null?npc.dialogueFont:GameFont.Current;text.fontSize=size;text.color=light?lightInk:ink;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;text.text=value;return text;
        }
        private Button MakeButton(Transform parent,string name,string caption,float x0,float y0,float x1,float y1,UnityEngine.Events.UnityAction action)
        {
            var image=Panel(parent,name,new Vector2(x0,y0),new Vector2(x1,y1),false);image.color=new Color(.65f,.44f,.22f,.24f);
            var button=image.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>{action();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);});
            var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
            var text=Label(button.transform,"Caption",caption,0,0,1,1,18);text.alignment=TextAnchor.MiddleCenter;
            PixelMenuButtonStyle.Apply(button);
            return button;
        }
    }
}
