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
    /// One agent type of Project Settings > AI Navigation (<c>navigation | get agentTypes</c>). NavMesh.GetSettingsByID returns
    /// a copy of NavMeshBuildSettings, so this handle reads and writes the project settings asset instead: every
    /// surface that bakes for the type picks the change up on its next Bake(). Writes are undoable.
    /// </summary>
    [RqlDescription("NavMesh agent type (Project Settings > AI Navigation): the bake settings every surface of this type uses")]
    public sealed class NavMeshAgentType
    {
        internal NavMeshAgentType(int agentTypeID) { this.agentTypeID = agentTypeID; }

        [RqlId]
        public int agentTypeID { get; }

        [RqlDisplay, RqlDescription("e.g. \"Humanoid\"; NavMeshSurface / NavMeshAgent / NavMeshLink agentType")]
        public string name => NavMeshNames.AgentTypeName(agentTypeID);

        [RqlDescription("agent radius in meters (> 0)")]
        public float agentRadius { get => Float("agentRadius"); set => SetFloat("agentRadius", Require(value, value > 0, "> 0")); }

        [RqlDescription("agent height in meters (> 0)")]
        public float agentHeight { get => Float("agentHeight"); set => SetFloat("agentHeight", Require(value, value > 0, "> 0")); }

        [RqlDescription("maximum walkable slope in degrees (0 - 60)")]
        public float agentSlope { get => Float("agentSlope"); set => SetFloat("agentSlope", Require(value, value >= 0 && value <= 60, "between 0 and 60")); }

        [RqlDescription("maximum step height in meters (>= 0)")]
        public float agentClimb { get => Float("agentClimb"); set => SetFloat("agentClimb", Require(value, value >= 0, ">= 0")); }

        [RqlDescription("drop height for generated off-mesh links in meters (>= 0)")]
        public float ledgeDropHeight { get => Float("ledgeDropHeight"); set => SetFloat("ledgeDropHeight", Require(value, value >= 0, ">= 0")); }

        [RqlDescription("jump distance for generated off-mesh links in meters (>= 0)")]
        public float maxJumpAcrossDistance { get => Float("maxJumpAcrossDistance"); set => SetFloat("maxJumpAcrossDistance", Require(value, value >= 0, ">= 0")); }

        [RqlDescription("regions smaller than this (m²) are dropped (>= 0)")]
        public float minRegionArea { get => Float("minRegionArea"); set => SetFloat("minRegionArea", Require(value, value >= 0, ">= 0")); }

        [RqlDescription("use cellSize (voxel size) instead of the automatic one; a surface can still override it (overrideVoxelSize / voxelSize)")]
        public bool manualCellSize { get => Property("manualCellSize").boolValue; set => Write("manualCellSize", p => p.boolValue = value); }

        [RqlDescription("voxel size in meters (> 0), used when manualCellSize")]
        public float cellSize { get => Float("cellSize"); set => SetFloat("cellSize", Require(value, value > 0, "> 0")); }

        [RqlDescription("use tileSize instead of the automatic one")]
        public bool manualTileSize { get => Property("manualTileSize").boolValue; set => Write("manualTileSize", p => p.boolValue = value); }

        [RqlDescription("tile size in voxels (16 - 1024), used when manualTileSize")]
        public int tileSize { get => Property("tileSize").intValue; set { Require(value, value >= 16 && value <= 1024, "between 16 and 1024"); Write("tileSize", p => p.intValue = value); } }

        public override string ToString() => name;

        private float Float(string property) => Property(property).floatValue;

        private void SetFloat(string property, float value) => Write(property, p => p.floatValue = value);

        private SerializedProperty Property(string name) => Element(out _).FindPropertyRelative(name);

        private void Write(string property, Action<SerializedProperty> assign)
        {
            var element = Element(out var settings);
            assign(element.FindPropertyRelative(property));
            settings.ApplyModifiedProperties();
        }

        private SerializedProperty Element(out SerializedObject settings)
        {
            settings = NavMeshNames.RequireSettings();
            var list = settings.FindProperty("m_Settings");
            for (int i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("agentTypeID").intValue == agentTypeID) return element;
            }
            throw new InvalidOperationException($"NavMesh agent type #{agentTypeID} no longer exists");
        }

        private static T Require<T>(T value, bool ok, string range) =>
            ok ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "must be " + range);
    }
}
