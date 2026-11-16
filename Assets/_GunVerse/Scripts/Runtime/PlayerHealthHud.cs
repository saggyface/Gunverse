using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerHealthHud : NetworkBehaviour
    {
        private PlayerHealth playerHealth;
        private GameHudController hud;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                return;
            }

            playerHealth = GetComponent<PlayerHealth>();
            hud = GameHudController.Instance;
            if (hud == null)
            {
                hud = FindAnyObjectByType<GameHudController>();
            }

            if (hud != null)
            {
                hud.BindPlayerHealth(playerHealth);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            if (hud != null)
            {
                hud.UnbindPlayerHealth();
            }
        }
    }
}
