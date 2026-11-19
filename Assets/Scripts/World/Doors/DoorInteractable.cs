using UnityEngine;

namespace World
{
    [DisallowMultipleComponent,RequireComponent(typeof(MapPropAnimation))]
    public sealed class DoorInteractable : WorldInteractable
    {
        public MapPropAnimation door;
        public bool locked;
        public GameItemDefinition requiredKey;
        public QuestDefinition requiredCompletedQuest;
        public override bool IsAvailable=>base.IsAvailable && door!=null && door.isActiveAndEnabled &&
            door.openingFrames!=null && door.openingFrames.Length>0 && !door.IsOpen && !door.IsMoving;
        private void Reset(){door=GetComponent<MapPropAnimation>();interactionLabel="Mở cửa";interactionRadius=1.8f;if(door!=null&&door.visual!=null)interactionPoint=door.visual.transform;}
        private void Awake(){if(door==null)door=GetComponent<MapPropAnimation>();}
        protected override bool Interact(PlayerInteractor player)
        {
            if(!IsAvailable)return false;
            if(player.IsInCombat){player.ShowNotice("Không thể mở cửa khi đang giao tranh.");return false;}
            if(requiredCompletedQuest!=null && QuestService.Status(requiredCompletedQuest)!=QuestStatus.Completed)
            {player.ShowNotice("Cần hoàn thành nhiệm vụ: "+requiredCompletedQuest.title+".");return false;}
            if(requiredKey!=null && ItemInventory.Count(requiredKey)<=0)
            {player.ShowNotice("Cần "+requiredKey.displayName+" để mở cửa.");return false;}
            if(locked){player.ShowNotice("Cửa đang bị khóa.");return false;}
            door.Open();
            return door.IsMoving||door.IsOpen;
        }
    }
}
