using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class GunHoldBuilder
{
    private const float TwoHandThreshold = 0.28f;
    private const float LongForwardExtent = 0.6f;
    private const float NormalTPDistance = 0.38f;
    private const float NormalTPHeight = 1.48f;
    private const float LongTPHeight = 1.25f;
    private const float LongClearance = 0.18f;

    public static string Build()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int oneHand = 0;
        int twoHand = 0;
        int throwHand = 0;
        int skipped = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            GunVerse.GunItem gunItem = prefab.GetComponent<GunVerse.GunItem>();
            if (gunItem == null)
            {
                continue;
            }

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                skipped++;
                continue;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 heldSize = Vector3.Scale(bounds.size, gunItem.PickupScale);
            float heldMax = Mathf.Max(heldSize.x, Mathf.Max(heldSize.y, heldSize.z));
            float forwardExtent = heldSize.z;
            GunVerse.GunHoldType holdType = gunItem.FireAsSelfProjectile
                ? GunVerse.GunHoldType.TwoHandThrow
                : heldMax >= TwoHandThreshold
                    ? GunVerse.GunHoldType.TwoHand
                    : GunVerse.GunHoldType.OneHand;

            float tpHoldDistance = Mathf.Max(NormalTPDistance, forwardExtent * 0.5f + LongClearance);
            float tpHoldHeight = forwardExtent > LongForwardExtent ? LongTPHeight : NormalTPHeight;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                GunVerse.GunItem targetItem = contents.GetComponent<GunVerse.GunItem>();
                if (targetItem == null)
                {
                    continue;
                }

                Transform primary = FindOrCreate(contents.transform, "GripPrimary");
                Transform support = FindOrCreate(contents.transform, "GripSupport");
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;
                Vector3 right = contents.transform.right;
                Vector3 forward = contents.transform.forward;
                Vector3 up = contents.transform.up;
                Vector3 primaryWorld = center + right * (extents.x * 0.55f) - forward * (extents.z * 0.45f) + up * (extents.y * 0.05f);
                Vector3 supportWorld = center - right * (extents.x * 0.55f) + forward * (extents.z * 0.10f) + up * (extents.y * 0.05f);
                primary.localPosition = contents.transform.InverseTransformPoint(primaryWorld);
                primary.localRotation = Quaternion.identity;
                support.localPosition = contents.transform.InverseTransformPoint(supportWorld);
                support.localRotation = Quaternion.identity;

                SerializedObject serializedObject = new SerializedObject(targetItem);
                serializedObject.FindProperty("holdType").enumValueIndex = (int)holdType;
                serializedObject.FindProperty("gripPrimary").objectReferenceValue = primary;
                serializedObject.FindProperty("gripSupport").objectReferenceValue = support;
                serializedObject.FindProperty("tpHoldDistance").floatValue = tpHoldDistance;
                serializedObject.FindProperty("tpHoldHeight").floatValue = tpHoldHeight;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            if (holdType == GunVerse.GunHoldType.OneHand) oneHand++;
            else if (holdType == GunVerse.GunHoldType.TwoHand) twoHand++;
            else throwHand++;
        }

        AssetDatabase.SaveAssets();
        GunVerse.EditorTools.GunItemPrefabRegistry.Rebuild();

        StringBuilder summary = new StringBuilder();
        summary.AppendLine("One-hand: " + oneHand);
        summary.AppendLine("Two-hand: " + twoHand);
        summary.AppendLine("Two-hand throw: " + throwHand);
        summary.AppendLine("Skipped (no visual): " + skipped);
        return summary.ToString();
    }

    private static Transform FindOrCreate(Transform root, string name)
    {
        Transform existing = root.Find(name);
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }
}
