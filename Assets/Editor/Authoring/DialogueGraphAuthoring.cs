using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>All graph edits operate on the existing conversation asset and participate in Undo.</summary>
public static class DialogueGraphAuthoring
{
    public static void Edit(DialogueDefinition asset,string label,Action change)
    {
        if(asset==null||EditorApplication.isPlayingOrWillChangePlaymode)return;
        Undo.RecordObject(asset,label);change();EditorUtility.SetDirty(asset);
    }
    public static DialogueDefinition.Node AddNode(DialogueDefinition asset)
    {
        if(asset==null||EditorApplication.isPlayingOrWillChangePlaymode)return null;
        string id="line_"+Guid.NewGuid().ToString("N").Substring(0,8);
        var node=new DialogueDefinition.Node{id=id,text="Lời thoại mới"};
        Edit(asset,"Thêm bảng thoại",()=>{if(asset.nodes==null)asset.nodes=new List<DialogueDefinition.Node>();asset.nodes.Add(node);if(string.IsNullOrEmpty(asset.startNodeId))asset.startNodeId=id;});return node;
    }
    public static void Rename(DialogueDefinition asset,DialogueDefinition.Node node,string id)
    {
        if(node==null||asset==null||!asset.nodes.Contains(node))return;
        if(string.IsNullOrWhiteSpace(id)||id!=id.Trim())throw new InvalidOperationException("ID không được trống hoặc có khoảng trắng ở đầu/cuối.");
        if(asset.nodes.Any(n=>n!=null&&n!=node&&n.id==id))throw new InvalidOperationException("ID bảng thoại đã tồn tại.");
        string before=node.id;
        Edit(asset,"Đổi ID bảng thoại",()=>
        {
            node.id=id;if(asset.startNodeId==before)asset.startNodeId=id;
            if(asset.autoAcceptedStartNodeId==before)asset.autoAcceptedStartNodeId=id;
            foreach(var n in asset.nodes.Where(n=>n!=null))
            {
                if(n.nextNodeId==before)n.nextNodeId=id;
                if(n.choices!=null)foreach(var c in n.choices.Where(c=>c!=null))if(c.nextNodeId==before)c.nextNodeId=id;
            }
            if(asset.entryRules!=null)foreach(var rule in asset.entryRules.Where(r=>r!=null))if(rule.startNodeId==before)rule.startNodeId=id;
        });
    }
    public static void Delete(DialogueDefinition asset,DialogueDefinition.Node node)
    {
        if(asset==null||node==null)return;
        Edit(asset,"Xóa bảng thoại",()=>
        {
            asset.nodes.Remove(node);
            foreach(var n in asset.nodes.Where(n=>n!=null))
            {
                if(n.nextNodeId==node.id)n.nextNodeId="";
                if(n.choices!=null)foreach(var c in n.choices.Where(c=>c!=null))if(c.nextNodeId==node.id)c.nextNodeId="";
            }
            if(asset.entryRules!=null)foreach(var r in asset.entryRules.Where(r=>r!=null))if(r.startNodeId==node.id)r.startNodeId="";
            if(asset.startNodeId==node.id)asset.startNodeId=asset.nodes.FirstOrDefault(n=>n!=null)?.id??"";
            if(asset.autoAcceptedStartNodeId==node.id)asset.autoAcceptedStartNodeId="";
        });
    }
    public static List<string> Validate(DialogueDefinition asset)
    {
        var result=new List<string>();if(asset==null)return result;
        var nodes=asset.nodes??new List<DialogueDefinition.Node>();
        var ids=new HashSet<string>();
        foreach(var n in nodes)
        {
            if(n==null){result.Add("Có bảng thoại rỗng.");continue;}
            if(string.IsNullOrWhiteSpace(n.id)||!ids.Add(n.id))result.Add("ID trống hoặc trùng: "+n.id);
        }
        Action<string,string> target=(id,from)=>{if(!string.IsNullOrEmpty(id)&&!ids.Contains(id))result.Add(from+": đích không tồn tại — "+id);};
        if(string.IsNullOrEmpty(asset.startNodeId))result.Add("Chưa nối nhánh mặc định từ Bắt đầu.");
        target(asset.startNodeId,"Bắt đầu");
        if(asset.autoAcceptQuest!=null)target(asset.autoAcceptedStartNodeId,"Vừa tự nhận nhiệm vụ");
        foreach(var n in nodes.Where(n=>n!=null))
        {
            target(n.nextNodeId,n.id);
            if(n.choices==null)continue;
            foreach(var c in n.choices)
            {
                if(c==null){result.Add(n.id+": có lựa chọn rỗng.");continue;}
                target(c.nextNodeId,n.id+" / "+c.text);
                if((c.action==DialogueAction.AcceptQuest||c.action==DialogueAction.ReportQuest)&&c.quest==null)result.Add(n.id+": lựa chọn nhận/trả chưa gán nhiệm vụ.");
            }
        }
        if(asset.entryRules!=null)for(int i=0;i<asset.entryRules.Count;i++)
        {
            var r=asset.entryRules[i];
            if(r==null){result.Add("Quy tắc "+(i+1)+" rỗng.");continue;}
            if(string.IsNullOrEmpty(r.startNodeId))result.Add("Quy tắc "+(i+1)+" chưa nối đích.");
            target(r.startNodeId,"Quy tắc "+(i+1));
            if(string.IsNullOrEmpty(r.encounterId)&&r.quest==null)result.Add("Quy tắc "+(i+1)+" không có điều kiện: luôn khớp và che các quy tắc phía sau.");
        }
        return result.Distinct().ToList();
    }
}

[FilePath("ProjectSettings/DialogueGraphLayouts.asset",FilePathAttribute.Location.ProjectFolder)]
public sealed class DialogueGraphLayouts : ScriptableSingleton<DialogueGraphLayouts>
{
    [Serializable] public sealed class Position {public string assetGuid,key;public Vector2 value;}
    [SerializeField] List<Position> positions=new List<Position>();
    public Vector2 Get(DialogueDefinition asset,string key,Vector2 fallback)
    {string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));return positions.Find(p=>p.assetGuid==guid&&p.key==key)?.value??fallback;}
    public void Set(DialogueDefinition asset,string key,Vector2 value)
    {
        string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));if(string.IsNullOrEmpty(guid))return;
        Undo.RecordObject(this,"Di chuyển bảng hội thoại");
        var p=positions.Find(x=>x.assetGuid==guid&&x.key==key);
        if(p==null)positions.Add(new Position{assetGuid=guid,key=key,value=value});else p.value=value;
        Save(true);
    }
    public void Persist(){Save(true);}
}
