using UnityEngine;

namespace World
{
    /// <summary>Place once per scene location; collected seeds remain collected across saves.</summary>
    public sealed class LifeSeedPickup : WorldInteractable
    {
        [Tooltip("Unique per placed pickup. Assigned automatically in the Editor.")]
        public string pickupId;
        [Min(1)] public int quantity = 1;
        public static string ItemName => LifeSeedItem.Name;
        public static Sprite Icon => LifeSeedItem.Definition!=null&&LifeSeedItem.Definition.icon!=null?LifeSeedItem.Definition.icon:Resources.Load<Sprite>("LifeSeedIcon");
        private string SaveKey => gameObject.scene.path + "." + pickupId;
        private void Reset() { interactionLabel = "Nhặt Hạt sinh mệnh"; interactionRadius = 1.5f; }
        private void Start() { if (!string.IsNullOrEmpty(pickupId) && GameEconomy.HasCollectedLifeSeed(SaveKey)) gameObject.SetActive(false); }
        protected override bool Interact(PlayerInteractor player)
        {
            if (string.IsNullOrEmpty(pickupId) || !GameEconomy.CollectLifeSeed(SaveKey, quantity)) return false;
            player.ShowNotice("Đã nhận " + quantity + " Hạt sinh mệnh.");
            gameObject.SetActive(false);
            return true;
        }
    }
}
