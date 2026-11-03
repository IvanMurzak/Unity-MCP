/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-InputSystem)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.InputSystem
{
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;
    using InputControlAttribute = UnityEngine.InputSystem.Layouts.InputControlAttribute;
    using InputControlPath = UnityEngine.InputSystem.InputControlPath;
    using InputSys = UnityEngine.InputSystem.InputSystem;
    using NameAndParameters = UnityEngine.InputSystem.Utilities.NameAndParameters;

    /// <summary>
    /// Checks run before anything is written, so a typo fails with a list of valid values instead of producing a binding
    /// that silently never triggers: names, control paths (the device layout must exist), interactions, processors,
    /// composites and their part names, expected control types and binding groups (must be control schemes of the asset).
    /// </summary>
    public static class InputValidation
    {
        /// <summary>Composites shipped with the Input System (the registry itself is internal).</summary>
        public const string KnownComposites = "1DAxis, 2DVector, 3DVector, ButtonWithOneModifier, ButtonWithTwoModifiers, OneModifier, TwoModifiers";

        public static string Name(string? name, string what)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException($"{what} name is empty");
            if (name!.Trim() != name) throw new ArgumentException($"{what} name '{name}' has leading or trailing spaces");
            if (name.Contains('/')) throw new ArgumentException($"{what} name '{name}' must not contain '/' (it separates \"Map/Action\")");
            return name;
        }

        /// <summary>A control scheme name: a valid name without ';' (it separates binding groups).</summary>
        public static string SchemeName(string? name)
        {
            Name(name, "control scheme");
            if (name!.Contains(';')) throw new ArgumentException($"control scheme name '{name}' must not contain ';' (it separates binding groups)");
            return name;
        }

        public static void UniqueName(IEnumerable<string> existing, string name, string what, string where)
        {
            if (existing.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"{what} '{name}' already exists in {where}");
        }

        /// <summary>A control path such as "&lt;Gamepad&gt;/buttonSouth": not empty, and its device layout must be registered.</summary>
        public static string ControlPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("control path is empty (e.g. \"<Keyboard>/space\", \"<Gamepad>/leftStick\")");
            string? layout;
            try { layout = InputControlPath.TryGetDeviceLayout(path); }
            catch (Exception ex) { throw new ArgumentException($"control path '{path}' is malformed: {ex.Message}"); }
            if (string.IsNullOrEmpty(layout) || layout == "*") return path!;
            if (!InputSys.ListLayouts().Contains(layout, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"control path '{path}': unknown device layout '<{layout}>' (e.g. <Keyboard>, <Mouse>, <Gamepad>, <Touchscreen>, <Pen>, <Joystick>, <XRController>)");
            return path!;
        }

        /// <summary>A device requirement of a control scheme ("&lt;Keyboard&gt;", "&lt;Gamepad&gt;"): a control path with a known device layout.</summary>
        public static string DevicePath(string? path)
        {
            ControlPath(path);
            if (!path!.StartsWith("<", StringComparison.Ordinal) && !path.StartsWith("*", StringComparison.Ordinal))
                throw new ArgumentException($"device '{path}' must be a layout path such as \"<Keyboard>\" or \"<Gamepad>\"");
            return path;
        }

        /// <summary>Interactions such as "hold(duration=0.4),tap"; null / empty = none.</summary>
        public static string? Interactions(string? text) =>
            NamedList(text, "interaction", name => InputSys.TryGetInteraction(name) != null, () => InputSys.ListInteractions());

        /// <summary>Processors such as "normalize(min=0,max=1),invert"; null / empty = none.</summary>
        public static string? Processors(string? text) =>
            NamedList(text, "processor", name => InputSys.TryGetProcessor(name) != null, () => InputSys.ListProcessors());

        private static string? NamedList(string? text, string what, Func<string, bool> exists, Func<IEnumerable<string>> all)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            List<NameAndParameters> parsed;
            try { parsed = NameAndParameters.ParseMultiple(text).ToList(); }
            catch (Exception ex) { throw new ArgumentException($"{what}s '{text}' are malformed: {ex.Message}"); }
            foreach (var entry in parsed)
                if (!exists(entry.name))
                    throw new ArgumentException($"unknown {what} '{entry.name}' (known: {string.Join(", ", all().OrderBy(n => n))})");
            return text;
        }

        /// <summary>The composite type registered under <paramref name="composite"/> (e.g. "2DVector").</summary>
        public static Type Composite(string? composite)
        {
            if (string.IsNullOrWhiteSpace(composite)) throw new ArgumentException($"composite is empty ({KnownComposites})");
            var name = NameAndParameters.Parse(composite).name;
            return InputSys.TryGetBindingComposite(name)
                ?? throw new ArgumentException($"unknown composite '{name}' ({KnownComposites})");
        }

        /// <summary>The part names of a composite ("up", "down", "left", "right" for 2DVector): its [InputControl] fields.</summary>
        public static List<string> PartNames(Type composite) =>
            composite.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => f.GetCustomAttribute<InputControlAttribute>() != null)
                .Select(f => f.Name)
                .ToList();

        public static string PartName(Type composite, string? part)
        {
            var names = PartNames(composite);
            var match = names.FirstOrDefault(n => string.Equals(n, part, StringComparison.OrdinalIgnoreCase));
            return match ?? throw new ArgumentException($"composite {CompositeName(composite)} has no part '{part}' (parts: {string.Join(", ", names)})");
        }

        private static string CompositeName(Type composite) => composite.Name.EndsWith("Composite", StringComparison.Ordinal)
            ? composite.Name.Substring(0, composite.Name.Length - "Composite".Length)
            : composite.Name;

        /// <summary>An expected control type is a control layout ("Button", "Axis", "Vector2", "Stick" …); null / empty = any.</summary>
        public static string? ExpectedControlType(string? layout)
        {
            if (string.IsNullOrEmpty(layout)) return null;
            if (!InputSys.ListLayouts().Contains(layout, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException($"unknown control type '{layout}' (e.g. Button, Axis, Vector2, Vector3, Stick, Dpad, Quaternion, Integer, Touch)");
            return layout;
        }

        /// <summary>Binding groups ("Gamepad" or "Keyboard&amp;Mouse;Gamepad") must name control schemes of the asset.</summary>
        public static string? Groups(InputActionAsset asset, string? groups)
        {
            if (string.IsNullOrWhiteSpace(groups)) return null;
            var known = asset.controlSchemes.Select(s => s.bindingGroup).Where(g => !string.IsNullOrEmpty(g)).ToList();
            foreach (var group in groups!.Split(';'))
            {
                if (known.Any(k => string.Equals(k, group, StringComparison.OrdinalIgnoreCase))) continue;
                throw new ArgumentException(known.Count == 0
                    ? $"binding group '{group}': the asset has no control schemes (add one with call AddControlScheme(name, [\"<Keyboard>\"]))"
                    : $"binding group '{group}' is not a control scheme of the asset (schemes: {string.Join(", ", known)})");
            }
            return groups;
        }
    }
}
