/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Terrain)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Terrains
{
    /// <summary>
    /// TreePrototype / DetailPrototype: TerrainData returns copies of these classes, so a member write on an element
    /// (<c>set terrainData.treePrototypes[0].bendFactor = 0.3</c>) would change a throw-away copy. Their members are read-only
    /// (the write fails instead of being lost); an element is replaced whole from an object literal, which this adapter builds:
    /// <c>set terrainData.treePrototypes[0] = {prefab: ref("GameObject#…"), bendFactor: 0.3}</c>.
    /// </summary>
    public sealed class UnityEngine_TerrainPrototype_Adapter : RqlTypeAdapter
    {
        public UnityEngine_TerrainPrototype_Adapter(Type prototype) : base(prototype) { }

        public override string Name => Target.Name;

        public override string Description =>
            $"{Target.Name}: TerrainData holds copies — replace an element whole: set terrainData.{Field}[0] = {{{Example}}}";

        private string Field => Target == typeof(UnityEngine.TreePrototype) ? "treePrototypes" : "detailPrototypes";

        private string Example => Target == typeof(UnityEngine.TreePrototype) ? "prefab: ref(\"GameObject#…\"), bendFactor: 0.3" : "prototypeTexture: ref(\"Texture2D#…\"), minHeight: 0.5";

        public override void ConfigureMembers(MemberTable members) =>
            members.ReadOnly(members.Members.Where(m => m.CanWrite).Select(m => m.Name).ToArray());

        public override AdapterOutcome TryCoerce(object? value, Type target, RqlKernel kernel, out object? result, out string? reason)
        {
            result = null;
            reason = null;
            if (!(value is ObjectValue literal)) return AdapterOutcome.NotHandled;
            var instance = Activator.CreateInstance(Target);
            foreach (var kv in literal)
            {
                var property = Target.GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance);
                var field = property == null ? Target.GetField(kv.Key, BindingFlags.Public | BindingFlags.Instance) : null;
                var type = property != null && property.CanWrite ? property.PropertyType : field?.FieldType;
                if (type == null)
                {
                    reason = $"{Target.Name} has no writable member '{kv.Key}'";
                    return AdapterOutcome.Failed;
                }
                if (!kernel.TryCoerce(kv.Value, type, out var coerced, out reason, kv.Key)) return AdapterOutcome.Failed;
                if (property != null) property.SetValue(instance, coerced);
                else field!.SetValue(instance, coerced);
            }
            result = instance;
            return AdapterOutcome.Ok;
        }
    }
}
