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
    using InputAction = UnityEngine.InputSystem.InputAction;
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;
    using InputActionType = UnityEngine.InputSystem.InputActionType;
    using Setup = UnityEngine.InputSystem.InputActionSetupExtensions;

    /// <summary>
    /// One action of an InputActionAsset, addressed by its id. Members mirror the Input System editor's action properties;
    /// writes and calls are validated, saved to the .inputactions file and undoable.
    /// </summary>
    [RqlDescription("Input action: name type expectedControlType interactions processors initialStateCheck (writable), map, id, bindings; " +
                    "call AddBinding(path, groups?, interactions?, processors?), AddComposite(composite, {part: path}, groups?, interactions?, processors?), Remove()")]
    public sealed class ActionView
    {
        private readonly Guid _id;

        internal ActionView(InputActionAsset asset, InputAction action)
        {
            this.asset = asset;
            _id = action.id;
        }

        [RqlReference]
        [RqlDescription("the InputActionAsset that owns the action")]
        public InputActionAsset asset { get; }

        [RqlDescription("the action's id (stable across renames)")]
        public string id => _id.ToString();

        internal InputAction Action =>
            (asset != null ? asset.FindAction(_id) : null)
            ?? throw new InvalidOperationException($"action {_id} no longer exists in the asset");

        [RqlDescription("name of the action map that holds the action")]
        public string map => Action.actionMap?.name ?? "";

        [RqlDescription("action name, unique in its map; bindings follow a rename")]
        public string name
        {
            get => Action.name;
            set
            {
                var action = Action;
                if (action.name == value) return;
                InputValidation.Name(value, "action");
                InputValidation.UniqueName(action.actionMap.actions.Where(a => a.id != _id).Select(a => a.name), value, "action", $"map '{action.actionMap.name}'");
                InputAssetStore.Edit(asset, "rename action", () => Setup.Rename(action, value));
            }
        }

        [RqlDescription("Button (pressed / released), Value (continuous, with initial state check) or PassThrough (every control change); " +
                        "leaving Value turns initialStateCheck off")]
        public InputActionType type
        {
            get => Action.type;
            set
            {
                var action = Action;
                if (action.type == value) return;
                InputAssetStore.Edit(asset, "set action type", () =>
                {
                    // The file stores the computed check (always true for Value), so it would stick after leaving Value.
                    if (action.type == InputActionType.Value) action.wantsInitialStateCheck = false;
                    InputAssetStore.SetField(action, "m_Type", value);
                });
            }
        }

        [RqlDescription("control layout the action expects (\"Button\", \"Axis\", \"Vector2\", \"Stick\" …); empty = any")]
        public string expectedControlType
        {
            get => Action.expectedControlType ?? "";
            set
            {
                var action = Action;
                var layout = InputValidation.ExpectedControlType(value);
                if ((action.expectedControlType ?? "") == (layout ?? "")) return;
                InputAssetStore.Edit(asset, "set expected control type", () => action.expectedControlType = layout);
            }
        }

        [RqlDescription("interactions applied to every binding, e.g. \"hold(duration=0.5)\" or \"tap,slowTap\"; empty = none")]
        public string interactions
        {
            get => Action.interactions ?? "";
            set
            {
                var action = Action;
                var text = InputValidation.Interactions(value);
                if ((action.interactions ?? "") == (text ?? "")) return;
                InputAssetStore.Edit(asset, "set action interactions", () => InputAssetStore.SetField(action, "m_Interactions", text));
            }
        }

        [RqlDescription("processors applied to every binding, e.g. \"normalize(min=-1,max=1)\" or \"invert\"; empty = none")]
        public string processors
        {
            get => Action.processors ?? "";
            set
            {
                var action = Action;
                var text = InputValidation.Processors(value);
                if ((action.processors ?? "") == (text ?? "")) return;
                InputAssetStore.Edit(asset, "set action processors", () => InputAssetStore.SetField(action, "m_Processors", text));
            }
        }

        [RqlDescription("Button / PassThrough only: report a control already actuated when the action is enabled (always on for Value)")]
        public bool initialStateCheck
        {
            get => Action.wantsInitialStateCheck;
            set
            {
                var action = Action;
                if (action.wantsInitialStateCheck == value) return;
                InputAssetStore.Edit(asset, "set initial state check", () => action.wantsInitialStateCheck = value);
            }
        }

        [RqlDescription("the action's bindings in order; a composite is its root followed by its parts")]
        public IReadOnlyList<BindingView> bindings
        {
            get
            {
                var action = Action;
                var list = new List<BindingView>();
                for (int i = 0; i < action.bindings.Count; i++) list.Add(new BindingView(asset, action, i));
                return list.AsReadOnly();
            }
        }

        [RqlDescription("number of bindings (composite parts included)")]
        public int bindingCount => Action.bindings.Count;

        [RqlCallable("bind a control path (\"<Keyboard>/space\", \"<Gamepad>/buttonSouth\", \"<Mouse>/leftButton\") with optional control-scheme " +
                     "groups (\"Gamepad\" or \"Keyboard&Mouse;Gamepad\"), interactions and processors; returns the binding")]
        public BindingView AddBinding(string path, string? groups = null, string? interactions = null, string? processors = null)
        {
            var action = Action;
            InputValidation.ControlPath(path);
            groups = InputValidation.Groups(asset, groups);
            interactions = InputValidation.Interactions(interactions);
            processors = InputValidation.Processors(processors);
            Guid id = default;
            InputAssetStore.Edit(asset, "add binding", () =>
            {
                Setup.AddBinding(action, path, interactions, processors, groups);
                EnsureBindingIds(action);
                id = action.bindings[action.bindings.Count - 1].id;
            });
            return BindingView.Find(asset, _id, id);
        }

        [RqlCallable("add a composite binding (\"2DVector\", \"1DAxis\", \"3DVector\", \"ButtonWithOneModifier\" …) with its parts as " +
                     "{part: path}, e.g. {up: \"<Keyboard>/w\", down: \"<Keyboard>/s\", left: \"<Keyboard>/a\", right: \"<Keyboard>/d\"}; " +
                     "groups apply to every part; returns the composite (root) binding")]
        public BindingView AddComposite(string composite, Dictionary<string, string> parts, string? groups = null, string? interactions = null, string? processors = null)
        {
            var action = Action;
            var type = InputValidation.Composite(composite);
            if (parts == null || parts.Count == 0)
                throw new ArgumentException($"parts are empty; {composite} has {string.Join(", ", InputValidation.PartNames(type))}");
            var resolved = parts.Select(p => (name: InputValidation.PartName(type, p.Key), path: InputValidation.ControlPath(p.Value))).ToList();
            groups = InputValidation.Groups(asset, groups);
            interactions = InputValidation.Interactions(interactions);
            processors = InputValidation.Processors(processors);
            Guid id = default;
            InputAssetStore.Edit(asset, "add composite", () =>
            {
                int root = action.bindings.Count;
                var syntax = Setup.AddCompositeBinding(action, composite, interactions, processors);
                foreach (var (name, path) in resolved) syntax = syntax.With(name, path, groups);
                EnsureBindingIds(action);
                id = action.bindings[root].id;
            });
            return BindingView.Find(asset, _id, id);
        }

        [RqlCallable("remove the action and its bindings; returns how many actions are left in its map")]
        public int Remove()
        {
            var action = Action;
            var map = action.actionMap;
            InputAssetStore.Edit(asset, "remove action", () => Setup.RemoveAction(action));
            return asset.FindActionMap(map.id)?.actions.Count ?? 0;
        }

        /// <summary>Bindings are addressed by id; give every binding without one (older or hand-written files) a new id.</summary>
        internal static void EnsureBindingIds(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.id != Guid.Empty) continue;
                binding.id = Guid.NewGuid();
                Setup.ChangeBinding(action, i).To(binding);
            }
        }

        public override string ToString() => asset != null && asset.FindAction(_id) is InputAction a ? $"action {a.actionMap?.name}/{a.name}" : $"action {_id}";
    }
}
