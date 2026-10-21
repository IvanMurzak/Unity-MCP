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
using Rql.Execution;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.InputSystem
{
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;
    using InputControlScheme = UnityEngine.InputSystem.InputControlScheme;
    using Setup = UnityEngine.InputSystem.InputActionSetupExtensions;

    /// <summary>
    /// InputActionAsset (a <c>.inputactions</c> file): the reflected members (runtime state, ReadOnlyArrays of maps and of
    /// InputControlScheme / InputBinding structs whose writes would go to copies) are replaced by views — <c>maps</c>,
    /// <c>actions</c>, <c>controlSchemes</c> — and the structural operations are methods. Every write persists to the file.
    /// </summary>
    public sealed class UnityEngine_InputSystem_InputActionAsset_Adapter : RqlTypeAdapter<InputActionAsset>
    {
        public override string Description =>
            "InputActionAsset (.inputactions): name path maps actions controlSchemes; call AddMap(name), " +
            "AddControlScheme(name, requiredDevices?, optionalDevices?, bindingGroup?); maps / actions / bindings / schemes have their own " +
            "writable members and Remove(); every change is saved to the file";

        protected override void Members(MemberTable<InputActionAsset> m)
        {
            m.Add("path", a => AssetDatabase.GetAssetPath(a), description: "asset path (empty for an asset created at runtime)")
             .Add("maps", Maps, description: "the action maps (name, actions …)")
             .Add("actions", Actions, description: "every action of every map (map, name, type, bindings …)")
             .Add("controlSchemes", Schemes, description: "the control schemes (name, bindingGroup, requiredDevices, optionalDevices)")
             .Only("name", "path", "maps", "actions", "controlSchemes")
             .ReadOnly("name");

            m.Untyped.AddMethod("AddMap", new[] { new CallableParameter("name", typeof(string)) }, typeof(ActionMapView),
                (o, a) => AddMap((InputActionAsset)o, (string)a[0]!),
                "add an empty action map; returns it (add actions with call AddAction on it)");
            m.Untyped.AddMethod("AddControlScheme",
                new[]
                {
                    new CallableParameter("name", typeof(string)),
                    new CallableParameter("requiredDevices", typeof(List<string>), true, null),
                    new CallableParameter("optionalDevices", typeof(List<string>), true, null),
                    new CallableParameter("bindingGroup", typeof(string), true, null),
                },
                typeof(ControlSchemeView),
                (o, a) => AddControlScheme((InputActionAsset)o, (string)a[0]!, (List<string>?)a[1], (List<string>?)a[2], (string?)a[3]),
                "add a control scheme with device paths, e.g. AddControlScheme(\"Gamepad\", [\"<Gamepad>\"]); bindingGroup defaults to the name; " +
                "bindings join it through their groups; returns the scheme");
        }

        private static IReadOnlyList<ActionMapView> Maps(InputActionAsset asset) =>
            asset.actionMaps.Select(map => new ActionMapView(asset, map)).ToList().AsReadOnly();

        private static IReadOnlyList<ActionView> Actions(InputActionAsset asset) =>
            asset.actionMaps.SelectMany(map => map.actions).Select(action => new ActionView(asset, action)).ToList().AsReadOnly();

        private static IReadOnlyList<ControlSchemeView> Schemes(InputActionAsset asset) =>
            asset.controlSchemes.Select(s => new ControlSchemeView(asset, s.name)).ToList().AsReadOnly();

        internal static ActionMapView AddMap(InputActionAsset asset, string name)
        {
            InputValidation.Name(name, "action map");
            InputValidation.UniqueName(asset.actionMaps.Select(m => m.name), name, "action map", asset.name);
            Guid id = default;
            InputAssetStore.Edit(asset, "add action map", () => id = Setup.AddActionMap(asset, name).id);
            return new ActionMapView(asset, asset.FindActionMap(id) ?? throw new InvalidOperationException($"action map '{name}' was not saved"));
        }

        internal static ControlSchemeView AddControlScheme(InputActionAsset asset, string name, List<string>? required, List<string>? optional, string? bindingGroup)
        {
            InputValidation.SchemeName(name);
            InputValidation.UniqueName(asset.controlSchemes.Select(s => s.name), name, "control scheme", asset.name);
            bindingGroup = string.IsNullOrWhiteSpace(bindingGroup) ? name : bindingGroup;
            if (bindingGroup!.Contains(';')) throw new ArgumentException($"binding group '{bindingGroup}' must not contain ';'");
            var devices = new List<InputControlScheme.DeviceRequirement>();
            foreach (var path in required ?? new List<string>())
                devices.Add(new InputControlScheme.DeviceRequirement { controlPath = InputValidation.DevicePath(path), isOptional = false });
            foreach (var path in optional ?? new List<string>())
                devices.Add(new InputControlScheme.DeviceRequirement { controlPath = InputValidation.DevicePath(path), isOptional = true });
            InputAssetStore.Edit(asset, "add control scheme", () => Setup.AddControlScheme(asset, new InputControlScheme(name, devices, bindingGroup)));
            return new ControlSchemeView(asset, name);
        }
    }
}
