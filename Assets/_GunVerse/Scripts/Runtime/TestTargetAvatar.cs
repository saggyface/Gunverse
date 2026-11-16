using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class TestTargetAvatar : NetworkBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Collider hurtbox;

        private PlayerHealth playerHealth;

        public override void OnNetworkSpawn()
        {
            playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.DeathStateChanged += OnDeathStateChanged;
                ApplyState(playerHealth.IsDead);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (playerHealth != null)
            {
                playerHealth.DeathStateChanged -= OnDeathStateChanged;
            }
        }

        private void OnDeathStateChanged(bool isDead)
        {
            ApplyState(isDead);
        }

        private void ApplyState(bool isDead)
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(!isDead);
            }

            if (hurtbox != null)
            {
                hurtbox.enabled = !isDead;
            }
        }
    }
}
