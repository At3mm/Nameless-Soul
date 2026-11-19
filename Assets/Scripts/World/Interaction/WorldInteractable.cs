using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        public static readonly HashSet<WorldInteractable> Active = new HashSet<WorldInteractable>();
        [Min(0.2f)] public float interactionRadius = 1.5f;
        public string interactionLabel = "Tương tác";
        public Transform interactionPoint;
        public virtual bool IsAvailable => isActiveAndEnabled;
        public Vector3 InteractionPosition => interactionPoint != null ? interactionPoint.position : transform.position;
        protected virtual void OnEnable() { Active.Add(this); }
        protected virtual void OnDisable() { Active.Remove(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() { Active.Clear(); }
        public bool TryInteract(PlayerInteractor player)
        {
            if (!IsAvailable || player == null || !player.CanInteract ||
                Vector2.Distance(player.transform.position, InteractionPosition) > interactionRadius) return false;
            return Interact(player);
        }
        protected abstract bool Interact(PlayerInteractor player);
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(InteractionPosition, interactionRadius);
        }
    }
}
