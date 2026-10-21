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
using Rql;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Navigation
{
    /// <summary>
    /// One of the 32 NavMesh areas of Project Settings > AI Navigation (<c>navigation | get areas</c>): the index is what
    /// area / defaultArea fields and area-mask bits refer to. name and cost write the project settings (undoable).
    /// </summary>
    [RqlDescription("NavMesh area (Project Settings > AI Navigation): index, name, traversal cost")]
    public sealed class NavMeshArea
    {
        /// <summary>Walkable, Not Walkable and Jump: Unity does not allow renaming them.</summary>
        public const int BuiltInCount = 3;

        internal NavMeshArea(int index) { this.index = index; }

        [RqlId, RqlDescription("0 - 31: the value of area / defaultArea fields, bit index in area masks")]
        public int index { get; }

        [RqlDisplay, RqlDescription("\"\" for an unused slot; built-in areas 0 - 2 cannot be renamed")]
        public string name
        {
            get => Element(out _).FindPropertyRelative("name").stringValue;
            set
            {
                if (index < BuiltInCount) throw new InvalidOperationException($"built-in area {index} ('{name}') cannot be renamed");
                var other = NavMeshNames.AreaIndex(value);
                if (!string.IsNullOrEmpty(value) && other != null && other != index) throw new ArgumentException($"area {other} is already named '{value}'");
                Write(e => e.FindPropertyRelative("name").stringValue = value ?? "");
            }
        }

        [RqlDescription("traversal cost multiplier (>= 1)")]
        public float cost
        {
            get => Element(out _).FindPropertyRelative("cost").floatValue;
            set
            {
                if (!(value >= 1)) throw new ArgumentOutOfRangeException(nameof(value), value, "must be >= 1");
                Write(e => e.FindPropertyRelative("cost").floatValue = value);
            }
        }

        public override string ToString() => NavMeshNames.AreaName(index);

        private void Write(Action<SerializedProperty> assign)
        {
            var element = Element(out var settings);
            assign(element);
            settings.ApplyModifiedProperties();
        }

        private SerializedProperty Element(out SerializedObject settings)
        {
            settings = NavMeshNames.RequireSettings();
            var areas = settings.FindProperty("areas");
            if (areas == null || index >= areas.arraySize) throw new InvalidOperationException($"NavMesh area {index} does not exist");
            return areas.GetArrayElementAtIndex(index);
        }
    }
}
