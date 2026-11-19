using System.Collections.Generic;
using UnityEngine;

namespace World
{
    [DisallowMultipleComponent, RequireComponent(typeof(PolygonCollider2D))]
    public sealed class AreaTrigger : MonoBehaviour
    {
        public AreaDefinition area;
        [Tooltip("Higher priority wins where areas overlap.")] public int priority;
        public Color gizmoColor = new Color(.7f, .8f, .25f, .65f);
        internal static readonly HashSet<AreaTrigger> Active = new HashSet<AreaTrigger>();
        private Collider2D[] shapes;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() { Active.Clear(); }
        private void Awake() { shapes = GetComponents<Collider2D>(); }
        private void OnEnable() { Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
        private void Reset() { GetComponent<PolygonCollider2D>().isTrigger = true; }
        private void OnValidate()
        {
            foreach (var shape in GetComponents<Collider2D>()) shape.isTrigger = true;
            shapes = null;
        }
        public bool Contains(Vector2 position)
        {
            if (shapes == null) shapes = GetComponents<Collider2D>();
            foreach (var shape in shapes) if (shape.enabled && shape.OverlapPoint(position)) return true;
            return false;
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            UnityEditor.Handles.color = gizmoColor;
            foreach (var shape in GetComponents<Collider2D>())
            {
                var polygon = shape as PolygonCollider2D;
                if (polygon != null)
                    for (int path = 0; path < polygon.pathCount; path++)
                    {
                        var points = polygon.GetPath(path);
                        for (int i = 0; i < points.Length; i++)
                            UnityEditor.Handles.DrawLine(transform.TransformPoint(points[i] + polygon.offset), transform.TransformPoint(points[(i + 1) % points.Length] + polygon.offset));
                    }
                var box = shape as BoxCollider2D;
                if (box != null)
                {
                    Gizmos.color = gizmoColor; Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.offset, box.size); Gizmos.matrix = Matrix4x4.identity;
                }
            }
            UnityEditor.Handles.Label(transform.position, area != null ? area.displayName + " [" + area.areaId + "]" : "Chưa gán khu vực");
        }
#endif
    }
}
