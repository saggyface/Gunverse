using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace GunVerse
{
    public class JoinCodeDisplay : MonoBehaviour
    {
        private void Start()
        {
            TextMeshProUGUI label = GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                return;
            }

            MultiplayerSessionManager sessionManager = MultiplayerSessionManager.Instance;
            bool shouldShow = sessionManager != null
                && sessionManager.CurrentSession != null
                && NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsHost;

            label.text = shouldShow ? "Join Code: " + sessionManager.CurrentSession.Code : string.Empty;
        }
    }
}
