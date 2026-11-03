/*
┌─────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                    │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Animation)  │
│  Copyright (c) 2026 Ivan Murzak                                         │
│  Licensed under the Apache License, Version 2.0.                        │
│  See the LICENSE file in the project root for more information.         │
└─────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): the <c>animation</c> source
    /// (Animators, CreateClip, CreateController), AnimationClip curves / events / settings views and methods, AnimatorController
    /// layers / parameters views and state-machine methods, transition conditions, the Animator component's safe members,
    /// AnimationCurve / Keyframe literals, and the agent guide rendered into the unity-rql skill.
    /// </summary>
    public sealed class AnimationModule : IRqlModule
    {
        public const string GuideTopic = "Animation";

        private const string W = @"unity.asset(""Assets/Animations/Walk.anim"")";
        private const string P = @"unity.asset(""Assets/Animations/Player.controller"")";

        internal const string Guide =
@"Clips and controllers are assets: `unity.assets(""t:AnimationClip"")`, `unity.asset(""Assets/…/Walk.anim"")`,
`unity.asset(""Assets/…/Player.controller"")`. An AnimationClip has `curves` (path type property keyCount timeRange keys, keys with
writable time value inTangent outTangent …), `objectCurves` (sprite swaps), `events` and `settings` (loopTime loopBlend mirror …), all
writable views, plus ordinary members (frameRate wrapMode legacy length isLooping). An AnimatorController has `layers` (name
defaultWeight blendingMode defaultState `states` anyStateTransitions) and `parameters` (name type defaultFloat defaultInt defaultBool);
states and transitions are assets whose members (speed motion tag … / hasExitTime exitTime duration conditions …) are set directly.
Writes and calls are undoable; creating an asset is not. `rql-schema ""AnimationClip""` / `""AnimatorController""` list the methods.

Clips (curve `path` is the child path under the Animator, """" = its own GameObject; `type` the animated component):

```
animation | call CreateClip(""Assets/Animations/Walk.anim"")   empty clip, 60 fps
animation | call CreateClip(""Assets/Animations/Idle.anim"", 30, true)   path, frameRate, loop
unity.assets(""t:AnimationClip"") | select name length frameRate isLooping n=(curves.count()) e=(events.count())
" + W + @" | call SetCurve("""", ""Transform"", ""localPosition.y"", [[0, 0], [0.5, 1], [1, 0]])   path, type, property, keys [time, value(, inTangent, outTangent)]
" + W + @" | call SetCurve(""Body"", ""SpriteRenderer"", ""m_Color.a"", [{time: 0, value: 1}, {time: 1, value: 0, inTangent: -1}])
" + W + @" | get curves | select path type property keyCount timeRange
" + W + @" | get curves | where property == ""localPosition.y"" | get keys | select index time value inTangent outTangent
" + W + @" | get curves | where property == ""localPosition.y"" | set keys[1].value = 2, keys[1].outTangent = 0
" + W + @" | get curves | where property == ""localPosition.y"" | call AddKey(1.5, 0.5)   returns the key index; RemoveKey(index), SetKeys([[t, v], …]), Evaluate(t)
" + W + @" | get curves | where path == ""Body"" | set postWrapMode = ""Loop""
" + W + @" | call RemoveCurve(""Body"", ""SpriteRenderer"", ""m_Color.a"")   or … | get curves | where … | call Remove()
" + W + @" | set frameRate = 30, settings.loopTime = true
" + W + @" | call AddEvent(0.5, ""Footstep"", ""left"")   time, functionName, stringParameter?, floatParameter?, intParameter?
" + W + @" | get events | select index time functionName stringParameter
" + W + @" | set events[0].time = 0.25, events[0].intParameter = 1
" + W + @" | call RemoveEvent(0)   ClearEvents() / ClearCurves() remove all
```

Animator controllers (states by name; a trailing `layer` argument is a layer name or index, default 0):

```
animation | call CreateController(""Assets/Animations/Player.controller"")   Base Layer, empty state machine (CreateController(path, motion) adds a state)
" + P + @" | call AddParameter(""Speed"", ""Float"")   Float, Int, Bool or Trigger
" + P + @" | call AddParameter(""Grounded"", ""Bool"", true)   optional default value
" + P + @" | call AddParameter(""Jump"", ""Trigger"")
" + P + @" | call AddState(""Idle"", unity.asset(""Assets/Animations/Idle.anim""))   name, motion?, layer?; the first state becomes the default
" + P + @" | call AddState(""Walk"", " + W + @")
" + P + @" | call AddTransition(""Idle"", ""Walk"", ""Speed"", ""Greater"", 0.1)   from, to (""Exit"" = exit), parameter?, mode?, threshold?, layer?
" + P + @" | call AddTransition(""Walk"", ""Idle"", ""Speed"", ""Less"", 0.1)
" + P + @" | call AddAnyStateTransition(""Idle"", ""Jump"")   mode defaults by parameter type: Bool / Trigger If, Float Greater, Int Equals
" + P + @" | call SetDefaultState(""Idle"")
" + P + @" | get layers | select index name defaultWeight defaultState.name states.name
" + P + @" | get layers | get states | select name speed motion.name transitions.destinationState.name
" + P + @" | get layers | get states | where name == ""Walk"" | set speed = 1.5
" + P + @" | get layers | get states | where name == ""Idle"" | get transitions | set hasExitTime = true, exitTime = 0.9, duration = 0.25
" + P + @" | get layers | get states | where name == ""Idle"" | get transitions | call AddCondition(""Grounded"", ""If"")   RemoveCondition(index), ClearConditions()
" + P + @" | get layers | get states | get transitions | select destinationState.name hasExitTime conditions
" + P + @" | get parameters | select name type defaultFloat defaultBool
" + P + @" | get parameters | where name == ""Speed"" | set defaultFloat = 0.5
" + P + @" | call AddLayer(""Upper Body"")   returns the layer index
" + P + @" | set layers[1].defaultWeight = 0.5, layers[1].blendingMode = ""Additive""
" + P + @" | call AddState(""Wave"", null, ""Upper Body"")
" + P + @" | call RemoveTransition(""Walk"", ""Idle"")   from ""AnyState"" / to ""Exit"" for those transitions
" + P + @" | call RemoveState(""Wave"", ""Upper Body"")
" + P + @" | call RemoveLayer(""Upper Body"")
" + P + @" | call RemoveTransition(""AnyState"", ""Idle"")
" + P + @" | call RemoveParameter(""Jump"")   conditions using it are removed too
```

Animator components (`animation | get animators` = every Animator in the loaded scenes):

```
editor | call CreateGameObject(""Player"")
objects | where name == ""Player"" | call AddComponent(""Animator"")
animation | get animators | set runtimeAnimatorController = " + P + @", applyRootMotion = false
animation | get animators | select gameObject.name runtimeAnimatorController.name isHuman cullingMode
```

Parameter names and types are read-only (conditions refer to them): rename by RemoveParameter + AddParameter. New transitions
have no exit time and no blend duration; set them on the transition. Only the Animator's configuration members are readable (its
playback state is not meaningful in Edit Mode); in Play Mode `call SetParameter(""Speed"", 2)` / `call Play(""Walk"")` drive it.
Other components' AnimationCurve fields take keyframe lists too: `set curve = [[0, 0], [1, 1]]`.";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            ctx.AddSource("animation", () => AnimationEditorApi.Instance,
                   "animation: animators (every Animator in the loaded scenes), CreateClip(path, frameRate, loop), CreateController(path, motion)")
               .AddAdapter(new UnityEngine_AnimationClip_Adapter())
               .AddAdapter(new UnityEditor_Animations_AnimatorController_Adapter())
               .AddAdapter(new UnityEditor_Animations_AnimatorStateMachine_Adapter())
               .AddAdapter(new UnityEditor_Animations_AnimatorState_Adapter())
               .AddAdapter(new UnityEditor_Animations_AnimatorTransitionBase_Adapter())
               .AddAdapter(new UnityEngine_Animator_Adapter())
               .AddAdapter(new UnityEngine_AnimationCurve_Adapter())
               .AddAdapter(new UnityEngine_Keyframe_Adapter())
               .AddGuide(GuideTopic, Guide);
        }
    }
}
