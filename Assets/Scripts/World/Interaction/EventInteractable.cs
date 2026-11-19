using UnityEngine.Events;

namespace World
{
    // Attach to doors, chests or switches and configure the event in the Inspector.
    public sealed class EventInteractable : WorldInteractable
    {
        public bool singleUse;
        public UnityEvent onInteract = new UnityEvent();
        private bool used;

        public override bool IsAvailable => base.IsAvailable && (!singleUse || !used);

        protected override bool Interact(PlayerInteractor player)
        {
            if (!IsAvailable) return false;
            used = true;
            onInteract.Invoke();
            return true;
        }
    }
}
