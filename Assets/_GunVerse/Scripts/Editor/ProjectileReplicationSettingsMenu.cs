using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class ProjectileReplicationSettingsMenu
    {
        private const string SETTINGS_PATH =
            "Assets/_GunVerse/Resources/ProjectileReplicationSettings.asset";

        [MenuItem("Tools/Gun Verse/Toggle Client Visual Projectiles")]
        public static void ToggleClientVisualProjectiles()
        {
            ProjectileReplicationSettings settings =
                AssetDatabase.LoadAssetAtPath<ProjectileReplicationSettings>(SETTINGS_PATH);

            if (settings == null)
            {
                Debug.LogError("[ProjectileReplicationSettingsMenu] Missing " + SETTINGS_PATH);
                return;
            }

            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty property = serializedSettings.FindProperty("useClientVisualProjectiles");
            if (property == null)
            {
                Debug.LogError("[ProjectileReplicationSettingsMenu] Missing useClientVisualProjectiles.");
                return;
            }

            property.boolValue = !property.boolValue;
            serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[ProjectileReplicationSettingsMenu] Client visual projectiles: " +
                (property.boolValue ? "on" : "off") +
                ". Restart the match for the server to apply it.");
        }
    }
}
