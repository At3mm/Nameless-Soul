using UnityEngine;

namespace World
{
    [CreateAssetMenu(menuName = "World/Map Definition")]
    public sealed class WorldMapDefinition : ScriptableObject
    {
        public string scenePath;
        public string displayName;
        public Texture2D image;
        public Rect worldBounds;
    }
}
