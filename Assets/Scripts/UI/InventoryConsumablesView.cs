using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InventoryGridView))]
public sealed class InventoryConsumablesView : MonoBehaviour
{
    public enum Category { Support, BossRelics }
    public Category CurrentCategory { get; private set; }
    public Text selectionLabel;
    public bool darkTheme;
    private ConsumableKind inspected=(ConsumableKind)(-1);
    private string inspectedBossItem,inspectedQuestEntry;
    private RectTransform questDetailsScroll;
    private Transform originalDetailParent;
    private Vector2 originalDetailMin,originalDetailMax,originalDetailPivot,originalDetailPosition;
    private void LayoutQuestDetails(bool expanded)
    {
        var rect=selectionLabel.rectTransform;
        if(originalDetailParent==null){originalDetailParent=rect.parent;originalDetailMin=rect.anchorMin;originalDetailMax=rect.anchorMax;originalDetailPivot=rect.pivot;originalDetailPosition=rect.anchoredPosition;}
        if(expanded)
        {
            if(questDetailsScroll==null)
            {
                var go=new GameObject("Quest details scroll",typeof(RectTransform),typeof(Image),typeof(Mask),typeof(ScrollRect));go.transform.SetParent(originalDetailParent,false);questDetailsScroll=go.GetComponent<RectTransform>();
                questDetailsScroll.anchorMin=originalDetailMin;questDetailsScroll.anchorMax=originalDetailMax;questDetailsScroll.pivot=originalDetailPivot;
                // Keep the original detail area's top edge below its heading as the viewport grows.
                questDetailsScroll.anchoredPosition=new Vector2(originalDetailPosition.x,15-(250-130)*(1-originalDetailPivot.y));questDetailsScroll.sizeDelta=new Vector2(230,250);
                go.GetComponent<Mask>().showMaskGraphic=false;var scroll=go.GetComponent<ScrollRect>();scroll.viewport=questDetailsScroll;scroll.content=rect;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            }
            questDetailsScroll.gameObject.SetActive(true);rect.SetParent(questDetailsScroll,false);rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(0,340);rect.sizeDelta=new Vector2(0,Mathf.Max(340,selectionLabel.preferredHeight+24));
        }
        else if(questDetailsScroll!=null)
        {
            rect.SetParent(originalDetailParent,false);rect.anchorMin=originalDetailMin;rect.anchorMax=originalDetailMax;rect.pivot=originalDetailPivot;rect.anchoredPosition=new Vector2(originalDetailPosition.x,15);rect.sizeDelta=new Vector2(230,130);questDetailsScroll.gameObject.SetActive(false);
        }
    }
    private sealed class QuestEntry { public string id,title,description;public Sprite icon;public GameItemDefinition definition; }
    private List<QuestEntry> QuestEntries()
    {
        var entries=new List<QuestEntry>();
        if(GameEconomy.ConsumableCount(ConsumableKind.ShrineReturn)>0 && !QuestService.OwnedItems().Exists(i=>i.id==ConsumableCatalog.ShrineReturnItemId))
        {
            var charm=ConsumableCatalog.Default.Find(ConsumableKind.ShrineReturn);
            entries.Add(new QuestEntry{id="item:"+ConsumableCatalog.ShrineReturnItemId,title=charm.displayName,icon=charm.icon,description=charm.description+"\n\nCó thể tái sử dụng. Gắn vào ô dùng nhanh để sử dụng ngoài menu."});
        }
        entries.AddRange(DefinitionEntries(ItemCategory.Support));
        foreach(var quest in QuestService.Accepted())entries.Add(new QuestEntry{id="quest:"+quest.questId,title=quest.title,icon=quest.icon,description=QuestService.StatusText(QuestService.Status(quest))+"\n\n"+quest.description+"\n\n"+QuestService.ProgressText(quest)+"\n\nPhần thưởng: "+QuestService.RewardText(quest)});
        return entries;
    }
    private List<QuestEntry> DefinitionEntries(ItemCategory category)
    {
        var entries=new List<QuestEntry>();
        foreach(var item in ItemInventory.Owned(category))entries.Add(new QuestEntry{id="item:"+item.itemId,title=item.displayName,icon=item.icon,definition=item,
            description=item.rarity+" · Có: "+ItemInventory.Count(item)+"\n\n"+item.description});
        return entries;
    }
    private List<QuestEntry> PresentedEntries()
    {
        if(CurrentCategory==Category.BossRelics)return DefinitionEntries(ItemCategory.BossRelic);
        return QuestEntries();
    }
    private void SelectDefinition(string id)
    {inspectedQuestEntry=id;inspected=(ConsumableKind)(-1);inspectedBossItem=null;notice="";Refresh();GetComponentInParent<PauseBookNavigation>()?.FocusContentFromPointer();MenuSFX.Play(MenuSFX.Kind.Move);}
    public void SelectQuestEntry(string id){CurrentCategory=Category.Support;inspected=(ConsumableKind)(-1);inspectedBossItem=null;inspectedQuestEntry=id;notice="";Refresh();GetComponentInParent<PauseBookNavigation>()?.FocusContentFromPointer();MenuSFX.Play(MenuSFX.Kind.Move);}
    private string notice="";
    private readonly List<Button> categoryButtons=new List<Button>();
    private static readonly string[] CategoryTitles={"Vật phẩm","Cổ vật cường địch"};
    public void SelectCategory(Category category)
    {
        if((int)category<0 || (int)category>=CategoryTitles.Length)return;
        CurrentCategory=category;inspected=(ConsumableKind)(-1);inspectedBossItem=null;inspectedQuestEntry=null;notice="";
        var grid=GetComponent<InventoryGridView>();if(grid.scrollRect!=null && grid.scrollRect.content!=null)grid.scrollRect.content.anchoredPosition=Vector2.zero;
        Refresh();GetComponentInParent<PauseBookNavigation>()?.FocusContentFromPointer();MenuSFX.Play(MenuSFX.Kind.Move);
    }
    public void CycleCategory(int direction)=>SelectCategory((Category)(((int)CurrentCategory+direction%2+2)%2));
    public void SelectItem(ConsumableKind kind){CurrentCategory=Category.Support;inspected=kind;inspectedBossItem=null;inspectedQuestEntry=null;notice="";Refresh();GetComponentInParent<PauseBookNavigation>()?.FocusContentFromPointer();MenuSFX.Play(MenuSFX.Kind.Move);}
    public void SelectBossItem(string id){CurrentCategory=Category.BossRelics;inspectedBossItem=id;inspectedQuestEntry=null;inspected=(ConsumableKind)(-1);notice="";Refresh();GetComponentInParent<PauseBookNavigation>()?.FocusContentFromPointer();MenuSFX.Play(MenuSFX.Kind.Move);}
    private static List<BossItemInventory.Item> RelicEntries() => BossItemInventory.OwnedItems().FindAll(item => GameItemDefinition.FindItem(item.Id)==null);
    public void MoveSelection(int horizontal,int vertical)
    {
        var legacy=new List<ConsumableKind>();
        if(CurrentCategory==Category.Support&&ConsumableCatalog.Default!=null)foreach(var item in ConsumableCatalog.Default.items)
            if(item!=null&&item.kind!=ConsumableKind.ShrineReturn&&GameEconomy.ConsumableCount(item.kind)>0)legacy.Add(item.kind);
        var bosses=CurrentCategory==Category.BossRelics?RelicEntries():new List<BossItemInventory.Item>();
        var entries=PresentedEntries();int count=legacy.Count+bosses.Count+entries.Count;if(count==0)return;
        int index=entries.FindIndex(e=>e.id==inspectedQuestEntry);
        if(index>=0)index+=legacy.Count+bosses.Count;
        else if(!string.IsNullOrEmpty(inspectedBossItem))index=legacy.Count+bosses.FindIndex(b=>b.Id==inspectedBossItem);
        else index=legacy.IndexOf(inspected);
        if(index<0)index=0;
        int step=horizontal!=0?horizontal:vertical*Mathf.Max(1,GetComponent<InventoryGridView>().columns);
        index=(index+step%count+count)%count;
        if(index<legacy.Count)SelectItem(legacy[index]);
        else if(index<legacy.Count+bosses.Count)SelectBossItem(bosses[index-legacy.Count].Id);
        else SelectDefinition(entries[index-legacy.Count-bosses.Count].id);
        var grid=GetComponent<InventoryGridView>();
        if(grid.CanScroll)
        {
            var content=grid.scrollRect.content;float top=index/Mathf.Max(1,grid.columns)*(grid.grid.cellSize.y+grid.grid.spacing.y)+grid.grid.padding.top;
            var position=content.anchoredPosition;position.y=Mathf.Clamp(top,0,Mathf.Max(0,content.rect.height-grid.scrollRect.viewport.rect.height));content.anchoredPosition=position;
        }
    }
    private GameItemDefinition InspectedDefinition()=>PresentedEntries().Find(e=>e.id==inspectedQuestEntry)?.definition;
    public bool ActivateInspected()
    {
        var item=InspectedDefinition();
        if(item!=null&&item.itemId!=ConsumableCatalog.ShrineReturnItemId)return ToggleDefinition(item);
        return CurrentCategory==Category.Support&&inspectedQuestEntry=="item:"+ConsumableCatalog.ShrineReturnItemId?ToggleEquipped(ConsumableKind.ShrineReturn):CurrentCategory==Category.Support&&ConsumableCatalog.Valid(inspected)&&ToggleEquipped(inspected);
    }
    private bool ToggleDefinition(GameItemDefinition item)
    {
        if(item==null)return false;
        bool was=GameEconomy.IsCustomEquipped(item.itemId),changed=GameEconomy.ToggleCustomQuickItem(item);
        notice=changed?(was?"Đã tháo khỏi ô dùng nhanh.":"Đã gắn vào ô dùng nhanh."):!item.CanUse?"Vật phẩm này không có hiệu ứng sử dụng.":"Ô dùng nhanh đã đủ 5 món hoặc không có vật phẩm.";
        FindFirstObjectByType<World.PlayerInteractor>()?.ShowNotice(notice);MenuSFX.Play(changed?MenuSFX.Kind.Confirm:MenuSFX.Kind.Error);Refresh();return changed;
    }
    private void OnEnable(){GameEconomy.Changed+=Refresh;BossItemInventory.Changed+=Refresh;QuestService.Changed+=Refresh;Refresh();}
    private void OnDisable(){GameEconomy.Changed-=Refresh;BossItemInventory.Changed-=Refresh;QuestService.Changed-=Refresh;}
    public bool ToggleEquipped(ConsumableKind kind)
    {
        if(!ConsumableCatalog.CanEquip(kind)){notice="Dùng vật phẩm này tại tượng thần → Thanh tẩy.";FindFirstObjectByType<World.PlayerInteractor>()?.ShowNotice(notice);Refresh();return false;}
        GetComponentInParent<PauseBookNavigation>()?.FocusContentFromPointer();
        bool wasEquipped=GameEconomy.QuickItemIndex(kind)>=0;
        bool changed=GameEconomy.ToggleQuickItem(kind);
        MenuSFX.Play(changed?MenuSFX.Kind.Confirm:MenuSFX.Kind.Error);
        notice=changed?(wasEquipped?"Đã tháo khỏi ô dùng nhanh.":"Đã gắn vào ô dùng nhanh."):GameEconomy.QuickItemCount>=GameEconomy.QuickItemLimit?"Đã đủ 5 món. Hãy tháo một món.":"Không có vật phẩm để gắn vào ô dùng nhanh.";
        FindFirstObjectByType<World.PlayerInteractor>()?.ShowNotice(notice);Refresh();
        return changed;
    }
    public void Refresh()
    {
        var catalog=ConsumableCatalog.Default;var grid=GetComponent<InventoryGridView>();
        if(catalog==null || grid.scrollRect==null || grid.scrollRect.content==null)return;
        var owned=new List<ConsumableCatalog.Item>();var icons=new List<Sprite>();
        if(CurrentCategory==Category.Support)foreach(var item in catalog.items)if(item!=null && item.kind!=ConsumableKind.ShrineReturn && GameEconomy.ConsumableCount(item.kind)>0){owned.Add(item);icons.Add(item.icon);}
        var bossItems=CurrentCategory==Category.BossRelics?RelicEntries():new List<BossItemInventory.Item>();foreach(var item in bossItems)icons.Add(item.Icon);
        var questEntries=PresentedEntries();foreach(var entry in questEntries)icons.Add(entry.icon);
        if(questEntries.Count>0&&!questEntries.Exists(e=>e.id==inspectedQuestEntry)&&owned.Count==0&&bossItems.Count==0)inspectedQuestEntry=questEntries[0].id;
        var questEntry=questEntries.Find(e=>e.id==inspectedQuestEntry);BuildCategoryTabs();
        grid.SetItems(icons);
        if(grid.emptyMessage!=null){var empty=grid.emptyMessage.GetComponent<Text>();if(empty!=null)empty.text=CurrentCategory==Category.Support?"Chưa có vật phẩm.":"Chưa có cổ vật cường địch.";}
        var headingLabel=transform.Find("BagHeading")?.GetComponent<Text>();if(headingLabel!=null)headingLabel.text=CategoryTitles[(int)CurrentCategory].ToUpperInvariant();
        if(!string.IsNullOrEmpty(inspectedBossItem) && !bossItems.Exists(i=>i.Id==inspectedBossItem))inspectedBossItem=null;
        if(string.IsNullOrEmpty(inspectedQuestEntry) && string.IsNullOrEmpty(inspectedBossItem) && !ConsumableCatalog.Valid(inspected))
        {if(owned.Count>0)inspected=owned[0].kind;else if(bossItems.Count>0)inspectedBossItem=bossItems[0].Id;}
        var font=selectionLabel!=null?selectionLabel.font:GameFont.Current;
        for(int i=0;i<grid.scrollRect.content.childCount;i++)
        {
            var cell=grid.scrollRect.content.GetChild(i);var existing=cell.Find("Consumable count");
            if(existing!=null)existing.gameObject.SetActive(false);
            bool selectedCell=i<owned.Count?string.IsNullOrEmpty(inspectedQuestEntry)&&owned[i].kind==inspected:i<owned.Count+bossItems.Count?bossItems[i-owned.Count].Id==inspectedBossItem:i<icons.Count&&questEntries[i-owned.Count-bossItems.Count].id==inspectedQuestEntry;
            var button=cell.GetComponent<Button>();if(button==null)button=cell.gameObject.AddComponent<Button>();button.onClick.RemoveAllListeners();button.interactable=i<icons.Count;
            // The old side-tab sprite has an asymmetric silhouette; use a full square cell.
            var cellImage=cell.GetComponent<Image>();
            if(cellImage!=null)
            {
                cellImage.sprite=null;cellImage.type=Image.Type.Simple;cellImage.preserveAspect=false;
                cellImage.color=selectedCell?new Color(.50f,.34f,.16f,.85f):new Color(.35f,.22f,.10f,.65f);
            }
            var outline=cell.GetComponent<Outline>();if(outline==null)outline=cell.gameObject.AddComponent<Outline>();
            outline.effectColor=new Color(1f,.85f,.35f,1);outline.effectDistance=new Vector2(2,-2);
            outline.enabled=false;
            DrawSelection(cell,selectedCell);PixelMenuButtonStyle.Select(button,selectedCell);
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.92f,.7f);colors.selectedColor=Color.white;button.colors=colors;
            if(i>=icons.Count)continue;
            var hit=cell.GetComponent<Image>();if(hit!=null){hit.raycastTarget=true;button.targetGraphic=hit;}
            var icon=cell.Find("Icon")?.GetComponent<Image>();if(icon!=null){CenterIcon(icon.rectTransform);icon.preserveAspect=true;icon.raycastTarget=true;}
            var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
            if(i<owned.Count){var item=owned[i];button.onClick.AddListener(()=>SelectItem(item.kind));}
            else if(i<owned.Count+bossItems.Count){var item=bossItems[i-owned.Count];button.onClick.AddListener(()=>SelectBossItem(item.Id));}
            else {var entry=questEntries[i-owned.Count-bossItems.Count];button.onClick.AddListener(()=>SelectDefinition(entry.id));}
        }

        if(selectionLabel!=null)
        {
            selectionLabel.color=darkTheme?new Color(.9f,.88f,.80f):new Color(.25f,.16f,.08f);
            selectionLabel.fontSize=FontSize(16);selectionLabel.alignment=TextAnchor.UpperLeft;
            selectionLabel.rectTransform.sizeDelta=new Vector2(230,130);
            selectionLabel.verticalOverflow=VerticalWrapMode.Truncate;
            var position=selectionLabel.rectTransform.anchoredPosition;position.y=15;selectionLabel.rectTransform.anchoredPosition=position;
            if(string.IsNullOrEmpty(inspectedBossItem) && !ConsumableCatalog.Valid(inspected) && owned.Count>0)inspected=owned[0].kind;
            var selected=CurrentCategory==Category.Support?catalog.Find(inspected):null;
            var bossItem=bossItems.Find(i=>i.Id==inspectedBossItem);
            selectionLabel.text=questEntry!=null?questEntry.title+"\n\n"+questEntry.description:bossItem!=null?bossItem.DisplayName+"\nCổ vật cường địch · Có: "+bossItem.Quantity+"\n\n"+bossItem.Description:selected!=null?selected.displayName+"\nCó: "+GameEconomy.ConsumableCount(inspected)+"\n\n"+selected.description:CurrentCategory==Category.BossRelics?"Cổ vật cường địch\n\nHạ cường địch để nhận cổ vật và lưu giữ dấu ấn chiến thắng.":"Chọn vật phẩm ở trang bên trái.";
            LayoutQuestDetails(questEntry!=null);
            var equip=MakeControl("Equip quick item",new Vector2(0,-140),new Vector2(230,30));
            var custom=questEntry?.definition;
            bool customUsable=custom!=null&&custom.CanUse&&custom.itemId!=ConsumableCatalog.ShrineReturnItemId;
            bool returnCharm=CurrentCategory==Category.Support && inspectedQuestEntry=="item:"+ConsumableCatalog.ShrineReturnItemId;
            var equipKind=returnCharm?ConsumableKind.ShrineReturn:inspected;
            if(questEntry!=null)
            {
                Place((RectTransform)equip.transform,new Vector2(0,-selectionLabel.preferredHeight-12),new Vector2(230,30));
                selectionLabel.rectTransform.sizeDelta=new Vector2(0,Mathf.Max(340,selectionLabel.preferredHeight+160));
            }
            equip.SetActive(customUsable || returnCharm || CurrentCategory==Category.Support&&questEntry==null&&ConsumableCatalog.CanEquip(inspected));
            var equipButton=equip.GetComponent<Button>();if(equipButton==null)equipButton=equip.AddComponent<Button>();
            var background=equip.GetComponent<Image>();if(background==null)background=equip.AddComponent<Image>();background.color=new Color(.35f,.22f,.10f,.3f);background.raycastTarget=true;equipButton.targetGraphic=background;
            var equipText=MakeLabel(equip.transform,"Label",Vector2.zero,new Vector2(230,30),font);
            equipText.alignment=TextAnchor.MiddleCenter;
            equipText.text=(customUsable?GameEconomy.IsCustomEquipped(custom.itemId):GameEconomy.QuickItemIndex(equipKind)>=0)?"Tháo khỏi dùng nhanh":"Trang bị dùng nhanh";
            equipButton.onClick.RemoveAllListeners();equipButton.interactable=customUsable || (returnCharm || selected!=null) && (GameEconomy.ConsumableCount(equipKind)>0 || GameEconomy.QuickItemIndex(equipKind)>=0);
            equipButton.onClick.AddListener(()=>{if(customUsable)ToggleDefinition(custom);else ToggleEquipped(equipKind);});PixelMenuButtonStyle.Apply(equipButton);
            var heading=MakeLabel(selectionLabel.transform,"Quick heading",new Vector2(0,-180),new Vector2(230,22),font);
            Place(heading.rectTransform,new Vector2(0,questEntry!=null?-selectionLabel.preferredHeight-60:-180),new Vector2(230,22));
            heading.gameObject.SetActive(CurrentCategory==Category.Support);
            heading.text=string.IsNullOrEmpty(notice)?"DÙNG NHANH   "+GameEconomy.QuickItemCount+" / 5":notice;
            var root=selectionLabel.transform.Find("Quick items");
            if(root==null){var panel=new GameObject("Quick items",typeof(RectTransform));panel.transform.SetParent(selectionLabel.transform,false);root=panel.transform;var rt=(RectTransform)root;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(0,-218);rt.sizeDelta=new Vector2(230,48);}
            Place((RectTransform)root,new Vector2(0,questEntry!=null?-selectionLabel.preferredHeight-104:-224),new Vector2(230,42));
            root.gameObject.SetActive(CurrentCategory==Category.Support);
            for(int i=0;i<5;i++)
            {
                var child=root.Find("Quick "+i);GameObject slot;
                if(child==null){slot=new GameObject("Quick "+i,typeof(RectTransform),typeof(Image),typeof(Button));slot.transform.SetParent(root,false);}else slot=child.gameObject;
                var rt=slot.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(i*46,0);rt.sizeDelta=new Vector2(42,42);
                var kind=GameEconomy.QuickItemAt(i);var definition=catalog.Find(kind);var customQuick=GameItemDefinition.FindItem(GameEconomy.CustomQuickItemAt(i-GameEconomy.LegacyQuickItemCount));var image=slot.GetComponent<Image>();image.sprite=null;image.color=new Color(.35f,.22f,.10f,.28f);
                var iconChild=slot.transform.Find("Icon");var iconObject=iconChild!=null?iconChild.gameObject:new GameObject("Icon",typeof(RectTransform),typeof(Image));
                if(iconChild==null)iconObject.transform.SetParent(slot.transform,false);
                CenterIcon((RectTransform)iconObject.transform);var quickIcon=iconObject.GetComponent<Image>();quickIcon.sprite=customQuick!=null?customQuick.icon:definition!=null?definition.icon:null;quickIcon.enabled=definition!=null||customQuick!=null;quickIcon.preserveAspect=true;quickIcon.raycastTarget=false;
                DrawSelection(slot.transform,definition!=null && kind==inspected);
                var button=slot.GetComponent<Button>();PixelMenuButtonStyle.Select(button,definition!=null && kind==inspected);button.onClick.RemoveAllListeners();button.interactable=definition!=null||customQuick!=null;if(customQuick!=null)button.onClick.AddListener(()=>ToggleDefinition(customQuick));else if(definition!=null)button.onClick.AddListener(()=>{GameEconomy.ToggleQuickItem(kind);MenuSFX.Play(MenuSFX.Kind.Back);});
            }
        }
    }
    private void BuildCategoryTabs()
    {
        var root=transform.Find("Inventory category tabs");
        if(root==null)
        {
            var go=new GameObject("Inventory category tabs",typeof(RectTransform));go.transform.SetParent(transform,false);root=go.transform;
            var rect=(RectTransform)root;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,205);rect.sizeDelta=new Vector2(730,38);
        }
        var font=selectionLabel!=null?selectionLabel.font:GameFont.Current;
        for(int i=0;i<CategoryTitles.Length;i++)
        {
            int index=i;var existing=root.Find("Category "+i);
            var go=existing!=null?existing.gameObject:new GameObject("Category "+i,typeof(RectTransform),typeof(Image),typeof(Button));
            if(existing==null)go.transform.SetParent(root,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2((i-.5f)*366,0);rect.sizeDelta=new Vector2(354,36);
            var image=go.GetComponent<Image>();image.color=i==(int)CurrentCategory?new Color(.48f,.36f,.19f,.9f):new Color(.16f,.14f,.10f,.8f);image.raycastTarget=true;
            var button=go.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
            var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>SelectCategory((Category)index));
            var text=MakeLabel(go.transform,"Label",new Vector2(4,-3),new Vector2(346,30),font);text.fontSize=FontSize(14);text.alignment=TextAnchor.MiddleCenter;
            text.text=CategoryTitles[i].ToUpperInvariant();text.color=i==(int)CurrentCategory?new Color(1,.88f,.57f):new Color(.82f,.78f,.68f);
            DrawSelection(go.transform,i==(int)CurrentCategory);PixelMenuButtonStyle.Select(button,i==(int)CurrentCategory);
            if(categoryButtons.Count<=i)categoryButtons.Add(button);else categoryButtons[i]=button;
        }
        root.SetAsLastSibling();
    }
    private int FontSize(int size)=>selectionLabel!=null && selectionLabel.font!=null && selectionLabel.font.name.StartsWith("VT323")?Mathf.RoundToInt(size*1.25f):size;
    private static void CenterIcon(RectTransform rect)
    {
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
        rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
        rect.sizeDelta=new Vector2(32,32);rect.localScale=Vector3.one;
    }
    private static void DrawSelection(Transform parent,bool selected)
    {
        var root=parent.Find("Selection frame");
        if(root==null){var go=new GameObject("Selection frame",typeof(RectTransform));go.transform.SetParent(parent,false);root=go.transform;var rect=(RectTransform)root;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        root.gameObject.SetActive(selected);if(!selected)return;
        for(int i=0;i<4;i++)
        {
            var existing=root.Find("Edge "+i);var go=existing!=null?existing.gameObject:new GameObject("Edge "+i,typeof(RectTransform),typeof(Image));if(existing==null)go.transform.SetParent(root,false);
            var rect=(RectTransform)go.transform;
            rect.anchorMin=i==0?new Vector2(0,1):i==1?Vector2.zero:i==2?Vector2.zero:new Vector2(1,0);
            rect.anchorMax=i==0?Vector2.one:i==1?new Vector2(1,0):i==2?new Vector2(0,1):Vector2.one;
            rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=i<2?new Vector2(0,3):new Vector2(3,0);
            var image=go.GetComponent<Image>();image.color=new Color(1,.88f,.40f);image.raycastTarget=false;
            var shadow=go.GetComponent<Shadow>();if(shadow==null)shadow=go.AddComponent<Shadow>();shadow.effectColor=new Color(.2f,.10f,.03f,.9f);shadow.effectDistance=new Vector2(1,-1);
        }
        root.SetAsLastSibling();
    }
    private GameObject MakeControl(string name,Vector2 position,Vector2 size)
    {
        var existing=selectionLabel.transform.Find(name);
        var go=existing!=null?existing.gameObject:new GameObject(name,typeof(RectTransform));
        if(existing==null)go.transform.SetParent(selectionLabel.transform,false);
        Place((RectTransform)go.transform,position,size);return go;
    }
    private static void Place(RectTransform rect,Vector2 position,Vector2 size)
    {rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;}
    private Text MakeLabel(Transform parent,string name,Vector2 position,Vector2 size,Font font)
    {
        var existing=parent.Find(name);var go=existing!=null?existing.gameObject:new GameObject(name,typeof(RectTransform),typeof(Text));
        if(existing==null)go.transform.SetParent(parent,false);Place((RectTransform)go.transform,position,size);
        var text=go.GetComponent<Text>();text.font=font;text.fontSize=FontSize(15);text.color=darkTheme?new Color(.9f,.88f,.80f):new Color(.25f,.16f,.08f);text.raycastTarget=false;return text;
    }

}



