/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Navigation)     │
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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Navigation
{
    /// <summary>Member configuration shared by the navigation component adapters.</summary>
    public static class NavigationMembers
    {
        /// <summary>
        /// Hide the private [SerializeField] backing fields (m_AgentTypeID …) that duplicate a public property: they double the
        /// output, and writing them bypasses the property logic (NavMeshLink setters call UpdateLink(), its costModifier is
        /// m_CostModifier signed by m_IsOverridingCost). Backing fields without a property stay visible.
        /// </summary>
        public static MemberTable HideBackingFields(MemberTable table, params string[] alsoHide)
        {
            var properties = new HashSet<string>(table.Members.Select(m => m.Name).Where(n => !IsBackingField(n)), StringComparer.OrdinalIgnoreCase);
            return table.HideWhere(m => IsBackingField(m.Name)
                                        && (properties.Contains(m.Name.Substring(2)) || m.Name == "m_SerializedVersion" || alsoHide.Contains(m.Name)));
        }

        private static bool IsBackingField(string name) => name.StartsWith("m_", StringComparison.Ordinal);

        /// <summary>A writable <c>agentType</c> name member over an agent-type id property.</summary>
        public static MemberTable<T> AddAgentType<T>(MemberTable<T> m, Func<T, int> get, Action<T, int> set) =>
            m.Add<string>("agentType", c => NavMeshNames.AgentTypeName(get(c)), (c, v) => set(c, NavMeshNames.RequireAgentTypeId(v)),
                (c, v) => NavMeshNames.ValidateAgentType(v), "agent type name (e.g. \"Humanoid\"); assigning sets agentTypeID");

        /// <summary>A writable area name member (e.g. <c>areaName</c>) over an area index property.</summary>
        public static MemberTable<T> AddAreaName<T>(MemberTable<T> m, string name, Func<T, int> get, Action<T, int> set, string indexMember) =>
            m.Add<string>(name, c => NavMeshNames.AreaName(get(c)), (c, v) => set(c, NavMeshNames.RequireAreaIndex(v)),
                (c, v) => NavMeshNames.ValidateArea(v), $"area name (\"Walkable\", \"Not Walkable\", \"Jump\" or a project area); assigning sets {indexMember}");

        /// <summary>
        /// NavMeshModifier / NavMeshModifierVolume keep the agent types they affect in a private list (m_AffectedAgents:
        /// [-1] = all, empty = none). Exposed as names: ["All"], [] or ["Humanoid", …]; written through SerializedObject (undoable).
        /// </summary>
        public static MemberTable<T> AddAffectedAgents<T>(MemberTable<T> m) where T : Component
        {
            m.Untyped.Hide("m_AffectedAgents");
            return m.Add<List<string>>("affectedAgents", c => AffectedAgents(c), (c, v) => SetAffectedAgents(c, v),
                (c, v) => v?.Where(n => !NavMeshNames.IsAll(n)).Select(NavMeshNames.ValidateAgentType).FirstOrDefault(r => r != null),
                "agent types this applies to: [\"All\"], [] (none) or names such as [\"Humanoid\"]");
        }

        private static List<string> AffectedAgents(Component c)
        {
            var list = new SerializedObject(c).FindProperty("m_AffectedAgents");
            var names = new List<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var id = list.GetArrayElementAtIndex(i).intValue;
                names.Add(id == -1 ? NavMeshNames.All : NavMeshNames.AgentTypeName(id));
            }
            return names;
        }

        private static void SetAffectedAgents(Component c, List<string>? names)
        {
            var ids = names == null ? new List<int>()
                : names.Any(NavMeshNames.IsAll) ? new List<int> { -1 }
                : names.Select(NavMeshNames.RequireAgentTypeId).Distinct().ToList();
            var so = new SerializedObject(c);
            var list = so.FindProperty("m_AffectedAgents");
            list.arraySize = ids.Count;
            for (int i = 0; i < ids.Count; i++) list.GetArrayElementAtIndex(i).intValue = ids[i];
            so.ApplyModifiedProperties();
        }
    }
}
