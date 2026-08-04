using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class CharacterCreatorTool : EditorWindow
{
    private string characterName = "New Character";
    private CharacterType charType = CharacterType.Enemy;
    private Sprite characterSprite;

    // Thư mục gốc chứa toàn bộ dữ liệu của nhân vật (Ví dụ: thư mục Player hoặc Orc)
    private DefaultAsset characterRootFolder;

    public enum CharacterType
    {
        Player,
        Enemy
    }

    [MenuItem("Tools/Character Creator")]
    public static void ShowWindow()
    {
        GetWindow<CharacterCreatorTool>("Character Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Cài Đặt Nhân Vật Mới", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        characterName = EditorGUILayout.TextField("Tên nhân vật:", characterName);
        charType = (CharacterType)EditorGUILayout.EnumPopup("Loại nhân vật:", charType);

        EditorGUILayout.Space();
        GUILayout.Label("Đồ họa & Cấu trúc thư mục", EditorStyles.boldLabel);

        characterSprite = (Sprite)EditorGUILayout.ObjectField("Sprite mặc định:", characterSprite, typeof(Sprite), false);

        // Ô kéo thả thư mục Gốc của nhân vật
        characterRootFolder = (DefaultAsset)EditorGUILayout.ObjectField("Thư mục Gốc của NV (Folder):", characterRootFolder, typeof(DefaultAsset), false);

        if (characterRootFolder != null)
        {
            string path = AssetDatabase.GetAssetPath(characterRootFolder);
            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorGUILayout.HelpBox("Vui lòng chỉ kéo thả Thư mục (Folder), không kéo file!", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Tool sẽ quét tất cả các thư mục con bên trong '" + characterRootFolder.name + "' để tạo Animation, và lưu vào thư mục 'Animation' ở ngoài cùng.", MessageType.Info);
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("TẠO NHÂN VẬT & ANIMATION", GUILayout.Height(40)))
        {
            CreateCharacter();
        }
    }

    private void CreateCharacter()
    {
        GameObject newChar = new GameObject(characterName);

        if (characterSprite != null)
        {
            SpriteRenderer sr = newChar.AddComponent<SpriteRenderer>();
            sr.sprite = characterSprite;
        }

        Animator anim = newChar.AddComponent<Animator>();

        // XỬ LÝ TẠO ANIMATION TỪ CẤU TRÚC FOLDER
        if (characterRootFolder != null)
        {
            string rootPath = AssetDatabase.GetAssetPath(characterRootFolder);
            if (AssetDatabase.IsValidFolder(rootPath))
            {
                SetupAnimationsFromStructure(rootPath, characterName, anim);
            }
        }

        Rigidbody2D rb = newChar.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        newChar.AddComponent<BoxCollider2D>();

        if (charType == CharacterType.Player)
        {
            newChar.tag = "Player";
            // Kế thừa từ CharacterBase
            newChar.AddComponent<PlayerController>();
            CreateAttackPoint(newChar.transform, "Player AttackPoint", new Vector2(0.5f, 0f));
        }
        else if (charType == CharacterType.Enemy)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer > -1) newChar.layer = enemyLayer;

            // Kế thừa từ CharacterBase
            newChar.AddComponent<OrcBehavior>();
            CreateAttackPoint(newChar.transform, "Enemy AttackPoint", new Vector2(-0.5f, 0f));
        }

        Undo.RegisterCreatedObjectUndo(newChar, "Create " + characterName);
        Selection.activeObject = newChar;
        Debug.Log("Đã tạo thành công: " + characterName);
    }

    private void CreateAttackPoint(Transform parent, string name, Vector2 localPosition)
    {
        GameObject attackPoint = new GameObject(name);
        attackPoint.transform.SetParent(parent);
        attackPoint.transform.localPosition = localPosition;
    }

    // --- HÀM MỚI: QUÉT FOLDER ĐỂ TẠO ANIMATION ---
    private void SetupAnimationsFromStructure(string rootFolderPath, string charName, Animator anim)
    {
        // 1. Kiểm tra và tạo thư mục Animation ngoài cùng nếu chưa có
        string animFolderPath = rootFolderPath + "/Animation";
        if (!AssetDatabase.IsValidFolder(animFolderPath))
        {
            AssetDatabase.CreateFolder(rootFolderPath, "Animation");
        }

        // Lấy tất cả các thư mục con bên trong thư mục gốc
        string[] allSubDirs = Directory.GetDirectories(rootFolderPath, "*", SearchOption.AllDirectories);

        List<AnimationClip> generatedClips = new List<AnimationClip>();

        foreach (string dir in allSubDirs)
        {
            // Sửa lỗi ở dòng này: "\\" thay vì "\"
            string assetPath = dir.Replace("\\", "/");

            // Bỏ qua thư mục Animation và Script
            if (assetPath.Contains("/Animation") || assetPath.Contains("/Script")) continue;

            string folderName = Path.GetFileName(assetPath);

            // Tìm tất cả ảnh Sprite ngay trong thư mục này
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { assetPath });
            List<Sprite> sprites = new List<Sprite>();

            foreach (string guid in spriteGuids)
            {
                string spritePath = AssetDatabase.GUIDToAssetPath(guid);

                // Đảm bảo chỉ lấy ảnh nằm trực tiếp trong thư mục hiện tại (không lấy ảnh của thư mục con bên trong)
                // Sửa lỗi ở dòng này: "\\" thay vì "\"
                if (Path.GetDirectoryName(spritePath).Replace("\\", "/") != assetPath) continue;

                Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
                foreach (Object obj in allAssets)
                {
                    if (obj is Sprite s) sprites.Add(s);
                }
            }

            // Nếu thư mục có chứa ảnh, tạo Animation Clip tương ứng
            if (sprites.Count > 0)
            {
                // Sắp xếp ảnh theo tên
                sprites = sprites.OrderBy(s => s.name).ToList();

                AnimationClip clip = CreateClip(sprites, folderName);
                string clipPath = animFolderPath + "/" + folderName + ".anim";

                AssetDatabase.CreateAsset(clip, clipPath);
                generatedClips.Add(clip);
                Debug.Log("Tạo Clip: " + clipPath);
            }
        }

        // 2. Tạo Animator Controller trong thư mục Animation
        string controllerPath = animFolderPath + "/" + charName + "_Controller.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // 3. Thêm các Parameter tiêu chuẩn
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("TakeDamage", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        // 4. Thêm các Clip vào Controller
        foreach (AnimationClip clip in generatedClips)
        {
            AnimatorState state = controller.AddMotion(clip);

            // Đặt Idle làm mặc định nếu có
            if (clip.name.ToLower().Contains("idle"))
            {
                controller.layers[0].stateMachine.defaultState = state;
            }
        }

        // Gắn controller vào nhân vật
        anim.runtimeAnimatorController = controller;

        // Lưu các thay đổi
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Hoàn tất tạo cấu trúc Animation tại: " + animFolderPath);
    }

    private AnimationClip CreateClip(List<Sprite> sprites, string clipName)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 12; // Chỉnh FPS tùy ý

        // Thiết lập lặp lại (Loop) cho các hành động đi bộ, đứng yên
        bool isLooping = clipName.ToLower().Contains("idle") || clipName.ToLower().Contains("walk") || clipName.ToLower().Contains("run");
        if (isLooping)
        {
            AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        }

        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe();
            keyframes[i].time = (float)i / clip.frameRate;
            keyframes[i].value = sprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        return clip;
    }
}