using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Collections.Generic;

public class MCAnimatorSetupMenu
{
    [MenuItem("Tools/Setup MC Animator")]
    public static void SetupAnimator()
    {
        // Load Animator Controller
        string controllerPath = "Assets/Model/MC/animation/MC 1.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Animator Controller at " + controllerPath, "OK");
            return;
        }

        // Get base layer
        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        // Define animation clips
        Dictionary<string, AnimationClip> animationClips = new Dictionary<string, AnimationClip>()
        {
            // Idle animations
            { "idle up", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Model/MC/animation/idle up.anim") },
            { "idle down", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Model/MC/animation/idle down.anim") },
            { "idle left", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Model/MC/animation/idle left.anim") },
            { "idle right", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Model/MC/animation/idle right.anim") },
            
            // Walk animations
            { "walk up", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Model/MC/animation/walk up.anim") },
            { "walk down", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Model/MC/animation/walk down.anim") },
            { "walk left", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Model/MC/animation/walk left.anim") },
            { "walk right", AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Model/MC/animation/walk right.anim") },
        };

        // Create states
        Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();
        foreach (var clip in animationClips)
        {
            // Check if state already exists
            AnimatorState state = null;
            foreach (var s in stateMachine.states)
            {
                if (s.state.name == clip.Key)
                {
                    state = s.state;
                    break;
                }
            }
            
            if (state == null)
                state = stateMachine.AddState(clip.Key);
                
            state.motion = clip.Value;
            states[clip.Key] = state;
            Debug.Log($"✓ Created state: {clip.Key}");
        }

        // Set idle down as default
        if (states.ContainsKey("idle down"))
            stateMachine.defaultState = states["idle down"];

        // Add transitions between idle and walk states
        AddIdleWalkTransitions(states);

        // Add idle transitions (between different idle directions)
        AddDirectionTransitions(states);

        // Mark as dirty
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", "✓ MC Animator setup complete!\n\nIdle + Walk 4 directions configured.\nTransitions added automatically.", "OK");
        Debug.Log("✓ Animator setup complete!");
    }

    static void AddIdleWalkTransitions(Dictionary<string, AnimatorState> states)
    {
        // Idle to Walk transitions (when moving)
        AddTransition(states["idle down"], states["walk down"], "Speed", 0.1f, AnimatorConditionMode.Greater);
        AddTransition(states["idle up"], states["walk up"], "Speed", 0.1f, AnimatorConditionMode.Greater);
        AddTransition(states["idle left"], states["walk left"], "Speed", 0.1f, AnimatorConditionMode.Greater);
        AddTransition(states["idle right"], states["walk right"], "Speed", 0.1f, AnimatorConditionMode.Greater);

        // Walk to Idle transitions (when stopped)
        AddTransition(states["walk down"], states["idle down"], "Speed", 0.1f, AnimatorConditionMode.Less);
        AddTransition(states["walk up"], states["idle up"], "Speed", 0.1f, AnimatorConditionMode.Less);
        AddTransition(states["walk left"], states["idle left"], "Speed", 0.1f, AnimatorConditionMode.Less);
        AddTransition(states["walk right"], states["idle right"], "Speed", 0.1f, AnimatorConditionMode.Less);

        Debug.Log("✓ Idle ↔ Walk transitions added");
    }

    static void AddDirectionTransitions(Dictionary<string, AnimatorState> states)
    {
        // IDLE DIRECTION TRANSITIONS
        // To Down
        AddTransition(states["idle up"], states["idle down"], "LastVertical", 0.5f, AnimatorConditionMode.Greater);
        AddTransition(states["idle left"], states["idle down"], "LastVertical", 0.5f, AnimatorConditionMode.Greater);
        AddTransition(states["idle right"], states["idle down"], "LastVertical", 0.5f, AnimatorConditionMode.Greater);

        // To Up
        AddTransition(states["idle down"], states["idle up"], "LastVertical", -0.5f, AnimatorConditionMode.Less);
        AddTransition(states["idle left"], states["idle up"], "LastVertical", -0.5f, AnimatorConditionMode.Less);
        AddTransition(states["idle right"], states["idle up"], "LastVertical", -0.5f, AnimatorConditionMode.Less);

        // To Left
        AddTransition(states["idle down"], states["idle left"], "LastHorizontal", -0.5f, AnimatorConditionMode.Less);
        AddTransition(states["idle up"], states["idle left"], "LastHorizontal", -0.5f, AnimatorConditionMode.Less);
        AddTransition(states["idle right"], states["idle left"], "LastHorizontal", -0.5f, AnimatorConditionMode.Less);

        // To Right
        AddTransition(states["idle down"], states["idle right"], "LastHorizontal", 0.5f, AnimatorConditionMode.Greater);
        AddTransition(states["idle up"], states["idle right"], "LastHorizontal", 0.5f, AnimatorConditionMode.Greater);
        AddTransition(states["idle left"], states["idle right"], "LastHorizontal", 0.5f, AnimatorConditionMode.Greater);

        // WALK DIRECTION TRANSITIONS
        // To Down
        AddTransition(states["walk up"], states["walk down"], "LastVertical", 0.5f, AnimatorConditionMode.Greater);
        AddTransition(states["walk left"], states["walk down"], "LastVertical", 0.5f, AnimatorConditionMode.Greater);
        AddTransition(states["walk right"], states["walk down"], "LastVertical", 0.5f, AnimatorConditionMode.Greater);

        // To Up
        AddTransition(states["walk down"], states["walk up"], "LastVertical", -0.5f, AnimatorConditionMode.Less);
        AddTransition(states["walk left"], states["walk up"], "LastVertical", -0.5f, AnimatorConditionMode.Less);
        AddTransition(states["walk right"], states["walk up"], "LastVertical", -0.5f, AnimatorConditionMode.Less);

        // To Left
        AddTransition(states["walk down"], states["walk left"], "LastHorizontal", -0.5f, AnimatorConditionMode.Less);
        AddTransition(states["walk up"], states["walk left"], "LastHorizontal", -0.5f, AnimatorConditionMode.Less);
        AddTransition(states["walk right"], states["walk left"], "LastHorizontal", -0.5f, AnimatorConditionMode.Less);

        // To Right
        AddTransition(states["walk down"], states["walk right"], "LastHorizontal", 0.5f, AnimatorConditionMode.Greater);
        AddTransition(states["walk up"], states["walk right"], "LastHorizontal", 0.5f, AnimatorConditionMode.Greater);
        AddTransition(states["walk left"], states["walk right"], "LastHorizontal", 0.5f, AnimatorConditionMode.Greater);

        Debug.Log("✓ Direction transitions added (16 transitions per state)");
    }

    static void AddTransition(AnimatorState from, AnimatorState to, string paramName, float threshold, AnimatorConditionMode mode)
    {
        // Check if transition already exists
        foreach (var t in from.transitions)
        {
            if (t.destinationState == to)
                return; // Transition already exists
        }

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.exitTime = 0f;
        transition.duration = 0.1f;
        transition.AddCondition(mode, threshold, paramName);
    }
}
