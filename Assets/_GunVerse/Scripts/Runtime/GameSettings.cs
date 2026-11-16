using System;
using UnityEngine;

namespace GunVerse
{
    public enum ViewmodelFovMode
    {
        Fixed70,
        MatchWorld
    }

    public static class GameSettings
    {
        private const string SENSITIVITY_KEY = "Settings.Sensitivity";
        private const string MASTER_VOLUME_KEY = "Settings.MasterVolume";
        private const string PLAYER_NAME_KEY = "Settings.PlayerName";
        private const string FIELD_OF_VIEW_KEY = "Settings.FieldOfView";
        private const string VIEWMODEL_FOV_KEY = "Settings.ViewmodelFov";

        public const float SENSITIVITY_MIN = 0.1f;
        public const float SENSITIVITY_MAX = 3f;
        public const float SENSITIVITY_DEFAULT = 1f;

        public const float MASTER_VOLUME_MIN = 0f;
        public const float MASTER_VOLUME_MAX = 1f;
        public const float MASTER_VOLUME_DEFAULT = 1f;

        public const float FIELD_OF_VIEW_MIN = 60f;
        public const float FIELD_OF_VIEW_MAX = 110f;
        public const float FIELD_OF_VIEW_DEFAULT = 80f;
        public const float VIEWMODEL_FIXED_FIELD_OF_VIEW = 70f;

        public const int PLAYER_NAME_MAX_LENGTH = 12;

        public static event Action<float> FieldOfViewChanged;
        public static event Action<ViewmodelFovMode> ViewmodelFovChanged;

        public static float Sensitivity
        {
            get => PlayerPrefs.GetFloat(SENSITIVITY_KEY, SENSITIVITY_DEFAULT);
            set
            {
                PlayerPrefs.SetFloat(SENSITIVITY_KEY, Mathf.Clamp(value, SENSITIVITY_MIN, SENSITIVITY_MAX));
                PlayerPrefs.Save();
            }
        }

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, MASTER_VOLUME_DEFAULT);
            set
            {
                PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, Mathf.Clamp(value, MASTER_VOLUME_MIN, MASTER_VOLUME_MAX));
                PlayerPrefs.Save();
                ApplyMasterVolume();
            }
        }

        public static string PlayerName
        {
            get => PlayerPrefs.GetString(PLAYER_NAME_KEY, string.Empty);
            set
            {
                PlayerPrefs.SetString(PLAYER_NAME_KEY, NormalizePlayerName(value));
                PlayerPrefs.Save();
            }
        }

        public static float FieldOfView
        {
            get => PlayerPrefs.GetFloat(FIELD_OF_VIEW_KEY, FIELD_OF_VIEW_DEFAULT);
            set
            {
                float clamped = Mathf.Clamp(value, FIELD_OF_VIEW_MIN, FIELD_OF_VIEW_MAX);
                PlayerPrefs.SetFloat(FIELD_OF_VIEW_KEY, clamped);
                PlayerPrefs.Save();
                FieldOfViewChanged?.Invoke(clamped);
            }
        }

        public static ViewmodelFovMode ViewmodelFov
        {
            get => (ViewmodelFovMode)PlayerPrefs.GetInt(VIEWMODEL_FOV_KEY, (int)ViewmodelFovMode.MatchWorld);
            set
            {
                PlayerPrefs.SetInt(VIEWMODEL_FOV_KEY, (int)value);
                PlayerPrefs.Save();
                ViewmodelFovChanged?.Invoke(value);
            }
        }

        public static string NormalizePlayerName(string value)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (normalized.Length > PLAYER_NAME_MAX_LENGTH)
            {
                normalized = normalized.Substring(0, PLAYER_NAME_MAX_LENGTH);
            }

            return normalized;
        }

        public static void ApplyMasterVolume()
        {
            AudioListener.volume = MasterVolume;
        }
    }
}
