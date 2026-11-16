using UnityEditor;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class GunPropPhysicsAssets
    {
        private const string PHYSICS_FOLDER = "Assets/_GunVerse/Physics";
        private const string MATERIAL_PATH = PHYSICS_FOLDER + "/GunProp.asset";
        private const string BOUNCY_MATERIAL_PATH = PHYSICS_FOLDER + "/BouncyGunProp.asset";
        private const string LIGHT_MATERIAL_PATH = PHYSICS_FOLDER + "/LightGunProp.asset";
        private const string MEDIUM_MATERIAL_PATH = PHYSICS_FOLDER + "/MediumGunProp.asset";
        private const string HEAVY_MATERIAL_PATH = PHYSICS_FOLDER + "/HeavyGunProp.asset";
        private const float BOUNCINESS = 0.3f;
        private const float DYNAMIC_FRICTION = 0.55f;
        private const float STATIC_FRICTION = 0.65f;
        private const float BOUNCY_BOUNCINESS = 0.85f;
        private const float BOUNCY_DYNAMIC_FRICTION = 0.3f;
        private const float BOUNCY_STATIC_FRICTION = 0.4f;
        private const float LIGHT_BOUNCINESS = 0.9f;
        private const float MEDIUM_BOUNCINESS = 0.65f;
        private const float HEAVY_BOUNCINESS = 0.4f;

        [MenuItem("Tools/Gun Verse/Apply Weight-Class Physics Materials")]
        public static void ApplyAllToProject()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<GunItem>() == null)
                {
                    continue;
                }

                ApplyForWeightClass(prefab);
                EditorUtility.SetDirty(prefab);
                changed++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GunPropPhysicsAssets] Applied weight-class materials to {changed} gun prefabs.");
        }

        public static PhysicsMaterial EnsureMaterial()
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(MATERIAL_PATH);
            if (material != null)
            {
                return material;
            }

            EnsureFolder(PHYSICS_FOLDER);
            material = new PhysicsMaterial("GunProp")
            {
                bounciness = BOUNCINESS,
                dynamicFriction = DYNAMIC_FRICTION,
                staticFriction = STATIC_FRICTION,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Average
            };

            AssetDatabase.CreateAsset(material, MATERIAL_PATH);
            return material;
        }

        public static void ApplyTo(GameObject root)
        {
            PhysicsMaterial material = EnsureMaterial();
            ApplyMaterial(root, material);
        }

        public static void ApplyForWeightClass(GameObject root)
        {
            GunItem gunItem = root != null ? root.GetComponent<GunItem>() : null;
            GunWeightClass weightClass = gunItem != null ? gunItem.WeightClass : GunWeightClass.Medium;
            ApplyMaterial(root, EnsureWeightMaterial(weightClass));
        }

        public static void ApplyBouncyTo(GameObject root)
        {
            ApplyMaterial(root, EnsureWeightMaterial(GunWeightClass.Light));
        }

        private static void ApplyMaterial(GameObject root, PhysicsMaterial material)
        {
            if (root == null || material == null)
            {
                return;
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                {
                    collider.sharedMaterial = material;
                }
            }
        }

        private static PhysicsMaterial EnsureWeightMaterial(GunWeightClass weightClass)
        {
            string path = weightClass switch
            {
                GunWeightClass.Light => LIGHT_MATERIAL_PATH,
                GunWeightClass.Heavy => HEAVY_MATERIAL_PATH,
                _ => MEDIUM_MATERIAL_PATH
            };

            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material != null)
            {
                return material;
            }

            EnsureFolder(PHYSICS_FOLDER);
            float bounciness = weightClass switch
            {
                GunWeightClass.Light => LIGHT_BOUNCINESS,
                GunWeightClass.Heavy => HEAVY_BOUNCINESS,
                _ => MEDIUM_BOUNCINESS
            };

            float dynamicFriction = weightClass == GunWeightClass.Light
                ? BOUNCY_DYNAMIC_FRICTION
                : DYNAMIC_FRICTION;
            float staticFriction = weightClass == GunWeightClass.Light
                ? BOUNCY_STATIC_FRICTION
                : STATIC_FRICTION;

            material = new PhysicsMaterial(weightClass + "GunProp")
            {
                bounciness = bounciness,
                dynamicFriction = dynamicFriction,
                staticFriction = staticFriction,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = weightClass == GunWeightClass.Light
                    ? PhysicsMaterialCombine.Minimum
                    : PhysicsMaterialCombine.Average
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static PhysicsMaterial EnsureBouncyMaterial()
        {
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(BOUNCY_MATERIAL_PATH);
            if (material != null)
            {
                return material;
            }

            EnsureFolder(PHYSICS_FOLDER);
            material = new PhysicsMaterial("BouncyGunProp")
            {
                bounciness = BOUNCY_BOUNCINESS,
                dynamicFriction = BOUNCY_DYNAMIC_FRICTION,
                staticFriction = BOUNCY_STATIC_FRICTION,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };

            AssetDatabase.CreateAsset(material, BOUNCY_MATERIAL_PATH);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
