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
using System.Collections.Generic;
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
            // A component's name and tag are its GameObject's: the same checks as on the GameObject.
            m.Add<string>("name", c => c.name, (c, v) => c.name = v, (c, v) => UnityEngine_GameObject_Adapter.ValidateName(v), "the GameObject's name (not empty)")
             .Add<string>("tag", c => c.tag, (c, v) => c.tag = v, (c, v) => UnityEngine_GameObject_Adapter.ValidateTag(v), "the GameObject's tag (a defined tag)")
             .Method("Destroy", c => Destroy(c), "remove this component (undoable)", ValidateDestroy);
        }

        // Unity refuses (and only logs) to remove a Transform or a component another one requires ([RequireComponent]). The call
        // validator rejects both for every target before the first removal (dry runs included); the check after the removal
        // stays for a refusal it cannot foresee.
        private static void Destroy(Component c)
        {
            if (c == null) return; // its GameObject, or a component it required, was destroyed by an earlier target of the same call
            // The components that require this one and that the same call removes go first, whatever the target order.
            if (RqlUnity.Batch is { } batch && batch.Removing.Add(c))
            {
                try
                {
                    foreach (var blocker in Blockers(c, out _))
                        if (batch.IsDestroying(blocker)) Destroy(blocker);
                }
                finally { batch.Removing.Remove(c); }
            }
            if (Rejection(c) is { } rejection) throw new InvalidOperationException(rejection); // a C# caller skips RQL's validator
            RqlUnity.DestroyUndoable(c, $"Unity did not remove {c.GetType().Name} from '{c.gameObject.name}': another component requires it");
        }

        // The call validator. Inside rql-mutate a component that others require may still be removed when the same call removes
        // them as well: that is decided once every target is validated (RqlUnity.CallBatch).
        internal static string? ValidateDestroy(Component c)
        {
            if (c == null || c is Transform || !(RqlUnity.Batch is { } batch)) return Rejection(c);
            var blockers = Blockers(c, out var reason);
            batch.Destroying(c, blockers, reason + " (or in the same call)");
            return null;
        }

        /// <summary>Why <paramref name="c"/> cannot be removed now, or null.</summary>
        internal static string? Rejection(Component c)
        {
            if (c == null) return null;
            if (c is Transform) return "a Transform cannot be removed; destroy its GameObject instead (… | call Destroy())";
            return Blockers(c, out var reason).Count > 0 ? reason : null;
        }

        // The other components on c's GameObject whose [RequireComponent] only c satisfies, and the message for the first one.
        private static List<Component> Blockers(Component c, out string reason)
        {
            reason = "";
            var blockers = new List<Component>();
            var type = c.GetType();
            foreach (var other in c.GetComponents<Component>())
            {
                if (other == null || other == c) continue;
                foreach (RequireComponent require in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                {
                    foreach (var required in new[] { require.m_Type0, require.m_Type1, require.m_Type2 })
                    {
                        // Another component of the required type keeps the requirement met.
                        if (required == null || !required.IsAssignableFrom(type) || c.GetComponents(required).Length >= 2 || blockers.Contains(other)) continue;
                        if (blockers.Count == 0)
                            reason = $"{other.GetType().Name} on '{c.gameObject.name}' requires {required.Name}: remove the {other.GetType().Name} first";
                        blockers.Add(other);
                    }
                }
            }
            return blockers;
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
