using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class DashEffectPrefabSetup
{
    public const string Folder="Assets/Effects/Animations/Dash";
    public const string ClipPath=Folder+"/DashEffect.anim";
    public const string ControllerPath=Folder+"/DashEffect.controller";
    public const string PrefabPath="Assets/Effects/Prefabs/DashEffect.prefab";
    [MenuItem("Tools/Effects/Create Reusable Dash Effect")]
    public static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi lưu hiệu ứng.");
        var source=DashEffectAsset.Default;
        if(source==null || source.frames==null || source.frames.Length==0)throw new InvalidOperationException("Chưa có sprite hiệu ứng dash.");
        EnsureFolder(Folder);EnsureFolder("Assets/Effects/Prefabs");
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if(clip==null)
        {
            clip=new AnimationClip{name="DashEffect",frameRate=12};
            var keys=new ObjectReferenceKeyframe[source.frames.Length];
            for(int i=0;i<source.frames.Length;i++)keys[i]=new ObjectReferenceKeyframe{time=i/12f,value=source.frames[i]};
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);
            AssetDatabase.CreateAsset(clip,ClipPath);
        }
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state=controller.layers[0].stateMachine.AddState("DashEffect");state.motion=clip;
            controller.layers[0].stateMachine.defaultState=state;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        }
        if(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)==null)
        {
            var root=new GameObject("DashEffect");
            try
            {
                var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=source.frames[0];renderer.sharedMaterial=source.material;renderer.color=source.tint;
                renderer.sortingLayerName="Characters";renderer.sortingOrder=1;
                root.transform.localScale=Vector3.one*source.scale;
                var animator=root.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                root.AddComponent<OneShotEffectAnimation>();
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);Selection.activeObject=prefab;EditorGUIUtility.PingObject(prefab);
    }
    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        int split=path.LastIndexOf('/');string parent=path.Substring(0,split);EnsureFolder(parent);AssetDatabase.CreateFolder(parent,path.Substring(split+1));
    }
}
