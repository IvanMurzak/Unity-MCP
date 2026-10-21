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
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Navigation
{
    using NavMesh = UnityEngine.AI.NavMesh;

    /// <summary>
    /// Agent-type and area names (what agents read and write instead of raw ids / indices / bitmasks), and the
    /// NavMeshProjectSettings asset (Project Settings > AI Navigation) that stores agent types and areas.
    /// </summary>
    public static class NavMeshNames
    {
        public const int AreaCount = 32;
        public const string All = "All";

        // ------------------------------------------------------------ agent types

        /// <summary>The agent type's name ("Humanoid"), or "#id" when the id is not defined in the project.</summary>
        public static string AgentTypeName(int id)
        {
            var name = NavMesh.GetSettingsNameFromID(id);
            return string.IsNullOrEmpty(name) ? "#" + id : name;
        }

        public static int? AgentTypeId(string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
            {
                var id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                if (string.Equals(NavMesh.GetSettingsNameFromID(id), name, StringComparison.OrdinalIgnoreCase)) return id;
            }
            return null;
        }

        public static IEnumerable<string> AgentTypeNames()
        {
            for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
                yield return AgentTypeName(NavMesh.GetSettingsByIndex(i).agentTypeID);
        }

        /// <summary>A rejection reason for an unknown agent-type name, or null.</summary>
        public static string? ValidateAgentType(string? name) =>
            AgentTypeId(name) == null ? $"no NavMesh agent type '{name}' (defined: {string.Join(", ", AgentTypeNames())})" : null;

        public static int RequireAgentTypeId(string? name) =>
            AgentTypeId(name) ?? throw new ArgumentException(ValidateAgentType(name));

        // ------------------------------------------------------------ areas

        /// <summary>The names of the 32 areas ("" for unnamed slots), read from the project settings.</summary>
        public static string[] AreaNames()
        {
            var names = new string[AreaCount];
            var areas = Settings()?.FindProperty("areas");
            for (int i = 0; i < AreaCount; i++)
                names[i] = areas != null && i < areas.arraySize ? areas.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue : "";
            return names;
        }

        /// <summary>The area's name ("Walkable"), or "#index" for an unnamed slot.</summary>
        public static string AreaName(int index, string[]? names = null)
        {
            names ??= AreaNames();
            return index >= 0 && index < names.Length && !string.IsNullOrEmpty(names[index]) ? names[index] : "#" + index;
        }

        public static int? AreaIndex(string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var names = AreaNames();
            for (int i = 0; i < names.Length; i++)
                if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase)) return i;
            return null;
        }

        public static string? ValidateArea(string? name) =>
            AreaIndex(name) == null ? $"no NavMesh area '{name}' (defined: {string.Join(", ", AreaNames().Where(n => n.Length > 0))})" : null;

        public static int RequireAreaIndex(string? name) => AreaIndex(name) ?? throw new ArgumentException(ValidateArea(name));

        /// <summary>An area mask as names: ["All"] for every area, else the named areas whose bit is set ("#index" if unnamed).</summary>
        public static List<string> AreaMaskNames(int mask)
        {
            if (mask == NavMesh.AllAreas) return new List<string> { All };
            var names = AreaNames();
            var list = new List<string>();
            for (int i = 0; i < AreaCount; i++)
                if ((mask & (1 << i)) != 0) list.Add(AreaName(i, names));
            return list;
        }

        public static string? ValidateAreaMask(IEnumerable<string>? names) =>
            names?.Where(n => !IsAll(n)).Select(ValidateArea).FirstOrDefault(r => r != null);

        public static int AreaMask(IEnumerable<string>? names)
        {
            int mask = 0;
            foreach (var n in names ?? Enumerable.Empty<string>())
            {
                if (IsAll(n)) return NavMesh.AllAreas;
                mask |= 1 << RequireAreaIndex(n);
            }
            return mask;
        }

        public static bool IsAll(string? name) => string.Equals(name, All, StringComparison.OrdinalIgnoreCase);

        // ------------------------------------------------------------ project settings

        /// <summary>
        /// A fresh SerializedObject over Project Settings > AI Navigation (agent types: m_Settings / m_SettingNames, areas).
        /// Writes go through ApplyModifiedProperties, so they are undoable.
        /// </summary>
        public static SerializedObject? Settings()
        {
            var singleton = Unsupported.GetSerializedAssetInterfaceSingleton("NavMeshProjectSettings");
            return singleton == null ? null : new SerializedObject(singleton);
        }

        public static SerializedObject RequireSettings() =>
            Settings() ?? throw new InvalidOperationException("the NavMeshProjectSettings asset (Project Settings > AI Navigation) is unavailable");
    }
}
