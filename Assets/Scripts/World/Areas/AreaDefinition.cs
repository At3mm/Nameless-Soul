using UnityEngine;

namespace World
{
    [CreateAssetMenu(menuName = "Game/World Area")]
    public sealed class AreaDefinition : ScriptableObject
    {
        public string areaId = "new-area";
        public string displayName = "Khu vực mới";
        public string subtitle;
        public AudioClip entrySound;
        [Header("Enemy damage at spawn")]
        public EnemyDamageProfile enemyDamageProfile;
    }
}
