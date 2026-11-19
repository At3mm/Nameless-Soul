using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public static class DialogueGraphRegression
{
    static int checks;
    static void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);checks++;}
    [MenuItem("Tools/Dialogue and Quests/Test Dialogue Graph")]
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Dừng Play trước khi test.");
        var original=AssetDatabase.LoadAssetAtPath<DialogueDefinition>("Assets/Dialogue/BlackKnightConversation.asset");
        string originalJson=original!=null?EditorJsonUtility.ToJson(original):"";
        var asset=ScriptableObject.CreateInstance<DialogueDefinition>();var clone=ScriptableObject.CreateInstance<DialogueDefinition>();
        DialogueGraphWindow window=null;checks=0;
        try
        {
            var quest=Resources.Load<QuestDefinition>("Quests/BlackKnightChallenge");
            var a=new DialogueDefinition.Node{id="a",text="Giữ nguyên lời thoại",nextNodeId="b"};
            var b=new DialogueDefinition.Node{id="b",text="Đích"};var c=new DialogueDefinition.Node{id="c",text="Nhánh khác"};
            var choice=new DialogueDefinition.Choice{text="Nhận lời",action=DialogueAction.AcceptQuest,quest=quest,nextNodeId="b"};a.choices.Add(choice);
            asset.nodes=new List<DialogueDefinition.Node>{a,b,c};asset.startNodeId="a";asset.autoAcceptQuest=quest;asset.autoAcceptedStartNodeId="b";
            asset.entryRules.Add(new DialogueDefinition.EntryRule{encounterId="test:first-boss",mustBeDefeated=false,startNodeId="b"});
            Check(DialogueGraphAuthoring.Validate(asset).Count==0,"Valid graph rejected");
            DialogueGraphAuthoring.Rename(asset,b,"renamed");
            Check(a.nextNodeId=="renamed"&&choice.nextNodeId=="renamed"&&asset.entryRules[0].startNodeId=="renamed"&&asset.autoAcceptedStartNodeId=="renamed","Rename did not update all inbound references");
            Check(choice.action==DialogueAction.AcceptQuest&&choice.quest==quest&&a.text=="Giữ nguyên lời thoại","Rename changed dialogue or quest action");
            string snapshot=EditorJsonUtility.ToJson(asset);bool refused=false;
            try{DialogueGraphAuthoring.Rename(asset,b,"a");}catch(InvalidOperationException){refused=true;}
            Check(refused&&snapshot==EditorJsonUtility.ToJson(asset),"Duplicate ID changed graph");
            refused=false;try{DialogueGraphAuthoring.Rename(asset,b," ");}catch(InvalidOperationException){refused=true;}Check(refused,"Empty ID accepted");
            Undo.IncrementCurrentGroup();Undo.FlushUndoRecordObjects();
            DialogueGraphAuthoring.Delete(asset,b);Undo.FlushUndoRecordObjects();
            Check(asset.nodes.Count==2&&a.nextNodeId==""&&choice.nextNodeId==""&&asset.entryRules[0].startNodeId==""&&asset.autoAcceptedStartNodeId=="","Delete left dangling references");
            Undo.PerformUndo();
            Check(asset.nodes.Count==3&&asset.nodes[0].nextNodeId=="renamed"&&asset.entryRules[0].startNodeId=="renamed","Undo did not restore deleted node and links");
            a=asset.nodes.Find(n=>n.id=="a");choice=a.choices[0];
            var added=DialogueGraphAuthoring.AddNode(asset);Check(added!=null&&asset.nodes.Select(n=>n.id).Distinct().Count()==asset.nodes.Count,"New node ID collision");
            window=DialogueGraphWindow.Open(asset);
            var graph=window.rootVisualElement.Query<GraphView>().First();Check(graph!=null,"Graph view not constructed");
            Check(graph.nodes.Count()==asset.nodes.Count+asset.entryRules.Count+1,"Existing nodes/rules not imported into view");
            var output=graph.ports.First(p=>p.direction==Direction.Output&&p.portName=="Mặc định");
            var input=graph.ports.First(p=>p.direction==Direction.Input&&(p.userData as string)=="c");
            Check(graph.GetCompatiblePorts(output,null).Contains(input),"Dialogue input is not connectable");
            Check(!graph.GetCompatiblePorts(output,null).Any(p=>p.userData is bool),"Fixed rule-order ports can be rewired");
            var edge=output.ConnectTo(input);
            graph.graphViewChanged(new GraphViewChange{edgesToCreate=new List<Edge>{edge}});
            Check(asset.startNodeId=="c","Dragged default edge did not update runtime asset");
            graph.graphViewChanged(new GraphViewChange{elementsToRemove=new List<GraphElement>{edge}});
            Check(asset.startNodeId==""&&DialogueGraphAuthoring.Validate(asset).Any(e=>e.Contains("mặc định")),"Disconnect did not clear target / validate");
            Check(choice.action==DialogueAction.AcceptQuest&&choice.quest==quest,"Edge editing changed quest action");
            string json=EditorJsonUtility.ToJson(asset);EditorJsonUtility.FromJsonOverwrite(json,clone);
            Check(clone.nodes.Count==asset.nodes.Count&&clone.entryRules[0].startNodeId==asset.entryRules[0].startNodeId&&clone.nodes[0].choices[0].quest==quest,"Graph data did not serialize");
            if(original!=null)Check(EditorJsonUtility.ToJson(original)==originalJson,"Original Black Knight dialogue was modified");
            string result="PASS "+checks+" checks: model links, rename, delete, Undo, duplicate IDs, graph import, compatible ports, connect/disconnect, quest action preservation and serialization.";
            Debug.Log(result);return result;
        }
        finally
        {
            if(window!=null)window.Close();Undo.ClearUndo(asset);UnityEngine.Object.DestroyImmediate(asset);UnityEngine.Object.DestroyImmediate(clone);
        }
    }
}
