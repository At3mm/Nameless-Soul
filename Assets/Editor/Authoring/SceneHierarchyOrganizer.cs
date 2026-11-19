using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using World;

/// <summary>Root-only scene grouping with serialized-data, prefab, identity and geometry checks.</summary>
public static class SceneHierarchyOrganizer
{
    [Serializable] public sealed class ComponentRecord { public string id,type,hash; }
    [Serializable] public sealed class ObjectRecord
    {
        public string id,parent,name,tag,prefab;public int layer,staticFlags;public bool active,keepRoot;
        public Vector3 position,scale;public Quaternion rotation;public List<ComponentRecord> components=new List<ComponentRecord>();
    }
    [Serializable] public sealed class Snapshot
    {public string scenePath,sceneBackupPath;public int roots,missing;public List<ObjectRecord> objects=new List<ObjectRecord>();}
    public static string LastReport { get; private set; }
    private static readonly string[] GroupNames={"Environment","Characters","World","UI","Cameras"};
    private static readonly HashSet<string> PersistentTypes=new HashSet<string>{"ScreenFader","CheckpointService","AreaManager","GameMusicController","PixelPresentation","EnemyAudioPool","CombatSFX","MenuSFX","FootstepSFX","CurrencyGainNotice","BossRewardNotice","AreaNotice"};
    private static string Id(UnityEngine.Object obj)=>obj==null?"":GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
    public static Snapshot Capture(Scene scene)
    {
        var snapshot=new Snapshot{scenePath=scene.path,roots=scene.rootCount};
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            var go=t.gameObject;var components=go.GetComponents<Component>();
            var entry=new ObjectRecord{id=Id(go),parent=Id(t.parent!=null?t.parent.gameObject:null),name=go.name,tag=go.tag,layer=go.layer,active=go.activeSelf,staticFlags=(int)GameObjectUtility.GetStaticEditorFlags(go),position=t.position,rotation=t.rotation,scale=t.lossyScale,prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go),keepRoot=t.parent==null&&components.Any(c=>c!=null&&PersistentTypes.Contains(c.GetType().Name))};
            foreach(var component in components)
            {
                if(component==null){snapshot.missing++;continue;}
                var json=JObject.Parse(EditorJsonUtility.ToJson(component));
                if(component is Transform)
                {
                    var fields=json.Properties().FirstOrDefault()?.Value as JObject;
                    if(fields!=null)foreach(var field in new[]{"m_Father","m_RootOrder","m_LocalPosition","m_LocalRotation","m_LocalScale"})fields.Remove(field);
                }
                string hash;using(var sha=SHA256.Create())hash=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None))));
                entry.components.Add(new ComponentRecord{id=Id(component),type=component.GetType().FullName,hash=hash});
            }
            snapshot.objects.Add(entry);
        }
        return snapshot;
    }
    public static List<string> Validate(Snapshot before,Scene scene)
    {
        var after=Capture(scene);var issues=new List<string>();
        if(before.scenePath!=scene.path)issues.Add("Scene path changed.");
        if(before.missing!=after.missing)issues.Add("Missing-script count changed.");
        var current=after.objects.ToDictionary(o=>o.id);var ids=new HashSet<string>(before.objects.Select(o=>o.id));
        foreach(var old in before.objects)
        {
            if(!current.TryGetValue(old.id,out var now)){issues.Add("Object identity lost: "+old.name);continue;}
            if(old.name!=now.name||old.tag!=now.tag||old.layer!=now.layer||old.active!=now.active||old.staticFlags!=now.staticFlags||old.prefab!=now.prefab)issues.Add("Object/prefab settings changed: "+old.name);
            if(Vector3.Distance(old.position,now.position)>.0001f||Vector3.Distance(old.scale,now.scale)>.0001f||Quaternion.Angle(old.rotation,now.rotation)>.001f)issues.Add("World transform changed: "+old.name);
            if(!string.IsNullOrEmpty(old.parent)&&old.parent!=now.parent)issues.Add("Internal child parent changed: "+old.name);
            if(old.keepRoot&&!string.IsNullOrEmpty(now.parent))issues.Add("Persistent service must remain root: "+old.name);
            if(old.components.Count!=now.components.Count)issues.Add("Component count changed: "+old.name);
            var components=now.components.ToDictionary(c=>c.id);
            foreach(var c in old.components)
            {
                if(!components.TryGetValue(c.id,out var actual)||actual.type!=c.type){issues.Add("Component identity changed: "+old.name+" / "+c.type);continue;}
                if(actual.hash==c.hash)continue;
                // Animated Tilemaps rebuild sprite caches on scene load. A clean scene with an
                // identical persisted Tilemap document has unchanged authored cells and references.
                if(c.type=="UnityEngine.Tilemaps.Tilemap"&&!scene.isDirty&&SameSavedTilemap(before,scene,c.id))continue;
                issues.Add("Serialized data/reference changed: "+old.name+" / "+c.type);
            }
        }
        foreach(var added in after.objects.Where(o=>!ids.Contains(o.id)))if(added.components.Count!=1||added.components[0].type!=typeof(Transform).FullName||!added.active||Vector3.Distance(added.position,Vector3.zero)>.0001f||Vector3.Distance(added.scale,Vector3.one)>.0001f||Quaternion.Angle(added.rotation,Quaternion.identity)>.001f)issues.Add("Unexpected new object: "+added.name);
        return issues;
    }
    [MenuItem("Tools/World/Organize Active Scene Hierarchy")]
    public static void OrganizeMenu(){Debug.Log(Organize());}
    public static string Organize()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi gom Hierarchy.");
        var scene=SceneManager.GetActiveScene();
        if(!scene.IsValid()||!scene.isLoaded||string.IsNullOrEmpty(scene.path)||EditorSceneManager.IsPreviewScene(scene)||PrefabStageUtility.GetCurrentPrefabStage()!=null)throw new InvalidOperationException("Mở một scene đã lưu, không chạy trong Prefab Mode.");
        var originalRoots=scene.GetRootGameObjects();
        foreach(var name in GroupNames)
        {
            var existing=originalRoots.FirstOrDefault(go=>go.name==name);
            if(existing!=null)CheckGroup(existing.transform);
        }
        bool wasDirty=scene.isDirty;var before=Capture(scene);
        string folder="Library/DeveloperTools/HierarchyCleanup/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6);
        Directory.CreateDirectory(folder);File.Copy(scene.path,folder+"/SceneBefore.unity");
        before.sceneBackupPath=folder+"/SceneBefore.unity";File.WriteAllText(folder+"/Snapshot.json",JsonUtility.ToJson(before));
        SessionState.SetString("SceneHierarchyOrganizer.Snapshot",folder+"/Snapshot.json");
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Organize scene hierarchy");
        int moved=0;
        try
        {
            var groups=GroupNames.ToDictionary(n=>n,n=>Group(scene,null,n));Physics2D.SyncTransforms();
            var zones=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<AreaTrigger>(true)).Where(z=>z.isActiveAndEnabled&&z.area!=null).ToArray();
            foreach(var go in originalRoots)
            {
                if(GroupNames.Contains(go.name)&&go.GetComponents<Component>().Length==1)continue;
                var record=before.objects.First(o=>o.id==Id(go));if(record.keepRoot)continue;
                Transform destination=null;
                if(go.GetComponent<EnemyClassification>()!=null)
                {
                    var zone=zones.Where(z=>z.Contains(go.transform.position)).OrderByDescending(z=>z.priority).ThenBy(z=>z.area.areaId,StringComparer.Ordinal).FirstOrDefault();
                    string area=zone!=null?zone.area.displayName:"Ngoài khu vực";
                    if(string.IsNullOrWhiteSpace(area))area=zone.area.areaId;
                    area=area.Replace('/','-').Replace('\\','-');
                    destination=Group(scene,Group(scene,groups["Characters"],"Enemies"),area);
                }
                else if(go.GetComponent<PlayerController>()!=null)destination=groups["Characters"];
                else if(go.GetComponent<NpcInteractable>()!=null)destination=Group(scene,groups["Characters"],"NPCs");
                else if(go.GetComponent<CheckpointInteractable>()!=null)destination=Group(scene,groups["World"],"Checkpoints");
                else if(go.GetComponent<SceneTransitionTrigger>()!=null)destination=Group(scene,groups["World"],"Transitions");
                else if(go.GetComponent<SceneSpawnPoint>()!=null)destination=Group(scene,groups["World"],"SpawnPoints");
                else if(go.GetComponent<SettingsMenuToggle>()!=null||go.GetComponent<Canvas>()!=null)destination=groups["UI"];
                else if(go.GetComponent<Camera>()!=null||go.GetComponents<Component>().Any(c=>c!=null&&c.GetType().Namespace=="Unity.Cinemachine"))destination=groups["Cameras"];
                else if(go.GetComponent<Grid>()!=null||go.GetComponents<Component>().Any(c=>c!=null&&c.GetType().Name=="Light2D"))destination=groups["Environment"];
                else if(go.GetComponentsInChildren<AreaTrigger>(true).Length>0)destination=groups["World"];
                if(destination==null)continue;
                Undo.SetTransformParent(go.transform,destination,"Group "+go.name);moved++;
            }
            for(int i=0;i<GroupNames.Length;i++)groups[GroupNames[i]].SetSiblingIndex(i);
            Physics2D.SyncTransforms();CheckpointIdentityEditor.EnsureScene(scene,true);
            var issues=Validate(before,scene);issues.AddRange(CheckpointIdentityEditor.ValidateScene(scene));
            if(issues.Count>0)throw new InvalidOperationException(string.Join("\n",issues.Take(12)));
            EditorSceneManager.MarkSceneDirty(scene);Undo.CollapseUndoOperations(undo);
            // Preserve work that was already unsaved before this operation.
            bool saved=!wasDirty&&EditorSceneManager.SaveScene(scene);
            LastReport="Grouped "+moved+" roots; "+before.roots+" -> "+scene.rootCount+" scene roots. Verified "+before.objects.Count+" objects and all serialized components/references. "+(saved?"Scene saved.":"Scene has unsaved changes; use Ctrl+S when ready.")+" Backup: "+folder;
            File.WriteAllText(folder+"/Report.txt",LastReport);Collapse(groups.Values);
            return LastReport;
        }
        catch
        {Undo.RevertAllDownToGroup(undo);Physics2D.SyncTransforms();throw;}
    }
    private static void CheckGroup(Transform group)
    {
        if(group.GetComponents<Component>().Length!=1||!group.gameObject.activeSelf||group.localPosition!=Vector3.zero||group.localRotation!=Quaternion.identity||group.localScale!=Vector3.one||PrefabUtility.IsPartOfPrefabInstance(group))throw new InvalidOperationException("Tên nhóm đã được dùng bởi đối tượng khác / transform không trung tính: "+group.name);
    }
    private static Transform Group(Scene scene,Transform parent,string name)
    {
        Transform found=parent==null?scene.GetRootGameObjects().FirstOrDefault(g=>g.name==name)?.transform:parent.Cast<Transform>().FirstOrDefault(t=>t.name==name);
        if(found!=null){CheckGroup(found);return found;}
        var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);Undo.RegisterCreatedObjectUndo(go,"Create hierarchy group");
        if(parent!=null)go.transform.SetParent(parent,false);return go.transform;
    }
    private static void Collapse(IEnumerable<Transform> groups)
    {
        // UI convenience only; an unavailable Hierarchy window must not roll back saved scene data.
        try
        {
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
            var method=type?.GetMethod("SetExpandedRecursive",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
            if(method==null)return;
            foreach(var window in Resources.FindObjectsOfTypeAll(type))foreach(var group in groups)method.Invoke(window,new object[]{group.gameObject.GetInstanceID(),false});
        }
        catch(System.Reflection.TargetInvocationException){}
    }
    public static string ValidateLast()
    {
        string path=SessionState.GetString("SceneHierarchyOrganizer.Snapshot","");if(!File.Exists(path))throw new InvalidOperationException("No hierarchy snapshot in this Editor session.");
        var before=JsonUtility.FromJson<Snapshot>(File.ReadAllText(path));if(string.IsNullOrEmpty(before.sceneBackupPath))before.sceneBackupPath=Path.GetDirectoryName(path)+"/SceneBefore.unity";
        var issues=Validate(before,SceneManager.GetSceneByPath(before.scenePath));
        if(issues.Count>0)throw new InvalidOperationException(string.Join("\n",issues.Take(12)));
        return "Hierarchy verification passed for "+before.objects.Count+" original objects; serialized fields/references, prefab links, names, IDs, geometry and active states unchanged.";
    }
    private static bool SameSavedTilemap(Snapshot before,Scene scene,string componentId)
    {
        if(string.IsNullOrEmpty(before.sceneBackupPath)||!File.Exists(before.sceneBackupPath)||!GlobalObjectId.TryParse(componentId,out var id))return false;
        string original=SceneDocument(File.ReadAllText(before.sceneBackupPath),id.targetObjectId);
        return original!=null&&original==SceneDocument(File.ReadAllText(scene.path),id.targetObjectId);
    }
    private static string SceneDocument(string text,ulong id)
    {
        var match=System.Text.RegularExpressions.Regex.Match(text,@"(?m)^--- !u!\d+ &"+id+@"(?: stripped)?\r?\n");if(!match.Success)return null;
        int next=text.IndexOf("\n--- !u!",match.Index+match.Length,StringComparison.Ordinal);
        return text.Substring(match.Index,next<0?text.Length-match.Index:next+1-match.Index);
    }
}
