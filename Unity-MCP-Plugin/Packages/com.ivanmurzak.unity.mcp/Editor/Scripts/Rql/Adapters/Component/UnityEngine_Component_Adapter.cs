/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using Rql.Execution;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>
    /// Components: the back-pointers are references (not followed by generic recurse); Destroy removes the component.
    /// Where a component is expected, a GameObject stands for its component of that type, e.g.
    /// <c>set transform.parent = ref("GameObject#…")</c> or <c>set target = ref("GameObject#…")</c> for a Light field.
    /// </summary>
    public sealed class UnityEngine_Component_Adapter : RqlTypeAdapter<Component>
    {
        protected override void Members(MemberTable<Component> m)
        {
            m.Untyped.Reference("gameObject", "transform");
            m.Method("Destroy", c => Destroy(c), "remove this component (undoable)");
        }

        // Unity refuses (and only logs) to remove a Transform or a component another one requires ([RequireComponent]): report
        // it instead of a receipt for a change that did not happen.
        private static void Destroy(Component c)
        {
            if (c is Transform) throw new InvalidOperationException("a Transform cannot be removed; destroy its GameObject instead (… | call Destroy())");
            RqlUnity.DestroyUndoable(c, $"Unity did not remove {c.GetType().Name} from '{c.gameObject.name}': another component requires it");
        }

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (!(value is GameObject go)) return AdapterOutcome.NotHandled;
            if (go == null) { reason = "the GameObject was destroyed"; return AdapterOutcome.Failed; }
            var component = go.GetComponent(target);
            if (component == null) { reason = $"GameObject '{go.name}' has no {target.Name}"; return AdapterOutcome.Failed; }
            result = component;
            return AdapterOutcome.Ok;
        }
    }
}
