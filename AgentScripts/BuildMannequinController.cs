using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class BuildMannequinController
{
    private const string ControllerPath = "Assets/_GunVerse/Animations/Player/MannequinLocomotion.controller";
    private const string UalPath = "Assets/_GunVerse/Models/ThirdParty/Quaternius/UniversalAnimationLibrary/AnimationLibrary_Unity_Standard.fbx";

    public static string Build()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            return "Missing mannequin controller.";
        }

        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState locomotion = null;
        for (int i = 0; i < stateMachine.states.Length; i++)
        {
            if (stateMachine.states[i].state.name == "Locomotion")
            {
                locomotion = stateMachine.states[i].state;
            }
            else if (stateMachine.states[i].state.name == "Jump" || stateMachine.states[i].state.name == "Land" || stateMachine.states[i].state.name == "Death")
            {
                stateMachine.RemoveState(stateMachine.states[i].state);
            }
        }

        if (locomotion == null)
        {
            return "Missing Locomotion state.";
        }

        AnimatorState jump = stateMachine.AddState("Jump");
        jump.motion = LoadClip("Rig|Jump_Start");
        AnimatorState land = stateMachine.AddState("Land");
        land.motion = LoadClip("Rig|Jump_Land");
        AnimatorState death = stateMachine.AddState("Death");
        death.motion = LoadClip("Rig|Death01");

        AnimatorStateTransition toJump = locomotion.AddTransition(jump);
        toJump.hasExitTime = false;
        toJump.duration = 0.08f;
        toJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");

        AnimatorStateTransition toLand = jump.AddTransition(land);
        toLand.hasExitTime = false;
        toLand.duration = 0.08f;
        toLand.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");

        AnimatorStateTransition toLocomotion = land.AddTransition(locomotion);
        toLocomotion.hasExitTime = true;
        toLocomotion.exitTime = 0.8f;
        toLocomotion.duration = 0.1f;

        AnimatorStateTransition anyDeath = stateMachine.AddAnyStateTransition(death);
        anyDeath.hasExitTime = false;
        anyDeath.duration = 0.1f;
        anyDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

        AnimatorStateTransition deathToLocomotion = death.AddTransition(locomotion);
        deathToLocomotion.hasExitTime = false;
        deathToLocomotion.duration = 0.1f;
        deathToLocomotion.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return "Mannequin controller rebuilt with Jump, Land, and Death.";
    }

    private static AnimationClip LoadClip(string name)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(UalPath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && clip.name == name)
            {
                return clip;
            }
        }

        return null;
    }
}
