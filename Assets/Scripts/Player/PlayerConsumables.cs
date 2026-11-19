using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterHealth))]
public sealed class PlayerConsumables : MonoBehaviour
{
    [Min(0)] public float useCooldown = 1;
    private float readyAt;
    public ConsumableKind Selected => GameEconomy.SelectedConsumable;
    public GameItemDefinition SelectedCustom => GameItemDefinition.FindItem(GameEconomy.SelectedCustomItem);
    private bool usingCustom;
    public bool CanAct
    {
        get
        {
            var health=GetComponent<CharacterHealth>();var controller=GetComponent<PlayerController>();
            var action=GetComponent<AnimationEventHandler>();var dash=GetComponent<PlayerDash>();
            return isActiveAndEnabled && health!=null && !health.isDead && Time.timeScale>0 && !World.NpcInteractable.BlocksPauseMenu &&
                (controller==null || controller.isActiveAndEnabled) && (action==null || !action.isActionLocked) && (dash==null || !dash.isDashing);
        }
    }
    public bool Equip(ConsumableKind kind)
    {
        if(GameEconomy.QuickItemIndex(kind)<0)return false;
        bool changed=SelectedCustom!=null||Selected!=kind;
        GameEconomy.SelectConsumable(kind);
        if(changed && Selected==kind)MenuSFX.Play(MenuSFX.Kind.Move);
        return true;
    }
    public void Cycle(int direction)
    {
        if(!CanAct || GameEconomy.QuickItemCount==0)return;
        int count=GameEconomy.QuickItemCount;
        int legacy=GameEconomy.LegacyQuickItemCount;
        int index=SelectedCustom!=null?legacy:GameEconomy.QuickItemIndex(Selected);
        if(SelectedCustom!=null)for(int i=0;i<count-legacy;i++)if(GameEconomy.CustomQuickItemAt(i)==SelectedCustom.itemId){index=legacy+i;break;}
        int next=(index+(direction<0?-1:1)+count)%count;
        if(next<legacy)Equip(GameEconomy.QuickItemAt(next));else GameEconomy.SelectCustomItem(GameEconomy.CustomQuickItemAt(next-legacy));
    }
    public void SelectFirst()
    {
        if(CanAct && GameEconomy.QuickItemCount>0){if(GameEconomy.LegacyQuickItemCount>0)Equip(GameEconomy.QuickItemAt(0));else GameEconomy.SelectCustomItem(GameEconomy.CustomQuickItemAt(0));}
    }
    public bool TryUseSelected() => SelectedCustom!=null?TryUseItem(SelectedCustom):TryUse(Selected);
    public bool TryUseItem(GameItemDefinition item)
    {
        if(!CanAct||GameControls.Blocked||usingCustom)return false;
        var actor=GetComponent<World.PlayerInteractor>();
        if(item==null||!item.CanUse){actor?.ShowNotice("Vật phẩm này không có hiệu ứng sử dụng.");return false;}
        if(ItemInventory.Count(item)<=0){actor?.ShowNotice("Đã hết "+item.displayName+".");return false;}
        if(Time.time<readyAt){actor?.ShowNotice("Chưa thể dùng vật phẩm tiếp.");return false;}
        usingCustom=true;bool applied=false;
        try
        {
            var stats=GetComponent<CharacterStats>();var health=GetComponent<CharacterHealth>();
            // Validate every configured effect before applying any of them.
            foreach(var effect in item.effects)
                if(effect==null||!System.Enum.IsDefined(typeof(ItemEffectKind),effect.kind)||float.IsNaN(effect.percent)||float.IsInfinity(effect.percent)||effect.percent<=0||effect.percent>100||float.IsNaN(effect.duration)||float.IsInfinity(effect.duration)||effect.duration<=0||
                    effect.kind==ItemEffectKind.ReturnToShrine&&item.effects.Length!=1)
                {actor?.ShowNotice("Hiệu ứng vật phẩm chưa được cấu hình hợp lệ.");return false;}
            foreach(var effect in item.effects)
            {
                switch(effect.kind)
                {
                    case ItemEffectKind.HealHealth:
                        if(stats!=null&&health.Heal(stats.maxHP*effect.percent*.01f)>0){applied=true;CombatSFX.Heal(transform.position);}break;
                    case ItemEffectKind.RestoreStamina:
                        if(stats!=null&&stats.currentStamina<stats.maxStamina){stats.currentStamina=Mathf.Min(stats.maxStamina,stats.currentStamina+stats.maxStamina*effect.percent*.01f);applied=true;}break;
                    case ItemEffectKind.CurePoison: case ItemEffectKind.CureBurn: case ItemEffectKind.CureStress:
                        var debuffs=GetComponent<CharacterDebuffs>();
                        var kind=effect.kind==ItemEffectKind.CurePoison?CharacterDebuffs.Kind.Poison:effect.kind==ItemEffectKind.CureBurn?CharacterDebuffs.Kind.Burning:CharacterDebuffs.Kind.Stress;
                        bool active=debuffs!=null&&(kind==CharacterDebuffs.Kind.Poison?debuffs.IsPoisoned:kind==CharacterDebuffs.Kind.Burning?debuffs.IsBurning:debuffs.IsStressed);
                        if(debuffs!=null&&(active||debuffs.GetBuildup(kind)>0)){debuffs.Remove(kind);applied=true;CombatSFX.Cleanse(transform.position);}break;
                    case ItemEffectKind.ReturnToShrine:
                        applied=World.CheckpointService.ReturnToLastShrine(actor);if(!applied)return false;break;
                    default:
                        if(stats==null)break;
                        var buffs=GetComponent<CharacterBuffs>();if(buffs==null)buffs=gameObject.AddComponent<CharacterBuffs>();
                        var buff=(CharacterBuffs.Kind)((int)effect.kind-(int)ItemEffectKind.Power);
                        buffs.Apply(buff,effect.duration,effect.percent);BuffUseVFX.Play(gameObject,buff);CombatSFX.Buff(buff,transform.position);applied=true;break;
                }
            }
            if(!applied){actor?.ShowNotice("Không có chỉ số hoặc trạng thái cần hồi phục.");return false;}
            if(item.consumable&&!ItemInventory.Remove(item,1))return false;
            readyAt=Time.time+Mathf.Max(0,item.useCooldown);actor?.ShowNotice("Đã dùng "+item.displayName+".");return true;
        }
        finally{usingCustom=false;}
    }
    public bool TryUse(ConsumableKind kind)
    {
        if(!CanAct || GameControls.Blocked)return false;
        var interactor=GetComponent<World.PlayerInteractor>();
        if(kind==ConsumableKind.SoulPurification){interactor?.ShowNotice("Dùng Hương thanh hồn tại tượng thần → Thanh tẩy.");return false;}
        if(!ConsumableCatalog.Valid(kind)){interactor?.ShowNotice("Chưa có vật phẩm trong ô dùng nhanh.");return false;}
        if(Time.time<readyAt){interactor?.ShowNotice("Chưa thể dùng vật phẩm tiếp.");return false;}
        var catalog=ConsumableCatalog.Default;var item=catalog!=null?catalog.Find(kind):null;
        if(item==null)return false;
        if(GameEconomy.ConsumableCount(kind)<=0){GetComponent<World.PlayerInteractor>()?.ShowNotice("Đã hết "+item.displayName+".");return false;}
        if(kind==ConsumableKind.ShrineReturn)
        {
            var actor=GetComponent<World.PlayerInteractor>();
            if(actor!=null && actor.IsInCombat){actor.ShowNotice("Không thể hồi quy khi đang giao tranh.");return false;}
            bool returned=World.CheckpointService.ReturnToLastShrine(actor);
            if(returned)readyAt=Time.time+Mathf.Max(0,useCooldown);
            return returned;
        }
        bool used=GameEconomy.TryUseConsumable(kind,()=>Apply(item));
        if(used)
        {
            if ((int)kind >= 3) BuffUseVFX.Play(gameObject, (CharacterBuffs.Kind)((int)kind - 3));
            if ((int)kind >= 3) CombatSFX.Buff((CharacterBuffs.Kind)((int)kind - 3), transform.position);
            else CombatSFX.Cleanse(transform.position);
            readyAt=Time.time+Mathf.Max(0,useCooldown);
            GetComponent<World.PlayerInteractor>()?.ShowNotice("Đã dùng "+item.displayName+((int)kind>=3?" ("+item.duration+" giây).":"."));
        }
        else if((int)kind<3)GetComponent<World.PlayerInteractor>()?.ShowNotice("Không có trạng thái cần giải.");
        return used;
    }
    private bool Apply(ConsumableCatalog.Item item)
    {
        if((int)item.kind<3)
        {
            var debuffs=GetComponent<CharacterDebuffs>();if(debuffs==null)return false;
            bool active=item.kind==ConsumableKind.Antidote?debuffs.IsPoisoned:item.kind==ConsumableKind.BurnRemedy?debuffs.IsBurning:debuffs.IsStressed;
            var state=item.kind==ConsumableKind.Antidote?CharacterDebuffs.Kind.Poison:item.kind==ConsumableKind.BurnRemedy?CharacterDebuffs.Kind.Burning:CharacterDebuffs.Kind.Stress;
            if(!active && debuffs.GetBuildup(state)<=0)return false;
            debuffs.Remove(item.kind==ConsumableKind.Antidote?CharacterDebuffs.Kind.Poison:item.kind==ConsumableKind.BurnRemedy?CharacterDebuffs.Kind.Burning:CharacterDebuffs.Kind.Stress);
            return true;
        }
        if(float.IsNaN(item.duration)||float.IsInfinity(item.duration)||item.duration<=0||float.IsNaN(item.percent)||float.IsInfinity(item.percent)||item.percent<=0)return false;
        var buffs=GetComponent<CharacterBuffs>();if(buffs==null)buffs=gameObject.AddComponent<CharacterBuffs>();
        var kind=(CharacterBuffs.Kind)((int)item.kind-3);buffs.Apply(kind,item.duration,item.percent);return buffs.Has(kind);
    }
}
