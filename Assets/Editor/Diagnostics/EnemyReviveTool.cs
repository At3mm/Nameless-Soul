using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Editor testing command; uses initial actor snapshots rather than reviving disabled corpse scripts.</summary>
[InitializeOnLoad]
public sealed class EnemyReviveTool : EditorWindow
{
    const string Pending="Noname.EnemyRevive.Pending";
    const string AllScenes="Noname.EnemyRevive.AllScenes";
    bool allLoadedScenes;
    static EnemyReviveTool(){EditorApplication.playModeStateChanged+=OnPlayState;}
    [MenuItem("Tools/Game Testing/Revive All Enemies")]
    public static void OpenWindow(){GetWindow<EnemyReviveTool>("Revive Enemies").minSize=new Vector2(400,180);}
    void OnGUI()
    {
        EditorGUILayout.LabelField("HỒI SINH QUÁI ĐỂ TEST",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Hồi sinh quái thường, Elite và Boss về vị trí ban đầu, đầy máu và hoạt động lại. Xóa trạng thái đã hạ của các quái trên map được chọn. Gold, EXP, vật phẩm và checkpoint được giữ nguyên.",MessageType.Info);
        EditorGUILayout.LabelField("Map hiện tại",SceneManager.GetActiveScene().name);
        allLoadedScenes=EditorGUILayout.Toggle("Áp dụng mọi scene đang tải",allLoadedScenes);
        using(new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying))
        {
            if(GUILayout.Button(EditorApplication.isPlaying?"Hồi sinh tất cả quái ngay":"Play và hồi sinh tất cả quái",GUILayout.Height(34)))
            {
                if(EditorApplication.isPlaying)Revive(allLoadedScenes);
                else{SessionState.SetBool(Pending,true);SessionState.SetBool(AllScenes,allLoadedScenes);EditorApplication.EnterPlaymode();}
            }
        }
    }
    static void OnPlayState(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.EraseBool(Pending);return;}
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending,false))return;
        SessionState.EraseBool(Pending);bool all=SessionState.GetBool(AllScenes,false);
        EditorApplication.delayCall+=()=>{if(EditorApplication.isPlaying)Revive(all);};
    }
    public static int Revive(bool allScenes=false)
    {
        if(!EditorApplication.isPlaying)throw new System.InvalidOperationException("Revive requires Play Mode.");
        int restored=0;
        if(allScenes){for(int i=0;i<SceneManager.sceneCount;i++)restored+=World.NormalEnemyRespawns.ResetAllForTesting(SceneManager.GetSceneAt(i));}
        else restored=World.NormalEnemyRespawns.ResetAllForTesting(SceneManager.GetActiveScene());
        Debug.Log("[Enemy Revive] Đã khôi phục "+restored+" quái (gồm Elite/Boss).");return restored;
    }
}
