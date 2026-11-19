using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GraphNode = UnityEditor.Experimental.GraphView.Node;

public sealed class DialogueGraphWindow : EditorWindow
{
    [SerializeField] DialogueDefinition conversation;
    [SerializeField] string inspected="start";
    DialogueView graph;
    IMGUIContainer details;
    Label status;
    bool rebuildQueued;
    Vector2 detailScroll;
    string search="";
    [SerializeField] bool showLibrary;
    [SerializeField] string librarySearch="";
    ScrollView libraryList;
    Label libraryCount;

    [MenuItem("Tools/Dialogue and Quests/Dialogue Graph")]
    public static void OpenMenu()=>Open(Selection.activeObject as DialogueDefinition);
    [MenuItem("Assets/Open in Dialogue Graph",true)] static bool CanOpen()=>Selection.activeObject is DialogueDefinition;
    [MenuItem("Assets/Open in Dialogue Graph")] static void OpenSelected()=>Open(Selection.activeObject as DialogueDefinition);
    [OnOpenAsset(1)] static bool OpenAsset(int instanceId,int line)
    {var asset=EditorUtility.InstanceIDToObject(instanceId) as DialogueDefinition;if(asset==null)return false;Open(asset);return true;}
    public static DialogueGraphWindow Open(DialogueDefinition asset)
    {
        var window=GetWindow<DialogueGraphWindow>("Dialogue Graph");window.minSize=new Vector2(1000,620);
        if(asset!=null)window.SetAsset(asset);window.Show();return window;
    }
    void OnEnable(){Undo.undoRedoPerformed+=OnUndo;EditorApplication.playModeStateChanged+=OnPlayMode;EditorApplication.projectChanged+=RefreshLibrary;rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown,TrickleDown.TrickleDown);BuildUI();}
    void OnDisable(){Undo.undoRedoPerformed-=OnUndo;EditorApplication.playModeStateChanged-=OnPlayMode;EditorApplication.projectChanged-=RefreshLibrary;rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown,TrickleDown.TrickleDown);}
    void OnKeyDown(KeyDownEvent e){if((e.ctrlKey||e.commandKey)&&e.keyCode==KeyCode.S){Save();e.StopPropagation();}}
    void OnUndo(){DialogueGraphLayouts.instance.Persist();QueueRebuild();}
    void OnPlayMode(PlayModeStateChange state)=>QueueRebuild();
    void OnSelectionChange(){if(Selection.activeObject is DialogueDefinition asset&&asset!=conversation)SetAsset(asset);}
    public void SetAsset(DialogueDefinition asset){conversation=asset;showLibrary=false;inspected="start";BuildUI();}
    bool Editable=>conversation!=null&&!EditorApplication.isPlayingOrWillChangePlaymode;

    void BuildUI()
    {
        rootVisualElement.Clear();
        var toolbar=new Toolbar();rootVisualElement.Add(toolbar);
        var picker=new ObjectField{objectType=typeof(DialogueDefinition),allowSceneObjects=false,value=conversation};picker.style.width=230;
        picker.RegisterValueChangedCallback(e=>SetAsset(e.newValue as DialogueDefinition));toolbar.Add(picker);
        toolbar.Add(new ToolbarButton(NewConversation){text="Tạo hội thoại"});
        toolbar.Add(new ToolbarButton(Save){text="Lưu · Ctrl+S"});
        var spacer=new VisualElement();spacer.style.flexGrow=1;toolbar.Add(spacer);
        var find=new ToolbarSearchField{value=search};find.style.width=160;find.RegisterValueChangedCallback(e=>search=e.newValue);toolbar.Add(find);
        toolbar.Add(new ToolbarButton(Find){text="Tìm"});
        var tabs=new Toolbar();rootVisualElement.Add(tabs);
        var graphTab=new ToolbarToggle{text="Đồ thị",value=!showLibrary};
        var libraryTab=new ToolbarToggle{text="Tất cả hội thoại",value=showLibrary};
        graphTab.RegisterValueChangedCallback(e=>{if(showLibrary){showLibrary=false;BuildUI();}else graphTab.SetValueWithoutNotify(true);});
        libraryTab.RegisterValueChangedCallback(e=>{if(!showLibrary){showLibrary=true;BuildUI();}else libraryTab.SetValueWithoutNotify(true);});
        tabs.Add(graphTab);tabs.Add(libraryTab);
        var actions=new Toolbar();rootVisualElement.Add(actions);
        actions.style.display=showLibrary?DisplayStyle.None:DisplayStyle.Flex;
        actions.Add(new ToolbarButton(()=>AddLine(new Vector2(500,100))){text="+ Bảng thoại"});
        actions.Add(new ToolbarButton(AddRule){text="+ Điều kiện vào"});
        actions.Add(new ToolbarButton(()=>{if(Editable){graph.AutoLayout();RefreshStatus();}}){text="Sắp xếp"});
        actions.Add(new ToolbarButton(()=>graph?.FrameAll()){text="Xem toàn bộ"});
        var body=new VisualElement();body.style.flexDirection=FlexDirection.Row;body.style.flexGrow=1;rootVisualElement.Add(body);
        body.style.display=showLibrary?DisplayStyle.None:DisplayStyle.Flex;
        graph=new DialogueView(this);graph.style.flexGrow=1;body.Add(graph);
        details=new IMGUIContainer(DrawDetails);details.style.width=340;details.style.flexShrink=0;details.style.backgroundColor=new Color(.19f,.19f,.19f);body.Add(details);
        var library=new VisualElement();library.style.flexGrow=1;library.style.display=showLibrary?DisplayStyle.Flex:DisplayStyle.None;rootVisualElement.Add(library);
        var libraryToolbar=new Toolbar();library.Add(libraryToolbar);
        var libraryFind=new ToolbarSearchField{value=librarySearch};libraryFind.style.flexGrow=1;
        libraryFind.RegisterValueChangedCallback(e=>{librarySearch=e.newValue;RefreshLibrary();});libraryToolbar.Add(libraryFind);
        libraryToolbar.Add(new ToolbarButton(RefreshLibrary){text="Làm mới"});
        libraryCount=new Label();libraryCount.style.paddingLeft=10;libraryCount.style.paddingTop=8;libraryCount.style.paddingBottom=8;library.Add(libraryCount);
        libraryList=new ScrollView();libraryList.style.flexGrow=1;library.Add(libraryList);RefreshLibrary();
        status=new Label();status.style.whiteSpace=WhiteSpace.Normal;status.style.paddingLeft=8;status.style.paddingTop=5;status.style.paddingBottom=5;rootVisualElement.Add(status);
        graph.Rebuild();RefreshStatus();if(!showLibrary)graph.schedule.Execute(()=>graph.FrameAll()).ExecuteLater(120);
    }
    public static DialogueDefinition[] ProjectDialogues(string filter="")
    {
        return AssetDatabase.FindAssets("t:DialogueDefinition",new[]{"Assets"})
            .Select(g=>AssetDatabase.LoadAssetAtPath<DialogueDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a=>a!=null&&(string.IsNullOrWhiteSpace(filter)||(a.name+" "+AssetDatabase.GetAssetPath(a)).IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0))
            .OrderBy(a=>a.name,StringComparer.OrdinalIgnoreCase).ThenBy(AssetDatabase.GetAssetPath).ToArray();
    }
    public void ShowLibrary(){showLibrary=true;BuildUI();}
    void RefreshLibrary()
    {
        if(libraryList==null)return;libraryList.Clear();var assets=ProjectDialogues(librarySearch);
        libraryCount.text=assets.Length+" hội thoại"+(string.IsNullOrWhiteSpace(librarySearch)?" trong dự án":" phù hợp tìm kiếm");
        foreach(var asset in assets)
        {
            var row=new VisualElement();row.style.marginLeft=10;row.style.marginRight=10;row.style.marginBottom=8;row.style.paddingLeft=8;row.style.paddingRight=8;row.style.paddingTop=8;row.style.paddingBottom=8;
            row.style.backgroundColor=asset==conversation?new Color(.18f,.26f,.32f):new Color(.19f,.19f,.19f);
            var line=new VisualElement();line.style.flexDirection=FlexDirection.Row;row.Add(line);
            var open=new UnityEngine.UIElements.Button(()=>SetAsset(asset)){text=asset.name+(EditorUtility.IsDirty(asset)?" *":"")};open.style.flexGrow=1;open.style.height=30;line.Add(open);
            line.Add(new UnityEngine.UIElements.Button(()=>EditorGUIUtility.PingObject(asset)){text="Tìm asset"});
            var path=new Label(AssetDatabase.GetAssetPath(asset));path.style.whiteSpace=WhiteSpace.Normal;path.style.color=new Color(.7f,.75f,.8f);row.Add(path);
            row.Add(new Label((asset.nodes?.Count??0)+" bảng thoại · "+(asset.entryRules?.Count??0)+" điều kiện vào"));libraryList.Add(row);
        }
        if(assets.Length==0)libraryList.Add(new Label("Không tìm thấy hội thoại. Có thể bấm Tạo hội thoại ở thanh trên."));
    }
    public void QueueRebuild()
    {
        if(rebuildQueued)return;rebuildQueued=true;
        EditorApplication.delayCall+=()=>{if(this==null)return;rebuildQueued=false;graph?.Rebuild();RefreshStatus();details?.MarkDirtyRepaint();};
    }
    public void Inspect(string key){inspected=key;details?.MarkDirtyRepaint();}
    void RefreshStatus()
    {
        if(status==null)return;
        var errors=DialogueGraphAuthoring.Validate(conversation);
        status.text=conversation==null?"Chọn asset hội thoại hoặc tạo mới.":!Editable?"Dừng Play để chỉnh sửa hội thoại.":errors.Count>0?
            errors.Count+" vấn đề · "+errors[0]:"Kéo dây từ chấm bên phải sang chấm bên trái · Chuột giữa để kéo nền · Cuộn để zoom · Delete xóa bảng/dây · Ctrl+Z hoàn tác";
        status.style.color=errors.Count>0?new Color(1,.74f,.42f):new Color(.8f,.84f,.83f);
    }
    void Save()
    {
        if(!Editable)return;AssetDatabase.SaveAssetIfDirty(conversation);DialogueGraphLayouts.instance.Persist();RefreshStatus();
    }
    void NewConversation()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        string path=EditorUtility.SaveFilePanelInProject("Tạo hội thoại","Conversation","asset","Chọn nơi lưu hội thoại.","Assets/Dialogue");
        if(string.IsNullOrEmpty(path))return;SetAsset(QuestAuthoring.CreateDialogue(path,"NPC"));
    }
    public void AddLine(Vector2 position)
    {
        if(!Editable)return;var node=DialogueGraphAuthoring.AddNode(conversation);
        DialogueGraphLayouts.instance.Set(conversation,"node:"+node.id,position);inspected="node:"+node.id;QueueRebuild();
    }
    void AddRule()
    {
        if(!Editable)return;
        DialogueGraphAuthoring.Edit(conversation,"Thêm điều kiện vào thoại",()=>
        {if(conversation.entryRules==null)conversation.entryRules=new List<DialogueDefinition.EntryRule>();conversation.entryRules.Add(new DialogueDefinition.EntryRule());});
        inspected="rule:"+(conversation.entryRules.Count-1);QueueRebuild();
    }
    void Find()
    {
        if(graph==null||conversation==null||string.IsNullOrWhiteSpace(search))return;
        var node=graph.nodes.OfType<DialogueVisual>().FirstOrDefault(n=>n.Key.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0||n.title.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0||
            n.Line!=null&&(n.Line.text??"").IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0);
        if(node==null)return;graph.ClearSelection();graph.AddToSelection(node);Inspect(node.Key);graph.FrameSelection();
    }
    void DrawDetails()
    {
        using(var scroll=new EditorGUILayout.ScrollViewScope(detailScroll))
        {
            detailScroll=scroll.scrollPosition;
            if(conversation==null){EditorGUILayout.HelpBox("Chọn asset ở thanh trên. Có thể nhấp đúp asset hội thoại để mở đồ thị.",MessageType.Info);return;}
            EditorGUILayout.LabelField(conversation.name,EditorStyles.boldLabel);
            using(new EditorGUI.DisabledScope(!Editable))
            {
                var data=new SerializedObject(conversation);data.Update();
                string before=Structure();
                if(inspected.StartsWith("node:"))
                {
                    var line=conversation.nodes?.Find(n=>n!=null&&"node:"+n.id==inspected);
                    if(line==null){inspected="start";return;}
                    int index=conversation.nodes.IndexOf(line);var p=data.FindProperty("nodes").GetArrayElementAtIndex(index);
                    string id=EditorGUILayout.DelayedTextField("ID",line.id);
                    if(id!=line.id)
                    {
                        try{var pos=graph.nodes.OfType<DialogueVisual>().FirstOrDefault(n=>n.Line==line)?.GetPosition().position??Vector2.zero;DialogueGraphAuthoring.Rename(conversation,line,id);DialogueGraphLayouts.instance.Set(conversation,"node:"+id,pos);inspected="node:"+id;QueueRebuild();}
                        catch(Exception e){status.text=e.Message;}
                        return;
                    }
                    Field(p,"speaker","Người nói");Field(p,"text","Lời thoại");Field(p,"textQuest","Nhiệm vụ điền nội dung");
                    Field(p,"acknowledgeQuestCompletion","Đã xem hết thoại hậu nhiệm vụ");
                    EditorGUILayout.HelpBox("Nhiệm vụ đủ mục tiêu sẽ tự trả và phát thưởng khi mở hội thoại với NPC giao nhiệm vụ. Trường đánh dấu này chỉ ghi nhận đã xem xong thoại. Có thể dùng {title}, {progress}, {rewards}, {status} khi đã gán nhiệm vụ.",MessageType.None);
                    Target(p.FindPropertyRelative("nextNodeId"),"Tiếp tục / dự phòng");
                    EditorGUILayout.Space();EditorGUILayout.LabelField("Lựa chọn trả lời",EditorStyles.boldLabel);
                    var choices=p.FindPropertyRelative("choices");
                    for(int i=0;i<choices.arraySize;i++)
                    {
                        using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            var c=choices.GetArrayElementAtIndex(i);
                            EditorGUILayout.LabelField("Lựa chọn "+(i+1),EditorStyles.boldLabel);
                            Field(c,"text","Câu trả lời");Field(c,"action","Hành động");
                            var action=(DialogueAction)c.FindPropertyRelative("action").enumValueIndex;
                            if(action==DialogueAction.AcceptQuest||action==DialogueAction.ReportQuest)Field(c,"quest","Nhận / trả nhiệm vụ");
                            Field(c,"conditionQuest","Điều kiện hiển thị");
                            if(c.FindPropertyRelative("conditionQuest").objectReferenceValue!=null)Field(c,"requiredStatus","Trạng thái yêu cầu");
                            if(action!=DialogueAction.End&&action!=DialogueAction.OpenShop&&action!=DialogueAction.ChallengeBoss)Target(c.FindPropertyRelative("nextNodeId"),"Đi đến");
                            else EditorGUILayout.LabelField(action==DialogueAction.End?"Kết thúc đối thoại":action==DialogueAction.ChallengeBoss?"Đóng thoại và chấp nhận thách đấu":"Mở cửa hàng của NPC",EditorStyles.miniLabel);
                            if(GUILayout.Button("Xóa lựa chọn")){data.ApplyModifiedProperties();int remove=i;DialogueGraphAuthoring.Edit(conversation,"Xóa lựa chọn thoại",()=>line.choices.RemoveAt(remove));QueueRebuild();return;}
                        }
                    }
                    if(GUILayout.Button("+ Lựa chọn")){data.ApplyModifiedProperties();DialogueGraphAuthoring.Edit(conversation,"Thêm lựa chọn thoại",()=>{if(line.choices==null)line.choices=new List<DialogueDefinition.Choice>();line.choices.Add(new DialogueDefinition.Choice{text="Câu trả lời mới"});});QueueRebuild();return;}
                }
                else if(inspected.StartsWith("rule:")&&int.TryParse(inspected.Substring(5),out int ruleIndex)&&conversation.entryRules!=null&&ruleIndex<conversation.entryRules.Count)
                {
                    var p=data.FindProperty("entryRules").GetArrayElementAtIndex(ruleIndex);
                    EditorGUILayout.LabelField("Điều kiện vào #"+(ruleIndex+1),EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Kiểm tra từ trên xuống. Quy tắc đầu tiên thỏa mọi điều kiện sẽ được dùng. Bỏ trống điều kiện không cần kiểm tra.",MessageType.Info);
                    Field(p,"encounterId","Boss Persistent Id");Field(p,"mustBeDefeated","Boss đã bị hạ");
                    Field(p,"quest","Nhiệm vụ");Field(p,"requiredStatus","Trạng thái nhiệm vụ");Field(p,"onlyBeforeCompletionDialogueSeen","Chưa xem hết thoại hậu nhiệm vụ");Target(p.FindPropertyRelative("startNodeId"),"Đi đến");
                    using(new EditorGUILayout.HorizontalScope())
                    {
                        if(GUILayout.Button("↑ Ưu tiên cao hơn")&&ruleIndex>0){data.ApplyModifiedProperties();MoveRule(ruleIndex,-1);return;}
                        if(GUILayout.Button("↓ Ưu tiên thấp hơn")&&ruleIndex<conversation.entryRules.Count-1){data.ApplyModifiedProperties();MoveRule(ruleIndex,1);return;}
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Bắt đầu hội thoại",EditorStyles.boldLabel);Target(data.FindProperty("startNodeId"),"Nhánh mặc định");
                    EditorGUILayout.PropertyField(data.FindProperty("autoTurnInQuests"),new GUIContent("Tự trả khi nói chuyện"),true);
                    EditorGUILayout.PropertyField(data.FindProperty("autoAcceptQuest"),new GUIContent("Tự nhận nhiệm vụ"));
                    if(data.FindProperty("autoAcceptQuest").objectReferenceValue!=null)Target(data.FindProperty("autoAcceptedStartNodeId"),"Vừa nhận, chưa đủ mục tiêu");
                    EditorGUILayout.HelpBox("Các bảng màu cam là điều kiện vào. Nếu không có điều kiện nào khớp, hội thoại đi theo dây Mặc định màu xanh.",MessageType.Info);
                    if(conversation.entryRules!=null)for(int i=0;i<conversation.entryRules.Count;i++)if(GUILayout.Button("Sửa điều kiện #"+(i+1)))Inspect("rule:"+i);
                }
                if(data.ApplyModifiedProperties())
                {if(before!=Structure())QueueRebuild();else graph.UpdateCaptions();RefreshStatus();}
            }
            EditorGUILayout.Space();
            foreach(var error in DialogueGraphAuthoring.Validate(conversation))EditorGUILayout.HelpBox(error,MessageType.Warning);
        }
    }
    void MoveRule(int index,int offset)
    {DialogueGraphAuthoring.Edit(conversation,"Đổi thứ tự điều kiện thoại",()=>{var r=conversation.entryRules[index];conversation.entryRules.RemoveAt(index);conversation.entryRules.Insert(index+offset,r);});inspected="rule:"+(index+offset);QueueRebuild();}
    static void Field(SerializedProperty p,string name,string label)=>EditorGUILayout.PropertyField(p.FindPropertyRelative(name),new GUIContent(label),true);
    void Target(SerializedProperty p,string label)
    {
        var ids=new List<string>{""};ids.AddRange((conversation.nodes??new List<DialogueDefinition.Node>()).Where(n=>n!=null).Select(n=>n.id));
        if(!ids.Contains(p.stringValue))ids.Add(p.stringValue);
        var names=ids.Select(id=>string.IsNullOrEmpty(id)?"(Kết thúc / chưa nối)":id).ToArray();
        p.stringValue=ids[EditorGUILayout.Popup(label,Mathf.Max(0,ids.IndexOf(p.stringValue)),names)];
    }
    string Structure()=>conversation==null?"":conversation.autoAcceptQuest+":"+conversation.autoAcceptedStartNodeId+"|"+conversation.startNodeId+"|"+string.Join(";",(conversation.nodes??new List<DialogueDefinition.Node>()).Where(n=>n!=null).Select(n=>n.id+">"+n.nextNodeId+":"+string.Join(",",(n.choices??new List<DialogueDefinition.Choice>()).Where(c=>c!=null).Select(c=>c.action+">"+c.nextNodeId))))+"|"+string.Join(";",(conversation.entryRules??new List<DialogueDefinition.EntryRule>()).Where(r=>r!=null).Select(r=>r.startNodeId+":"+r.encounterId+":"+r.mustBeDefeated+":"+r.quest+":"+r.requiredStatus+":"+r.onlyBeforeCompletionDialogueSeen));

    sealed class Binding
    {
        public DialogueDefinition.Node Line;
        public DialogueDefinition.Choice Choice;
        public DialogueDefinition.EntryRule Rule;
        public bool Default;
        public bool AutoAccepted;
        public string Target(DialogueDefinition asset)=>Default?asset.startNodeId:AutoAccepted?asset.autoAcceptedStartNodeId:Rule!=null?Rule.startNodeId:Choice!=null?Choice.nextNodeId:Line.nextNodeId;
        public void Set(DialogueDefinition asset,string value){if(Default)asset.startNodeId=value;else if(AutoAccepted)asset.autoAcceptedStartNodeId=value;else if(Rule!=null)Rule.startNodeId=value;else if(Choice!=null)Choice.nextNodeId=value;else if(Line!=null)Line.nextNodeId=value;}
    }
    sealed class DialogueVisual : GraphNode
    {
        public string Key;
        public DialogueDefinition.Node Line;
        public DialogueDefinition.EntryRule Rule;
        public TextField Text,Speaker;
        public Action<string> Selected;
        public override void OnSelected(){base.OnSelected();Selected?.Invoke(Key);}
    }
    sealed class DialogueView : GraphView
    {
        readonly DialogueGraphWindow owner;
        readonly Dictionary<string,Port> inputs=new Dictionary<string,Port>();
        readonly List<Port> outputs=new List<Port>();
        public DialogueView(DialogueGraphWindow window)
        {
            owner=window;SetupZoom(.15f,1.6f);this.AddManipulator(new ContentDragger());this.AddManipulator(new SelectionDragger());this.AddManipulator(new RectangleSelector());
            var grid=new GridBackground();Insert(0,grid);grid.StretchToParentSize();
            graphViewChanged=Changed;
        }
        public override List<Port> GetCompatiblePorts(Port start,NodeAdapter adapter)
        {return ports.Where(p=>p.direction!=start.direction&&(p.direction==Direction.Input?p.userData is string:start.userData is string)&&!(p.userData is bool)&&!(start.userData is bool)).ToList();}
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);var position=contentViewContainer.WorldToLocal(evt.mousePosition);
            evt.menu.AppendAction("Thêm bảng thoại",a=>owner.AddLine(position),owner.Editable?DropdownMenuAction.Status.Normal:DropdownMenuAction.Status.Disabled);
        }
        DialogueVisual Box(string key,string title,Vector2 fallback,Color color)
        {
            var node=new DialogueVisual{Key=key,title=title,Selected=owner.Inspect};node.style.width=310;node.titleContainer.style.backgroundColor=color;
            node.SetPosition(new Rect(DialogueGraphLayouts.instance.Get(owner.conversation,key,fallback),new Vector2(310,180)));AddElement(node);return node;
        }
        Port Output(DialogueVisual node,string name,Binding binding)
        {
            var p=node.InstantiatePort(Orientation.Horizontal,Direction.Output,Port.Capacity.Single,typeof(bool));p.portName=name;p.tooltip=name;p.userData=binding;node.outputContainer.Add(p);outputs.Add(p);return p;
        }
        public void Rebuild()
        {
            graphViewChanged=null;DeleteElements(graphElements.ToList());inputs.Clear();outputs.Clear();
            if(owner.conversation==null){graphViewChanged=Changed;return;}
            var asset=owner.conversation;
            var start=Box("start","BẮT ĐẦU",new Vector2(-420,0),new Color(.12f,.32f,.25f));
            start.capabilities&=~Capabilities.Deletable;
            var hint=new Label("Điều kiện khớp đầu tiên → nhánh đó.\nKhông khớp → nhánh mặc định.");hint.style.whiteSpace=WhiteSpace.Normal;start.extensionContainer.Add(hint);
            Output(start,"Mặc định",new Binding{Default=true});
            if(asset.autoAcceptQuest!=null)
            {
                start.extensionContainer.Add(new Label("Tự nhận: "+asset.autoAcceptQuest.title));
                Output(start,"Vừa nhận, chưa đủ mục tiêu",new Binding{AutoAccepted=true});
            }
            var ruleInputs=new List<Port>();
            if(asset.entryRules!=null)for(int i=0;i<asset.entryRules.Count;i++)
            {
                var rule=asset.entryRules[i];if(rule==null)continue;
                var node=Box("rule:"+i,"ĐIỀU KIỆN #"+(i+1),new Vector2(-30,i*270),new Color(.42f,.28f,.12f));node.Rule=rule;
                var label=new Label(RuleText(rule));label.style.whiteSpace=WhiteSpace.Normal;label.style.maxWidth=280;node.extensionContainer.Add(label);
                var input=node.InstantiatePort(Orientation.Horizontal,Direction.Input,Port.Capacity.Single,typeof(bool));input.portName="Kiểm tra #"+(i+1);input.userData=true;node.inputContainer.Add(input);
                var root=start.InstantiatePort(Orientation.Horizontal,Direction.Output,Port.Capacity.Single,typeof(bool));root.portName="Điều kiện #"+(i+1);root.userData=true;start.outputContainer.Add(root);
                var fixedEdge=root.ConnectTo(input);fixedEdge.capabilities=(Capabilities)0;AddElement(fixedEdge);
                Output(node,"Nếu thỏa →",new Binding{Rule=rule});node.RefreshExpandedState();node.RefreshPorts();
            }
            if(asset.nodes!=null)for(int i=0;i<asset.nodes.Count;i++)
            {
                var line=asset.nodes[i];if(line==null)continue;
                var node=Box("node:"+line.id,line.id,new Vector2(420+(i%3)*400,(i/3)*380),new Color(.17f,.26f,.36f));node.Line=line;
                var input=node.InstantiatePort(Orientation.Horizontal,Direction.Input,Port.Capacity.Multi,typeof(bool));input.portName="Vào";input.userData=line.id;node.inputContainer.Add(input);if(!inputs.ContainsKey(line.id))inputs.Add(line.id,input);
                node.Speaker=new TextField("Người nói"){value=line.speaker??""};node.Speaker.RegisterValueChangedCallback(e=>{if(!owner.Editable)return;DialogueGraphAuthoring.Edit(asset,"Sửa người nói",()=>line.speaker=e.newValue);owner.details?.MarkDirtyRepaint();});node.extensionContainer.Add(node.Speaker);
                node.Text=new TextField{multiline=true,value=line.text??""};node.Text.style.height=96;node.Text.style.whiteSpace=WhiteSpace.Normal;node.Text.RegisterValueChangedCallback(e=>{if(!owner.Editable)return;DialogueGraphAuthoring.Edit(asset,"Sửa lời thoại",()=>line.text=e.newValue);owner.details?.MarkDirtyRepaint();});node.extensionContainer.Add(node.Text);
                Output(node,line.choices!=null&&line.choices.Count>0?"Tiếp tục nếu không có lựa chọn":"Tiếp tục",new Binding{Line=line});
                if(line.choices!=null)for(int c=0;c<line.choices.Count;c++)
                {
                    var choice=line.choices[c];if(choice==null)continue;
                    string caption=(c+1)+". "+choice.text+" ["+choice.action+"]";
                    if(choice.action==DialogueAction.End||choice.action==DialogueAction.OpenShop||choice.action==DialogueAction.ChallengeBoss){var label=new Label(caption);label.style.whiteSpace=WhiteSpace.Normal;label.style.maxWidth=280;node.extensionContainer.Add(label);}
                    else Output(node,caption,new Binding{Line=line,Choice=choice});
                }
                node.RefreshExpandedState();node.RefreshPorts();
            }
            foreach(var output in outputs)
            {
                var binding=(Binding)output.userData;string id=binding.Target(asset);
                if(!string.IsNullOrEmpty(id)&&inputs.TryGetValue(id,out var input))AddElement(output.ConnectTo(input));
            }
            start.RefreshExpandedState();start.RefreshPorts();graphViewChanged=Changed;
            SetEnabled(owner.Editable);
        }
        static string RuleText(DialogueDefinition.EntryRule r)=>(string.IsNullOrEmpty(r.encounterId)?"":(r.mustBeDefeated?"Đã hạ: ":"Chưa hạ: ")+r.encounterId+"\n")+(r.quest!=null?r.quest.title+" · "+QuestService.StatusText(r.requiredStatus)+(r.onlyBeforeCompletionDialogueSeen?" · Chưa xem hậu nhiệm vụ":""):"");
        public void UpdateCaptions()
        {
            foreach(var n in nodes.OfType<DialogueVisual>())if(n.Line!=null){n.Speaker?.SetValueWithoutNotify(n.Line.speaker??"");n.Text?.SetValueWithoutNotify(n.Line.text??"");}
            foreach(var p in outputs)if(p.userData is Binding b&&b.Choice!=null)p.portName=b.Choice.text+" ["+b.Choice.action+"]";
        }
        GraphViewChange Changed(GraphViewChange change)
        {
            if(!owner.Editable){change.edgesToCreate?.Clear();change.elementsToRemove?.Clear();return change;}
            var asset=owner.conversation;
            if(change.movedElements!=null)foreach(var n in change.movedElements.OfType<DialogueVisual>())DialogueGraphLayouts.instance.Set(asset,n.Key,n.GetPosition().position);
            bool structural=change.elementsToRemove?.Count>0||change.edgesToCreate?.Count>0;
            if(!structural)return change;
            Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            DialogueGraphAuthoring.Edit(asset,"Nối / xóa nhánh hội thoại",()=>
            {
                if(change.elementsToRemove!=null)
                {
                    foreach(var edge in change.elementsToRemove.OfType<Edge>())if(edge.output?.userData is Binding b)b.Set(asset,"");
                    foreach(var node in change.elementsToRemove.OfType<DialogueVisual>())
                    {
                        if(node.Line!=null)DialogueGraphAuthoring.Delete(asset,node.Line);
                        else if(node.Rule!=null)asset.entryRules.Remove(node.Rule);
                    }
                }
                if(change.edgesToCreate!=null)foreach(var edge in change.edgesToCreate)
                    if(edge.output?.userData is Binding binding&&edge.input?.userData is string id)binding.Set(asset,id);
            });
            Undo.CollapseUndoOperations(group);owner.QueueRebuild();return change;
        }
        public void AutoLayout()
        {
            var asset=owner.conversation;if(asset==null)return;
            DialogueGraphLayouts.instance.Set(asset,"start",new Vector2(-420,0));
            if(asset.entryRules!=null)for(int i=0;i<asset.entryRules.Count;i++)DialogueGraphLayouts.instance.Set(asset,"rule:"+i,new Vector2(-30,i*270));
            var levels=new Dictionary<string,int>();var queue=new Queue<string>();
            Action<string,int> visit=(id,depth)=>{if(!string.IsNullOrEmpty(id)&&!levels.ContainsKey(id)){levels[id]=depth;queue.Enqueue(id);}};
            visit(asset.startNodeId,0);if(asset.entryRules!=null)foreach(var rule in asset.entryRules.Where(r=>r!=null))visit(rule.startNodeId,0);
            if(asset.autoAcceptQuest!=null)visit(asset.autoAcceptedStartNodeId,0);
            while(queue.Count>0)
            {
                string id=queue.Dequeue();var node=asset.Find(id);if(node==null)continue;int next=levels[id]+1;visit(node.nextNodeId,next);
                if(node.choices!=null)foreach(var c in node.choices.Where(c=>c!=null&&c.action!=DialogueAction.End&&c.action!=DialogueAction.OpenShop&&c.action!=DialogueAction.ChallengeBoss))visit(c.nextNodeId,next);
            }
            var rows=new Dictionary<int,float>();
            foreach(var line in (asset.nodes??new List<DialogueDefinition.Node>()).Where(n=>n!=null))
            {
                int depth=levels.TryGetValue(line.id,out var d)?d:0;float y=rows.TryGetValue(depth,out var r)?r:0;
                DialogueGraphLayouts.instance.Set(asset,"node:"+line.id,new Vector2(420+depth*400,y));rows[depth]=y+330+(line.choices?.Count??0)*28;
            }
            Rebuild();schedule.Execute(()=>FrameAll()).ExecuteLater(100);
        }
    }
}

[CustomEditor(typeof(DialogueDefinition))]
public sealed class DialogueGraphInspector : Editor
{
    public override void OnInspectorGUI()
    {
        if(GUILayout.Button("Mở Dialogue Graph",GUILayout.Height(30)))DialogueGraphWindow.Open(target as DialogueDefinition);
        EditorGUILayout.Space();DrawDefaultInspector();
    }
}
