using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace World
{
    public sealed partial class NpcInteractable
    {
        [Header("Branching conversation (optional; overrides dialogueLines)")]
        public DialogueDefinition dialogue;
        public string CurrentNodeId { get; private set; }
        public int SelectedChoice { get; private set; }
        public IReadOnlyList<DialogueDefinition.Choice> VisibleChoices=>choices;
        private readonly List<DialogueDefinition.Choice> choices=new List<DialogueDefinition.Choice>();
        private readonly List<Image> choiceRows=new List<Image>();
        private GameObject ownedDialogueEvents,priorDialogueSelection;
        private RectTransform choicesContent;
        private ScrollRect choiceScroll;
        private int lastChoiceFrame=-1;
        private bool enteringNode;
        private bool HasDialogueContent=>dialogue!=null?dialogue.Find(dialogue.ResolveStartNodeId())!=null:dialogueLines!=null&&dialogueLines.Length>0;
        private void BeginConversation()
        {
            bool autoAccepted=dialogue.autoAcceptQuest!=null&&QuestService.Status(dialogue.autoAcceptQuest)==QuestStatus.Available&&QuestService.Accept(dialogue.autoAcceptQuest);
            dialogue.TurnInReadyQuests();
            CurrentNodeId=dialogue.ResolveStartNodeId();lastChoiceFrame=-1;
            if(autoAccepted&&QuestService.Status(dialogue.autoAcceptQuest)==QuestStatus.Active&&dialogue.Find(dialogue.autoAcceptedStartNodeId)!=null)
                CurrentNodeId=dialogue.autoAcceptedStartNodeId;
            QuestService.Changed+=OnQuestChanged;
            var panel=view.transform.Find("Dialogue panel").GetComponent<RectTransform>();panel.anchorMin=new Vector2(.12f,.07f);panel.anchorMax=new Vector2(.88f,.56f);
            SetAnchors(body.rectTransform,new Vector2(.06f,.57f),new Vector2(.94f,.80f));
            SetAnchors(hint.rectTransform,new Vector2(.06f,.04f),new Vector2(.94f,.12f));hint.fontSize=16;
            var title=panel.Find("Speaker").GetComponent<RectTransform>();SetAnchors(title,new Vector2(.06f,.82f),new Vector2(.94f,.95f));
            var arrow=panel.Find("Continue arrow");if(arrow!=null)arrow.gameObject.SetActive(false);
            view.AddComponent<GraphicRaycaster>();
            if(EventSystem.current==null){ownedDialogueEvents=new GameObject("Dialogue EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));ownedDialogueEvents.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            priorDialogueSelection=EventSystem.current.currentSelectedGameObject;EventSystem.current.SetSelectedGameObject(null);
            var scrolling=new GameObject("Choices",typeof(RectTransform),typeof(ScrollRect));scrolling.transform.SetParent(panel,false);SetAnchors(scrolling.GetComponent<RectTransform>(),new Vector2(.06f,.15f),new Vector2(.94f,.55f));
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask));viewport.transform.SetParent(scrolling.transform,false);SetAnchors(viewport.GetComponent<RectTransform>(),Vector2.zero,Vector2.one);viewport.GetComponent<Mask>().showMaskGraphic=false;
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport.transform,false);choicesContent=content.GetComponent<RectTransform>();choicesContent.anchorMin=new Vector2(0,1);choicesContent.anchorMax=Vector2.one;choicesContent.pivot=new Vector2(.5f,1);
            choiceScroll=scrolling.GetComponent<ScrollRect>();choiceScroll.viewport=viewport.GetComponent<RectTransform>();choiceScroll.content=choicesContent;choiceScroll.horizontal=false;choiceScroll.movementType=ScrollRect.MovementType.Clamped;
            EnterConversationNode(CurrentNodeId);
        }
        private void EnterConversationNode(string id)
        {
            CurrentNodeId=id;enteringNode=true;
            try{var node=dialogue.Find(id);if(node!=null){if(node.completeQuestWithoutReward!=null)QuestService.CompleteWithoutReward(node.completeQuestWithoutReward);if(node.acknowledgeQuestCompletion!=null)QuestService.AcknowledgeCompletionDialogue(node.acknowledgeQuestCompletion);}}
            finally{enteringNode=false;}
            RefreshConversation();
        }
        private static void SetAnchors(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        private void RefreshConversation()
        {
            var node=dialogue.Find(CurrentNodeId);if(node==null){RequestClose();return;}
            body.text=node.ResolvedText;
            view.transform.Find("Dialogue panel/Speaker").GetComponent<Text>().text=string.IsNullOrEmpty(node.speaker)?displayName:node.speaker;
            choices.Clear();choiceRows.Clear();SelectedChoice=0;
            foreach(Transform child in choicesContent){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            foreach(var choice in node.choices)if(choice!=null&&choice.IsVisible)choices.Add(choice);
            choicesContent.sizeDelta=new Vector2(0,choices.Count*44);
            for(int i=0;i<choices.Count;i++)
            {
                int index=i;var row=new GameObject("Choice "+i,typeof(RectTransform),typeof(Image),typeof(Button));row.transform.SetParent(choicesContent,false);
                var rect=row.GetComponent<RectTransform>();rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-i*44);rect.sizeDelta=new Vector2(0,40);
                var image=row.GetComponent<Image>();choiceRows.Add(image);var button=row.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;var nav=button.navigation;nav.mode=Navigation.Mode.None;button.navigation=nav;button.onClick.AddListener(()=>Choose(index));PixelMenuButtonStyle.Apply(button);
                var label=MakeText(row.transform,"Answer",new Vector2(.025f,0),new Vector2(.98f,1),20);label.text=choices[i].text;
                var events=row.AddComponent<EventTrigger>();var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};enter.callback.AddListener(_=>SelectChoice(index));events.triggers.Add(enter);
            }
            choicesContent.anchoredPosition=Vector2.zero;
            var interaction=GameControls.Display(GameControls.Action.Interact);
            hint.text=choices.Count>0?"↑ ↓ / W S: Chọn đáp án    "+interaction+" / Enter: Xác nhận    Esc: Rời đi":interaction+" / Enter: Tiếp tục    Esc: Rời đi";
            PaintChoices();
        }
        public void SelectChoice(int index)
        {
            if(!IsOpen||IsClosing||index<0||index>=choices.Count)return;
            SelectedChoice=index;PaintChoices();
            float top=index*44;float height=choiceScroll.viewport.rect.height;float current=choicesContent.anchoredPosition.y;
            if(top<current)current=top;else if(top+44>current+height)current=top+44-height;
            choicesContent.anchoredPosition=new Vector2(0,Mathf.Clamp(current,0,Mathf.Max(0,choices.Count*44-height)));
        }
        private void PaintChoices(){for(int i=0;i<choiceRows.Count;i++)PixelMenuButtonStyle.Select(choiceRows[i].GetComponent<Button>(),i==SelectedChoice);}
        private void HandleChoiceInput()
        {
            var kb=Keyboard.current;
            if(choices.Count>0){if(kb.upArrowKey.wasPressedThisFrame||kb.wKey.wasPressedThisFrame){SelectChoice((SelectedChoice+choices.Count-1)%choices.Count);MenuSFX.Play(MenuSFX.Kind.Move);}if(kb.downArrowKey.wasPressedThisFrame||kb.sKey.wasPressedThisFrame){SelectChoice((SelectedChoice+1)%choices.Count);MenuSFX.Play(MenuSFX.Kind.Move);}}
            if(GameControls.Pressed(GameControls.Action.Interact)||kb.enterKey.wasPressedThisFrame||kb.spaceKey.wasPressedThisFrame)AdvanceConversation();
        }
        private void AdvanceConversation()
        {
            if(choices.Count>0){Choose(SelectedChoice);return;}
            var node=dialogue.Find(CurrentNodeId);if(node==null||string.IsNullOrEmpty(node.nextNodeId)){RequestClose();return;}
            EnterConversationNode(node.nextNodeId);
        }
        public bool Choose(int index)
        {
            if(dialogue==null||!IsOpen||IsClosing||IsShopping||Time.frameCount==lastChoiceFrame||index<0||index>=choices.Count)return false;
            var choice=choices[index];if(!choice.IsVisible)return false;
            if(!string.IsNullOrEmpty(choice.nextNodeId)&&dialogue.Find(choice.nextNodeId)==null){actor.ShowNotice("Hội thoại chưa cấu hình nút: "+choice.nextNodeId);return false;}
            bool accepted=choice.action==DialogueAction.AcceptQuest?QuestService.Accept(choice.quest):choice.action==DialogueAction.ReportQuest?QuestService.Status(choice.quest)==QuestStatus.Completed||QuestService.TurnIn(choice.quest):
                choice.action==DialogueAction.ChallengeBoss?GetComponentInParent<QuestBossEncounter>()?.RequestChallenge()==true:true;
            if(!accepted){actor.ShowNotice("Chưa thể thực hiện lựa chọn này. Kiểm tra yêu cầu và phần thưởng.");RefreshConversation();return false;}
            lastChoiceFrame=Time.frameCount;MenuSFX.Play(MenuSFX.Kind.Confirm);
            if(choice.action==DialogueAction.End||choice.action==DialogueAction.ChallengeBoss){RequestClose();return true;}
            if(choice.action==DialogueAction.OpenShop){if(shop!=null&&shop.enabled)shop.Open(actor,view,this);else actor.ShowNotice("NPC chưa có cửa hàng.");return shop!=null&&shop.IsOpen;}
            if(string.IsNullOrEmpty(choice.nextNodeId)){RequestClose();return true;}
            EnterConversationNode(choice.nextNodeId);return true;
        }
        private void CloseConversationView()
        {
            QuestService.Changed-=OnQuestChanged;
            choices.Clear();choiceRows.Clear();CurrentNodeId=null;
            if(EventSystem.current!=null&&priorDialogueSelection!=null)EventSystem.current.SetSelectedGameObject(priorDialogueSelection);
            priorDialogueSelection=null;if(ownedDialogueEvents!=null)Destroy(ownedDialogueEvents);ownedDialogueEvents=null;
        }
        private void OnQuestChanged(){if(!enteringNode&&IsOpen&&!IsShopping&&dialogue!=null)RefreshConversation();}
    }
}
