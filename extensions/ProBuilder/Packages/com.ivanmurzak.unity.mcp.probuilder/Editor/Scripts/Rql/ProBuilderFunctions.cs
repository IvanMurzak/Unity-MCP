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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ProBuilder
{
    using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;

    /// <summary>
    /// Read-only selection helpers as expression functions, usable in rql-query and as call arguments, e.g.
    /// <c>… | call Extrude(probuilder.facing($, "Up"), 1)</c> or <c>… | call Bevel(probuilder.edges($, [4]), 0.1)</c>.
    /// The first argument is a ProBuilderMesh or its GameObject.
    /// </summary>
    internal static class ProBuilderFunctions
    {
        /// <summary>RQL encodes at most 20 items per list.</summary>
        public const int MaxVertices = 20;

        public const string FacingDescription =
            "probuilder.facing(mesh, direction): indices of the faces pointing Up, Down, Left, Right, Forward, Back or Other (local space, within 45°)";
        public const string EdgesDescription =
            "probuilder.edges(mesh, faces?): distinct edges [[a, b], …] of the faces (index list; default every face), for Bevel / SubdivideEdges / ConnectEdges";
        public const string OpenEdgesDescription =
            "probuilder.openEdges(mesh): border edges [[a, b], …] (a face on one side only: around holes, plane borders), for Bridge";
        public const string VerticesDescription =
            "probuilder.vertices(mesh, start?, count?): [{index, position}] local vertex positions (at most 20 per call; default 0, 20)";

        public static object? Facing(object?[] args)
        {
            if (args.Length != 2 || !(args[1] is string direction)) return Mismatch("probuilder.facing(mesh, direction)");
            if (!(MeshOf(args[0]) is ProBuilderMesh mesh)) return NotAMesh("probuilder.facing", args[0]);
            try { return ProBuilderOps.Facing(mesh, direction).Select(i => (object?)(long)i).ToList(); }
            catch (ArgumentException ex) { return new RqlError("type-mismatch", ex.Message); }
        }

        public static object? Edges(object?[] args)
        {
            if (args.Length < 1 || args.Length > 2) return Mismatch("probuilder.edges(mesh, faces?)");
            if (!(MeshOf(args[0]) is ProBuilderMesh mesh)) return NotAMesh("probuilder.edges", args[0]);
            var faces = mesh.faces;
            List<UnityEngine.ProBuilder.Face>? selected = null;
            if (args.Length == 2 && args[1] != null)
            {
                if (!(args[1] is IEnumerable list) || args[1] is string) return Mismatch("probuilder.edges(mesh, faces?): faces is a list of face indices");
                selected = new List<UnityEngine.ProBuilder.Face>();
                foreach (var item in list)
                {
                    if (!Int(item, out var i)) return Mismatch("probuilder.edges(mesh, faces?): faces is a list of face indices");
                    if (i < 0 || i >= faces.Count) return new RqlError("type-mismatch", $"probuilder.edges: face {i} does not exist (0 … {faces.Count - 1})");
                    selected.Add(faces[i]);
                }
            }
            return Pairs(ProBuilderOps.EdgesOf(mesh, selected));
        }

        public static object? OpenEdges(object?[] args)
        {
            if (args.Length != 1) return Mismatch("probuilder.openEdges(mesh)");
            if (!(MeshOf(args[0]) is ProBuilderMesh mesh)) return NotAMesh("probuilder.openEdges", args[0]);
            return Pairs(ProBuilderOps.OpenEdges(mesh));
        }

        private static List<object?> Pairs(IEnumerable<UnityEngine.ProBuilder.Edge> edges) =>
            edges.Select(e => (object?)new List<object?> { (long)e.a, (long)e.b }).ToList();

        public static object? Vertices(object?[] args)
        {
            if (args.Length < 1 || args.Length > 3) return Mismatch("probuilder.vertices(mesh, start?, count?)");
            if (!(MeshOf(args[0]) is ProBuilderMesh mesh)) return NotAMesh("probuilder.vertices", args[0]);
            int start = 0, count = MaxVertices;
            if (args.Length > 1 && !Int(args[1], out start)) return Mismatch("probuilder.vertices(mesh, start?, count?): integers");
            if (args.Length > 2 && !Int(args[2], out count)) return Mismatch("probuilder.vertices(mesh, start?, count?): integers");
            var positions = mesh.positions;
            if (count < 1 || count > MaxVertices)
                return new RqlError("type-mismatch", $"probuilder.vertices reads 1 … {MaxVertices} vertices per call (got {count}); page with start");
            if (start < 0 || start >= positions.Count)
                return new RqlError("type-mismatch", $"probuilder.vertices: start {start} is outside 0 … {positions.Count - 1}");
            var rows = new List<object?>();
            for (int i = start; i < Math.Min(positions.Count, start + count); i++)
                rows.Add(new Dictionary<string, object?> { ["index"] = (long)i, ["position"] = positions[i] });
            return rows;
        }

        // Unity's fake-null objects: compare with == null, never ?. / ??.
        private static ProBuilderMesh? MeshOf(object? value)
        {
            var mesh = value is GameObject go && go != null ? go.GetComponent<ProBuilderMesh>() : value as ProBuilderMesh;
            return mesh != null ? mesh : null;
        }

        private static bool Int(object? value, out int i)
        {
            var ok = Number(value, out var d) && Math.Floor(d) == d;
            i = ok ? (int)d : 0;
            return ok;
        }

        private static bool Number(object? value, out double d)
        {
            switch (value)
            {
                case double x: d = x; return true;
                case long l: d = l; return true;
                case int i: d = i; return true;
                case float f: d = f; return true;
                default: d = 0; return false;
            }
        }

        private static RqlError Mismatch(string signature) => new RqlError("type-mismatch", signature);

        private static RqlError NotAMesh(string fn, object? value) =>
            new RqlError("type-mismatch", $"{fn}: the first argument must be a ProBuilderMesh or its GameObject, got {(value == null ? "null" : value.GetType().Name)}");
    }
}
