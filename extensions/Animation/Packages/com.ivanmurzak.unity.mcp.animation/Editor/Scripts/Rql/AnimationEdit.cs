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
using System.IO;
using System.Linq;
using Rql.Execution;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    using AnimatorController = UnityEditor.Animations.AnimatorController;
    using AnimatorState = UnityEditor.Animations.AnimatorState;
    using AnimatorStateMachine = UnityEditor.Animations.AnimatorStateMachine;

    /// <summary>
    /// Shared checks, lookups and the undo / dirty bookkeeping. Writes through the view objects (curves, keys, events, clip
    /// settings, layers, parameters) have a plain object as RQL owner, so they record the asset that holds them themselves; the
    /// Undo entry joins the mutation's Undo group.
    /// </summary>
    internal static class AnimationEdit
    {
        // ------------------------------------------------------------ undo / dirty

        /// <summary>Record, change, mark dirty.</summary>
        public static void Edit(Object asset, string what, Action change)
        {
            Undo.RecordObject(asset, "RQL " + what);
            change();
            EditorUtility.SetDirty(asset);
        }

        // ------------------------------------------------------------ assets

        /// <summary>Validate a new asset path: under Assets/, the expected extension, not overwriting an existing asset; creates the folders.</summary>
        public static string NewAssetPath(string path, string extension)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("path is empty");
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException($"'{path}' must start with Assets/");
            if (!path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"'{path}' must end with {extension}");
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new ArgumentException($"an asset already exists at '{path}' (choose another path)");
            RqlEditorApi.Instance.CreateFolder(Path.GetDirectoryName(path)!);
            return path;
        }

        // ------------------------------------------------------------ curve bindings

        private static Dictionary<string, Type[]>? _types;

        /// <summary>
        /// The animated type of a curve binding by name: "Transform", "UnityEngine.SpriteRenderer", "GameObject" (m_IsActive),
        /// a MonoBehaviour class name … (full names first, then short names).
        /// </summary>
        public static Type ResolveType(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("type is empty: use the animated component type, e.g. \"Transform\"");
            _types ??= TypeCache.GetTypesDerivedFrom<Object>().Append(typeof(Object))
                .Where(t => !t.IsGenericTypeDefinition)
                .SelectMany(t => new[] { (key: t.FullName ?? t.Name, t), (key: t.Name, t) })
                .GroupBy(p => p.key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(p => p.t).Distinct().ToArray(), StringComparer.Ordinal);
            if (!_types.TryGetValue(name.Trim(), out var hits) || hits.Length == 0)
                throw new ArgumentException($"no Unity object type named '{name}' (curves animate a component type such as Transform, SpriteRenderer, or GameObject)");
            if (hits.Length > 1)
            {
                var exact = hits.Where(t => t.FullName == name).ToArray();
                if (exact.Length == 1) return exact[0];
                throw new ArgumentException($"'{name}' is ambiguous: " + string.Join(", ", hits.Select(t => t.FullName)) + " (use the full name)");
            }
            return hits[0];
        }

        // ------------------------------------------------------------ animator controllers

        /// <summary>A layer by name (string) or index (number).</summary>
        public static int LayerIndex(AnimatorController controller, object? layer)
        {
            var layers = controller.layers;
            if (layer is string name)
            {
                for (int i = 0; i < layers.Length; i++)
                    if (layers[i].name == name) return i;
                throw new ArgumentException($"no layer named '{name}' (layers: {string.Join(", ", layers.Select(l => l.name))})");
            }
            if (!TryInt(layer ?? 0, out var index)) throw new ArgumentException("layer must be a layer name or an index");
            CheckIndex(index, layers.Length, "layer", "the controller", nameof(layer));
            return index;
        }

        public static AnimatorStateMachine StateMachine(AnimatorController controller, object? layer)
        {
            var sm = controller.layers[LayerIndex(controller, layer)].stateMachine;
            if (sm == null) throw new InvalidOperationException("the layer has no state machine");
            return sm;
        }

        /// <summary>Every state of a state machine and its sub state machines, depth-first.</summary>
        public static List<AnimatorState> AllStates(AnimatorStateMachine? sm)
        {
            var all = new List<AnimatorState>();
            void Walk(AnimatorStateMachine m)
            {
                foreach (var child in m.states) if (child.state != null) all.Add(child.state);
                foreach (var sub in m.stateMachines) if (sub.stateMachine != null) Walk(sub.stateMachine);
            }
            if (sm != null) Walk(sm);
            return all;
        }

        /// <summary>The state machine that directly holds <paramref name="state"/> (the layer's machine or a sub state machine).</summary>
        public static AnimatorStateMachine Holder(AnimatorStateMachine root, AnimatorState state) => FindHolder(root, state) ?? root;

        private static AnimatorStateMachine? FindHolder(AnimatorStateMachine m, AnimatorState state)
        {
            if (m.states.Any(s => s.state == state)) return m;
            foreach (var sub in m.stateMachines)
            {
                if (sub.stateMachine == null) continue;
                var found = FindHolder(sub.stateMachine, state);
                if (found != null) return found;
            }
            return null;
        }

        public static AnimatorState State(AnimatorStateMachine sm, string name)
        {
            var states = AllStates(sm);
            return states.FirstOrDefault(s => s.name == name)
                   ?? throw new ArgumentException($"no state named '{name}' in this layer (states: {string.Join(", ", states.Select(s => s.name))})");
        }

        public static int ParameterIndex(AnimatorController controller, string name)
        {
            var ps = controller.parameters;
            for (int i = 0; i < ps.Length; i++)
                if (ps[i].name == name) return i;
            throw new ArgumentException($"no parameter named '{name}' (parameters: {string.Join(", ", ps.Select(p => p.name))})");
        }

        /// <summary>The usual condition for a parameter type: Bool / Trigger → If, Float → Greater, Int → Equals.</summary>
        public static AnimatorConditionMode DefaultMode(AnimatorControllerParameterType type) => type switch
        {
            AnimatorControllerParameterType.Float => AnimatorConditionMode.Greater,
            AnimatorControllerParameterType.Int => AnimatorConditionMode.Equals,
            _ => AnimatorConditionMode.If,
        };

        public static void CheckCondition(AnimatorControllerParameterType type, AnimatorConditionMode mode, string parameter)
        {
            var ok = type switch
            {
                AnimatorControllerParameterType.Float => mode == AnimatorConditionMode.Greater || mode == AnimatorConditionMode.Less,
                AnimatorControllerParameterType.Int => mode == AnimatorConditionMode.Greater || mode == AnimatorConditionMode.Less
                                                       || mode == AnimatorConditionMode.Equals || mode == AnimatorConditionMode.NotEqual,
                AnimatorControllerParameterType.Trigger => mode == AnimatorConditionMode.If,
                _ => mode == AnimatorConditionMode.If || mode == AnimatorConditionMode.IfNot,
            };
            if (!ok)
            {
                var allowed = type switch
                {
                    AnimatorControllerParameterType.Float => "Greater, Less",
                    AnimatorControllerParameterType.Int => "Greater, Less, Equals, NotEqual",
                    AnimatorControllerParameterType.Trigger => "If",
                    _ => "If, IfNot",
                };
                throw new ArgumentException($"condition mode {mode} does not apply to {type} parameter '{parameter}' (use {allowed})");
            }
        }

        // ------------------------------------------------------------ values

        /// <summary>Throw when <paramref name="index"/> is not an index into <paramref name="count"/> <paramref name="what"/>s of <paramref name="owner"/>.</summary>
        public static void CheckIndex(int index, int count, string what, string owner, string parameter = "index")
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(parameter, $"{what} index {index} is out of range: {owner} has {count} {what}(s)");
        }

        public static bool TryInt(object? value, out int result)
        {
            switch (value)
            {
                case int i: result = i; return true;
                case long l when l >= int.MinValue && l <= int.MaxValue: result = (int)l; return true;
                case short s: result = s; return true;
                case byte b: result = b; return true;
                case double d when Math.Floor(d) == d && d >= int.MinValue && d <= int.MaxValue: result = (int)d; return true;
                case float f when Math.Floor(f) == f: result = (int)f; return true;
                case decimal m when Math.Floor(m) == m: result = (int)m; return true;
                default: result = 0; return false;
            }
        }

        public static bool TryFloat(object? value, out float result)
        {
            switch (value)
            {
                case int i: result = i; return true;
                case long l: result = l; return true;
                case short s: result = s; return true;
                case byte b: result = b; return true;
                case double d: result = (float)d; return true;
                case float f: result = f; return true;
                case decimal m: result = (float)m; return true;
                default: result = 0f; return false;
            }
        }

        public static CallableParameter Req(string name, Type type) => new CallableParameter(name, type);

        public static CallableParameter Opt(string name, Type type, object? value) => new CallableParameter(name, type, true, value);
    }
}
