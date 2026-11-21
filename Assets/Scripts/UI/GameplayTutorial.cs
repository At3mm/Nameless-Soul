using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using World;

/// <summary>Brief contextual hints during ordinary exploration of the first area.</summary>
public sealed class GameplayTutorial : MonoBehaviour
{
    public enum Lesson { Move, Dash, Attack, Parry, Skill, Heal, Pray }
    public struct Context
    {
        public bool inArea, nearEnemy, skillReady, needsHealing, nearShrine;
        public float travelled;
    }
    public const string SaveKey = "Noname.GameplayTutorial.v1";
    private const int CompleteMask=127, SeenShift=8;
    public const int SessionStarted=1<<16, SessionClosed=1<<17;
    private static bool freshRunRequested;
    private bool ownsSession;
    private static GameplayTutorial instance;
    private GameplayTutorialSettings settings;
    private bool ownsSettings;
    private GameObject player;
    private CharacterStats stats;
    private CharacterHealth health;
    private CanvasGroup group;
    private Text title,body;
    private RectTransform timer;
    private const float HintWidth=400f;
    private Vector3 lastPosition;
    private float distance,age,scanAt,hintAge,nextHintAt;
    private Lesson? activeHint;
    private static int State=>int.TryParse(PlayerPrefs.GetString(SaveKey,"0"),out int value)?value&(CompleteMask|CompleteMask<<SeenShift|SessionStarted|SessionClosed):0;
    private static bool Done(int state,Lesson lesson)=>(state&(1<<(int)lesson))!=0;
    private static bool Introduced(int state,Lesson lesson)=>Done(state,lesson)||(state&(1<<((int)lesson+SeenShift)))!=0;
    private static bool Finished(int state)=>(state&SessionClosed)!=0||((state|(state>>SeenShift))&CompleteMask)==CompleteMask;
    public static bool CanBeginSession(int state)=>state==0;
    public static Lesson? SelectHint(int state,Context context,GameplayTutorialSettings config)
    {
        if(!context.inArea||config==null||Finished(state))return null;
        if(context.needsHealing&&!Introduced(state,Lesson.Heal))return Lesson.Heal;
        if(!Introduced(state,Lesson.Move))return Lesson.Move;
        if(context.nearShrine&&!Introduced(state,Lesson.Pray))return Lesson.Pray;
        if(context.nearEnemy)
        {
            if(!Introduced(state,Lesson.Attack))return Lesson.Attack;
            if(context.skillReady&&!Introduced(state,Lesson.Skill))return Lesson.Skill;
            if(Done(state,Lesson.Attack)&&!Introduced(state,Lesson.Parry))return Lesson.Parry;
        }
        if(Done(state,Lesson.Move)&&context.travelled>=config.dashHintDistance&&!Introduced(state,Lesson.Dash))return Lesson.Dash;
        return null;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic(){instance=null;freshRunRequested=false;SceneManager.sceneLoaded-=OnSceneLoaded;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap(){SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;Attach(SceneManager.GetActiveScene());}
    private static void OnSceneLoaded(Scene scene,LoadSceneMode mode){if(mode==LoadSceneMode.Single)Attach(scene);}
    private static void Attach(Scene scene)
    {
        if(scene.path==MainMenuController.ScenePath||instance!=null)return;
        var config=Resources.Load<GameplayTutorialSettings>("DefaultGameplayTutorial");
        if(scene.name!=(config!=null?config.sceneName:"WastelandScene"))return;
        if(!freshRunRequested&&MainMenuController.HasProgress)return;
        if(!CanBeginSession(State))return;
        // Persist the claim before creating UI: reloads and application restarts cannot reopen it.
        PlayerPrefs.SetString(SaveKey,(State|SessionStarted).ToString());PlayerPrefs.Save();freshRunRequested=false;
        var go=new GameObject("Area 1 Hints",typeof(RectTransform));SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<GameplayTutorial>();
    }
    public static void BeginNewRun(){freshRunRequested=true;PlayerPrefs.SetString(SaveKey,"0");PlayerPrefs.Save();}
    public static void Report(GameObject actor,Lesson lesson)
    {
        if(instance==null||actor==null||!actor.CompareTag("Player")||!actor.activeInHierarchy||!System.Enum.IsDefined(typeof(Lesson),lesson)||!instance.ownsSession||(State&SessionClosed)!=0||!instance.InArea(actor))return;
        instance.Mark(lesson);
    }
    private void Mark(Lesson lesson)
    {
        int next=State|(1<<(int)lesson);if(next==State)return;
        PlayerPrefs.SetString(SaveKey,next.ToString());PlayerPrefs.Save();
    }
    public void Skip(){CloseSession();}
    private void CancelActiveHint()
    {
        if(activeHint.HasValue){activeHint=null;nextHintAt=age+settings.hintSpacing;}
        Hide();
    }
    private void CloseSession()
    {
        if(ownsSession)
        {
            int next=State|SessionClosed;if(next!=State){PlayerPrefs.SetString(SaveKey,next.ToString());PlayerPrefs.Save();}
            ownsSession=false;
        }
        activeHint=null;Hide();
    }
    private void OnApplicationQuit(){CloseSession();}
    private void Awake()
    {
        instance=this;ownsSession=(State&SessionStarted)!=0&&(State&SessionClosed)==0;settings=Resources.Load<GameplayTutorialSettings>("DefaultGameplayTutorial");
        if(settings==null){settings=ScriptableObject.CreateInstance<GameplayTutorialSettings>();ownsSettings=true;}Build();
    }
    private void OnDestroy(){if(Application.isPlaying)CloseSession();if(instance==this)instance=null;if(ownsSettings)Destroy(settings);}
    private bool InArea(GameObject actor)
    {
        if(actor==null||settings==null||actor.scene.name!=settings.sceneName)return false;
        var zone=AreaManager.FindArea(actor.transform.position,actor.scene.handle);
        return zone!=null&&zone.area!=null&&zone.area.areaId==settings.areaId;
    }
    private bool Blocked=>Time.timeScale<=0||GameControls.Blocked||SettingsMenuToggle.AnyOpen||CheckpointMenu.AnyOpen||NpcInteractable.BlocksPauseMenu||InGameCutscene.AnyPlaying||CheckpointService.IsTraveling||CheckpointService.IsRespawning||ScreenFader.Instance.IsTransitioning;
    private void Hide(){if(group!=null){group.alpha=0;group.blocksRaycasts=false;}}
    private void Update()
    {
        if(Finished(State)&&!activeHint.HasValue){Hide();return;}
        if(player==null&&Time.unscaledTime>=scanAt)
        {
            scanAt=Time.unscaledTime+.5f;player=GameObject.FindGameObjectWithTag("Player");
            if(player!=null){stats=player.GetComponent<CharacterStats>();health=player.GetComponent<CharacterHealth>();lastPosition=player.transform.position;}
        }
        if(player==null||stats==null||health==null){Hide();return;}
        if(health.isDead||CheckpointService.IsRespawning||CheckpointService.IsTraveling||!InArea(player))
        {
            CloseSession();lastPosition=player.transform.position;return;
        }
        if(Blocked){CancelActiveHint();lastPosition=player.transform.position;return;}
        age+=Time.deltaTime;Vector3 position=player.transform.position;float delta=Vector2.Distance(lastPosition,position);lastPosition=position;
        var dash=player.GetComponent<PlayerDash>();var controller=player.GetComponent<PlayerController>();var actions=player.GetComponent<AnimationEventHandler>();
        if(delta<1f&&controller!=null&&controller.enabled&&(actions==null||!actions.isActionLocked)&&(dash==null||!dash.isDashing))
        {distance+=delta;if(!Done(State,Lesson.Move)&&distance>=settings.moveDistance)Mark(Lesson.Move);}
        if(activeHint.HasValue)
        {
            hintAge+=Time.deltaTime;
            if(Done(State,activeHint.Value)||hintAge>=settings.hintDuration){activeHint=null;nextHintAt=age+settings.hintSpacing;Hide();return;}
            Present(activeHint.Value);return;
        }
        if(age<settings.initialDelay||age<nextHintAt){Hide();return;}
        var context=new Context{inArea=true,travelled=distance,needsHealing=stats.currentHP<stats.maxHP*.65f&&player.GetComponent<PlayerHealthPotion>()?.Charges>0};
        foreach(var enemy in EnemyClassification.Active)
            if(enemy!=null&&enemy.isActiveAndEnabled&&enemy.gameObject.scene==player.scene&&enemy.Health!=null&&!enemy.Health.isDead&&!QuestBossEncounter.Protects(enemy.gameObject)&&Vector2.Distance(position,enemy.transform.position)<=settings.combatHintDistance){context.nearEnemy=true;break;}
        foreach(var interactable in WorldInteractable.Active)
            if(interactable is CheckpointInteractable&&interactable.gameObject.scene==player.scene&&interactable.IsAvailable&&Vector2.Distance(position,interactable.InteractionPosition)<=settings.shrineHintDistance){context.nearShrine=true;break;}
        var skills=player.GetComponent<SkillDamageManager>();var skill=skills!=null?skills.skills.Find(s=>s!=null&&s.skillName=="Skill2"):null;
        context.skillReady=skill!=null&&(!skill.usePostureToActivate||stats.HasEnoughPosture(skill.postureCost));
        var next=SelectHint(State,context,settings);if(!next.HasValue){Hide();return;}
        activeHint=next;hintAge=0;PlayerPrefs.SetString(SaveKey,(State|(1<<((int)next.Value+SeenShift))).ToString());PlayerPrefs.Save();Present(next.Value);
    }
    private void Present(Lesson lesson)
    {
        string[] headings={"DI CHUYỂN","LƯỚT VÀ CHẠY","CHIẾN ĐẤU","ĐỠ VÀ PHẢN ĐÒN","CHIẾN Ý","HỒI PHỤC","TƯỢNG THẦN"};
        string[] prompts={settings.move,settings.dash,settings.attack,settings.parry,settings.skill,settings.heal,settings.shrine};
        title.text=headings[(int)lesson];body.text=ExpandStyled(prompts[(int)lesson]);
        float height=Mathf.Max(136f,Mathf.Ceil(body.preferredHeight)+86f);
        var rect=(RectTransform)group.transform;rect.sizeDelta=new Vector2(HintWidth,height);
        ((RectTransform)body.transform).sizeDelta=new Vector2(HintWidth-48,height-86);
        float enter=Mathf.SmoothStep(0,1,PixelAnimationSettings.Progress(hintAge,.25f));
        float exit=Mathf.Clamp01((settings.hintDuration-PixelAnimationSettings.Sample(hintAge))/.3f);
        rect.anchoredPosition=new Vector2(-28+Mathf.Round((1-enter)*16),100);
        timer.anchorMax=new Vector2(Mathf.Clamp01(1-PixelAnimationSettings.Progress(hintAge,settings.hintDuration)),1);
        group.alpha=Mathf.Min(enter,exit);group.blocksRaycasts=false;
    }
    public static string Expand(string text)
    {
        text=text??"";foreach(var definition in GameControls.Definitions)text=text.Replace("{"+definition.action+"}",GameControls.Display(definition.action));return text;
    }
    private static string ExpandStyled(string text)
    {
        text=System.Security.SecurityElement.Escape(text??"");
        foreach(var definition in GameControls.Definitions)
            text=text.Replace("{"+definition.action+"}","<color=#F1D699>"+System.Security.SecurityElement.Escape(GameControls.Display(definition.action))+"</color>");
        return text;
    }
    private void Build()
    {
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;canvas.pixelPerfect=true;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var panel=new GameObject("Context hint",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));panel.transform.SetParent(transform,false);
        var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-28,100);rect.sizeDelta=new Vector2(HintWidth,150);
        var image=panel.GetComponent<Image>();var style=PixelMenuButtonStyle.Current;
        image.sprite=style!=null?style.background:null;image.type=Image.Type.Sliced;
        image.color=new Color(.34f,.32f,.25f,.97f);image.raycastTarget=false;
        group=panel.GetComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
        var gold=new Color(.87f,.74f,.48f);
        title=Label(rect,"Hint title",new Vector2(26,-18),new Vector2(HintWidth-52,26),18,gold);
        var marker=Label(rect,"Pixel ornament",new Vector2(13,-17),new Vector2(12,26),16,gold);marker.text="›";
        body=Label(rect,"Hint text",new Vector2(24,-58),new Vector2(HintWidth-48,100),20,new Color(.91f,.88f,.78f));body.supportRichText=true;body.lineSpacing=1.12f;
        Decoration(rect,"Header rule",new Vector2(24,-48),new Vector2(HintWidth-48,1),new Color(.65f,.54f,.32f,.45f));
        var track=Decoration(rect,"Time track",Vector2.zero,Vector2.zero,new Color(.10f,.09f,.06f,.8f));
        track.anchorMin=new Vector2(0,0);track.anchorMax=new Vector2(1,0);track.pivot=Vector2.zero;track.offsetMin=new Vector2(24,14);track.offsetMax=new Vector2(-24,16);
        timer=Decoration(track,"Time remaining",Vector2.zero,Vector2.zero,new Color(.70f,.58f,.34f,.75f));
        timer.anchorMin=Vector2.zero;timer.anchorMax=Vector2.one;timer.offsetMin=timer.offsetMax=Vector2.zero;
    }
    private RectTransform Decoration(RectTransform parent,string name,Vector2 pos,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;
        var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return rect;
    }
    private Text Label(RectTransform parent,string name,Vector2 pos,Vector2 size,int fontSize,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=pos;rect.sizeDelta=size;
        var label=go.GetComponent<Text>();label.font=GameFont.Current;label.fontSize=fontSize;label.color=color;label.raycastTarget=false;label.supportRichText=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;
        var shadow=go.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.85f);shadow.effectDistance=new Vector2(1,-1);return label;
    }
}
