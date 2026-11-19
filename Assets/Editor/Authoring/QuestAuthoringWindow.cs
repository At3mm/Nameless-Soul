using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class QuestAuthoringWindow : EditorWindow
{
    [SerializeField] private QuestDefinition selected;
    [SerializeField] private string search="";
    private QuestDefinition[] quests=Array.Empty<QuestDefinition>();
    private SerializedObject data;
    private Vector2 listScroll,detailScroll;
    private int tab;
    private bool editId;
    private bool validateAll;
    private EnemyClassification boss;
    private bool exact=true;
    private DialogueDefinition dialogue;
    private World.NpcInteractable npc;
    private World.QuestBossEncounter gate;
    private string message;
    [MenuItem("Tools/Dialogue and Quests/Quest Manager")]
    public static void Open(){var window=GetWindow<QuestAuthoringWindow>("Quản lý nhiệm vụ");window.minSize=new Vector2(1040,650);window.Show();}
    [MenuItem("Assets/Open in Quest Manager",true)] private static bool CanOpenAsset()=>Selection.activeObject is QuestDefinition;
    [MenuItem("Assets/Open in Quest Manager")] private static void OpenAsset(){Open();GetWindow<QuestAuthoringWindow>().Select(Selection.activeObject as QuestDefinition);}
    private void OnEnable(){Reload();Undo.undoRedoPerformed+=Refresh;EditorApplication.projectChanged+=Reload;SelectionChanged();}
    private void OnDisable(){Undo.undoRedoPerformed-=Refresh;EditorApplication.projectChanged-=Reload;}
    private void OnSelectionChange(){SelectionChanged();Repaint();}
    private void SelectionChanged()
    {
        if(Selection.activeObject is QuestDefinition q)Select(q);
        var go=Selection.activeGameObject;if(go==null)return;
        var b=go.GetComponent<EnemyClassification>();if(b!=null&&b.enemyType==EnemyClassification.EnemyType.Boss)boss=b;
        var n=go.GetComponentInChildren<World.NpcInteractable>(true);if(n!=null){npc=n;if(n.dialogue!=null)dialogue=n.dialogue;gate=n.GetComponentInParent<World.QuestBossEncounter>();}
    }
    private void Reload(){quests=AssetDatabase.FindAssets("t:QuestDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<QuestDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(q=>q!=null).OrderBy(q=>q.title).ToArray();Refresh();}
    private void Refresh(){data=selected!=null?new SerializedObject(selected):null;Repaint();}
    public void Select(QuestDefinition quest){selected=quest;editId=false;detailScroll=Vector2.zero;Refresh();}
    private void Act(Action action){try{action();}catch(Exception error){message=error.Message;Debug.LogWarning("Quest Manager: "+error.Message);}Repaint();}
    private void OnGUI()
    {
        float oldWidth=EditorGUIUtility.labelWidth;EditorGUIUtility.labelWidth=230;
        try{DrawWindow();}finally{EditorGUIUtility.labelWidth=oldWidth;}
    }
    private void DrawWindow()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if(GUILayout.Button("Tạo mới",EditorStyles.toolbarButton))Create(false,false);
                using(new EditorGUI.DisabledScope(selected==null))
                {if(GUILayout.Button("Nhân bản",EditorStyles.toolbarButton))Create(true,false);if(GUILayout.Button("Tạo nhiệm vụ tiếp",EditorStyles.toolbarButton))Create(false,true);}
                if(GUILayout.Button("Lưu asset",EditorStyles.toolbarButton)){if(data!=null&&data.targetObject!=null)data.ApplyModifiedProperties();if(selected!=null)AssetDatabase.SaveAssetIfDirty(selected);if(dialogue!=null)AssetDatabase.SaveAssetIfDirty(dialogue);message="Đã lưu nhiệm vụ và hội thoại đang chọn. Nếu gán NPC trong scene, lưu scene bằng Ctrl+S.";}
            }
            GUILayout.FlexibleSpace();if(GUILayout.Button("Làm mới",EditorStyles.toolbarButton))Reload();
        }
        if(EditorApplication.isPlayingOrWillChangePlaymode)EditorGUILayout.HelpBox("Chỉ xem khi đang chạy game. Dừng Play để chỉnh nhiệm vụ.",MessageType.Info);
        using(new EditorGUILayout.HorizontalScope())
        {
            DrawList();
            using(new EditorGUILayout.VerticalScope())
            {
                if(selected==null){EditorGUILayout.HelpBox("Chọn nhiệm vụ bên trái hoặc bấm Tạo mới. Tạo nhiệm vụ tiếp sẽ tự nối điều kiện với nhiệm vụ đang chọn.",MessageType.Info);return;}
                EditorGUILayout.LabelField(selected.title,EditorStyles.boldLabel);EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selected),EditorStyles.miniLabel);
                tab=GUILayout.Toolbar(tab,new[]{"Nhiệm vụ","Hội thoại & NPC","Kiểm tra"});
                detailScroll=EditorGUILayout.BeginScrollView(detailScroll);
                using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if(data==null||data.targetObject!=selected)data=new SerializedObject(selected);data.Update();
                    if(tab==0)DrawQuest();else if(tab==1)DrawDialogue();else DrawValidation();
                    data.ApplyModifiedProperties();
                }
                if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);
                EditorGUILayout.EndScrollView();
            }
        }
    }
    private void DrawList()
    {
        using(new EditorGUILayout.VerticalScope(GUILayout.Width(255)))
        {
            search=EditorGUILayout.TextField(search,EditorStyles.toolbarSearchField);listScroll=EditorGUILayout.BeginScrollView(listScroll);
            foreach(var q in quests)
            {
                if(q==null)continue;
                if(!string.IsNullOrEmpty(search)&&(q.title+" "+q.questId).IndexOf(search,StringComparison.OrdinalIgnoreCase)<0)continue;
                var old=GUI.backgroundColor;if(q==selected)GUI.backgroundColor=new Color(.68f,.83f,.62f);
                if(GUILayout.Button((string.IsNullOrWhiteSpace(q.title)?q.name:q.title)+"\n"+q.questId,GUILayout.MinHeight(45)))Select(q);GUI.backgroundColor=old;
            }
            EditorGUILayout.EndScrollView();EditorGUILayout.LabelField(quests.Length+" nhiệm vụ",EditorStyles.miniLabel);
        }
    }
    private void Create(bool copy,bool next)
    {
        var path=EditorUtility.SaveFilePanelInProject(next?"Tạo nhiệm vụ tiếp":copy?"Nhân bản nhiệm vụ":"Tạo nhiệm vụ","NewQuest","asset","Chọn thư mục Resources/Quests.","Assets/Resources/Quests");if(string.IsNullOrEmpty(path))return;
        Act(()=>{var q=QuestAuthoring.Create(path,copy?selected:null,next?selected:null);Reload();Select(q);EditorGUIUtility.PingObject(q);message="Đã tạo nhiệm vụ với ID riêng. Thêm mục tiêu và phần thưởng bên dưới.";});
    }
    private void Field(string property,string label)=>EditorGUILayout.PropertyField(data.FindProperty(property),new GUIContent(label),true);
    private void DrawQuest()
    {
        editId=EditorGUILayout.ToggleLeft("Cho phép sửa Quest Id (save dùng ID này để nhận diện)",editId);
        using(new EditorGUI.DisabledScope(!editId))Field("questId","Quest Id");
        Field("title","Tên nhiệm vụ");Field("description","Mô tả");Field("icon","Icon");Field("countPreviousKills","Tính boss đã hạ trước khi nhận");
        Field("requiredDefeatedEncounters","Boss phải hạ trước khi nhận (Persistent Id)");
        EditorGUILayout.Space();EditorGUILayout.LabelField("Chuỗi nhiệm vụ — mở sau khi đã trả xong",EditorStyles.boldLabel);
        var prerequisites=data.FindProperty("prerequisites");
        for(int i=0;i<prerequisites.arraySize;i++)using(new EditorGUILayout.HorizontalScope())
        {
            var p=prerequisites.GetArrayElementAtIndex(i);var old=p.objectReferenceValue as QuestDefinition;
            var next=EditorGUILayout.ObjectField(old,typeof(QuestDefinition),false) as QuestDefinition;
            if(next!=old)
            {
                if(next!=null&&(QuestAuthoring.WouldCycle(selected,next)||Enumerable.Range(0,prerequisites.arraySize).Any(j=>j!=i&&prerequisites.GetArrayElementAtIndex(j).objectReferenceValue==next)))message="Không thể nối: nhiệm vụ bị lặp hoặc tạo vòng lặp.";
                else p.objectReferenceValue=next;
            }
            if(old!=null&&GUILayout.Button("Mở",GUILayout.Width(38))){data.ApplyModifiedProperties();Select(old);GUIUtility.ExitGUI();}
            if(GUILayout.Button("×",GUILayout.Width(26))){Remove(prerequisites,i);break;}
        }
        if(GUILayout.Button("+ Điều kiện trước")){int i=prerequisites.arraySize++;prerequisites.GetArrayElementAtIndex(i).objectReferenceValue=null;}
        foreach(var next in quests.Where(q=>q!=null&&q.prerequisites!=null&&q.prerequisites.Contains(selected)))EditorGUILayout.LabelField("→ Mở tiếp: "+next.title,EditorStyles.miniLabel);
        EditorGUILayout.Space();EditorGUILayout.LabelField("Mục tiêu hạ boss / sở hữu cổ vật",EditorStyles.boldLabel);
        boss=EditorGUILayout.ObjectField("Boss trên map / prefab",boss,typeof(EnemyClassification),true) as EnemyClassification;
        exact=EditorGUILayout.Toggle("Chỉ đúng lần gặp này",exact);
        if(boss!=null&&EditorUtility.IsPersistent(boss))EditorGUILayout.HelpBox("Chọn prefab: mục tiêu tính theo loại boss. Muốn chọn một lần gặp, kéo boss từ Hierarchy.",MessageType.Info);
        using(new EditorGUI.DisabledScope(boss==null))if(GUILayout.Button("+ Thêm mục tiêu từ boss"))
        {data.ApplyModifiedProperties();Act(()=>{message=QuestAuthoring.AddBossObjective(selected,boss,exact)?"Đã thêm mục tiêu.":"Mục tiêu này đã có.";});data.Update();}
        var objectives=data.FindProperty("objectives");
        for(int i=0;i<objectives.arraySize;i++)using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            var o=objectives.GetArrayElementAtIndex(i);EditorGUILayout.PropertyField(o.FindPropertyRelative("description"),new GUIContent("Mô tả mục tiêu"));
            var relic=o.FindPropertyRelative("relic");
            EditorGUILayout.PropertyField(relic,new GUIContent("Cổ vật xác nhận"));
            if(relic.objectReferenceValue!=null)
                EditorGUILayout.HelpBox("Sở hữu đủ cổ vật là đủ điều kiện báo cáo. Tính cả cổ vật đã nhận trước nhiệm vụ; không tiêu hao khi báo cáo. Không cần Boss Id / Encounter Id.",MessageType.None);
            using(new EditorGUI.DisabledScope(relic.objectReferenceValue!=null))
            {
                EditorGUILayout.PropertyField(o.FindPropertyRelative("bossId"),new GUIContent("Boss Id (loại boss)"));
                EditorGUILayout.PropertyField(o.FindPropertyRelative("encounterId"),new GUIContent("Encounter Id","Để trống nếu chấp nhận mọi lần gặp của loại boss này. Muốn chọn một lần gặp, dùng boss trên Hierarchy."));
            }
            EditorGUILayout.PropertyField(o.FindPropertyRelative("count"),new GUIContent("Số lượng"));
            if(GUILayout.Button("Bỏ mục tiêu")){Remove(objectives,i);break;}
        }
        if(GUILayout.Button("+ Mục tiêu nhập tay")){var i=objectives.arraySize++;var o=objectives.GetArrayElementAtIndex(i);o.FindPropertyRelative("relic").objectReferenceValue=null;o.FindPropertyRelative("description").stringValue="Hạ boss / sở hữu cổ vật";o.FindPropertyRelative("bossId").stringValue="";o.FindPropertyRelative("encounterId").stringValue="";o.FindPropertyRelative("count").intValue=1;}
        EditorGUILayout.Space();EditorGUILayout.LabelField("Phần thưởng tự nhận khi quay lại NPC",EditorStyles.boldLabel);
        Field("gold","Vàng");Field("experience","EXP / Soul");Field("lifeSeeds","Hạt sinh mệnh");Field("consumables","Vật phẩm hỗ trợ");Field("items","Vật phẩm nhiệm vụ");
        EditorGUILayout.HelpBox("Thay đổi hỗ trợ Ctrl+Z. Bấm Lưu asset hoặc Ctrl+S để lưu. Tab Kiểm tra liệt kê cấu hình còn thiếu.",MessageType.None);
    }
    private void DrawDialogue()
    {
        EditorGUILayout.HelpBox("Thêm lựa chọn nhận / báo cáo vào hội thoại hiện có. Tool giữ nguyên các đoạn thoại cũ và không thêm trùng lựa chọn đã có.",MessageType.Info);
        var next=EditorGUILayout.ObjectField("NPC",npc,typeof(World.NpcInteractable),true) as World.NpcInteractable;
        if(next!=npc){npc=next;if(npc!=null){dialogue=npc.dialogue;gate=npc.GetComponentInParent<World.QuestBossEncounter>();}}
        dialogue=EditorGUILayout.ObjectField("Hội thoại",dialogue,typeof(DialogueDefinition),false) as DialogueDefinition;
        using(new EditorGUI.DisabledScope(dialogue==null))if(GUILayout.Button("Mở đồ thị hội thoại"))DialogueGraphWindow.Open(dialogue);
        if(GUILayout.Button("Tạo hội thoại mới"))
        {
            string path=EditorUtility.SaveFilePanelInProject("Tạo hội thoại","QuestConversation","asset","Chọn nơi lưu hội thoại.","Assets/Dialogue");
            if(!string.IsNullOrEmpty(path))Act(()=>{dialogue=QuestAuthoring.CreateDialogue(path,npc!=null?npc.displayName:"NPC");message="Đã tạo hội thoại. Bấm thêm lựa chọn nhận / trả để gắn nhiệm vụ.";});
        }
        using(new EditorGUI.DisabledScope(dialogue==null))if(GUILayout.Button("Thêm nhánh nhận / hậu nhiệm vụ"))Act(()=>{data.ApplyModifiedProperties();int count=QuestAuthoring.AddDialogueLinks(selected,dialogue);message=count==0?"Hội thoại đã có đủ lựa chọn nhận / trả cho nhiệm vụ này.":"Đã thêm "+count+" nhánh. Bạn có thể sửa câu chữ bằng Inspector của hội thoại.";});
        using(new EditorGUI.DisabledScope(npc==null||dialogue==null))if(GUILayout.Button("Gán hội thoại cho NPC"))Act(()=>{QuestAuthoring.BindDialogue(npc,dialogue);message="Đã gán hội thoại cho NPC. Lưu scene nếu NPC đặt trên map.";});
        if(dialogue!=null&&GUILayout.Button("Chọn hội thoại trong Inspector")){Selection.activeObject=dialogue;EditorGUIUtility.PingObject(dialogue);}
        EditorGUILayout.Space();EditorGUILayout.LabelField("NPC chuyển thành boss (tùy chọn)",EditorStyles.boldLabel);
        gate=EditorGUILayout.ObjectField("Quest Boss Encounter",gate,typeof(World.QuestBossEncounter),true) as World.QuestBossEncounter;
        using(new EditorGUI.DisabledScope(gate==null))if(GUILayout.Button("Dùng nhiệm vụ này làm điều kiện mở boss"))Act(()=>{QuestAuthoring.BindGate(gate,selected);message="Đã gán điều kiện: boss mở sau khi nhiệm vụ hoàn thành và đóng đối thoại.";});
    }
    private void DrawValidation()
    {
        validateAll=EditorGUILayout.ToggleLeft("Kiểm tra tất cả nhiệm vụ trong danh sách",validateAll);
        var errors=validateAll?quests.Where(q=>q!=null).SelectMany(q=>QuestAuthoring.Validate(q,quests).Select(e=>q.title+": "+e)).ToList():QuestAuthoring.Validate(selected,quests);
        if(errors.Count==0)EditorGUILayout.HelpBox("Cấu hình hợp lệ. Nhiệm vụ mở khi tất cả điều kiện trước đã được báo cáo / trả xong.",MessageType.Info);
        foreach(var error in errors)EditorGUILayout.HelpBox(error,MessageType.Error);
        if(EditorApplication.isPlaying)EditorGUILayout.LabelField("Trạng thái save hiện tại",QuestService.StatusText(QuestService.Status(selected)));
        if(GUILayout.Button("Chọn asset trong Project")){Selection.activeObject=selected;EditorGUIUtility.PingObject(selected);}
    }
    private static void Remove(SerializedProperty array,int index){int count=array.arraySize;array.DeleteArrayElementAtIndex(index);if(array.arraySize==count)array.DeleteArrayElementAtIndex(index);}
}
