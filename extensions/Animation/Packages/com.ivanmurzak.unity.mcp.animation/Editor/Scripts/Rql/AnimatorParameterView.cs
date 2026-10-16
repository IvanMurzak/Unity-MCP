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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    using AnimatorController = UnityEditor.Animations.AnimatorController;

    /// <summary>
    /// One parameter of an AnimatorController (controller.parameters[i]). AnimatorController.parameters returns copies, so every
    /// setter writes the parameter array back (undoable). Name and type are read-only: conditions refer to parameters by name,
    /// so rename / retype by RemoveParameter + AddParameter.
    /// </summary>
    [RqlDescription("AnimatorController parameter: index name type (Float, Int, Bool, Trigger; read-only), defaultFloat defaultInt defaultBool (writable)")]
    public sealed class AnimatorParameterView
    {
        internal AnimatorParameterView(AnimatorController controller, int index)
        {
            this.controller = controller;
            this.index = index;
        }

        internal static List<AnimatorParameterView> All(AnimatorController controller) =>
            Enumerable.Range(0, controller.parameters.Length).Select(i => new AnimatorParameterView(controller, i)).ToList();

        [RqlReference]
        [RqlDescription("the AnimatorController that owns the parameter")]
        public AnimatorController controller { get; }

        [RqlDescription("index of the parameter")]
        public int index { get; }

        private AnimatorControllerParameter[] Parameters()
        {
            var ps = controller.parameters;
            AnimationEdit.CheckIndex(index, ps.Length, "parameter", "the controller");
            return ps;
        }

        private AnimatorControllerParameter P => Parameters()[index];

        private void Write(string what, AnimatorControllerParameterType expected, Action<AnimatorControllerParameter> change)
        {
            var ps = Parameters();
            var p = ps[index];
            if (p.type != expected && !(expected == AnimatorControllerParameterType.Bool && p.type == AnimatorControllerParameterType.Trigger))
                throw new InvalidOperationException($"parameter '{p.name}' is a {p.type}: {what} applies to {expected} parameters");
            change(p);
            AnimationEdit.Edit(controller, "set parameter " + what, () => controller.parameters = ps);
        }

        [RqlDescription("parameter name (read-only: conditions refer to it)")]
        public string name => P.name;

        [RqlDescription("Float, Int, Bool or Trigger (read-only)")]
        public AnimatorControllerParameterType type => P.type;

        [RqlDescription("default value of a Float parameter")]
        public float defaultFloat { get => P.defaultFloat; set => Write("defaultFloat", AnimatorControllerParameterType.Float, p => p.defaultFloat = value); }

        [RqlDescription("default value of an Int parameter")]
        public int defaultInt { get => P.defaultInt; set => Write("defaultInt", AnimatorControllerParameterType.Int, p => p.defaultInt = value); }

        [RqlDescription("default value of a Bool parameter")]
        public bool defaultBool { get => P.defaultBool; set => Write("defaultBool", AnimatorControllerParameterType.Bool, p => p.defaultBool = value); }

        public override string ToString() => $"parameter '{P.name}'";
    }
}
