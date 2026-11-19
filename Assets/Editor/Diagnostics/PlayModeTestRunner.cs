using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;
namespace Unity.AI.Assistant.PlayModeTest
{
 [InitializeOnLoad] internal static class PlayModeTestRunner
 {
  static int stage,frame=-1;static double clock;static readonly List<string> logs=new List<string>();
  static PlayModeTestRunner(){string s=SessionState.GetString("PlayModeTest.State","Idle");if(s=="WaitingForCompile")EditorApplication.delayCall+=()=>{SessionState.SetString("PlayModeTest.State","EnteringPlayMode");EditorApplication.playModeStateChanged+=Changed;EditorApplication.isPlaying=true;};else if(s=="EnteringPlayMode"){EditorApplication.playModeStateChanged+=Changed;if(EditorApplication.isPlaying)Begin();}}
  static void Changed(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode)Begin();}
  static void Begin(){EditorApplication.playModeStateChanged-=Changed;SessionState.SetString("PlayModeTest.State","InPlayMode");clock=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}
  static Component Component(string type){foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))if(c!=null&&c.GetType().Name==type)return c;return null;}
  static GameObject Named(string name){foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t.gameObject;return null;}
  static bool Busy(){var t=Type.GetType("World.ScreenFader, Assembly-CSharp");var f=t.GetProperty("Instance").GetValue(null);return (bool)t.GetProperty("IsTransitioning").GetValue(f);}
  static void MoveToDoor(string name){var p=Named("Player");var door=Named(name);if(p==null||door==null)throw new Exception("Missing player/door "+name);var inv=p.GetComponent("CharacterInvulnerability");inv.GetType().GetMethod("GrantInvulnerability").Invoke(inv,new object[]{100f});var pos=door.GetComponent<Collider2D>().bounds.center;p.transform.position=pos;p.GetComponent<Rigidbody2D>().position=pos;p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
  static void Log(string text){logs.Add(text);Debug.Log("[Test] "+text);}
  static void Tick(){if(frame==Time.frameCount)return;frame=Time.frameCount;try{if(EditorApplication.timeSinceStartup-clock>50){Finish(false,"timeout stage="+stage);return;}var scene=SceneManager.GetActiveScene();if(stage==0){SceneManager.LoadScene("WastelandScene");stage=1;return;}if(stage==1&&scene.name=="WastelandScene"&&!Busy()){Log("Wasteland ready");MoveToDoor("CaveEntrance_Trigger");stage=2;return;}if(stage==2&&scene.name=="CaveScene"&&!Busy()){var p=Named("Player");Log("Cave arrival="+p.transform.position+" playerCount="+scene.GetRootGameObjects().Length);if(Vector2.Distance(p.transform.position,Vector2.zero)>1)throw new Exception("Unexpected Cave arrival");MoveToDoor("CaveExit_Trigger");stage=3;return;}if(stage==3&&scene.name=="WastelandScene"&&!Busy()){var p=Named("Player");Log("Wasteland return="+p.transform.position);if(Vector2.Distance(p.transform.position,new Vector2(9.99f,15.93f))>.5f)throw new Exception("Wrong Wasteland return marker");Finish(true,null);}}catch(Exception e){Finish(false,e.ToString());}}
  [Serializable]class Entry{public string key,value;public bool exists;}[Serializable]class Backup{public Entry[] entries;}[Serializable]class Result{public bool success;public string error;public string[] logs;}
  static void Finish(bool success,string error){EditorApplication.update-=Tick;var path=SessionState.GetString("CaveEndgame.Backup","")+"/PlayerPrefs.json";var backup=JsonUtility.FromJson<Backup>(File.ReadAllText(path));foreach(var e in backup.entries){if(e.exists)PlayerPrefs.SetString(e.key,e.value);else PlayerPrefs.DeleteKey(e.key);}PlayerPrefs.Save();SessionState.SetString("PlayModeTest.Result",JsonUtility.ToJson(new Result{success=success,error=error,logs=logs.ToArray()}));SessionState.SetString("PlayModeTest.State","Done");EditorApplication.isPlaying=false;}
 }
}
