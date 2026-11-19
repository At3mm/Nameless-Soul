using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Edits the existing runtime definitions; does not create a second inventory database.</summary>
public sealed class ItemAuthoringWindow : EditorWindow
{
    private enum Category { Items, BossRelic }
    [SerializeField] private Category category;
    [SerializeField] private UnityEngine.Object selected;
    [SerializeField] private int consumableIndex;
    private SerializedObject data;
    private UnityEngine.Object[] assets = new UnityEngine.Object[0];
    private string search = "", message = "";
    private Vector2 listScroll, detailScroll;
    [SerializeField] private int preset;

    [MenuItem("Tools/Items/Item Manager")]
    public static void Open()
    {
        var window = GetWindow<ItemAuthoringWindow>("Quản lý vật phẩm");
        window.minSize = new Vector2(850, 580);
        window.Show();
    }
    [MenuItem("Assets/Open in Item Manager", true)]
    private static bool CanOpen() => Selection.activeObject is ConsumableCatalog || Selection.activeObject is GameItemDefinition;
    [MenuItem("Assets/Open in Item Manager")]
    private static void OpenSelected() { Open(); GetWindow<ItemAuthoringWindow>().SelectAsset(Selection.activeObject); }
    private void OnEnable() { Reload(); Undo.undoRedoPerformed += Reload; EditorApplication.projectChanged += Reload; }
    private void OnDisable() { Undo.undoRedoPerformed -= Reload; EditorApplication.projectChanged -= Reload; }
    private void OnSelectionChange() { if (CanOpen()) SelectAsset(Selection.activeObject); }
    private void SelectAsset(UnityEngine.Object asset)
    {
        selected = asset;
        category = asset is GameItemDefinition item && item.InventoryCategory==ItemCategory.BossRelic ? Category.BossRelic : Category.Items;
        data = asset != null ? new SerializedObject(asset) : null;
        Reload();
    }
    private void Reload()
    {
        if(selected is GameItemDefinition definition)category=definition.InventoryCategory==ItemCategory.BossRelic?Category.BossRelic:Category.Items;
        else if((int)category>1)category=Category.Items;
        assets = AssetDatabase.FindAssets("t:GameItemDefinition")
            .Select(g => AssetDatabase.LoadAssetAtPath<GameItemDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(a => a != null && (category==Category.BossRelic ? a.InventoryCategory==ItemCategory.BossRelic : a.InventoryCategory!=ItemCategory.BossRelic)).OrderBy(a=>a.displayName).Cast<UnityEngine.Object>().ToArray();
        if (selected != null) data = new SerializedObject(selected);
        Repaint();
    }
    private static string Title(UnityEngine.Object asset) => asset is GameItemDefinition item ? item.displayName : asset.name;
    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            var next = (Category)GUILayout.Toolbar((int)category, new[] { "Vật phẩm", "Cổ vật cường địch" }, EditorStyles.toolbarButton);
            if (next != category) { category = next; selected = null; data = null; message = ""; Reload(); }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Làm mới", EditorStyles.toolbarButton)) Reload();
        }
        bool running = EditorApplication.isPlayingOrWillChangePlaymode;
        if (running) EditorGUILayout.HelpBox("Dừng Play để thêm hoặc chỉnh vật phẩm.", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawList();
            using (new EditorGUILayout.VerticalScope())
            using (new EditorGUI.DisabledScope(running))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(false))
                    {
                        if (GUILayout.Button("Thêm vật phẩm")) Create(false);
                        using (new EditorGUI.DisabledScope(selected == null)) if (GUILayout.Button("Nhân bản")) Create(true);
                    }
                    using (new EditorGUI.DisabledScope(selected == null))
                    {
                        if (GUILayout.Button("Lưu vật phẩm")) Save();
                        if (GUILayout.Button("Tìm asset")) EditorGUIUtility.PingObject(selected);
                    }
                }
                preset=EditorGUILayout.Popup("Mẫu tạo mới",preset,new[]{"Vật phẩm trống","Hồi máu","Hồi stamina","Buff sát thương","Giải độc","Bùa hồi quy","Chìa khóa","Bằng chứng hạ boss"});
                if (category == Category.Items) EditorGUILayout.HelpBox("Danh sách gồm thuốc, dây chuyền, chìa khóa và vật phẩm nhiệm vụ. Bấm Thêm vật phẩm để tạo món mới với hiệu ứng tùy chọn.", MessageType.None);
                if (selected == null) { EditorGUILayout.HelpBox("Chọn vật phẩm bên trái để sửa tên, mô tả và icon.", MessageType.Info); }
                else DrawDetails();
                if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.Info);
            }
        }
    }
    private void DrawList()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
        {
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            if (category == Category.Items)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<ConsumableCatalog>("Assets/Resources/DefaultConsumables.asset");
                if (catalog != null) for (int i = 0; i < catalog.items.Length; i++)
                {
                    var item = catalog.items[i];
                    if (item == null || item.kind == ConsumableKind.ShrineReturn || !Matches(item.displayName + " " + item.kind)) continue;
                    if (GUILayout.Button(new GUIContent(item.displayName, item.icon != null ? AssetPreview.GetMiniThumbnail(item.icon) : null), GUILayout.Height(38)))
                    { selected = catalog; consumableIndex = i; data = new SerializedObject(catalog); message = ""; }
                }
            }
            foreach (var asset in assets)
            {
                if (!Matches(Title(asset) + " " + ((GameItemDefinition)asset).itemId)) continue;
                var icon = ((GameItemDefinition)asset).icon;
                if (GUILayout.Button(new GUIContent(Title(asset), icon != null ? AssetPreview.GetMiniThumbnail(icon) : null), GUILayout.Height(38)))
                { selected = asset; data = new SerializedObject(asset); message = ""; }
            }
            EditorGUILayout.EndScrollView();
        }
    }
    private bool Matches(string value) => string.IsNullOrWhiteSpace(search) || value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    private void DrawDetails()
    {
        if (data == null || data.targetObject != selected) data = new SerializedObject(selected);
        data.Update();
        SerializedProperty root = null;
        if (selected is ConsumableCatalog)
        {
            var items = data.FindProperty("items");
            if (consumableIndex < 0 || consumableIndex >= items.arraySize) return;
            root = items.GetArrayElementAtIndex(consumableIndex);
        }
        detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selected), EditorStyles.wordWrappedMiniLabel);
        var id = Property(root, root == null ? "itemId" : "kind");
        if (root != null)
        {
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(id, new GUIContent("Loại hiệu ứng"));
        }
        else
        {
            string nextId = EditorGUILayout.DelayedTextField("ID vật phẩm", id.stringValue);
            if (nextId != id.stringValue)
            {
                try { ItemAuthoring.ValidateId(selected, nextId); id.stringValue = nextId; message = "Đã đổi ID. Save cũ nhận diện vật phẩm bằng ID cũ; tham chiếu asset trong nhiệm vụ vẫn được giữ."; }
                catch (Exception error) { message = error.Message; }
            }
        }
        EditorGUILayout.PropertyField(Property(root, "displayName"), new GUIContent("Tên hiển thị"));
        EditorGUILayout.PropertyField(Property(root, "description"), new GUIContent("Mô tả"));
        if (selected is BossItemDefinition)
            EditorGUILayout.PropertyField(data.FindProperty("showAsQuestItem"), new GUIContent("Hiển thị trong tab Vật phẩm"));
        bool shrineSeed=selected is GameItemDefinition seed&&seed.itemId==LifeSeedItem.Id;
        if(shrineSeed)EditorGUILayout.HelpBox("Hạt sinh mệnh: số lượng lấy từ save kinh tế. Elite/Boss tự cộng 1 hạt; hiến tế tại tượng thần để tăng sức chứa bình máu (3 → 10). Không gắn vào ô dùng nhanh.",MessageType.Info);
        if (root == null)
        {
            if(shrineSeed)EditorGUILayout.LabelField("Tab trong túi đồ","Vật phẩm");
            else if(!(selected is BossItemDefinition))
            {
                var categoryProperty=data.FindProperty("category");
                int currentTab=categoryProperty.enumValueIndex==(int)ItemCategory.BossRelic?1:0;
                EditorGUI.BeginChangeCheck();
                int nextTab=EditorGUILayout.Popup("Tab trong túi đồ",currentTab,new[]{"Vật phẩm","Cổ vật cường địch"});
                if(EditorGUI.EndChangeCheck())categoryProperty.enumValueIndex=(int)(nextTab==1?ItemCategory.BossRelic:ItemCategory.Support);
            }
            EditorGUILayout.PropertyField(data.FindProperty("rarity"), new GUIContent("Độ hiếm"));
            EditorGUILayout.PropertyField(data.FindProperty("roles"), new GUIContent("Vai trò (có thể chọn nhiều)"));
            if(shrineSeed)
            {
                EditorGUILayout.LabelField("Cách dùng","Hiến tế tại tượng thần");
                EditorGUILayout.LabelField("Tiêu hao khi hiến tế","1 hạt mỗi lần thành công");
                EditorGUILayout.LabelField("Sức chứa bình máu","Mặc định 3 · Tối đa 10");
                EditorGUILayout.LabelField("Dùng trực tiếp / ô nhanh","Không áp dụng");
                EditorGUILayout.LabelField("Số lượng tối đa","Không giới hạn");
            }
            else
            {
                EditorGUILayout.PropertyField(data.FindProperty("maxStack"), new GUIContent("Số lượng tối đa"));
                EditorGUILayout.PropertyField(data.FindProperty("consumable"), new GUIContent("Tiêu hao khi dùng trực tiếp","Chỉ áp dụng khi dùng Effects hoặc ô nhanh. Hiến tế tại tượng có quy tắc tiêu hao riêng."));
                EditorGUILayout.PropertyField(data.FindProperty("useCooldown"), new GUIContent("Hồi sử dụng (giây)"));
                EditorGUILayout.PropertyField(data.FindProperty("effects"), new GUIContent("Hiệu ứng sử dụng"), true);
            }
            EditorGUILayout.PropertyField(data.FindProperty("buyable"), new GUIContent("Cho phép cửa hàng bán"));
            if(data.FindProperty("buyable").boolValue) EditorGUILayout.PropertyField(data.FindProperty("buyPrice"), new GUIContent("Giá mua"));
            EditorGUILayout.PropertyField(data.FindProperty("sellable"), new GUIContent("Cho phép bán lại"));
            if(data.FindProperty("sellable").boolValue) EditorGUILayout.PropertyField(data.FindProperty("sellPrice"), new GUIContent("Giá bán"));
            if(!shrineSeed)EditorGUILayout.HelpBox("Vai trò nhiệm vụ/bằng chứng độc lập với tab hiển thị. Hiệu ứng: hồi máu / stamina, tăng sát thương / tốc độ / kháng / tiết kiệm stamina / giảm sát thương nhận, giải độc / đốt / stress, hồi quy. Hồi quy phải là hiệu ứng duy nhất. Không có hiệu ứng = vật phẩm dữ liệu/chìa khóa/bằng chứng. Tái sử dụng: bỏ Tiêu hao khi dùng trực tiếp.", MessageType.None);
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Biểu tượng vật phẩm", EditorStyles.boldLabel);
        var iconProperty = Property(root, "icon");
        EditorGUILayout.PropertyField(iconProperty, new GUIContent("Icon (Sprite)"));
        EditorGUILayout.HelpBox("Kéo Sprite từ Project vào ô Icon hoặc bấm nút tròn để chọn. Với sprite sheet, mở mũi tên của ảnh và kéo đúng ô sprite đã cắt. Ảnh cần được import là Sprite (2D and UI).", MessageType.None);
        var icon = iconProperty.objectReferenceValue as Sprite;
        if (icon != null)
        {
            var preview = AssetPreview.GetAssetPreview(icon);
            var rect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
            EditorGUI.DrawRect(rect, new Color(.12f, .12f, .12f));
            if (preview != null) GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit, true);
            else { GUI.Label(rect, AssetPreview.GetMiniThumbnail(icon)); Repaint(); }
            if (GUILayout.Button("Tìm sprite gốc", GUILayout.Width(160))) EditorGUIUtility.PingObject(icon);
        }
        if (root != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(Property(root, "buyPrice"), new GUIContent("Giá mua"));
            EditorGUILayout.PropertyField(Property(root, "sellPrice"), new GUIContent("Giá bán"));
            EditorGUILayout.PropertyField(Property(root, "duration"), new GUIContent("Thời gian hiệu ứng (giây)"));
            EditorGUILayout.PropertyField(Property(root, "percent"), new GUIContent("Cường độ (%)"));
        }
        else EditorGUILayout.HelpBox("Elite/Boss tự nhận thêm 1 Hạt sinh mệnh, độc lập với cổ vật được gán. Dùng asset này trong phần thưởng Quest Manager, Cổ vật xác nhận, chìa khóa cửa, Item Pickup hoặc Enemy Item Drop. Vai trò là nhãn phân loại; các điều kiện nhiệm vụ được gán trong Quest Manager.", MessageType.Info);
        data.ApplyModifiedProperties();
        if(selected is GameItemDefinition definition)
        {
            foreach(var error in ItemAuthoring.Validate(definition))EditorGUILayout.HelpBox(error,MessageType.Warning);
            if(GUILayout.Button("Tạo pickup trong scene"))
            {
                var go=new GameObject(definition.displayName+" Pickup");Undo.RegisterCreatedObjectUndo(go,"Tạo pickup vật phẩm");
                var pickup=Undo.AddComponent<World.ItemPickup>(go);pickup.item=definition;pickup.pickupId=Guid.NewGuid().ToString("N");
                var visual=Undo.AddComponent<SpriteRenderer>(go);visual.sprite=definition.icon;
                go.transform.position=SceneView.lastActiveSceneView!=null?SceneView.lastActiveSceneView.pivot:Vector3.zero;
                Selection.activeGameObject=go;EditorGUIUtility.PingObject(go);
            }
        }
        EditorGUILayout.EndScrollView();
    }
    private SerializedProperty Property(SerializedProperty root, string field) => root != null ? root.FindPropertyRelative(field) : data.FindProperty(field);
    private void Create(bool duplicate)
    {
        try { var created = ItemAuthoring.CreateItem(category==Category.BossRelic?ItemCategory.BossRelic:ItemCategory.Support, duplicate ? selected as GameItemDefinition : null); if(!duplicate)ItemAuthoring.ApplyPreset(created,preset); SelectAsset(created); Selection.activeObject = created; message = "Đã tạo asset với ID mới. Chỉnh tên, mô tả, icon rồi bấm Lưu vật phẩm."; }
        catch (Exception error) { message = error.Message; }
    }
    private void Save()
    {
        try { data?.ApplyModifiedProperties(); ItemAuthoring.Save(selected); message = "Đã lưu vật phẩm. Icon sẽ được dùng khi mở túi đồ/cửa hàng hoặc chạy game lần tiếp theo."; }
        catch (Exception error) { message = error.Message; }
    }
}

public static class ItemAuthoring
{
    public static void ApplyPreset(GameItemDefinition item,int preset)
    {
        if(item==null||preset<=0)return;
        Undo.RecordObject(item,"Áp dụng mẫu vật phẩm");
        item.category=preset>=6?ItemCategory.Quest:ItemCategory.Support;
        item.consumable=preset<5;item.maxStack=preset>=5?1:99;
        item.roles=preset==6?ItemRole.Key|ItemRole.Quest:preset==7?ItemRole.BossEvidence|ItemRole.Quest:ItemRole.None;
        if(preset==7)item.category=ItemCategory.BossRelic;
        string[] names={"","Thuốc hồi máu","Thuốc hồi stamina","Thuốc tăng sức mạnh","Thuốc giải độc","Bùa hồi quy","Chìa khóa","Cổ vật xác nhận"};
        item.displayName=names[Mathf.Clamp(preset,0,names.Length-1)];
        var kinds=new[]{ItemEffectKind.HealHealth,ItemEffectKind.RestoreStamina,ItemEffectKind.Power,ItemEffectKind.CurePoison,ItemEffectKind.ReturnToShrine};
        item.effects=preset<=5?new[]{new GameItemDefinition.Effect{kind=kinds[preset-1],percent=25,duration=30}}:new GameItemDefinition.Effect[0];
        EditorUtility.SetDirty(item);
    }
    public static void ValidateId(UnityEngine.Object asset, string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id != id.Trim())
            throw new InvalidOperationException("ID không được trống hoặc có khoảng trắng ở đầu/cuối.");
        string oldId = (asset as GameItemDefinition)?.itemId;
        if(oldId==LifeSeedItem.Id&&id!=oldId)throw new InvalidOperationException("Hạt sinh mệnh cần giữ ID hệ thống để hiến tế và lưu số lượng.");
        if (oldId == ConsumableCatalog.ShrineReturnItemId && id != oldId)
            throw new InvalidOperationException("Bùa hồi quy cần giữ ID hệ thống để dùng nhanh và dịch chuyển.");
        foreach (var type in new[] { "GameItemDefinition" })
            foreach (var guid in AssetDatabase.FindAssets("t:" + type))
            {
                var other = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(guid));
                if (other == asset) continue;
                string otherId = (other as GameItemDefinition)?.itemId;
                if (otherId == id) throw new InvalidOperationException("ID trùng với " + AssetDatabase.GetAssetPath(other));
            }
    }
    public static GameItemDefinition CreateItem(ItemCategory category, GameItemDefinition source=null)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi tạo vật phẩm.");
        const string folder="Assets/Resources/Items";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","Items");
        var item=ScriptableObject.CreateInstance<ItemDefinition>();
        if(source!=null)EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source),item);
        item.itemId="item-"+Guid.NewGuid().ToString("N");item.category=category;
        item.displayName=source!=null?source.displayName+" (bản sao)":"Vật phẩm mới";
        if(source==null){item.maxStack=99;item.roles=category==ItemCategory.Quest?ItemRole.Quest:category==ItemCategory.BossRelic?ItemRole.BossEvidence:ItemRole.None;}
        AssetDatabase.CreateAsset(item,AssetDatabase.GenerateUniqueAssetPath(folder+"/Item.asset"));Undo.RegisterCreatedObjectUndo(item,"Tạo vật phẩm");return item;
    }
    public static UnityEngine.Object Create(bool quest, UnityEngine.Object source = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Dừng Play trước khi tạo vật phẩm.");
        string folder = quest ? "Assets/Resources/QuestItems" : "Assets/Resources/BossRewards";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", quest ? "QuestItems" : "BossRewards");
        ScriptableObject asset = quest ? (ScriptableObject)ScriptableObject.CreateInstance<QuestItemDefinition>() : ScriptableObject.CreateInstance<BossItemDefinition>();
        if (source != null && source.GetType() == asset.GetType()) EditorUtility.CopySerialized(source, asset);
        string id = (quest ? "quest-item-" : "boss-relic-") + Guid.NewGuid().ToString("N");
        if (asset is QuestItemDefinition q) { q.itemId = id; q.displayName = source != null ? q.displayName + " (bản sao)" : "Vật phẩm nhiệm vụ mới"; }
        else { var b = (BossItemDefinition)asset; b.itemId = id; b.displayName = source != null ? b.displayName + " (bản sao)" : "Cổ vật mới"; }
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + (quest ? "QuestItem" : "BossRelic") + ".asset"));
        Undo.RegisterCreatedObjectUndo(asset, "Tạo vật phẩm");
        return asset;
    }
    public static List<string> Validate(GameItemDefinition item)
    {
        var errors=new List<string>();
        if(item==null){errors.Add("Chưa chọn vật phẩm.");return errors;}
        if(!AssetDatabase.GetAssetPath(item).Contains("/Resources/"))errors.Add("Vật phẩm phải ở trong Resources.");
        if(string.IsNullOrWhiteSpace(item.displayName))errors.Add("Tên vật phẩm đang trống.");
        if(item.maxStack<1)errors.Add("Số lượng tối đa phải ít nhất 1.");
        if(float.IsNaN(item.useCooldown)||float.IsInfinity(item.useCooldown)||item.useCooldown<0)errors.Add("Hồi sử dụng không hợp lệ.");
        if(item.buyPrice<0||item.sellPrice<0||item.buyable&&item.sellable&&item.sellPrice>item.buyPrice)errors.Add("Giá không hợp lệ; giá bán lại không được cao hơn giá mua.");
        if(item.itemId==LifeSeedItem.Id&&(item.CanUse||item.maxStack!=int.MaxValue||item.InventoryCategory==ItemCategory.BossRelic))errors.Add("Hạt sinh mệnh dùng tại tượng thần: không thêm hiệu ứng dùng nhanh và giữ số lượng tối đa 2147483647.");
        if(item.effects!=null)foreach(var effect in item.effects)
        {
            if(effect==null||!Enum.IsDefined(typeof(ItemEffectKind),effect.kind)||float.IsNaN(effect.percent)||float.IsInfinity(effect.percent)||effect.percent<=0||effect.percent>100||float.IsNaN(effect.duration)||float.IsInfinity(effect.duration)||effect.duration<=0)errors.Add("Hiệu ứng cần giá trị % > 0 đến 100 và thời gian > 0.");
            if(effect!=null&&effect.kind==ItemEffectKind.ReturnToShrine&&item.effects.Length!=1)errors.Add("Hồi quy phải là hiệu ứng duy nhất.");
        }
        return errors;
    }
    public static void Save(UnityEngine.Object asset)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Dừng Play trước khi lưu vật phẩm.");
        if (asset == null) return;
        string id = (asset as GameItemDefinition)?.itemId;
        if (id != null)
        {
            ValidateId(asset, id);
            var item=(GameItemDefinition)asset;
            var errors=Validate(item);
            if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
        }
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        // The reusable charm keeps its stable quick-slot presentation in the legacy catalog.
        if (asset is QuestItemDefinition charm && charm.itemId == ConsumableCatalog.ShrineReturnItemId)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ConsumableCatalog>("Assets/Resources/DefaultConsumables.asset");
            var entry = catalog != null ? catalog.Find(ConsumableKind.ShrineReturn) : null;
            if (entry != null)
            {
                Undo.RecordObject(catalog, "Đồng bộ Bùa hồi quy");
                entry.icon = charm.icon; entry.displayName = charm.displayName; entry.description = charm.description;
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
            }
        }
    }
}

