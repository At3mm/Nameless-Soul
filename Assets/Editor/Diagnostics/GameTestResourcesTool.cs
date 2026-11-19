using UnityEditor;
using UnityEngine;

public sealed class GameTestResourcesTool : EditorWindow
{
    [SerializeField] int gold = 1000, experience = 1000, quantity = 10;
    Vector2 scroll;
    string message;
    bool failed;
    GameItemDefinition customItem;

    [MenuItem("Tools/Game Testing/Add Gold EXP and Items")]
    public static void OpenWindow()
    {
        GetWindow<GameTestResourcesTool>("Test Resources").minSize = new Vector2(430, 420);
    }

    void OnEnable() { GameEconomy.Changed += Repaint; }
    void OnDisable() { GameEconomy.Changed -= Repaint; }
    void OnInspectorUpdate() { Repaint(); }

    void OnGUI()
    {
        EditorGUILayout.LabelField("CỘNG TÀI NGUYÊN ĐỂ TEST", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Dùng được cả khi đang Play và ngoài Play. Thay đổi lưu vào tiến trình hiện tại và cập nhật HUD/kho đồ ngay.", MessageType.Info);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode != EditorApplication.isPlaying))
        {
            EditorGUILayout.LabelField("Đang có", "Gold: " + GameEconomy.Gold + "   |   EXP: " + GameEconomy.Experience);
            gold = Mathf.Max(0, EditorGUILayout.IntField("Gold cộng thêm", gold));
            experience = Mathf.Max(0, EditorGUILayout.IntField("EXP cộng thêm", experience));
            if (GUILayout.Button("Cộng Gold và EXP", GUILayout.Height(28))) Grant(gold, experience, new int[ConsumableCatalog.Count]);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Vật phẩm", EditorStyles.boldLabel);
            quantity = Mathf.Max(1, EditorGUILayout.IntField("Số lượng cộng mỗi loại", quantity));
            if (GUILayout.Button("Cộng cả 5 loại đồ buff", GUILayout.Height(28)))
            {
                var counts = new int[ConsumableCatalog.Count];
                for (int i = 3; i < counts.Length; i++) counts[i] = quantity;
                Grant(0, 0, counts);
            }
            var catalog = ConsumableCatalog.Default;
            for (int i = 3; i < ConsumableCatalog.Count; i++) DrawItem((ConsumableKind)i, catalog);
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Đồ giải debuff", EditorStyles.boldLabel);
            for (int i = 0; i < 3; i++) DrawItem((ConsumableKind)i, catalog);
        }
        EditorGUILayout.Space();
        customItem=EditorGUILayout.ObjectField("Vật phẩm tùy biến",customItem,typeof(GameItemDefinition),false) as GameItemDefinition;
        using(new EditorGUI.DisabledScope(customItem==null))if(GUILayout.Button("Cộng vật phẩm tùy biến"))
        {
            failed=!QuestService.GrantItem(customItem,quantity);
            message=failed?"Không thể cộng: kiểm tra ID và số lượng tối đa.":"Đã cộng và lưu vật phẩm.";
        }
        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, failed ? MessageType.Warning : MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    void DrawItem(ConsumableKind kind, ConsumableCatalog catalog)
    {
        var item = catalog != null ? catalog.Find(kind) : null;
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(item != null ? item.displayName : kind.ToString());
            GUILayout.Label(GameEconomy.ConsumableCount(kind).ToString(), GUILayout.Width(65));
            if (GUILayout.Button("+ " + quantity, GUILayout.Width(85)))
            {
                var counts = new int[ConsumableCatalog.Count];
                counts[(int)kind] = quantity;
                Grant(0, 0, counts);
            }
        }
    }

    void Grant(int extraGold, int extraExperience, int[] counts)
    {
        failed = !GameEconomy.GrantForTesting(extraGold, extraExperience, counts);
        message = failed ? "Không thể cộng: số lượng vượt giới hạn hoặc đang có giao dịch. Dữ liệu giữ nguyên." : "Đã cộng và lưu tài nguyên.";
        Repaint();
    }
}
