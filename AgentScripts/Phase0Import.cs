using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class Phase0Import
{
    private const string MannequinPath = "Assets/_GunVerse/Models/Player/Mannequin.fbx";
    private const string UalPath = "Assets/_GunVerse/Models/ThirdParty/Quaternius/UniversalAnimationLibrary/AnimationLibrary_Unity_Standard.fbx";

    public static string Configure()
    {
        AssetDatabase.ImportAsset(MannequinPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(UalPath, ImportAssetOptions.ForceUpdate);

        ConfigureHumanoid(MannequinPath);
        ConfigureHumanoid(UalPath);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("=== MANNEQUIN ===");
        builder.AppendLine(Describe(MannequinPath));
        builder.AppendLine("=== UAL ===");
        builder.AppendLine(Describe(UalPath));
        return builder.ToString();
    }

    private static void ConfigureHumanoid(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("No ModelImporter at " + path);
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.autoGenerateAvatarMappingIfUnspecified = true;
        importer.importAnimation = true;
        importer.SaveAndReimport();
    }

    private static string Describe(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Path: " + path);
        builder.AppendLine("AnimType: " + (importer != null ? importer.animationType.ToString() : "missing"));
        builder.AppendLine("AvatarSetup: " + (importer != null ? importer.avatarSetup.ToString() : "missing"));
        builder.AppendLine("DefaultClips: " + (importer != null ? importer.defaultClipAnimations.Length : 0));

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        List<string> clipNames = new List<string>();
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Avatar avatar)
            {
                builder.AppendLine("Avatar: " + avatar.name + " human=" + avatar.isHuman + " valid=" + avatar.isValid);
                HumanDescription description = avatar.humanDescription;
                builder.AppendLine("MappedHumanBones: " + description.human.Length);
                builder.AppendLine("SkeletonBones: " + description.skeleton.Length);
                int mapped = 0;
                for (int i = 0; i < description.human.Length; i++)
                {
                    if (!string.IsNullOrEmpty(description.human[i].boneName))
                    {
                        mapped++;
                    }
                }

                builder.AppendLine("HumanEntriesWithBone: " + mapped);
            }

            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
            {
                clipNames.Add(clip.name + " (" + clip.length.ToString("F2") + "s, human=" + clip.humanMotion + ")");
            }
        }

        clipNames.Sort(StringComparer.OrdinalIgnoreCase);
        builder.AppendLine("Clips: " + clipNames.Count);
        foreach (string clipName in clipNames)
        {
            builder.AppendLine("  " + clipName);
        }

        return builder.ToString();
    }
}
