using UnityEngine;

namespace World
{
    [DisallowMultipleComponent]
    public sealed class CheckpointInteractable : WorldInteractable
    {
        [Tooltip("Enable to enter your own checkpoint ID. It must be unique within its scene.")]
        public bool manualCheckpointId;
        [Tooltip("Stable save identity. Manual IDs are never overwritten by automatic assignment.")]
        public string checkpointId = "";
        public Transform respawnPoint;
        public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position + Vector3.down;
        private void Reset() { interactionLabel = "Lưu checkpoint"; interactionRadius = 1.6f; }
        protected override bool Interact(PlayerInteractor player)
        {
            if (player.IsInCombat)
            { player.ShowNotice("Không thể dùng tượng khi đang giao tranh."); return false; }
            bool wasUnlocked = CheckpointService.IsUnlocked(gameObject.scene.path, checkpointId);
            var existingMenu = GetComponent<CheckpointMenu>();
            if (!CheckpointService.Unlock(gameObject.scene.path, checkpointId, RespawnPosition, existingMenu != null ? existingMenu.checkpointName : checkpointId))
            { player.ShowNotice("Không thể lưu checkpoint trong scene này."); return false; }
            player.ShowNotice(wasUnlocked ? "Cầu nguyện để lưu hành trình tại tượng này." : "Đã mở tượng thần. Cầu nguyện để lưu hành trình.");
            if (wasUnlocked)
            {
                var menu = GetComponent<CheckpointMenu>();
                if (menu == null) menu = gameObject.AddComponent<CheckpointMenu>();
                menu.Open(player);
            }
            else
            {
                var cutscene = GetComponent<InGameCutscene>();
                if (cutscene != null) cutscene.Play(player);
            }
            return true;
        }
        public static void RestorePlayer(PlayerInteractor player)
        {
            if (player == null) return;
            var stats = player.GetComponent<CharacterStats>();
            var health = player.GetComponent<CharacterHealth>();
            var debuffs = player.GetComponent<CharacterDebuffs>();
            if (debuffs != null) debuffs.ClearAll();
            if (stats != null)
            {
                if (health != null) health.Heal(Mathf.Max(0f, stats.maxHP - stats.currentHP));
                stats.currentStamina = stats.maxStamina;
            }
            var potion = player.GetComponent<PlayerHealthPotion>();
            if (potion != null) potion.AddCharges(potion.capacity);
        }
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(respawnPoint != null ? respawnPoint.position : transform.position + Vector3.down, 0.25f);
        }
    }
}
