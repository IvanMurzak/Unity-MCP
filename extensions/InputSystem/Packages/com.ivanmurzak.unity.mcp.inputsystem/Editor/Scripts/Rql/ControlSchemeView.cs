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
using Rql;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.InputSystem
{
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;
    using InputBinding = UnityEngine.InputSystem.InputBinding;
    using InputControlScheme = UnityEngine.InputSystem.InputControlScheme;
    using Setup = UnityEngine.InputSystem.InputActionSetupExtensions;

    /// <summary>
    /// One control scheme of an InputActionAsset, addressed by name (schemes have no id; a rename updates the view).
    /// InputControlScheme is a struct in the asset's scheme array, so every write builds a new scheme and stores it at the
    /// same index. Writes are validated, saved to the .inputactions file and undoable.
    /// </summary>
    [RqlDescription("Input control scheme: name bindingGroup requiredDevices optionalDevices (writable, devices as [\"<Keyboard>\", …]); call Remove()")]
    public sealed class ControlSchemeView
    {
        private string _name;

        internal ControlSchemeView(InputActionAsset asset, string name)
        {
            this.asset = asset;
            _name = name;
        }

        [RqlReference]
        [RqlDescription("the InputActionAsset that owns the scheme")]
        public InputActionAsset asset { get; }

        private int Index
        {
            get
            {
                if (asset == null) throw new InvalidOperationException("the InputActionAsset no longer exists");
                var schemes = asset.controlSchemes;
                for (int i = 0; i < schemes.Count; i++)
                    if (string.Equals(schemes[i].name, _name, StringComparison.OrdinalIgnoreCase)) return i;
                throw new InvalidOperationException($"control scheme '{_name}' no longer exists in the asset");
            }
        }

        private InputControlScheme Scheme => asset.controlSchemes[Index];

        [RqlDescription("scheme name, unique in the asset; a rename also renames the binding group and the groups of the bindings that use it")]
        public string name
        {
            get => Scheme.name;
            set
            {
                var scheme = Scheme;
                if (scheme.name == value) return;
                InputValidation.SchemeName(value);
                InputValidation.UniqueName(asset.controlSchemes.Where(s => s.name != scheme.name).Select(s => s.name), value, "control scheme", asset.name);
                var oldGroup = scheme.bindingGroup;
                bool renameGroup = string.Equals(oldGroup, scheme.name, StringComparison.Ordinal);
                var group = renameGroup ? value : oldGroup;
                Store("rename control scheme", new InputControlScheme(value, scheme.deviceRequirements, group), renameGroup ? oldGroup : null, value);
                _name = value;
            }
        }

        [RqlDescription("the group bindings use to belong to the scheme (usually the scheme name)")]
        public string bindingGroup
        {
            get => Scheme.bindingGroup ?? "";
            set
            {
                var scheme = Scheme;
                if ((scheme.bindingGroup ?? "") == value) return;
                if (string.IsNullOrWhiteSpace(value) || value.Contains(';'))
                    throw new ArgumentException($"binding group '{value}' must be non-empty and must not contain ';'");
                Store("set binding group", new InputControlScheme(scheme.name, scheme.deviceRequirements, value), scheme.bindingGroup, value);
            }
        }

        [RqlDescription("devices the scheme requires, e.g. [\"<Keyboard>\", \"<Mouse>\"] (assign the whole list)")]
        public IReadOnlyList<string> requiredDevices
        {
            get => Scheme.deviceRequirements.Where(d => !d.isOptional).Select(d => d.controlPath).ToList().AsReadOnly();
            set => SetDevices(value, optional: false);
        }

        [RqlDescription("devices the scheme uses when present, e.g. [\"<Mouse>\"] (assign the whole list)")]
        public IReadOnlyList<string> optionalDevices
        {
            get => Scheme.deviceRequirements.Where(d => d.isOptional).Select(d => d.controlPath).ToList().AsReadOnly();
            set => SetDevices(value, optional: true);
        }

        [RqlCallable("remove the scheme (bindings keep their groups); returns how many schemes are left")]
        public int Remove()
        {
            var scheme = Scheme;
            InputAssetStore.Edit(asset, "remove control scheme", () => Setup.RemoveControlScheme(asset, scheme.name));
            return asset.controlSchemes.Count;
        }

        private void SetDevices(IReadOnlyList<string>? devices, bool optional)
        {
            var scheme = Scheme;
            var list = (devices ?? Array.Empty<string>()).Select(InputValidation.DevicePath).ToList();
            var current = scheme.deviceRequirements.Where(d => d.isOptional == optional).Select(d => d.controlPath).ToList();
            if (current.SequenceEqual(list)) return;
            // Keep the other kind of device in place; this kind is replaced by the new list, in order.
            var requirements = scheme.deviceRequirements.Where(d => d.isOptional != optional).ToList();
            requirements.AddRange(list.Select(path => new InputControlScheme.DeviceRequirement { controlPath = path, isOptional = optional }));
            Store(optional ? "set optional devices" : "set required devices", new InputControlScheme(scheme.name, requirements, scheme.bindingGroup), null, null);
        }

        // Replace the scheme at its index (InputControlScheme is a struct, the array is the asset's serialized m_ControlSchemes)
        // and, on a group rename, rename that group in every binding of the asset.
        private void Store(string label, InputControlScheme scheme, string? oldGroup, string? newGroup)
        {
            int index = Index;
            InputAssetStore.Edit(asset, label, () =>
            {
                var schemes = (InputControlScheme[])InputAssetStore.GetField(asset, "m_ControlSchemes")!;
                schemes[index] = scheme;
                InputAssetStore.SetField(asset, "m_ControlSchemes", schemes);
                if (string.IsNullOrEmpty(oldGroup) || string.IsNullOrEmpty(newGroup)) return;
                foreach (var map in asset.actionMaps)
                    for (int i = 0; i < map.bindings.Count; i++)
                    {
                        var b = map.bindings[i];
                        if (string.IsNullOrEmpty(b.groups)) continue;
                        var groups = b.groups.Split(InputBinding.Separator);
                        if (!groups.Any(g => string.Equals(g, oldGroup, StringComparison.OrdinalIgnoreCase))) continue;
                        b.groups = string.Join(InputBinding.Separator.ToString(),
                            groups.Select(g => string.Equals(g, oldGroup, StringComparison.OrdinalIgnoreCase) ? newGroup : g));
                        Setup.ChangeBinding(map, i).To(b);
                    }
            });
        }

        public override string ToString() => $"control scheme {_name}";
    }
}
