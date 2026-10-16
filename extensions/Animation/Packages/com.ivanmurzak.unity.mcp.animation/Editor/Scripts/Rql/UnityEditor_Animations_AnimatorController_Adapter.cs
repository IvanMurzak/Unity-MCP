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
using System.Linq;
using Rql.Execution;
using UnityEditor;
using UnityEngine;
using static com.IvanMurzak.Unity.MCP.Editor.Rql.Animation.AnimationEdit;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    using AnimatorConditionMode = UnityEditor.Animations.AnimatorConditionMode;
    using AnimatorController = UnityEditor.Animations.AnimatorController;
    using AnimatorState = UnityEditor.Animations.AnimatorState;
    using AnimatorStateMachine = UnityEditor.Animations.AnimatorStateMachine;
    using AnimatorStateTransition = UnityEditor.Animations.AnimatorStateTransition;
    using AnimatorTransitionBase = UnityEditor.Animations.AnimatorTransitionBase;

    /// <summary>
    /// AnimatorController: layers and parameters (arrays of copies, whose element writes would be lost) become writable views;
    /// the structural operations are methods addressed by names (states by name, layers by name or index, default layer 0).
    /// The controller is the call owner, so the engine records it for Undo; Unity's own state machine / state methods record
    /// the sub-assets they change.
    /// </summary>
    public sealed class UnityEditor_Animations_AnimatorController_Adapter : RqlTypeAdapter<AnimatorController>
    {
        public override string Description =>
            "AnimatorController: layers (views: name defaultWeight blendingMode defaultState states …), parameters (views: name type default*); " +
            "call AddParameter / RemoveParameter, AddLayer / RemoveLayer, AddState / RemoveState / SetDefaultState, AddTransition / " +
            "AddAnyStateTransition / RemoveTransition (states by name; layer = name or index, default 0)";

        private const string LayerHelp = "layer: name or index, default 0";

        protected override void Members(MemberTable<AnimatorController> m)
        {
            m.Add("layers", c => AnimatorLayerView.All(c), description: "layers (views with writable name defaultWeight blendingMode … and states)")
             .Add("parameters", c => AnimatorParameterView.All(c), description: "parameters (views: name type defaultFloat defaultInt defaultBool)");

            var t = m.Untyped;
            Add(t, "AddParameter", typeof(int), "add a parameter of type Float, Int, Bool or Trigger with an optional default value; returns its index",
                (c, a) => AddParameter(c, (string)a[0]!, (AnimatorControllerParameterType)a[1]!, a[2]),
                Req("name", typeof(string)), Req("type", typeof(AnimatorControllerParameterType)), Opt("defaultValue", typeof(object), null));
            Add(t, "RemoveParameter", typeof(int), "remove the parameter named name and the transition conditions that use it; returns the remaining parameter count",
                (c, a) => RemoveParameter(c, (string)a[0]!), Req("name", typeof(string)));
            Add(t, "AddLayer", typeof(int), "add a layer (with an empty state machine); returns its index",
                (c, a) => AddLayer(c, (string)a[0]!), Req("name", typeof(string)));
            Add(t, "RemoveLayer", typeof(int), "remove a layer (name or index); returns the remaining layer count",
                (c, a) => RemoveLayer(c, a[0]), Req("layer", typeof(object)));
            Add(t, "AddState", typeof(AnimatorState), "add a state (optional motion: an AnimationClip or BlendTree; " + LayerHelp + "); " +
                                                      "the layer's first state becomes its default; returns the state",
                (c, a) => AddState(c, (string)a[0]!, (Motion?)a[1], a[2]),
                Req("name", typeof(string)), Opt("motion", typeof(Motion), null), Opt("layer", typeof(object), 0));
            Add(t, "RemoveState", typeof(int), "remove the state named name and the transitions into it (" + LayerHelp + "); returns the remaining state count",
                (c, a) => RemoveState(c, (string)a[0]!, a[1]), Req("name", typeof(string)), Opt("layer", typeof(object), 0));
            Add(t, "SetDefaultState", typeof(void), "make the state named name the layer's default (entry) state (" + LayerHelp + ")",
                (c, a) => { SetDefaultState(c, (string)a[0]!, a[1]); return null; }, Req("name", typeof(string)), Opt("layer", typeof(object), 0));
            Add(t, "AddTransition", typeof(AnimatorStateTransition),
                "add a transition from state `from` to state `to` (to = \"Exit\" for an exit transition) with an optional condition " +
                "(parameter, mode: If, IfNot, Greater, Less, Equals, NotEqual — default by parameter type, threshold); " +
                "no exit time, zero duration; returns the transition (" + LayerHelp + ")",
                (c, a) => AddTransition(c, (string)a[0]!, (string)a[1]!, (string?)a[2], (AnimatorConditionMode?)a[3], (float)a[4]!, a[5]),
                Req("from", typeof(string)), Req("to", typeof(string)), Opt("parameter", typeof(string), null),
                Opt("mode", typeof(AnimatorConditionMode?), null), Opt("threshold", typeof(float), 0f), Opt("layer", typeof(object), 0));
            Add(t, "AddAnyStateTransition", typeof(AnimatorStateTransition),
                "add a transition from Any State to state `to` with an optional condition (parameter, mode, threshold); returns the transition (" + LayerHelp + ")",
                (c, a) => AddAnyStateTransition(c, (string)a[0]!, (string?)a[1], (AnimatorConditionMode?)a[2], (float)a[3]!, a[4]),
                Req("to", typeof(string)), Opt("parameter", typeof(string), null), Opt("mode", typeof(AnimatorConditionMode?), null),
                Opt("threshold", typeof(float), 0f), Opt("layer", typeof(object), 0));
            Add(t, "RemoveTransition", typeof(int),
                "remove the transitions from state `from` (\"AnyState\" for Any State) to state `to` (\"Exit\" for exit transitions); returns how many were removed (" + LayerHelp + ")",
                (c, a) => RemoveTransition(c, (string)a[0]!, (string)a[1]!, a[2]),
                Req("from", typeof(string)), Req("to", typeof(string)), Opt("layer", typeof(object), 0));
        }

        // ------------------------------------------------------------ parameters

        internal static int AddParameter(AnimatorController c, string name, AnimatorControllerParameterType type, object? defaultValue)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("parameter name is empty");
            if (c.parameters.Any(p => p.name == name)) throw new ArgumentException($"the controller already has a parameter named '{name}'");
            var parameter = new AnimatorControllerParameter { name = name, type = type };
            if (defaultValue != null)
            {
                switch (type)
                {
                    case AnimatorControllerParameterType.Float:
                        if (!TryFloat(defaultValue, out var f)) throw new ArgumentException($"defaultValue of Float parameter '{name}' must be a number");
                        parameter.defaultFloat = f;
                        break;
                    case AnimatorControllerParameterType.Int:
                        if (!TryInt(defaultValue, out var i)) throw new ArgumentException($"defaultValue of Int parameter '{name}' must be an integer");
                        parameter.defaultInt = i;
                        break;
                    case AnimatorControllerParameterType.Bool:
                        if (!(defaultValue is bool b)) throw new ArgumentException($"defaultValue of Bool parameter '{name}' must be true or false");
                        parameter.defaultBool = b;
                        break;
                    default:
                        throw new ArgumentException("a Trigger parameter has no default value");
                }
            }
            c.AddParameter(parameter);
            return c.parameters.Length - 1;
        }

        internal static int RemoveParameter(AnimatorController c, string name)
        {
            var index = ParameterIndex(c, name);
            // A condition on a missing parameter never fires and shows as an error in the Animator window: drop them with it.
            foreach (var layer in c.layers)
                foreach (var t in Transitions(layer.stateMachine))
                    if (t.conditions.Any(x => x.parameter == name))
                    {
                        Undo.RecordObject(t, "RQL remove parameter conditions");
                        t.conditions = t.conditions.Where(x => x.parameter != name).ToArray();
                        EditorUtility.SetDirty(t);
                    }
            c.RemoveParameter(index);
            return c.parameters.Length;
        }

        private static System.Collections.Generic.IEnumerable<AnimatorTransitionBase> Transitions(AnimatorStateMachine? sm)
        {
            if (sm == null) yield break;
            foreach (var t in sm.anyStateTransitions) yield return t;
            foreach (var t in sm.entryTransitions) yield return t;
            foreach (var child in sm.states)
                if (child.state != null)
                    foreach (var t in child.state.transitions) yield return t;
            foreach (var sub in sm.stateMachines)
            {
                if (sub.stateMachine == null) continue;
                foreach (var t in sm.GetStateMachineTransitions(sub.stateMachine)) yield return t;
                foreach (var t in Transitions(sub.stateMachine)) yield return t;
            }
        }

        // ------------------------------------------------------------ layers

        internal static int AddLayer(AnimatorController c, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("layer name is empty");
            if (c.layers.Any(l => l.name == name)) throw new ArgumentException($"the controller already has a layer named '{name}'");
            c.AddLayer(name);
            return c.layers.Length - 1;
        }

        internal static int RemoveLayer(AnimatorController c, object? layer)
        {
            var index = LayerIndex(c, layer);
            if (c.layers.Length == 1) throw new InvalidOperationException("a controller needs at least one layer");
            c.RemoveLayer(index);
            return c.layers.Length;
        }

        // ------------------------------------------------------------ states

        internal static AnimatorState AddState(AnimatorController c, string name, Motion? motion, object? layer)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("state name is empty");
            var sm = StateMachine(c, layer);
            if (AllStates(sm).Any(s => s.name == name)) throw new ArgumentException($"the layer already has a state named '{name}'");
            var state = sm.AddState(name);
            if (motion != null) state.motion = motion;
            if (sm.defaultState == null)
            {
                Undo.RecordObject(sm, "RQL set default state");
                sm.defaultState = state;
            }
            EditorUtility.SetDirty(sm);
            return state;
        }

        internal static int RemoveState(AnimatorController c, string name, object? layer)
        {
            var sm = StateMachine(c, layer);
            var state = State(sm, name);
            Holder(sm, state).RemoveState(state);
            return AllStates(sm).Count;
        }

        internal static void SetDefaultState(AnimatorController c, string name, object? layer)
        {
            var sm = StateMachine(c, layer);
            var state = State(sm, name);
            var holder = Holder(sm, state);
            AnimationEdit.Edit(holder, "set default state", () => holder.defaultState = state);
        }

        // ------------------------------------------------------------ transitions

        internal static AnimatorStateTransition AddTransition(AnimatorController c, string from, string to, string? parameter,
            AnimatorConditionMode? mode, float threshold, object? layer)
        {
            var sm = StateMachine(c, layer);
            var source = State(sm, from);
            var condition = Condition(c, parameter, mode);
            var transition = to == "Exit" && !AllStates(sm).Any(s => s.name == "Exit")
                ? source.AddExitTransition()
                : source.AddTransition(State(sm, to));
            Configure(transition, condition, threshold);
            return transition;
        }

        internal static AnimatorStateTransition AddAnyStateTransition(AnimatorController c, string to, string? parameter,
            AnimatorConditionMode? mode, float threshold, object? layer)
        {
            var sm = StateMachine(c, layer);
            var target = State(sm, to);
            var condition = Condition(c, parameter, mode);
            var transition = sm.AddAnyStateTransition(target);
            Configure(transition, condition, threshold);
            return transition;
        }

        internal static int RemoveTransition(AnimatorController c, string from, string to, object? layer)
        {
            var sm = StateMachine(c, layer);
            var states = AllStates(sm);
            bool exit = to == "Exit" && !states.Any(s => s.name == "Exit");
            AnimatorState? target = exit ? null : State(sm, to);
            bool Match(AnimatorStateTransition t) => exit ? t.isExit : t.destinationState == target;

            int removed = 0;
            if (from == "AnyState" && !states.Any(s => s.name == "AnyState"))
            {
                foreach (var t in sm.anyStateTransitions.Where(Match).ToArray()) { sm.RemoveAnyStateTransition(t); removed++; }
            }
            else
            {
                var source = State(sm, from);
                foreach (var t in source.transitions.Where(Match).ToArray()) { source.RemoveTransition(t); removed++; }
            }
            if (removed == 0) throw new ArgumentException($"no transition from '{from}' to '{to}'");
            return removed;
        }

        private static (string parameter, AnimatorConditionMode mode)? Condition(AnimatorController c, string? parameter, AnimatorConditionMode? mode)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                if (mode.HasValue) throw new ArgumentException("a condition mode needs a parameter");
                return null;
            }
            var type = c.parameters[ParameterIndex(c, parameter!)].type;
            var m = mode ?? DefaultMode(type);
            CheckCondition(type, m, parameter!);
            return (parameter!, m);
        }

        // Scripted transitions fire on their condition: no exit time and an immediate blend unless set afterwards
        // (… | get transitions | set hasExitTime = true, exitTime = 0.9, duration = 0.25).
        private static void Configure(AnimatorStateTransition transition, (string parameter, AnimatorConditionMode mode)? condition, float threshold)
        {
            Undo.RecordObject(transition, "RQL configure transition");
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.hasFixedDuration = true;
            if (condition.HasValue) transition.AddCondition(condition.Value.mode, threshold, condition.Value.parameter);
            EditorUtility.SetDirty(transition);
        }

        // ------------------------------------------------------------ registration helper

        private static void Add(MemberTable table, string name, Type returns, string description,
            Func<AnimatorController, object?[], object?> body, params CallableParameter[] parameters) =>
            table.AddMethod(name, parameters, returns, (o, a) => body((AnimatorController)o, a), description);
    }
}
