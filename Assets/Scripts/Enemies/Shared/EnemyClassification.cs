using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats), typeof(CharacterHealth))]
public class EnemyClassification : MonoBehaviour
{
    public enum EnemyType { Normal, Elite, Boss }
    [Tooltip("Normal: quái thường; Elite: tinh anh; Boss: trùm.")]
    public EnemyType enemyType;
    public string displayName;
    [Tooltip("Stable boss type ID used by quest objectives, e.g. orc-rider or necromancer.")] public string questBossId;
    [Tooltip("Optional stable encounter ID for spawned Elite/Boss. Scene instances use their own stable scene identity.")]
    public string persistentId;
    [Tooltip("Keep this encounter ID when replacing its prefab. Preserves existing save/quest references; use a unique ID for every placed encounter.")]
    public bool preservePersistentId;
    private string defeatId;
    public string DefeatId => string.IsNullOrEmpty(defeatId)?(defeatId=EnemyDefeatState.GetIdentity(this)):defeatId;
    [Header("Hồi máu khi quay về (Elite / Boss)")]
    public bool healOnReturn = true;
    [Range(0f, 100f)] public float returnHealPercentPerSecond = 25f;
    public bool fullyHealWhenHome = true;
    public static readonly HashSet<EnemyClassification> Active = new HashSet<EnemyClassification>();
    public CharacterStats Stats { get; private set; }
    public CharacterHealth Health { get; private set; }
    public EnemyBehavior AI { get; private set; }
    public bool UsesDragonBar => enemyType != EnemyType.Normal;
    public bool IsEngaged => (AI != null && AI.currentState != EnemyBehavior.OrcState.Patrol) ||
        (GetComponent<EnemyBreak>() != null && GetComponent<EnemyBreak>().IsBroken);
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name.Replace("(Clone)", "").Trim() : displayName;
    private EnemyHealthBar[] worldHealthBars;

    private void Awake()
    {
        Stats = GetComponent<CharacterStats>();
        Health = GetComponent<CharacterHealth>();
        World.NormalEnemyRespawns.Register(this);
        if(ApplySavedDefeat())return;
        AI = GetComponent<EnemyBehavior>();
        if (GetComponent<EnemyCurrencyReward>() == null) gameObject.AddComponent<EnemyCurrencyReward>();
    }
    private void Start() { ApplyTag(); RefreshWorldHealthBars(); }
    private void OnEnable()
    {
        if(Application.isPlaying)World.NormalEnemyRespawns.Register(this);
        if(Application.isPlaying && ApplySavedDefeat())return;
        Active.Add(this);
#if UNITY_EDITOR
        if (!Application.isPlaying) { QueueEditorRefresh(); return; }
#endif
        RefreshWorldHealthBars();
    }
    private void OnDisable() { Active.Remove(this); }
    private bool ApplySavedDefeat()
    {
        if(!Application.isPlaying || enemyType==EnemyType.Normal || !EnemyDefeatState.WasDefeated(DefeatId))return false;
        var health=GetComponent<CharacterHealth>();if(health!=null)health.isDead=true;
        var stats=GetComponent<CharacterStats>();if(stats!=null)stats.currentHP=0;
        gameObject.SetActive(false);return true;
    }
    private void OnValidate()
    {
#if UNITY_EDITOR
        QueueEditorRefresh();
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Quests/Copy Encounter ID")]
    private void CopyQuestEncounterId(){UnityEditor.EditorGUIUtility.systemCopyBuffer=DefeatId;Debug.Log("Quest encounter ID: "+DefeatId,this);}
    private void QueueEditorRefresh()
    {
        // Tag changes and SetActive send messages: defer both until validation is complete.
        UnityEditor.EditorApplication.update -= ApplyEditorRefresh;
        UnityEditor.EditorApplication.update += ApplyEditorRefresh;
    }
    private void ApplyEditorRefresh()
    {
        UnityEditor.EditorApplication.update -= ApplyEditorRefresh;
        if (this == null) return;
        if(!Application.isPlaying && !preservePersistentId && enemyType!=EnemyType.Normal && !UnityEditor.EditorUtility.IsPersistent(this))
        {
            string sceneId=EnemyDefeatState.GetEditorSceneIdentity(this);
            if(!string.IsNullOrEmpty(sceneId) && persistentId!=sceneId &&
                (string.IsNullOrEmpty(persistentId) || persistentId.Contains("|GlobalObjectId_")))
            {
                persistentId=sceneId;defeatId=null;UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
        ApplyTag();
        RefreshWorldHealthBars();
    }
    private void OnDestroy() { UnityEditor.EditorApplication.update -= ApplyEditorRefresh; }
#endif

    public void RefreshWorldHealthBars()
    {
        if (this == null) return;
        if (worldHealthBars == null) worldHealthBars = GetComponentsInChildren<EnemyHealthBar>(true);
        bool show = !UsesDragonBar;
        foreach (var bar in worldHealthBars)
            if (bar != null && bar.gameObject != gameObject && bar.gameObject.activeSelf != show)
                bar.gameObject.SetActive(show);
    }
    private void ApplyTag()
    {
        string value = enemyType == EnemyType.Boss ? "EnemyBoss" : enemyType == EnemyType.Elite ? "EnemyElite" : "EnemyNormal";
        try { if (gameObject.tag != value) gameObject.tag = value; }
        catch (UnityException) { /* Tags are registered by project setup. */ }
    }
    private void LateUpdate()
    {
        ApplyTag();
        RefreshWorldHealthBars();
        if (!BossPhaseController.Protects(gameObject) && UsesDragonBar && healOnReturn && Health != null && !Health.isDead && AI != null && AI.currentState == EnemyBehavior.OrcState.Return &&
            (GetComponent<EnemyBreak>() == null || !GetComponent<EnemyBreak>().IsBroken))
            Stats.currentHP = Mathf.Min(Stats.maxHP, Stats.currentHP + Stats.maxHP * returnHealPercentPerSecond * .01f * Time.deltaTime);
    }
    public void OnReturnedHome()
    {
        if (UsesDragonBar && healOnReturn && fullyHealWhenHome && Health != null && !Health.isDead)
        {
            Stats.currentHP = Stats.maxHP;
            GetComponent<BossPhaseController>()?.ResetForEncounter();
        }
    }
}
