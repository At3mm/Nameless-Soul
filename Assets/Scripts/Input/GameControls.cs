using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>Keyboard/mouse bindings shared by gameplay, prompts and the settings screen.</summary>
public static class GameControls
{
    public const string SaveKey="Noname.Controls.v1";
    public enum Action { Up, Down, Left, Right, Dash, Attack, Parry, Skill, Heal, Item, PreviousItem, NextItem, Interact, Pause }
    public sealed class Definition
    {
        public readonly Action action;
        public readonly string label, defaultPath;
        public Definition(Action action,string label,string path){this.action=action;this.label=label;defaultPath=path;}
    }
    private static readonly Definition[] definitions={
        new Definition(Action.Up,"Di chuyển lên","<Keyboard>/w"),new Definition(Action.Down,"Di chuyển xuống","<Keyboard>/s"),
        new Definition(Action.Left,"Di chuyển trái","<Keyboard>/a"),new Definition(Action.Right,"Di chuyển phải","<Keyboard>/d"),
        new Definition(Action.Dash,"Lướt / giữ để chạy","<Keyboard>/leftShift"),new Definition(Action.Attack,"Đánh thường","<Mouse>/leftButton"),
        new Definition(Action.Parry,"Đỡ đòn","<Mouse>/rightButton"),new Definition(Action.Skill,"Kỹ năng","<Keyboard>/space"),
        new Definition(Action.Heal,"Uống bình máu","<Keyboard>/r"),new Definition(Action.Item,"Dùng vật phẩm","<Keyboard>/c"),
        new Definition(Action.PreviousItem,"Vật phẩm trước","<Keyboard>/q"),new Definition(Action.NextItem,"Vật phẩm tiếp","<Keyboard>/f"),
        new Definition(Action.Interact,"Tương tác / nói chuyện","<Keyboard>/e"),new Definition(Action.Pause,"Tạm dừng","<Keyboard>/escape")
    };
    public static IReadOnlyList<Definition> Definitions=>Array.AsReadOnly(definitions);
    [Serializable] private sealed class Entry { public int action; public string path; }
    [Serializable] private sealed class Save { public List<Entry> bindings=new List<Entry>(); }
    private sealed class LiveBinding { public InputAction input; public int index; public Action action; }
    private static readonly Dictionary<Action,string> paths=new Dictionary<Action,string>();
    private static readonly List<LiveBinding> live=new List<LiveBinding>();
    private static bool loaded;
    private static int blockedFrame=-1;
    public static event System.Action Changed;
    public static bool Blocked=>ControlBindingsMenu.IsOpen||Time.frameCount<=blockedFrame;
    public static void SuppressThisFrame(){blockedFrame=Time.frameCount;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){paths.Clear();live.Clear();loaded=false;Changed=null;blockedFrame=-1;}
    public static string Path(Action action){Load();return paths[action];}
    private static void Load()
    {
        if(loaded)return;loaded=true;foreach(var d in definitions)paths[d.action]=d.defaultPath;
        try
        {
            var save=JsonUtility.FromJson<Save>(PlayerPrefs.GetString(SaveKey,""));
            if(save==null||save.bindings==null)return;
            var proposed=new Dictionary<Action,string>(paths);var seen=new HashSet<Action>();
            foreach(var e in save.bindings)
            {
                if(e==null||!Enum.IsDefined(typeof(Action),e.action)||!seen.Add((Action)e.action)||!Valid(e.path,(Action)e.action))return;
                proposed[(Action)e.action]=e.path;
            }
            var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var p in proposed.Values)if(!used.Add(p))return;
            foreach(var p in proposed)paths[p.Key]=p.Value;
        }
        catch(ArgumentException){} // A damaged settings record falls back to the defaults.
    }
    private static bool Valid(string path,Action action)
    {
        if(string.IsNullOrEmpty(path))return false;
        if(path=="<Keyboard>/escape")return action==Action.Pause;
        if(path=="<Mouse>/leftButton"||path=="<Mouse>/rightButton"||path=="<Mouse>/middleButton"||path=="<Mouse>/forwardButton"||path=="<Mouse>/backButton")return true;
        const string prefix="<Keyboard>/";
        if(!path.StartsWith(prefix,StringComparison.Ordinal))return false;
        var layout=InputSystem.LoadLayout("Keyboard");
        foreach(var c in layout.controls)if(c.layout.ToString()=="Key"&&c.name.ToString()==path.Substring(prefix.Length))return true;
        return false;
    }
    public static bool TrySet(Action action,string path,out string message)
    {
        Load();message="";
        if(!Enum.IsDefined(typeof(Action),action)||!Valid(path,action)){message="Phím này không dùng được. Esc dành để hủy / quay lại.";return false;}
        foreach(var d in definitions)if(d.action!=action&&string.Equals(paths[d.action],path,StringComparison.OrdinalIgnoreCase))
        {message="Đã dùng cho: "+d.label+". Hãy đổi phím của thao tác đó trước.";return false;}
        paths[action]=path;Persist();return true;
    }
    public static void RestoreDefaults(){Load();foreach(var d in definitions)paths[d.action]=d.defaultPath;Persist();}
    private static void Persist()
    {
        var save=new Save();foreach(var d in definitions)save.bindings.Add(new Entry{action=(int)d.action,path=paths[d.action]});
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(save));PlayerPrefs.Save();
        var enabled=new HashSet<InputAction>();foreach(var binding in live)if(binding.input.enabled)enabled.Add(binding.input);
        foreach(var input in enabled)input.Disable();
        foreach(var binding in live)binding.input.ApplyBindingOverride(binding.index,paths[binding.action]);
        foreach(var input in enabled)input.Enable();
        SuppressThisFrame();Changed?.Invoke();
    }
    private static void Track(InputAction input,int index,Action action)
    {input.ApplyBindingOverride(index,Path(action));live.Add(new LiveBinding{input=input,index=index,action=action});}
    public static InputAction CreateButton(Action action)
    {
        var input=new InputAction(action.ToString(),InputActionType.Button,definitions[(int)action].defaultPath);Track(input,0,action);
        if(action==Action.Dash)input.AddBinding("<Gamepad>/buttonEast");
        if(action==Action.Pause)input.AddBinding("<Keyboard>/escape"); // Always keep a way back into settings.
        return input;
    }
    public static InputAction CreateMove()
    {
        var input=new InputAction("Move",InputActionType.Value,"<Gamepad>/leftStick");
        input.AddCompositeBinding("2DVector").With("Up",definitions[0].defaultPath).With("Down",definitions[1].defaultPath).With("Left",definitions[2].defaultPath).With("Right",definitions[3].defaultPath);
        for(int i=0;i<4;i++)Track(input,i+2,(Action)i);return input;
    }
    public static void Release(InputAction input){if(input==null)return;live.RemoveAll(x=>x.input==input);input.Dispose();}
    public static ButtonControl Button(Action action)
    {
        var path=Path(action);int slash=path.IndexOf('/');
        InputDevice device=path.StartsWith("<Keyboard>")?(InputDevice)Keyboard.current:Mouse.current;
        return device!=null?device.TryGetChildControl<ButtonControl>(path.Substring(slash+1)):null;
    }
    public static bool Pressed(Action action)=>!Blocked&&Button(action)?.wasPressedThisFrame==true;
    public static string Display(Action action)
    {
        var path=Path(action);
        switch(path){case "<Mouse>/leftButton":return "Chuột trái";case "<Mouse>/rightButton":return "Chuột phải";case "<Mouse>/middleButton":return "Chuột giữa";case "<Mouse>/forwardButton":return "Chuột 5";case "<Mouse>/backButton":return "Chuột 4";}
        return InputControlPath.ToHumanReadableString(path,InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
}
