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
using System;
using System.Collections.Generic;
using System.Linq;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    /// <summary>
    /// Animator component: most of its own properties describe the playing graph and log warnings or errors when read on an
    /// Animator that is not playing (Edit Mode, no controller), so only the configuration members are exposed (plus everything
    /// inherited from Behaviour / Component). Parameters and Play are methods that need an initialized Animator (Play Mode).
    /// </summary>
    public sealed class UnityEngine_Animator_Adapter : RqlTypeAdapter<Animator>
    {
        /// <summary>Animator-declared members that are safe to read at any time.</summary>
        internal static readonly HashSet<string> Safe = new HashSet<string>(StringComparer.Ordinal)
        {
            "runtimeAnimatorController", "avatar", "applyRootMotion", "updateMode", "cullingMode", "speed", "fireEvents",
            "keepAnimatorStateOnDisable", "writeDefaultValuesOnDisable", "stabilizeFeet", "layersAffectMassCenter", "logWarnings",
            "isHuman", "hasRootMotion", "hasTransformHierarchy", "isOptimizable", "isInitialized", "animatePhysics",
        };

        public override string Description =>
            "Animator: runtimeAnimatorController (assign an AnimatorController asset), avatar, applyRootMotion, updateMode, cullingMode, speed, " +
            "isHuman, isInitialized; Play Mode: call SetParameter(name, value), Play(state, layer)";

        protected override void Members(MemberTable<Animator> m)
        {
            m.HideWhere(a => a.Member != null && a.Member.DeclaringType == typeof(Animator) && !Safe.Contains(a.Name))
             .Reference("runtimeAnimatorController", "avatar");

            var t = m.Untyped;
            t.AddMethod("SetParameter", new[] { AnimationEdit.Req("name", typeof(string)), AnimationEdit.Req("value", typeof(object)) }, typeof(void),
                (o, a) => { SetParameter((Animator)o, (string)a[0]!, a[1]); return null; },
                "Play Mode: set a parameter of the playing controller (Float / Int number, Bool true / false, Trigger true = set, false = reset)");
            t.AddMethod("Play", new[] { AnimationEdit.Req("state", typeof(string)), AnimationEdit.Opt("layer", typeof(int), -1), AnimationEdit.Opt("normalizedTime", typeof(float), 0f) },
                typeof(void), (o, a) => { Play((Animator)o, (string)a[0]!, (int)a[1]!, (float)a[2]!); return null; },
                "Play Mode: jump to a state by name (layer -1 = the first layer that has it) at normalizedTime");
        }

        private static void Require(Animator animator)
        {
            if (!Application.isPlaying || !animator.isInitialized)
                throw new InvalidOperationException($"the Animator on '{animator.gameObject.name}' is not playing (enter Play Mode with a controller assigned); " +
                                                    "in Edit Mode change the controller asset's parameters instead");
        }

        internal static void SetParameter(Animator animator, string name, object? value)
        {
            Require(animator);
            var parameter = animator.parameters.FirstOrDefault(p => p.name == name)
                            ?? throw new ArgumentException($"the Animator has no parameter named '{name}'");
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float:
                    if (!AnimationEdit.TryFloat(value, out var f)) throw new ArgumentException($"'{name}' is a Float parameter: give a number");
                    animator.SetFloat(name, f);
                    break;
                case AnimatorControllerParameterType.Int:
                    if (!AnimationEdit.TryInt(value, out var i)) throw new ArgumentException($"'{name}' is an Int parameter: give an integer");
                    animator.SetInteger(name, i);
                    break;
                case AnimatorControllerParameterType.Bool:
                    if (!(value is bool b)) throw new ArgumentException($"'{name}' is a Bool parameter: give true or false");
                    animator.SetBool(name, b);
                    break;
                default:
                    if (value is bool fire && !fire) animator.ResetTrigger(name);
                    else animator.SetTrigger(name);
                    break;
            }
        }

        internal static void Play(Animator animator, string state, int layer, float normalizedTime)
        {
            Require(animator);
            var hash = Animator.StringToHash(state);
            var found = layer >= 0
                ? animator.HasState(layer, hash)
                : Enumerable.Range(0, animator.layerCount).Any(l => animator.HasState(l, hash));
            if (!found) throw new ArgumentException($"the Animator has no state named '{state}'" + (layer >= 0 ? $" on layer {layer}" : ""));
            animator.Play(hash, layer, normalizedTime);
        }
    }
}
