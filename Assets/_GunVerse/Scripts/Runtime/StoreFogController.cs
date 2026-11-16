using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GunVerse
{
    public sealed class StoreFogController : NetworkBehaviour
    {
        public const float MAX_FOG_DENSITY = 0.06f;
        public const float DEFAULT_INTENSITY = 0.25f;

        public static Color FogColor => ResolveFogColor();

        private readonly NetworkVariable<bool> fogEnabled = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<float> fogIntensity = new NetworkVariable<float>(
            DEFAULT_INTENSITY,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private bool capturedDefaults;
        private bool previousFog;
        private Color previousFogColor;
        private float previousFogDensity;
        private FogMode previousFogMode;

        public override void OnNetworkSpawn()
        {
            CaptureDefaults();

            if (IsServer)
            {
                MultiplayerSessionManager sessionManager = MultiplayerSessionManager.Instance;
                fogEnabled.Value = sessionManager != null && sessionManager.StoreFogEnabled;
                fogIntensity.Value = sessionManager != null
                    ? sessionManager.StoreFogIntensity
                    : DEFAULT_INTENSITY;
            }

            fogEnabled.OnValueChanged += OnFogEnabledChanged;
            fogIntensity.OnValueChanged += OnFogIntensityChanged;
            ApplyFog();
        }

        public override void OnNetworkDespawn()
        {
            fogEnabled.OnValueChanged -= OnFogEnabledChanged;
            fogIntensity.OnValueChanged -= OnFogIntensityChanged;
            RestoreDefaults();
        }

        private void OnFogEnabledChanged(bool previousValue, bool newValue)
        {
            ApplyFog();
        }

        private void OnFogIntensityChanged(float previousValue, float newValue)
        {
            ApplyFog();
        }

        private void ApplyFog()
        {
            if (!fogEnabled.Value)
            {
                RestoreDefaults();
                return;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = Mathf.Clamp01(fogIntensity.Value) * MAX_FOG_DENSITY;
        }

        private static Color ResolveFogColor()
        {
            return SceneManager.GetActiveScene().name == GameplayMapCatalog.BOPBUY_SCENE_NAME
                ? ArtPalette.BopBuy.Fog
                : ArtPalette.GroceryStore.Fog;
        }

        private void CaptureDefaults()
        {
            if (capturedDefaults)
            {
                return;
            }

            previousFog = RenderSettings.fog;
            previousFogColor = RenderSettings.fogColor;
            previousFogDensity = RenderSettings.fogDensity;
            previousFogMode = RenderSettings.fogMode;
            capturedDefaults = true;
        }

        private void RestoreDefaults()
        {
            if (!capturedDefaults)
            {
                return;
            }

            RenderSettings.fog = previousFog;
            RenderSettings.fogColor = previousFogColor;
            RenderSettings.fogDensity = previousFogDensity;
            RenderSettings.fogMode = previousFogMode;
        }
    }
}
