/*
┌──────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                     │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-ProBuilder)  │
│  Copyright (c) 2026 Ivan Murzak                                          │
│  Licensed under the Apache License, Version 2.0.                         │
│  See the LICENSE file in the project root for more information.          │
└──────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ProBuilder
{
    using Edge = UnityEngine.ProBuilder.Edge;
    using Face = UnityEngine.ProBuilder.Face;
    using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;

    /// <summary>
    /// Shared ProBuilder bookkeeping behind the RQL surface: face / edge selection checks (nothing is clamped or skipped
    /// silently), face directions, and the rebuild after an edit. The engine records the call target (the ProBuilderMesh, whose
    /// serialized geometry is the whole edit) for Undo; the Unity Mesh that ProBuilder generates is not serialized, so meshes
    /// edited here are rebuilt after Undo / Redo (ProBuilder itself only does that for the selected meshes).
    /// </summary>
    internal static class ProBuilderOps
    {
        /// <summary>Dot product with the axis from which a face counts as facing that way (0.7 ≈ 45°).</summary>
        public const float DirectionThreshold = 0.7f;

        public static readonly string[] Directions = { "Up", "Down", "Left", "Right", "Forward", "Back" };

        /// <summary><see cref="Directions"/> followed by "Other": every value <see cref="Direction"/> returns, in report order.</summary>
        public static readonly string[] DirectionsAndOther = Directions.Append("Other").ToArray();

        private static readonly Vector3[] Axes = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        private static readonly HashSet<ProBuilderMesh> Edited = new HashSet<ProBuilderMesh>();
        private static bool _hooked;

        // ------------------------------------------------------------ rebuild / undo

        /// <summary>Regenerate the Unity Mesh from the ProBuilder data and keep it in sync on Undo / Redo.</summary>
        public static void Rebuild(ProBuilderMesh mesh)
        {
            mesh.ToMesh();
            mesh.Refresh();
            EditorUtility.SetDirty(mesh);
            if (!_hooked)
            {
                Undo.undoRedoPerformed += Resync;
                _hooked = true;
            }
            Edited.Add(mesh);
        }

        private static void Resync()
        {
            Edited.RemoveWhere(m => m == null);
            foreach (var mesh in Edited)
            {
                mesh.ToMesh();
                mesh.Refresh();
            }
        }

        // ------------------------------------------------------------ selections

        /// <summary>The faces at <paramref name="indices"/>: every index must exist; duplicates are ignored.</summary>
        public static Face[] Faces(ProBuilderMesh mesh, int[]? indices, string parameter = "faces")
        {
            var faces = mesh.faces;
            if (faces.Count == 0) throw new InvalidOperationException($"'{mesh.name}' has no faces");
            if (indices == null || indices.Length == 0)
                throw new ArgumentException($"{parameter} is empty: pass face indices such as [0, 1] or probuilder.facing($, \"Up\")");
            var invalid = indices.Where(i => i < 0 || i >= faces.Count).Distinct().ToArray();
            if (invalid.Length > 0)
                throw new ArgumentOutOfRangeException(parameter, $"face index {string.Join(", ", invalid)} does not exist on '{mesh.name}' (0 … {faces.Count - 1})");
            return indices.Distinct().Select(i => faces[i]).ToArray();
        }

        /// <summary>Edges given as [a, b] vertex index pairs; each must be an edge of one of the mesh's faces (either order).</summary>
        public static List<Edge> Edges(ProBuilderMesh mesh, int[][]? edges, string parameter = "edges")
        {
            if (edges == null || edges.Length == 0)
                throw new ArgumentException($"{parameter} is empty: pass vertex index pairs such as [[0, 1], [2, 3]] or probuilder.edges($, faces)");
            var known = new HashSet<Edge>(mesh.faces.SelectMany(f => f.edges));
            var result = new List<Edge>();
            var seen = new HashSet<Edge>();
            foreach (var pair in edges)
            {
                if (pair == null || pair.Length != 2)
                    throw new ArgumentException($"{parameter}: every edge is a pair of vertex indices [a, b], got [{(pair == null ? "" : string.Join(", ", pair))}]");
                var edge = new Edge(pair[0], pair[1]);
                if (!known.Contains(edge))
                    throw new ArgumentException($"{parameter}: [{pair[0]}, {pair[1]}] is not an edge of '{mesh.name}' (list them with probuilder.edges($) or faces.edges)");
                if (seen.Add(edge)) result.Add(edge);
            }
            return result;
        }

        public static int[] IndicesOf(ProBuilderMesh mesh, IEnumerable<Face> faces)
        {
            var set = new HashSet<Face>(faces);
            var all = mesh.faces;
            var indices = new List<int>();
            for (int i = 0; i < all.Count; i++)
                if (set.Contains(all[i])) indices.Add(i);
            return indices.ToArray();
        }

        public static int[][] EdgeList(IEnumerable<Edge> edges) => edges.Select(e => new[] { e.a, e.b }).ToArray();

        /// <summary>Distinct edges of <paramref name="faces"/> (every face when null).</summary>
        public static List<Edge> EdgesOf(ProBuilderMesh mesh, IEnumerable<Face>? faces = null)
        {
            var result = new List<Edge>();
            var seen = new HashSet<Edge>();
            foreach (var face in faces ?? mesh.faces)
                foreach (var edge in face.edges)
                    if (seen.Add(edge)) result.Add(edge);
            return result;
        }

        /// <summary>Border edges: edges with a face on one side only (around holes and open shapes such as planes).</summary>
        public static List<Edge> OpenEdges(ProBuilderMesh mesh) =>
            UnityEngine.ProBuilder.WingedEdge.GetWingedEdges(mesh).Where(w => w.opposite == null).Select(w => w.edge.local).ToList();

        // ------------------------------------------------------------ geometry

        /// <summary>Local-space normal of a face from its first triangle.</summary>
        public static Vector3 Normal(ProBuilderMesh mesh, Face face)
        {
            var indexes = face.indexes;
            if (indexes.Count < 3) return Vector3.zero;
            var positions = mesh.positions;
            var v0 = positions[indexes[0]];
            return Vector3.Cross(positions[indexes[1]] - v0, positions[indexes[2]] - v0).normalized;
        }

        public static Vector3 Center(ProBuilderMesh mesh, Face face)
        {
            var positions = mesh.positions;
            var distinct = face.distinctIndexes;
            var center = Vector3.zero;
            foreach (var i in distinct) center += positions[i];
            return distinct.Count > 0 ? center / distinct.Count : center;
        }

        /// <summary>The local axis a normal points along (Up, Down, Left, Right, Forward, Back), or "Other".</summary>
        public static string Direction(Vector3 normal)
        {
            for (int i = 0; i < Axes.Length; i++)
                if (Vector3.Dot(normal, Axes[i]) >= DirectionThreshold) return Directions[i];
            return "Other";
        }

        public static string ParseDirection(string direction)
        {
            var trimmed = direction?.Trim();
            foreach (var d in DirectionsAndOther)
                if (string.Equals(d, trimmed, StringComparison.OrdinalIgnoreCase)) return d;
            throw new ArgumentException($"unknown direction '{direction}' (Up, Down, Left, Right, Forward, Back, Other)");
        }

        /// <summary>Indices of the faces whose normal points along <paramref name="direction"/> (local space).</summary>
        public static List<int> Facing(ProBuilderMesh mesh, string direction)
        {
            var d = ParseDirection(direction);
            var faces = mesh.faces;
            var result = new List<int>();
            for (int i = 0; i < faces.Count; i++)
                if (Direction(Normal(mesh, faces[i])) == d) result.Add(i);
            return result;
        }

        /// <summary>Local bounds of the vertex positions.</summary>
        public static Bounds LocalBounds(ProBuilderMesh mesh)
        {
            var positions = mesh.positions;
            if (positions.Count == 0) return new Bounds();
            var bounds = new Bounds(positions[0], Vector3.zero);
            for (int i = 1; i < positions.Count; i++) bounds.Encapsulate(positions[i]);
            return bounds;
        }

        public static MeshRenderer Renderer(ProBuilderMesh mesh)
        {
            var renderer = mesh.GetComponent<MeshRenderer>();
            return renderer != null ? renderer : throw new InvalidOperationException($"'{mesh.name}' has no MeshRenderer");
        }
    }
}
