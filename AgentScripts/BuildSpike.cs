using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class BuildSpike
{
    private const string MannequinPath = "Assets/_GunVerse/Models/Player/Mannequin.fbx";
    private const string UalPath = "Assets/_GunVerse/Models/ThirdParty/Quaternius/UniversalAnimationLibrary/AnimationLibrary_Unity_Standard.fbx";
    private const string ControllerFolder = "Assets/_GunVerse/Animations/Player";
    private const string ControllerPath = ControllerFolder + "/MannequinLocomotion.controller";
    private const string SpikeFolder = "Assets/_GunVerse/Scenes/Spike";
    private const string ScenePath = SpikeFolder + "/MannequinSpike.unity";

    private const float TPHoldHeight = 1.48f;
    private const float TPHoldDistance = 0.38f;
    private const float LongTPHoldHeight = 1.25f;
    private const float LongTPHoldMinForwardExtent = 0.6f;
    private const float LongTPHoldClearance = 0.18f;
    private const float ModelForwardOffset = 180f;
    private const float MaxBodyAimDifference = 90f;
    private const float MaxTorsoYaw = 60f;
    private const float MaxHeadYaw = 30f;

    private sealed class Entry
    {
        public string Label;
        public string GunPath;
        public float AimYaw;
        public bool TwoHand;
    }

    private static readonly Entry[] Entries =
    {
        new Entry { Label = "Battery 1H 0", GunPath = "Assets/_GunVerse/Prefabs/Items/BatteryGun.prefab", AimYaw = 0f, TwoHand = false },
        new Entry { Label = "Phone 1H 30", GunPath = "Assets/_GunVerse/Prefabs/Items/PhoneGun.prefab", AimYaw = 30f, TwoHand = false },
        new Entry { Label = "Toaster 2H 60", GunPath = "Assets/_GunVerse/Prefabs/Items/ToasterGun.prefab", AimYaw = 60f, TwoHand = true },
        new Entry { Label = "TV 2H 90", GunPath = "Assets/_GunVerse/Prefabs/LargeProps/TVGun.prefab", AimYaw = 90f, TwoHand = true },
        new Entry { Label = "Microwave 2H 135", GunPath = "Assets/_GunVerse/Prefabs/LargeProps/MicrowaveGun.prefab", AimYaw = 135f, TwoHand = true },
        new Entry { Label = "Shelf Throw 180", GunPath = "Assets/_GunVerse/Prefabs/LargeProps/Baked/ShelfRunLongGun.prefab", AimYaw = 180f, TwoHand = true }
    };

    private struct TransformState
    {
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;

        public TransformState(Transform transform)
        {
            LocalPosition = transform.localPosition;
            LocalRotation = transform.localRotation;
            LocalScale = transform.localScale;
        }

        public void Apply(Transform transform)
        {
            transform.localPosition = LocalPosition;
            transform.localRotation = LocalRotation;
            transform.localScale = LocalScale;
        }
    }

    public static string Build()
    {
        EnsureFolder(ControllerFolder);
        EnsureFolder(SpikeFolder);
        AssetDatabase.Refresh();

        GameObject mannequinModel = AssetDatabase.LoadAssetAtPath<GameObject>(MannequinPath);
        Avatar mannequinAvatar = LoadAsset<Avatar>(MannequinPath);
        AnimationClip idle = LoadClip("Rig|Idle_Loop");
        AnimationClip walk = LoadClip("Rig|Walk_Loop");
        AnimationClip run = LoadClip("Rig|Sprint_Loop");
        if (mannequinModel == null || mannequinAvatar == null || idle == null || walk == null || run == null)
        {
            return "Missing mannequin model, avatar, or UAL clips.";
        }

        AnimatorController controller = BuildController(idle, walk, run);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.46f, 0.50f);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "SpikeFloor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(3.2f, 1f, 2.2f);

        GameObject lightObject = new GameObject("SpikeKeyLight");
        lightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
        Light keyLight = lightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 1.5f;
        keyLight.shadows = LightShadows.Soft;

        GameObject fillObject = new GameObject("SpikeFillLight");
        fillObject.transform.rotation = Quaternion.Euler(8f, 180f, 0f);
        Light fillLight = fillObject.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.9f;

        GameObject cameraObject = new GameObject("SpikeCamera");
        cameraObject.transform.position = new Vector3(0f, 3.2f, 14f);
        cameraObject.transform.LookAt(new Vector3(0f, 1.0f, 0f));
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
        camera.fieldOfView = 46f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 60f;
        cameraObject.AddComponent<UniversalAdditionalCameraData>();

        bool startedAnimationMode = !AnimationMode.InAnimationMode();
        if (startedAnimationMode)
        {
            AnimationMode.StartAnimationMode();
        }

        Dictionary<Transform, TransformState> states = new Dictionary<Transform, TransformState>();
        StringBuilder summary = new StringBuilder();
        int count = Entries.Length;

        for (int i = 0; i < count; i++)
        {
            Entry entry = Entries[i];
            float x = (i - (count - 1) * 0.5f) * 3.1f;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(mannequinModel);
            instance.name = "Spike_" + entry.Label.Replace(' ', '_');
            instance.transform.position = new Vector3(x, 0f, 0f);
            instance.transform.rotation = Quaternion.identity;

            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.avatar = mannequinAvatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            AnimationMode.SampleAnimationClip(instance, walk, 0.35f);
            instance.transform.position = new Vector3(x, 0f, 0f);
            instance.transform.rotation = Quaternion.identity;

            PoseEntry(instance, entry, x);

            foreach (Transform transform in instance.GetComponentsInChildren<Transform>(true))
            {
                states[transform] = new TransformState(transform);
            }

            summary.AppendLine(entry.Label + " -> " + entry.GunPath + " yaw=" + entry.AimYaw.ToString("F0"));
        }

        if (startedAnimationMode)
        {
            AnimationMode.StopAnimationMode();
        }

        foreach (KeyValuePair<Transform, TransformState> pair in states)
        {
            pair.Value.Apply(pair.Key);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return "Built " + ScenePath + System.Environment.NewLine + summary;
    }

    private static void PoseEntry(GameObject instance, Entry entry, float x)
    {
        Transform root = instance.transform;
        Transform spine = FindBone(root, "mixamorig1:Spine");
        Transform rightArm = FindBone(root, "mixamorig1:RightArm");
        Transform rightForeArm = FindBone(root, "mixamorig1:RightForeArm");
        Transform rightHand = FindBone(root, "mixamorig1:RightHand");
        Transform rightMiddle = FindBone(root, "mixamorig1:RightHandMiddle1");
        Transform leftArm = FindBone(root, "mixamorig1:LeftArm");
        Transform leftForeArm = FindBone(root, "mixamorig1:LeftForeArm");
        Transform leftHand = FindBone(root, "mixamorig1:LeftHand");
        Transform leftMiddle = FindBone(root, "mixamorig1:LeftHandMiddle1");
        Transform head = FindBone(root, "mixamorig1:Head");
        if (spine == null || rightArm == null || rightForeArm == null || rightHand == null ||
            leftArm == null || leftForeArm == null || leftHand == null || head == null)
        {
            return;
        }

        float cappedAim = Mathf.Clamp(entry.AimYaw, -MaxBodyAimDifference, MaxBodyAimDifference);
        float bodyYaw = entry.AimYaw - cappedAim;
        float torsoYaw = Mathf.Clamp(cappedAim, -MaxTorsoYaw, MaxTorsoYaw);
        float headYaw = Mathf.Clamp(cappedAim - torsoYaw, -MaxHeadYaw, MaxHeadYaw);

        root.rotation = Quaternion.Euler(0f, bodyYaw + ModelForwardOffset, 0f);
        spine.rotation = Quaternion.AngleAxis(torsoYaw, Vector3.up) * spine.rotation;
        head.rotation = Quaternion.AngleAxis(headYaw, Vector3.up) * head.rotation;

        Quaternion aimRotation = Quaternion.Euler(0f, entry.AimYaw, 0f);

        GameObject gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.GunPath);
        GunVerse.GunItem gunItem = gunPrefab != null ? gunPrefab.GetComponent<GunVerse.GunItem>() : null;
        if (gunPrefab == null || gunItem == null)
        {
            return;
        }

        GameObject gun = (GameObject)PrefabUtility.InstantiatePrefab(gunPrefab);
        gun.name = "SpikeGun_" + entry.Label.Replace(' ', '_');
        Bounds prefabBounds = GetRendererBounds(gunPrefab);
        Vector3 heldSize = Vector3.Scale(prefabBounds.size, gunItem.PickupScale);
        float forwardExtent = heldSize.z;
        float holdHeight = forwardExtent > LongTPHoldMinForwardExtent ? LongTPHoldHeight : TPHoldHeight;
        float holdDistance = Mathf.Max(TPHoldDistance, forwardExtent * 0.5f + LongTPHoldClearance);
        Vector3 gunPosition = new Vector3(x, 0f, 0f) + Vector3.up * holdHeight + aimRotation * new Vector3(0f, 0f, holdDistance);
        Quaternion gunRotation = aimRotation * Quaternion.Euler(gunItem.PickupRotationEuler);
        gun.transform.SetPositionAndRotation(gunPosition, gunRotation);
        gun.transform.localScale = gunItem.PickupScale;
        foreach (Renderer renderer in gun.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = true;
        }

        Bounds gunBounds = GetRendererBounds(gun);
        Vector3 rightShoulder = rightArm.position;
        Vector3 leftShoulder = leftArm.position;
        Vector3 primaryTarget = gunBounds.ClosestPoint(rightShoulder);
        Vector3 supportTarget = gunBounds.ClosestPoint(leftShoulder);

        Vector3 rightPole = Vector3.down + Vector3.forward * -0.35f + root.right * 0.25f;
        SolveArm(rightArm, rightForeArm, rightHand, primaryTarget, gunRotation, rightPole);
        AlignHand(rightHand, rightMiddle, gunRotation);

        if (entry.TwoHand)
        {
            Vector3 leftPole = Vector3.down + Vector3.forward * -0.35f + root.right * -0.25f;
            SolveArm(leftArm, leftForeArm, leftHand, supportTarget, gunRotation, leftPole);
            AlignHand(leftHand, leftMiddle, gunRotation);
        }
    }

    private static void SolveArm(Transform upper, Transform foreArm, Transform hand, Vector3 target, Quaternion handRotation, Vector3 pole)
    {
        Vector3 shoulder = upper.position;
        Vector3 elbow = foreArm.position;
        float upperLength = Vector3.Distance(shoulder, elbow);
        float foreArmLength = Vector3.Distance(elbow, hand.position);
        Vector3 toTarget = target - shoulder;
        float distance = toTarget.magnitude;
        if (distance < 0.001f)
        {
            return;
        }

        float clamped = Mathf.Clamp(distance, Mathf.Abs(upperLength - foreArmLength) + 0.001f, upperLength + foreArmLength - 0.001f);
        Vector3 direction = toTarget.normalized;
        Vector3 poleDirection = pole - Vector3.Dot(pole, direction) * direction;
        if (poleDirection.sqrMagnitude < 0.0001f)
        {
            poleDirection = Vector3.down;
        }

        poleDirection.Normalize();
        float distanceAlong = (upperLength * upperLength - foreArmLength * foreArmLength + clamped * clamped) / (2f * clamped);
        float height = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - distanceAlong * distanceAlong));
        Vector3 elbowTarget = shoulder + direction * distanceAlong + poleDirection * height;

        AlignBone(upper, foreArm, elbowTarget);
        AlignBone(foreArm, hand, target);
        hand.rotation = handRotation;
    }

    private static void AlignBone(Transform bone, Transform child, Vector3 desiredChildPosition)
    {
        Vector3 current = child.position - bone.position;
        Vector3 desired = desiredChildPosition - bone.position;
        if (current.sqrMagnitude < 0.000001f || desired.sqrMagnitude < 0.000001f)
        {
            return;
        }

        bone.rotation = Quaternion.FromToRotation(current, desired) * bone.rotation;
    }

    private static void AlignHand(Transform hand, Transform middle, Quaternion gunRotation)
    {
        if (middle == null)
        {
            return;
        }

        Vector3 fingerDirection = middle.position - hand.position;
        if (fingerDirection.sqrMagnitude < 0.000001f)
        {
            return;
        }

        Vector3 forward = gunRotation * Vector3.forward;
        hand.rotation = Quaternion.FromToRotation(fingerDirection.normalized, forward) * hand.rotation;

        Vector3 handUp = Vector3.ProjectOnPlane(hand.up, forward);
        Vector3 desiredUp = Vector3.ProjectOnPlane(gunRotation * Vector3.up, forward);
        if (handUp.sqrMagnitude > 0.0001f && desiredUp.sqrMagnitude > 0.0001f)
        {
            float angle = Vector3.SignedAngle(handUp, desiredUp, forward);
            hand.rotation = Quaternion.AngleAxis(angle, forward) * hand.rotation;
        }
    }

    private static Transform FindBone(Transform root, string name)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name == name)
            {
                return transform;
            }
        }

        return null;
    }

    private static Bounds GetRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.one * 0.2f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static AnimatorController BuildController(AnimationClip idle, AnimationClip walk, AnimationClip run)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState state = stateMachine.AddState("Locomotion");
        BlendTree blendTree = new BlendTree
        {
            name = "LocomotionBlend",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(blendTree, controller);
        blendTree.AddChild(idle, 0f);
        blendTree.AddChild(walk, 0.5f);
        blendTree.AddChild(run, 1f);
        state.motion = blendTree;
        stateMachine.defaultState = state;
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip LoadClip(string name)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(UalPath);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is AnimationClip clip && clip.name == name)
            {
                return clip;
            }
        }

        return null;
    }

    private static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is T typed)
            {
                return typed;
            }
        }

        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }
}
