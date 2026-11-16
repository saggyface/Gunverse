using UnityEngine;

namespace GunVerse
{
    [CreateAssetMenu(menuName = "Gun Verse/Projectile Replication Settings")]
    public sealed class ProjectileReplicationSettings : ScriptableObject
    {
        public const string RESOURCE_PATH = "ProjectileReplicationSettings";

        [SerializeField] private bool useClientVisualProjectiles;

        public bool UseClientVisualProjectiles => useClientVisualProjectiles;

        public static ProjectileReplicationSettings Load()
        {
            return Resources.Load<ProjectileReplicationSettings>(RESOURCE_PATH);
        }
    }
}
