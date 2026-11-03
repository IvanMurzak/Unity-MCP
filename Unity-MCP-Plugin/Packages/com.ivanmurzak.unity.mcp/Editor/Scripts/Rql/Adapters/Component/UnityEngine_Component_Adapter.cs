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
        // validator rejects a Transform and RqlUnity.ValidateBatch an unmet requirement, for the whole call before the first
        // removal (dry runs included); the check after the removal stays for a refusal they cannot foresee.
        private static void Destroy(Component c)
        {
            if (c == null) return; // its GameObject, or a component it required, was destroyed by an earlier target of the same call
            // The components that require this one and that the same call removes go first, whatever the target order.
            if (RqlUnity.Destroying is { } destroying && Removing.Add(c))
            {
                try
                {
                    foreach (var (blocker, _) in Blockers(c))
                        if (destroying.Contains(blocker)) Destroy(blocker);
                }
                finally { Removing.Remove(c); }
            }
            if (Rejection(c) is { } rejection) throw new InvalidOperationException(rejection); // a C# caller skips RQL's validator
            RqlUnity.DestroyUndoable(c, $"Unity did not remove {c.GetType().Name} from '{c.gameObject.name}': another component requires it");
        }

        // Components whose dependents are being removed first (two components requiring each other end the recursion).
        private static readonly HashSet<Component> Removing = new HashSet<Component>();

        // The call validator: a Transform never goes. A component that others require may still go when the same call removes
        // them as well, which RqlUnity.ValidateBatch decides once every target is known.
        internal static string? ValidateDestroy(Component c) => c is Transform ? Rejection(c) : null;

        /// <summary>Why <paramref name="c"/> cannot be removed now, or null.</summary>
        internal static string? Rejection(Component c)
        {
            if (c == null) return null;
            if (c is Transform) return "a Transform cannot be removed; destroy its GameObject instead (… | call Destroy())";
            var blockers = Blockers(c);
            return blockers.Count > 0 ? blockers[0].Reason : null;
        }

        // The other components on c's GameObject whose [RequireComponent] only c satisfies, each with the reason it blocks. A
        // provider the running call removes does not count (two colliders removed together leave a Collider requirement unmet).
        internal static List<(Component Blocker, string Reason)> Blockers(Component c)
        {
            var blockers = new List<(Component, string)>();
            var type = c.GetType();
            var destroying = RqlUnity.Destroying;
            bool Survives(Component x) => x != null && x != c && (destroying == null || !destroying.Contains(x));
            foreach (var other in c.GetComponents<Component>())
            {
                if (other == null || other == c) continue;
                foreach (RequireComponent require in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                {
                    foreach (var required in new[] { require.m_Type0, require.m_Type1, require.m_Type2 })
                    {
                        // Another component of the required type keeps the requirement met.
                        if (required == null || !required.IsAssignableFrom(type) || Array.Exists(c.GetComponents(required), Survives) || blockers.Exists(b => b.Item1 == other)) continue;
                        blockers.Add((other, $"{other.GetType().Name} on '{c.gameObject.name}' requires {required.Name}: remove the {other.GetType().Name} first"));
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
