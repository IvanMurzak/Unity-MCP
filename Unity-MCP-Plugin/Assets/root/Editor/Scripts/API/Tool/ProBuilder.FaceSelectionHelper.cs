/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Kieran Hannigan (https://github.com/KaiStarkk)          │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#if PROBUILDER_ENABLED
#nullable enable
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// Direction for semantic face selection.
    /// </summary>
    public enum FaceDirection
    {
        [Description("Faces pointing upward (+Y)")]
        Up,
        [Description("Faces pointing downward (-Y)")]
        Down,
        [Description("Faces pointing left (-X)")]
        Left,
        [Description("Faces pointing right (+X)")]
        Right,
        [Description("Faces pointing forward (+Z)")]
        Forward,
        [Description("Faces pointing backward (-Z)")]
        Back
    }

    /// <summary>
    /// Detail level for mesh information output.
    /// </summary>
    public enum MeshInfoDetailLevel
    {
        [Description("Condensed face direction summary (token-efficient)")]
        Summary,
        [Description("Detailed face-by-face information")]
        Full
    }

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
        /// Maps FaceDirection enum to corresponding vectors.
        /// </summary>
        private static readonly Dictionary<FaceDirection, Vector3> DirectionMap = new()
        {
            { FaceDirection.Up, Vector3.up },
            { FaceDirection.Down, Vector3.down },
            { FaceDirection.Left, Vector3.left },
            { FaceDirection.Right, Vector3.right },
            { FaceDirection.Forward, Vector3.forward },
            { FaceDirection.Back, Vector3.back },
        };

        /// <summary>
        /// Selects face indices by direction from a ProBuilder mesh.
        /// </summary>
        /// <param name="mesh">The ProBuilder mesh</param>
        /// <param name="direction">Direction enum value</param>
        /// <param name="error">Error message if no faces found</param>
        /// <returns>Array of face indices matching the direction, or null on error</returns>
        public static int[]? SelectFacesByDirection(ProBuilderMesh mesh, FaceDirection direction, out string? error)
        {
            error = null;

            var targetDir = DirectionMap[direction];

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
        /// Returns dictionary with FaceDirection enum keys plus "other" for unclassified faces.
        /// </summary>
        public static Dictionary<FaceDirection, List<int>> GetFaceDirectionSummary(ProBuilderMesh mesh, out List<int> otherFaces)
        {
            var result = new Dictionary<FaceDirection, List<int>>();
            foreach (FaceDirection dir in System.Enum.GetValues(typeof(FaceDirection)))
            {
                result[dir] = new List<int>();
            }
            otherFaces = new List<int>();

            var faces = mesh.faces;
            var positions = mesh.positions;

            for (int i = 0; i < faces.Count; i++)
            {
                var normal = CalculateFaceNormal(faces[i], positions);
                var assigned = false;

                foreach (var kvp in DirectionMap)
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
                    otherFaces.Add(i);
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
