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
using Rql;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    using AnimatorController = UnityEditor.Animations.AnimatorController;
    using AnimatorControllerLayer = UnityEditor.Animations.AnimatorControllerLayer;
    using AnimatorLayerBlendingMode = UnityEditor.Animations.AnimatorLayerBlendingMode;
    using AnimatorState = UnityEditor.Animations.AnimatorState;
    using AnimatorStateMachine = UnityEditor.Animations.AnimatorStateMachine;
    using AnimatorStateTransition = UnityEditor.Animations.AnimatorStateTransition;

    /// <summary>
    /// One layer of an AnimatorController (controller.layers[i]). AnimatorController.layers returns copies of the layer objects,
    /// so a plain reflected element would lose writes; every setter writes the layer array back (undoable). The layer's states
    /// and transitions are AnimatorState / AnimatorStateTransition assets whose members are ordinary writable members.
    /// </summary>
    [RqlDescription("AnimatorController layer: index name defaultWeight blendingMode (Override / Additive) syncedLayerIndex iKPass avatarMask " +
                    "(writable), stateMachine, defaultState (writable), states (every state, sub state machines included), anyStateTransitions")]
    public sealed class AnimatorLayerView
    {
        internal AnimatorLayerView(AnimatorController controller, int index)
        {
            this.controller = controller;
            this.index = index;
        }

        internal static List<AnimatorLayerView> All(AnimatorController controller) =>
            Enumerable.Range(0, controller.layers.Length).Select(i => new AnimatorLayerView(controller, i)).ToList();

        [RqlReference]
        [RqlDescription("the AnimatorController that owns the layer")]
        public AnimatorController controller { get; }

        [RqlDescription("index of the layer (0 = base layer)")]
        public int index { get; }

        private AnimatorControllerLayer Layer => Layers()[index];

        private AnimatorControllerLayer[] Layers()
        {
            var layers = controller.layers;
            AnimationEdit.CheckIndex(index, layers.Length, "layer", "the controller");
            return layers;
        }

        private void Write(string what, Action<AnimatorControllerLayer> change)
        {
            var layers = Layers();
            change(layers[index]);
            AnimationEdit.Edit(controller, "set layer " + what, () => controller.layers = layers);
        }

        [RqlDescription("layer name (unique inside the controller)")]
        public string name
        {
            get => Layer.name;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("a layer name cannot be empty");
                if (controller.layers.Where((l, i) => i != index).Any(l => l.name == value))
                    throw new ArgumentException($"the controller already has a layer named '{value}'");
                Write("name", l => l.name = value);
            }
        }

        [RqlDescription("weight 0 … 1 (the base layer always plays at full weight)")]
        public float defaultWeight
        {
            get => Layer.defaultWeight;
            set
            {
                if (value < 0f || value > 1f) throw new ArgumentException($"defaultWeight {value} must be 0 … 1");
                Write("weight", l => l.defaultWeight = value);
            }
        }

        [RqlDescription("Override or Additive")]
        public AnimatorLayerBlendingMode blendingMode { get => Layer.blendingMode; set => Write("blending", l => l.blendingMode = value); }

        [RqlDescription("index of the layer this one syncs its states with, -1 = none")]
        public int syncedLayerIndex
        {
            get => Layer.syncedLayerIndex;
            set
            {
                if (value == index || value < -1 || value >= controller.layers.Length)
                    throw new ArgumentException($"syncedLayerIndex {value} must be -1 or another layer's index");
                Write("synced layer", l => l.syncedLayerIndex = value);
            }
        }

        [RqlDescription("synced layer drives timing too")]
        public bool syncedLayerAffectsTiming { get => Layer.syncedLayerAffectsTiming; set => Write("synced timing", l => l.syncedLayerAffectsTiming = value); }

        [RqlDescription("call OnAnimatorIK on this layer")]
        public bool iKPass { get => Layer.iKPass; set => Write("IK pass", l => l.iKPass = value); }

        [RqlReference]
        [RqlDescription("AvatarMask limiting the layer to some body parts / transforms (null = none)")]
        public AvatarMask? avatarMask { get => Layer.avatarMask; set => Write("avatar mask", l => l.avatarMask = value); }

        [RqlReference]
        [RqlDescription("the layer's state machine (states, stateMachines, anyStateTransitions, entryTransitions …)")]
        public AnimatorStateMachine? stateMachine => Layer.stateMachine;

        [RqlReference]
        [RqlDescription("the state the layer starts in; write a state ref or use call SetDefaultState(name, layer)")]
        public AnimatorState? defaultState
        {
            get => Layer.stateMachine?.defaultState;
            set
            {
                var sm = Layer.stateMachine ?? throw new InvalidOperationException("the layer has no state machine");
                if (value != null && !AnimationEdit.AllStates(sm).Contains(value)) throw new ArgumentException($"state '{value.name}' is not in layer '{name}'");
                var holder = value == null ? sm : AnimationEdit.Holder(sm, value);
                AnimationEdit.Edit(holder, "set default state", () => holder.defaultState = value);
            }
        }

        [RqlDescription("every state of the layer (sub state machines included); their members (speed, motion, transitions …) are writable")]
        public List<AnimatorState> states => AnimationEdit.AllStates(Layer.stateMachine);

        [RqlDescription("transitions from Any State")]
        public List<AnimatorStateTransition> anyStateTransitions => Layer.stateMachine?.anyStateTransitions.ToList() ?? new List<AnimatorStateTransition>();

        public override string ToString() => $"layer {index} '{Layer.name}'";
    }
}
