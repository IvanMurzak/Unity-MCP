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
using Rql.Execution;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ProBuilder
{
    using ExtrudeMethod = UnityEngine.ProBuilder.ExtrudeMethod;
    using Face = UnityEngine.ProBuilder.Face;
    using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;

    /// <summary>
    /// ProBuilderMesh: the raw geometry arrays (positions, sharedVertices, UVs, private serialized fields) are hidden — they are
    /// large and writing them bypasses the mesh rebuild; the mesh reads as counts, bounds, a face direction summary, material
    /// slots and a read-only <c>faces</c> view. Editing operations are methods that take face indices ([0, 1]) or edges as vertex
    /// index pairs ([[0, 1]]), validate them, edit the mesh and rebuild it. The engine records the mesh for Undo before the call.
    /// </summary>
    public sealed class UnityEngine_ProBuilder_ProBuilderMesh_Adapter : RqlTypeAdapter<ProBuilderMesh>
    {
        private static readonly string[] Kept = { "faceCount", "vertexCount", "edgeCount", "triangleCount" };

        public override string Description =>
            "ProBuilderMesh: faceCount vertexCount edgeCount triangleCount bounds worldBounds directions materials faces (views); call Extrude, " +
            "DeleteFaces, SetFaceMaterial, FlipNormals, Bevel, Bridge, ConnectEdges, SubdivideEdges, SetPivot, SetPivotAt";

        private static CallableParameter P<TP>(string name) => new CallableParameter(name, typeof(TP));
        private static CallableParameter P<TP>(string name, TP fallback) => new CallableParameter(name, typeof(TP), true, fallback);

        private static void Add(MemberTable<ProBuilderMesh> m, string name, CallableParameter[] ps, Type returns, Func<ProBuilderMesh, object?[], object?> body, string description) =>
            m.Untyped.AddMethod(name, ps, returns, (o, a) => body((ProBuilderMesh)o, a), description);

        protected override void Members(MemberTable<ProBuilderMesh> m)
        {
            m.HideWhere(a => a.Member != null && a.Member.DeclaringType == typeof(ProBuilderMesh) && !Kept.Contains(a.Name))
             .Add("bounds", p => ProBuilderOps.LocalBounds(p), description: "local-space bounds of the vertices (center, size, min, max)")
             .Add("worldBounds", p => { var r = p.GetComponent<MeshRenderer>(); return r != null ? r.bounds : new Bounds(); },
                    description: "world-space bounds of the rendered mesh")
             .Add("directions", p => Directions(p),
                    description: "face indices by local direction, e.g. {Up: [4], Down: [5], …, Other: [...]} (only directions that have faces)")
             .Add("materials", p => ProBuilderMaterialView.All(p), description: "material slots (views: index material faceCount)")
             .Add("faces", p => ProBuilderFaceView.All(p),
                    description: "faces (read-only views: index vertexCount center normal direction submeshIndex material vertices edges)");

            Add(m, "Extrude", new[] { P<int[]>("faces"), P("distance", 0.5f), P("method", ExtrudeMethod.FaceNormal) }, typeof(int[]),
                (p, a) => Extrude(p, (int[])a[0]!, (float)a[1]!, (ExtrudeMethod)a[2]!),
                "extrude faces along their normals by distance (negative = inward); method FaceNormal (as a group), IndividualFaces " +
                "or VertexNormal; returns the indices of the new side faces");
            Add(m, "DeleteFaces", new[] { P<int[]>("faces") }, typeof(int), (p, a) => DeleteFaces(p, (int[])a[0]!),
                "delete faces (at least one face must remain); face indices shift afterwards; returns the remaining face count");
            Add(m, "SetFaceMaterial", new[] { P<int[]>("faces"), P<Material>("material") }, typeof(int),
                (p, a) => SetFaceMaterial(p, (int[])a[0]!, (Material)a[1]!),
                "render faces with a material (\"Assets/…/X.mat\", unity.asset(…) or a ref); adds a material slot when needed; returns the submesh index");
            Add(m, "FlipNormals", new[] { P<int[]?>("faces", null) }, typeof(int), (p, a) => FlipNormals(p, (int[]?)a[0]),
                "reverse the winding of faces (all faces when omitted); returns the number flipped");
            Add(m, "Bevel", new[] { P<int[][]>("edges"), P("amount", 0.1f) }, typeof(int[]),
                (p, a) => Bevel(p, (int[][])a[0]!, (float)a[1]!),
                "bevel edges ([[a, b], …] vertex pairs) by amount, 0 < amount < 1 (fraction towards the face centers); returns the new face indices");
            Add(m, "Bridge", new[] { P<int[]>("edgeA"), P<int[]>("edgeB"), P("allowNonManifold", false) }, typeof(int),
                (p, a) => Bridge(p, (int[])a[0]!, (int[])a[1]!, (bool)a[2]!),
                "create a face between two open edges ([a, b] vertex pairs, e.g. the borders of deleted faces); returns the new face index");
            Add(m, "ConnectEdges", new[] { P<int[][]>("edges") }, typeof(int[]), (p, a) => ConnectEdges(p, (int[][])a[0]!),
                "insert edges connecting the midpoints of at least 2 edges that share faces; returns the indices of the new faces");
            Add(m, "SubdivideEdges", new[] { P<int[][]>("edges"), P("subdivisions", 1) }, typeof(int),
                (p, a) => SubdivideEdges(p, (int[][])a[0]!, (int)a[1]!),
                "insert subdivisions vertices into each edge (1 = split in half); returns the number of vertices added");
            Add(m, "SetPivot", new[] { P("location", "Center") }, typeof(Vector3), (p, a) => SetPivot(p, (string)a[0]!),
                "move the pivot to the bounds \"Center\" or the \"FirstVertex\" without moving the geometry in the world; returns the new world pivot");
            Add(m, "SetPivotAt", new[] { P<Vector3>("position") }, typeof(Vector3), (p, a) => MovePivot(p, (Vector3)a[0]!),
                "move the pivot to a world position \"(x, y, z)\" without moving the geometry in the world; returns it");
        }

        private static Dictionary<string, List<int>> Directions(ProBuilderMesh mesh)
        {
            var result = new Dictionary<string, List<int>>();
            var faces = mesh.faces;
            for (int i = 0; i < faces.Count; i++)
            {
                var d = ProBuilderOps.Direction(ProBuilderOps.Normal(mesh, faces[i]));
                if (!result.TryGetValue(d, out var list)) result[d] = list = new List<int>();
                list.Add(i);
            }
            // Fixed order: the axes first, then Other.
            return ProBuilderOps.DirectionsAndOther.Where(result.ContainsKey).ToDictionary(d => d, d => result[d]);
        }

        // ------------------------------------------------------------ faces

        private static int[] Extrude(ProBuilderMesh mesh, int[] indices, float distance, ExtrudeMethod method)
        {
            var faces = ProBuilderOps.Faces(mesh, indices);
            if (distance == 0f) throw new ArgumentException("distance is 0: nothing to extrude");
            var created = mesh.Extrude(faces, method, distance);
            if (created == null || created.Length == 0) throw new InvalidOperationException("ProBuilder created no faces: the faces cannot be extruded this way");
            ProBuilderOps.Rebuild(mesh);
            return ProBuilderOps.IndicesOf(mesh, created);
        }

        private static int DeleteFaces(ProBuilderMesh mesh, int[] indices)
        {
            var faces = ProBuilderOps.Faces(mesh, indices);
            if (faces.Length >= mesh.faceCount) throw new ArgumentException($"cannot delete all {mesh.faceCount} faces: at least one face must remain (Destroy the GameObject instead)");
            mesh.DeleteFaces(faces);
            ProBuilderOps.Rebuild(mesh);
            return mesh.faceCount;
        }

        private static int SetFaceMaterial(ProBuilderMesh mesh, int[] indices, Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material), "material is null: pass \"Assets/…/X.mat\", unity.asset(…) or a ref");
            var faces = ProBuilderOps.Faces(mesh, indices);
            var renderer = ProBuilderOps.Renderer(mesh);
            Undo.RecordObject(renderer, "RQL ProBuilder set face material");
            mesh.SetMaterial(faces, material);
            ProBuilderOps.Rebuild(mesh);
            EditorUtility.SetDirty(renderer);
            return faces[0].submeshIndex;
        }

        private static int FlipNormals(ProBuilderMesh mesh, int[]? indices)
        {
            var faces = indices == null ? mesh.faces.ToArray() : ProBuilderOps.Faces(mesh, indices);
            if (faces.Length == 0) throw new InvalidOperationException($"'{mesh.name}' has no faces");
            foreach (var face in faces) face.Reverse();
            ProBuilderOps.Rebuild(mesh);
            return faces.Length;
        }

        // ------------------------------------------------------------ edges

        private static int[] Bevel(ProBuilderMesh mesh, int[][] pairs, float amount)
        {
            if (!(amount > 0f && amount < 1f)) throw new ArgumentException($"amount {amount} must be between 0 and 1 (exclusive); 0.05 … 0.2 are typical");
            var edges = ProBuilderOps.Edges(mesh, pairs);
            var created = UnityEngine.ProBuilder.MeshOperations.Bevel.BevelEdges(mesh, edges, amount);
            if (created == null || created.Count == 0) throw new InvalidOperationException("ProBuilder created no faces: the edges cannot be beveled (already beveled to the maximum?)");
            ProBuilderOps.Rebuild(mesh);
            return ProBuilderOps.IndicesOf(mesh, created);
        }

        private static int Bridge(ProBuilderMesh mesh, int[] edgeA, int[] edgeB, bool allowNonManifold)
        {
            var edges = ProBuilderOps.Edges(mesh, new[] { edgeA, edgeB });
            if (edges.Count != 2) throw new ArgumentException("edgeA and edgeB are the same edge");
            var face = mesh.Bridge(edges[0], edges[1], allowNonManifold);
            if (face == null)
                throw new InvalidOperationException("ProBuilder could not bridge the edges: both must be open (border) edges, unless allowNonManifold is true");
            ProBuilderOps.Rebuild(mesh);
            var index = ProBuilderOps.IndicesOf(mesh, new[] { face });
            return index.Length > 0 ? index[0] : -1;
        }

        private static int[] ConnectEdges(ProBuilderMesh mesh, int[][] pairs)
        {
            var edges = ProBuilderOps.Edges(mesh, pairs);
            if (edges.Count < 2) throw new ArgumentException("ConnectEdges needs at least 2 edges");
            var result = mesh.Connect(edges);
            var created = result.item1 ?? new Face[0];
            if (created.Length == 0 && (result.item2 == null || result.item2.Length == 0))
                throw new InvalidOperationException("ProBuilder created no geometry: the edges must share a face to be connected");
            ProBuilderOps.Rebuild(mesh);
            return ProBuilderOps.IndicesOf(mesh, created);
        }

        private static int SubdivideEdges(ProBuilderMesh mesh, int[][] pairs, int subdivisions)
        {
            if (subdivisions < 1 || subdivisions > 64) throw new ArgumentException($"subdivisions {subdivisions} must be 1 … 64");
            var edges = ProBuilderOps.Edges(mesh, pairs);
            int before = mesh.vertexCount;
            var created = mesh.AppendVerticesToEdge(edges, subdivisions);
            if (created == null || created.Count == 0) throw new InvalidOperationException("ProBuilder created no edges: the edges cannot be subdivided");
            ProBuilderOps.Rebuild(mesh);
            return mesh.vertexCount - before;
        }

        // ------------------------------------------------------------ pivot

        private static Vector3 SetPivot(ProBuilderMesh mesh, string location)
        {
            Vector3 local;
            if (string.Equals(location, "Center", StringComparison.OrdinalIgnoreCase)) local = ProBuilderOps.LocalBounds(mesh).center;
            else if (string.Equals(location, "FirstVertex", StringComparison.OrdinalIgnoreCase))
            {
                if (mesh.vertexCount == 0) throw new InvalidOperationException($"'{mesh.name}' has no vertices");
                local = mesh.positions[0];
            }
            else throw new ArgumentException($"unknown pivot location '{location}' (Center, FirstVertex; SetPivotAt(\"(x, y, z)\") for a world position)");
            return MovePivot(mesh, mesh.transform.TransformPoint(local));
        }

        private static Vector3 MovePivot(ProBuilderMesh mesh, Vector3 world)
        {
            var transform = mesh.transform;
            var offset = transform.InverseTransformPoint(world);
            Undo.RecordObject(transform, "RQL ProBuilder set pivot");
            mesh.positions = mesh.positions.Select(p => p - offset).ToArray();
            transform.position = world;
            ProBuilderOps.Rebuild(mesh);
            EditorUtility.SetDirty(transform);
            return world;
        }
    }
}
