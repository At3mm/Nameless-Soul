using System;
using UnityEditor;
using UnityEngine;
public static class AreaOneHintValidation
{
    static void Check(bool ok,string label,ref int count){if(!ok)throw new InvalidOperationException(label);count++;}
    static int Done(GameplayTutorial.Lesson l)=>1<<(int)l;
    static int Seen(GameplayTutorial.Lesson l)=>1<<((int)l+8);
    [MenuItem("Tools/Game Testing/Validate Area 1 Hints")]
    public static void RunMenu(){Debug.Log("Area 1 hint checks: "+Run());}
    public static int Run()
    {
        int checks=0;var s=ScriptableObject.CreateInstance<GameplayTutorialSettings>();
        try
        {
            var c=new GameplayTutorial.Context{inArea=true};
            Check(GameplayTutorial.SelectHint(0,c,s)==GameplayTutorial.Lesson.Move,"First contextual movement hint",ref checks);
            c.inArea=false;c.nearEnemy=c.nearShrine=c.needsHealing=c.skillReady=true;c.travelled=100;
            for(int state=0;state<128;state++)Check(!GameplayTutorial.SelectHint(state,c,s).HasValue,"No hint outside area 1",ref checks);
            c=new GameplayTutorial.Context{inArea=true};int moved=Done(GameplayTutorial.Lesson.Move);
            Check(!GameplayTutorial.SelectHint(moved,c,s).HasValue,"No forced next lesson while idle",ref checks);
            c.travelled=s.dashHintDistance;Check(GameplayTutorial.SelectHint(moved,c,s)==GameplayTutorial.Lesson.Dash,"Dash after exploration",ref checks);
            c.travelled=0;c.nearEnemy=true;Check(GameplayTutorial.SelectHint(moved,c,s)==GameplayTutorial.Lesson.Attack,"Attack only near enemy",ref checks);
            Check(!GameplayTutorial.SelectHint(moved|Seen(GameplayTutorial.Lesson.Attack),c,s).HasValue,"Seen attack does not force parry before an attack",ref checks);
            Check(GameplayTutorial.SelectHint(moved|Done(GameplayTutorial.Lesson.Attack),c,s)==GameplayTutorial.Lesson.Parry,"Contextual parry after attacking",ref checks);
            c.skillReady=true;Check(GameplayTutorial.SelectHint(moved|Done(GameplayTutorial.Lesson.Attack),c,s)==GameplayTutorial.Lesson.Skill,"Skill hint when usable",ref checks);
            c.needsHealing=true;Check(GameplayTutorial.SelectHint(moved,c,s)==GameplayTutorial.Lesson.Heal,"Healing when injured",ref checks);
            c.needsHealing=c.nearEnemy=c.skillReady=false;c.nearShrine=true;Check(GameplayTutorial.SelectHint(moved,c,s)==GameplayTutorial.Lesson.Pray,"Prayer near shrine",ref checks);
            c.nearShrine=false;Check(!GameplayTutorial.SelectHint(moved|Seen(GameplayTutorial.Lesson.Pray),c,s).HasValue,"No persistent explore task",ref checks);
            int allSeen=127<<8;Check(!GameplayTutorial.SelectHint(allSeen,c,s).HasValue,"Seen hints do not replay",ref checks);
            Check(!GameplayTutorial.SelectHint(127,c,s).HasValue,"Completed old saves remain complete",ref checks);
            Check(GameplayTutorial.CanBeginSession(0),"Fresh run may begin its one intro session",ref checks);
            for(int state=1;state<128;state++)Check(!GameplayTutorial.CanBeginSession(state),"Legacy progress cannot reopen an intro",ref checks);
            foreach(GameplayTutorial.Lesson lesson in Enum.GetValues(typeof(GameplayTutorial.Lesson)))
                Check(!GameplayTutorial.CanBeginSession(Seen(lesson)),"An already viewed hint cannot reopen a session",ref checks);
            Check(!GameplayTutorial.CanBeginSession(GameplayTutorial.SessionStarted),"Persisted start claim blocks restart and reload",ref checks);
            Check(!GameplayTutorial.CanBeginSession(GameplayTutorial.SessionClosed),"Closed first session never restarts",ref checks);
            c=new GameplayTutorial.Context{inArea=true,nearEnemy=true,nearShrine=true,needsHealing=true,skillReady=true,travelled=100};
            for(int state=0;state<128;state++)Check(!GameplayTutorial.SelectHint(state|GameplayTutorial.SessionClosed,c,s).HasValue,"No late or repeated hint after intro closed",ref checks);
            Check(s.areaId=="area1"&&s.sceneName=="WastelandScene","Configured area identity",ref checks);
            Check(!GameplayTutorial.Expand("{Attack} / {Dash}").Contains("{"),"Uses current key bindings",ref checks);
        }
        finally{UnityEngine.Object.DestroyImmediate(s);}
        return checks;
    }
}
