using System;
using UnityEngine;

namespace World
{
    [CreateAssetMenu(menuName = "World/Map Definition")]
    public sealed class WorldMapDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class CheckpointLocation
        {
            public string displayName;
            public Vector2 worldPosition;
        }

        [Serializable]
        public sealed class AreaLocation
        {
            public string areaId;
            public string displayName;
            public Vector2 worldPosition;
        }

        public string scenePath;
        public string displayName;
        public Texture2D image;
        public Rect worldBounds;
        public Sprite playerMarker;
        public CheckpointLocation[] checkpoints = Array.Empty<CheckpointLocation>();
        public AreaLocation[] areas = Array.Empty<AreaLocation>();
    }
}
