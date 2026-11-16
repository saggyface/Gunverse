using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GunVerse.EditorTools
{
    public static class PlayerAnimationSetupBuilder
    {
        private const string ANIMATION_LIBRARY_PATH =
            "Assets/_GunVerse/Models/ThirdParty/Quaternius/UniversalAnimationLibrary/AnimationLibrary_Unity_Standard.fbx";
        private const string CONTROLLER_PATH = "Assets/_GunVerse/Animations/Player/MannequinLocomotion.controller";
        private const string BODY_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/MannequinBody.prefab";
        private const string PLAYER_PREFAB_PATH = "Assets/_GunVerse/Prefabs/Player/NetworkPlayer.prefab";
        private const string SPEED_PARAMETER = "Speed";
        private const string GROUNDED_PARAMETER = "Grounded";
        private const string JUMPING_PARAMETER = "Jumping";
        private const string DEAD_PARAMETER = "Dead";
        private const string IDLE_CLIP = "Rig|Idle_Loop";
        private const string WALK_CLIP = "Rig|Walk_Loop";
        private const string RUN_CLIP = "Rig|Sprint_Loop";
        private const string JUMP_START_CLIP = "Rig|Jump_Start";
        private const string JUMP_LOOP_CLIP = "Rig|Jump_Loop";
        private const string DEATH_CLIP = "Rig|Death01";
        private const float CAMERA_EYE_HEIGHT = 1.63f;
        private const float CAMERA_EYE_FORWARD = 0.08f;
        private const float LOCOMOTION_TO_AIR_DURATION = 0.08f;
        private const float AIR_TRANSITION_DURATION = 0.1f;
        private const float AIR_START_PEAK_TIME = 0.5f;

        private static readonly string[] LOOPING_CLIPS =
        {
            IDLE_CLIP,
            WALK_CLIP,
            "Rig|Jog_Fwd_Loop",
            RUN_CLIP,
            JUMP_LOOP_CLIP
        };

        [MenuItem("Tools/Gun Verse/Rebuild Player Animation")]
        public static void Build()
        {
            ConfigureLoopingClips();
            AnimatorController controller = RebuildController();
            ConfigureBodyPrefab(controller);
            ConfigurePlayerPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerAnimationSetupBuilder] Rebuilt player animation and rig wiring.");
        }

        private static void ConfigureLoopingClips()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ANIMATION_LIBRARY_PATH) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[PlayerAnimationSetupBuilder] Animation library importer not found.");
                return;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            HashSet<string> looping = new HashSet<string>(LOOPING_CLIPS);
            foreach (ModelImporterClipAnimation clip in clips)
            {
                if (looping.Contains(clip.name))
                {
                    clip.loopTime = true;
                }
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimatorController RebuildController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            RemoveStates(stateMachine);
            RemoveBlendTrees();

            AddParameter(controller, SPEED_PARAMETER, AnimatorControllerParameterType.Float);
            AddParameter(controller, GROUNDED_PARAMETER, AnimatorControllerParameterType.Bool);
            AddParameter(controller, JUMPING_PARAMETER, AnimatorControllerParameterType.Bool);
            AddParameter(controller, DEAD_PARAMETER, AnimatorControllerParameterType.Bool);

            AnimatorState locomotion = stateMachine.AddState("Locomotion");
            AnimatorState airStart = stateMachine.AddState("AirStart");
            AnimatorState airLoop = stateMachine.AddState("AirLoop");
            AnimatorState death = stateMachine.AddState("Death");

            BlendTree blendTree = CreateLocomotionBlendTree(controller);
            locomotion.motion = blendTree;
            airStart.motion = FindClip(JUMP_START_CLIP);
            airLoop.motion = FindClip(JUMP_LOOP_CLIP);
            death.motion = FindClip(DEATH_CLIP);
            stateMachine.defaultState = locomotion;

            AddTransition(locomotion, airStart, LOCOMOTION_TO_AIR_DURATION,
                (AnimatorConditionMode.IfNot, GROUNDED_PARAMETER),
                (AnimatorConditionMode.If, JUMPING_PARAMETER));
            AddTransition(locomotion, airLoop, LOCOMOTION_TO_AIR_DURATION,
                (AnimatorConditionMode.IfNot, GROUNDED_PARAMETER),
                (AnimatorConditionMode.IfNot, JUMPING_PARAMETER));
            AddExitTimeTransition(airStart, airLoop, AIR_TRANSITION_DURATION, AIR_START_PEAK_TIME);
            AddTransition(airStart, locomotion, AIR_TRANSITION_DURATION,
                (AnimatorConditionMode.If, GROUNDED_PARAMETER));
            AddTransition(airLoop, locomotion, AIR_TRANSITION_DURATION,
                (AnimatorConditionMode.If, GROUNDED_PARAMETER));

            AnimatorStateTransition deathTransition = stateMachine.AddAnyStateTransition(death);
            ConfigureTransition(deathTransition, AIR_TRANSITION_DURATION, false, 0f);
            deathTransition.canTransitionToSelf = false;
            deathTransition.AddCondition(AnimatorConditionMode.If, 0f, DEAD_PARAMETER);

            AddTransition(death, locomotion, AIR_TRANSITION_DURATION,
                (AnimatorConditionMode.IfNot, DEAD_PARAMETER));

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RemoveStates(AnimatorStateMachine stateMachine)
        {
            AnimatorStateTransition[] anyStateTransitions = stateMachine.anyStateTransitions;
            foreach (AnimatorStateTransition transition in anyStateTransitions)
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }

            ChildAnimatorState[] states = stateMachine.states;
            foreach (ChildAnimatorState state in states)
            {
                stateMachine.RemoveState(state.state);
            }
        }

        private static void RemoveBlendTrees()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(CONTROLLER_PATH);
            foreach (Object asset in assets)
            {
                if (asset is BlendTree blendTree)
                {
                    AssetDatabase.RemoveObjectFromAsset(blendTree);
                    Object.DestroyImmediate(blendTree, true);
                }
            }
        }

        private static void AddParameter(
            AnimatorController controller,
            string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == parameterName)
                {
                    return;
                }
            }

            controller.AddParameter(parameterName, parameterType);
        }

        private static BlendTree CreateLocomotionBlendTree(AnimatorController controller)
        {
            BlendTree blendTree = new BlendTree
            {
                name = "LocomotionBlend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = SPEED_PARAMETER,
                useAutomaticThresholds = false,
                minThreshold = 0f,
                maxThreshold = 1f
            };

            AssetDatabase.AddObjectToAsset(blendTree, controller);
            blendTree.AddChild(FindClip(IDLE_CLIP), 0f);
            blendTree.AddChild(FindClip(WALK_CLIP), 0.5f);
            blendTree.AddChild(FindClip(RUN_CLIP), 1f);
            return blendTree;
        }

        private static AnimationClip FindClip(string clipName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(ANIMATION_LIBRARY_PATH))
            {
                if (asset is AnimationClip clip && clip.name == clipName)
                {
                    return clip;
                }
            }

            Debug.LogError("[PlayerAnimationSetupBuilder] Missing clip: " + clipName);
            return null;
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            float duration,
            params (AnimatorConditionMode Mode, string Parameter)[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureTransition(transition, duration, false, 0f);
            foreach ((AnimatorConditionMode mode, string parameter) in conditions)
            {
                transition.AddCondition(mode, 0f, parameter);
            }
        }

        private static void AddExitTimeTransition(
            AnimatorState from,
            AnimatorState to,
            float duration,
            float exitTime)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureTransition(transition, duration, true, exitTime);
        }

        private static void ConfigureTransition(
            AnimatorStateTransition transition,
            float duration,
            bool hasExitTime,
            float exitTime)
        {
            transition.hasExitTime = hasExitTime;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.hasFixedDuration = true;
            transition.canTransitionToSelf = true;
        }

        private static void ConfigureBodyPrefab(AnimatorController controller)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(BODY_PREFAB_PATH);
            try
            {
                Animator animator = contents.GetComponentInChildren<Animator>(true);
                if (animator == null)
                {
                    Debug.LogError("[PlayerAnimationSetupBuilder] MannequinBody has no Animator.");
                    return;
                }

                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.SaveAsPrefabAsset(contents, BODY_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigurePlayerPrefab()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(PLAYER_PREFAB_PATH);
            try
            {
                Transform cameraPivot = contents.transform.Find("CameraPivot");
                if (cameraPivot != null)
                {
                    cameraPivot.localPosition = new Vector3(0f, CAMERA_EYE_HEIGHT, CAMERA_EYE_FORWARD);
                }

                if (contents.GetComponent<PlayerRigDiagnostics>() == null)
                {
                    contents.AddComponent<PlayerRigDiagnostics>();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, PLAYER_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
