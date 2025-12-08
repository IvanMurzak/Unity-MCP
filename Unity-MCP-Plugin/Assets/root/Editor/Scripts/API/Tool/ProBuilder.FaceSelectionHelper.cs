/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Kieran Hannigan (https://github.com/KaiStarkk)          │
│  Project: Ivan Murzak (https://github.com/IvanMurzak/Unity-MCP)  │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#if PROBUILDER_ENABLED
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// Helper for semantic face selection by direction.
    /// Allows selecting faces by their normal direction instead of indices.
    /// </summary>
    public static class FaceSelectionHelper
    {
        /// <summary>
        /// Direction threshold for face normal matching (dot product).
        /// 0.7 = ~45 degree tolerance
        /// </summary>
        private const float DirectionThreshold = 0.7f;

        /// <summary>
        /// Maps direction names to their corresponding vectors.
        /// </summary>
        private static readonly Dictionary<string, Vector3> DirectionMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "up", Vector3.up },
            { "down", Vector3.down },
            { "left", Vector3.left },
            { "right", Vector3.right },
            { "forward", Vector3.forward },
            { "back", Vector3.back },
            { "front", Vector3.forward },    // alias
            { "backward", Vector3.back },    // alias
            { "top", Vector3.up },           // alias
            { "bottom", Vector3.down },      // alias
        };

        /// <summary>
        /// Gets the list of valid direction names for documentation.
        /// </summary>
        public static string ValidDirections => "up, down, left, right, forward, back (aliases: top, bottom, front, backward)";

        /// <summary>
        /// Selects face indices by direction from a ProBuilder mesh.
        /// </summary>
        /// <param name="mesh">The ProBuilder mesh</param>
        /// <param name="direction">Direction name (up, down, left, right, forward, back)</param>
        /// <param name="error">Error message if direction is invalid</param>
        /// <returns>Array of face indices matching the direction, or null on error</returns>
        public static int[]? SelectFacesByDirection(ProBuilderMesh mesh, string direction, out string? error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(direction))
            {
                error = $"Direction cannot be empty. Valid directions: {ValidDirections}";
                return null;
            }

            if (!DirectionMap.TryGetValue(direction.Trim(), out var targetDir))
            {
                error = $"Invalid direction '{direction}'. Valid directions: {ValidDirections}";
                return null;
            }

            var faces = mesh.faces;
            var positions = mesh.positions;
            var matchingIndices = new List<int>();

            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces[i];
                var normal = CalculateFaceNormal(face, positions);
                var dot = Vector3.Dot(normal.normalized, targetDir);

                if (dot >= DirectionThreshold)
                {
                    matchingIndices.Add(i);
                }
            }

            if (matchingIndices.Count == 0)
            {
                error = $"No faces found facing '{direction}'. The mesh may not have faces in that direction.";
                return null;
            }

            return matchingIndices.ToArray();
        }

        /// <summary>
        /// Calculates the approximate normal of a face.
        /// </summary>
        private static Vector3 CalculateFaceNormal(Face face, IList<Vector3> positions)
        {
            var indices = face.indexes;
            if (indices.Count < 3)
                return Vector3.up;

            // Use first triangle to calculate normal
            var v0 = positions[indices[0]];
            var v1 = positions[indices[1]];
            var v2 = positions[indices[2]];

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;

            return Vector3.Cross(edge1, edge2).normalized;
        }

        /// <summary>
        /// Gets a summary of face directions for a mesh (for condensed output).
        /// </summary>
        public static Dictionary<string, List<int>> GetFaceDirectionSummary(ProBuilderMesh mesh)
        {
            var result = new Dictionary<string, List<int>>
            {
                { "up", new List<int>() },
                { "down", new List<int>() },
                { "left", new List<int>() },
                { "right", new List<int>() },
                { "forward", new List<int>() },
                { "back", new List<int>() },
                { "other", new List<int>() }
            };

            var faces = mesh.faces;
            var positions = mesh.positions;

            for (int i = 0; i < faces.Count; i++)
            {
                var normal = CalculateFaceNormal(faces[i], positions);
                var assigned = false;

                foreach (var kvp in DirectionMap.Take(6)) // Only primary 6 directions
                {
                    if (Vector3.Dot(normal.normalized, kvp.Value) >= DirectionThreshold)
                    {
                        result[kvp.Key].Add(i);
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    result["other"].Add(i);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the center position of a face.
        /// </summary>
        public static Vector3 GetFaceCenter(Face face, IList<Vector3> positions)
        {
            var center = Vector3.zero;
            var indices = face.distinctIndexes;
            foreach (var idx in indices)
            {
                center += positions[idx];
            }
            return center / indices.Count;
        }
    }
}
#endif
