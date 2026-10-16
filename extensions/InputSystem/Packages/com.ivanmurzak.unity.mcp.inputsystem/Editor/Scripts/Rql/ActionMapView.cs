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
    using InputActionMap = UnityEngine.InputSystem.InputActionMap;
    using InputActionType = UnityEngine.InputSystem.InputActionType;
    using Setup = UnityEngine.InputSystem.InputActionSetupExtensions;

    /// <summary>
    /// One action map of an InputActionAsset, addressed by its id (a re-import replaces the map objects, the ids stay).
    /// Writes and calls are validated, saved to the .inputactions file and undoable.
    /// </summary>
    [RqlDescription("Input action map: name (writable), id, actions, actionCount; call AddAction(name, type?, expectedControlType?, binding?), Remove()")]
    public sealed class ActionMapView
    {
        private readonly Guid _id;

        internal ActionMapView(InputActionAsset asset, InputActionMap map)
        {
            this.asset = asset;
            _id = map.id;
        }

        [RqlReference]
        [RqlDescription("the InputActionAsset that owns the map")]
        public InputActionAsset asset { get; }

        [RqlDescription("the map's id (stable across renames)")]
        public string id => _id.ToString();

        internal InputActionMap Map =>
            (asset != null ? asset.FindActionMap(_id) : null)
            ?? throw new InvalidOperationException($"action map {_id} no longer exists in the asset");

        [RqlDescription("map name, unique in the asset; renaming keeps the actions and bindings")]
        public string name
        {
            get => Map.name;
            set
            {
                var map = Map;
                if (map.name == value) return;
                InputValidation.Name(value, "action map");
                InputValidation.UniqueName(asset.actionMaps.Where(m => m.id != _id).Select(m => m.name), value, "action map", asset.name);
                InputAssetStore.Edit(asset, "rename action map", () => InputAssetStore.SetField(map, "m_Name", value));
            }
        }

        [RqlDescription("the map's actions")]
        public IReadOnlyList<ActionView> actions => Map.actions.Select(a => new ActionView(asset, a)).ToList().AsReadOnly();

        [RqlDescription("number of actions")]
        public int actionCount => Map.actions.Count;

        [RqlCallable("add an action: type Button (default), Value or PassThrough, optional expectedControlType (\"Vector2\", \"Axis\", " +
                     "\"Button\" …) and an initial binding path; returns the action")]
        public ActionView AddAction(string name, InputActionType type = InputActionType.Button, string? expectedControlType = null, string? binding = null)
        {
            var map = Map;
            InputValidation.Name(name, "action");
            InputValidation.UniqueName(map.actions.Select(a => a.name), name, "action", $"map '{map.name}'");
            expectedControlType = InputValidation.ExpectedControlType(expectedControlType);
            if (!string.IsNullOrEmpty(binding)) InputValidation.ControlPath(binding);

            Guid id = default;
            InputAssetStore.Edit(asset, "add action", () =>
            {
                var action = Setup.AddAction(map, name, type, string.IsNullOrEmpty(binding) ? null : binding);
                if (expectedControlType != null) action.expectedControlType = expectedControlType;
                ActionView.EnsureBindingIds(action);
                id = action.id;
            });
            return new ActionView(asset, asset.FindAction(id) ?? throw new InvalidOperationException($"action '{name}' was not saved"));
        }

        [RqlCallable("remove the map with its actions and bindings; returns how many maps are left")]
        public int Remove()
        {
            var map = Map;
            InputAssetStore.Edit(asset, "remove action map", () => Setup.RemoveActionMap(asset, map));
            return asset.actionMaps.Count;
        }

        public override string ToString() => asset != null && asset.FindActionMap(_id) is InputActionMap m ? $"map {m.name}" : $"map {_id}";
    }
}
