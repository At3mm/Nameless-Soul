using UnityEngine;

[DisallowMultipleComponent,RequireComponent(typeof(CharacterHealth))]
public sealed class EnemyItemDrop : MonoBehaviour
{
    public GameItemDefinition item;
    [Min(1)] public int quantity=1;
    [Range(0,1)] public float chance=1f;
    public bool oncePerEncounter=true;
    private CharacterHealth health;
    private bool granted;
    private void Awake(){health=GetComponent<CharacterHealth>();CharacterHealth.AnyDied+=OnDied;}
    private void OnDestroy(){CharacterHealth.AnyDied-=OnDied;}
    private void OnDied(CharacterHealth dead)
    {
        // CharacterHealth disables behaviours before broadcasting death; keep this subscription until destruction.
        if(dead!=health||granted||item==null||!health.isDead)return;
        granted=true;if(Random.value>=chance)return;
        var enemy=GetComponent<EnemyClassification>();
        string receipt=oncePerEncounter&&enemy!=null?"item-drop:"+enemy.DefeatId+":"+item.itemId:null;
        if(QuestService.GrantItem(item,quantity,receipt))
            FindFirstObjectByType<World.PlayerInteractor>()?.ShowNotice("Đã nhận "+quantity+" × "+item.displayName+".");
    }
}
