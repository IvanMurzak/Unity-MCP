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
using Rql;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.InputSystem
{
    using InputAction = UnityEngine.InputSystem.InputAction;
    using InputActionAsset = UnityEngine.InputSystem.InputActionAsset;
    using InputBinding = UnityEngine.InputSystem.InputBinding;
    using Setup = UnityEngine.InputSystem.InputActionSetupExtensions;

    /// <summary>
    /// One binding of an action. InputBinding is a struct inside the map's binding array, so a reflected copy would lose
    /// writes; this view addresses the binding by action id + binding id (removing other bindings does not shift it) and
    /// writes through the Input System's binding syntax. Writes are validated, saved to the .inputactions file and undoable.
    /// </summary>
    [RqlDescription("Input binding: path groups interactions processors name (writable), index, id, action, isComposite, isPartOfComposite; " +
                    "call Remove() (a composite with its parts)")]
    public sealed class BindingView
    {
        private readonly Guid _action;
        private readonly Guid _id;
        private readonly int _fallbackIndex; // only for bindings without an id (hand-written files)

        internal BindingView(InputActionAsset asset, InputAction action, int index)
        {
            this.asset = asset;
            _action = action.id;
            _id = action.bindings[index].id;
            _fallbackIndex = index;
        }

        internal static BindingView Find(InputActionAsset asset, Guid action, Guid binding)
        {
            var a = asset.FindAction(action) ?? throw new InvalidOperationException($"action {action} no longer exists in the asset");
            for (int i = 0; i < a.bindings.Count; i++)
                if (a.bindings[i].id == binding) return new BindingView(asset, a, i);
            throw new InvalidOperationException($"binding {binding} was not saved");
        }

        [RqlReference]
        [RqlDescription("the InputActionAsset that owns the binding")]
        public InputActionAsset asset { get; }

        private InputAction Action =>
            (asset != null ? asset.FindAction(_action) : null)
            ?? throw new InvalidOperationException($"action {_action} no longer exists in the asset");

        private int IndexIn(InputAction action)
        {
            int i = IndexOf(action);
            return i >= 0 ? i : throw new InvalidOperationException(
                $"binding {(_id == Guid.Empty ? "#" + _fallbackIndex : _id.ToString())} no longer exists on action '{action.name}'");
        }

        // -1 when the binding is gone.
        private int IndexOf(InputAction action)
        {
            if (_id == Guid.Empty) return _fallbackIndex < action.bindings.Count ? _fallbackIndex : -1;
            for (int i = 0; i < action.bindings.Count; i++)
                if (action.bindings[i].id == _id) return i;
            return -1;
        }

        private InputBinding Binding
        {
            get
            {
                var action = Action;
                return action.bindings[IndexIn(action)];
            }
        }

        [RqlDescription("position in the action's binding list (composite parts count)")]
        public int index => IndexIn(Action);

        [RqlDescription("the binding's id")]
        public string id => _id.ToString();

        [RqlDescription("\"map/action\" the binding triggers")]
        public string action
        {
            get
            {
                var a = Action;
                return $"{a.actionMap?.name}/{a.name}";
            }
        }

        [RqlDescription("control path (\"<Gamepad>/buttonSouth\"); for a composite root the composite (\"2DVector\")")]
        public string path
        {
            get => Binding.path ?? "";
            set
            {
                var b = Binding;
                if (b.path == value) return;
                if (b.isComposite) InputValidation.Composite(value);
                else InputValidation.ControlPath(value);
                Change("set binding path", b => { b.path = value; return b; });
            }
        }

        [RqlDescription("control-scheme groups (\"Gamepad\", \"Keyboard&Mouse;Gamepad\"); each must be a control scheme of the asset")]
        public string groups
        {
            get => Binding.groups ?? "";
            set
            {
                if ((Binding.groups ?? "") == (value ?? "")) return;
                var groups = InputValidation.Groups(asset, value);
                Change("set binding groups", b => { b.groups = groups; return b; });
            }
        }

        [RqlDescription("interactions (\"hold(duration=0.4)\", \"tap\", \"press(behavior=2)\"); empty = none")]
        public string interactions
        {
            get => Binding.interactions ?? "";
            set
            {
                if ((Binding.interactions ?? "") == (value ?? "")) return;
                var text = InputValidation.Interactions(value);
                Change("set binding interactions", b => { b.interactions = text; return b; });
            }
        }

        [RqlDescription("processors (\"invert\", \"scale(factor=2)\", \"stickDeadzone\"); empty = none")]
        public string processors
        {
            get => Binding.processors ?? "";
            set
            {
                if ((Binding.processors ?? "") == (value ?? "")) return;
                var text = InputValidation.Processors(value);
                Change("set binding processors", b => { b.processors = text; return b; });
            }
        }

        [RqlDescription("binding name; for a composite part the part (\"up\", \"negative\", \"modifier\" …)")]
        public string name
        {
            get => Binding.name ?? "";
            set
            {
                var action = Action;
                int i = IndexIn(action);
                var b = action.bindings[i];
                if ((b.name ?? "") == (value ?? "")) return;
                if (b.isPartOfComposite)
                {
                    int root = i;
                    while (root > 0 && !action.bindings[root].isComposite) root--;
                    value = InputValidation.PartName(InputValidation.Composite(action.bindings[root].path), value);
                }
                Change("set binding name", b => { b.name = value; return b; });
            }
        }

        [RqlDescription("true for the root of a composite (its path is the composite name)")]
        public bool isComposite => Binding.isComposite;

        [RqlDescription("true for a part of a composite (its name is the part)")]
        public bool isPartOfComposite => Binding.isPartOfComposite;

        [RqlCallable("remove the binding (a composite root removes its parts too); returns how many bindings the action has left")]
        public int Remove()
        {
            var action = Action;
            // A part whose composite was removed earlier in the same call stage is already gone.
            if (_id != Guid.Empty && IndexOf(action) < 0) return action.bindings.Count;
            int i = IndexIn(action);
            InputAssetStore.Edit(asset, "remove binding", () => Setup.ChangeBinding(action, i).Erase());
            return Action.bindings.Count;
        }

        // The binding syntax's With* methods append to the existing value, so the binding is replaced whole with a changed copy.
        private void Change(string label, Func<InputBinding, InputBinding> change)
        {
            var action = Action;
            int i = IndexIn(action);
            var changed = change(action.bindings[i]);
            InputAssetStore.Edit(asset, label, () => Setup.ChangeBinding(action, i).To(changed));
        }

        public override string ToString()
        {
            try
            {
                var a = Action;
                int i = IndexIn(a);
                return $"binding {a.actionMap?.name}/{a.name}#{i} {a.bindings[i].path}";
            }
            catch (InvalidOperationException)
            {
                return $"binding {_id}";
            }
        }
    }
}
