using UnityEngine;

namespace World
{
    public sealed class ItemPickup : WorldInteractable
    {
        public GameItemDefinition item;
        [Min(1)] public int quantity=1;
        public string pickupId;
        private string Receipt => "item-pickup:"+gameObject.scene.path+":"+pickupId;
        public override bool IsAvailable => base.IsAvailable&&item!=null&&!string.IsNullOrWhiteSpace(pickupId)&&!QuestService.HasItemReceipt(Receipt);
        private void Reset(){pickupId=System.Guid.NewGuid().ToString("N");interactionLabel="Nhặt vật phẩm";}
        private void Start(){if(!string.IsNullOrEmpty(pickupId)&&QuestService.HasItemReceipt(Receipt))gameObject.SetActive(false);}
        protected override bool Interact(PlayerInteractor player)
        {
            if(!QuestService.GrantItem(item,quantity,Receipt)){player.ShowNotice("Không đủ chỗ chứa "+item.displayName+".");return false;}
            player.ShowNotice("Đã nhận "+quantity+" × "+item.displayName+".");gameObject.SetActive(false);return true;
        }
    }
}
