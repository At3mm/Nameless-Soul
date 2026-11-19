using System.Collections.Generic;
using UnityEngine;

namespace World
{
    /// <summary>Same actor and encounter ID: friendly quest giver, then hostile after reporting a quest.</summary>
    [DefaultExecutionOrder(-100),DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterHealth),typeof(EnemyClassification),typeof(Rigidbody2D))]
    public sealed class QuestBossEncounter : MonoBehaviour
    {
        public enum Stage { Friendly,WaitingForCombat,Hostile,Defeated }
        public QuestDefinition requiredQuest;
        public NpcInteractable conversation;
        [Tooltip("Chỉ bắt đầu đánh khi người chơi chọn ChallengeBoss trong hội thoại.")]
        public bool requireDialogueChallenge;
        [Min(0)] public float aggressionDelay=.8f;
        public string idleState="BlackKnightIdle";
        public Stage CurrentStage { get; private set; }
        public bool IsFriendly=>CurrentStage==Stage.Friendly||CurrentStage==Stage.WaitingForCombat;
        private readonly List<Behaviour> suspended=new List<Behaviour>();
        private readonly Dictionary<Transform,int> layers=new Dictionary<Transform,int>();
        private Rigidbody2D body;
        private RigidbodyType2D bossBodyType;
        private RigidbodyConstraints2D bossConstraints;
        private EnemyClassification classification;
        private string bossTag;
        private bool reevaluate=true,initialized,challengeAccepted;
        private float waited;
        public static bool Protects(GameObject actor)
        {var encounter=actor.GetComponent<QuestBossEncounter>();return encounter!=null&&encounter.isActiveAndEnabled&&encounter.IsFriendly;}
        private void Awake(){if(enabled)Initialize();}
        private void Initialize()
        {
            if(initialized)return;
            body=GetComponent<Rigidbody2D>();classification=GetComponent<EnemyClassification>();
            bossBodyType=body.bodyType;bossConstraints=body.constraints;bossTag=gameObject.tag;
            if(EnemyDefeatState.WasDefeated(classification.DefeatId)){CurrentStage=Stage.Defeated;gameObject.SetActive(false);return;}
            GetComponent<EnemyCombat>()?.CancelAttack();
            foreach(var component in GetComponents<Behaviour>())
            {
                if(component is EnemyBehavior||component is EnemyCombat||component is BossMovesetDirector||component is BossPhaseController||component is EnemyBreak||component is EnemyClassification||component is EnemyCurrencyReward||component is EnemyAudio||component is EnemyAreaDamage)
                    if(component.enabled){suspended.Add(component);component.enabled=false;}
            }
            int hostileLayer=gameObject.layer;
            foreach(var child in GetComponentsInChildren<Transform>(true))
            {layers[child]=child.gameObject.layer;if(child.gameObject.layer==hostileLayer)child.gameObject.layer=0;}
            body.linearVelocity=Vector2.zero;body.angularVelocity=0;body.bodyType=RigidbodyType2D.Static;body.constraints=RigidbodyConstraints2D.FreezeAll;
            gameObject.tag="Untagged";
            var animator=GetComponent<Animator>();if(animator!=null){animator.SetBool("IsWalking",false);if(animator.HasState(0,Animator.StringToHash(idleState)))animator.Play(idleState,0,0);}
            if(conversation!=null)conversation.gameObject.SetActive(true);
            initialized=true;
        }
        private void OnEnable(){Initialize();if(!gameObject.activeInHierarchy)return;QuestService.Changed+=RefreshQuestState;reevaluate=true;}
        private void OnDisable(){QuestService.Changed-=RefreshQuestState;}
        public void RefreshQuestState(){reevaluate=true;}
        public bool RequestChallenge()
        {
            if(!isActiveAndEnabled||!initialized||CurrentStage==Stage.Hostile||CurrentStage==Stage.Defeated||
                conversation==null||!conversation.IsOpen||requiredQuest==null||QuestService.Status(requiredQuest)!=QuestStatus.Completed)return false;
            challengeAccepted=true;waited=0;CurrentStage=Stage.WaitingForCombat;reevaluate=false;return true;
        }
        private void Update()
        {
            if(!initialized||CurrentStage==Stage.Defeated||CurrentStage==Stage.Hostile)return;
            if(reevaluate)
            {
                reevaluate=false;
                if(requiredQuest!=null&&QuestService.Status(requiredQuest)==QuestStatus.Completed&&(!requireDialogueChallenge||challengeAccepted))CurrentStage=Stage.WaitingForCombat;
                else {CurrentStage=Stage.Friendly;waited=0;if(conversation!=null)conversation.enabled=true;}
            }
            if(CurrentStage!=Stage.WaitingForCombat)return;
            // Reporting refreshes the dialogue synchronously. Let its final lines and closing fade finish.
            if(conversation!=null&&(conversation.IsOpen||conversation.IsClosing)||NpcInteractable.BlocksPauseMenu||SettingsMenuToggle.AnyOpen||CheckpointMenu.AnyOpen||InGameCutscene.AnyPlaying||Time.timeScale<=0)return;
            if(conversation!=null)conversation.enabled=false;
            waited+=Time.deltaTime;
            if(waited>=Mathf.Max(0,aggressionDelay))BecomeHostile();
        }
        private void BecomeHostile()
        {
            if(conversation!=null)conversation.gameObject.SetActive(false);
            foreach(var item in layers)if(item.Key!=null)item.Key.gameObject.layer=item.Value;
            body.bodyType=bossBodyType;body.constraints=bossConstraints;body.linearVelocity=Vector2.zero;body.angularVelocity=0;
            gameObject.tag=bossTag;CurrentStage=Stage.Hostile;
            foreach(var component in suspended)if(component!=null)component.enabled=true;
            suspended.Clear();
            GetComponent<AnimationEventHandler>()?.UnlockAction();
            GetComponent<EnemyAwareness>()?.MaxAlert();
            var ai=GetComponent<EnemyBehavior>();if(ai!=null)ai.currentState=EnemyBehavior.OrcState.Chase;
        }
    }
}
