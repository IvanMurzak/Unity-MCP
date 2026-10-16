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
using Rql;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ProBuilder
{
    using Face = UnityEngine.ProBuilder.Face;
    using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;

    /// <summary>
    /// One face of a ProBuilderMesh (mesh.faces[i]) as a read-only summary: ProBuilder's Face objects carry raw index arrays and
    /// writing them would leave the mesh inconsistent, so faces are changed through the mesh's methods (Extrude, DeleteFaces,
    /// SetFaceMaterial …) with the index shown here.
    /// </summary>
    [RqlDescription("ProBuilder face (read-only): index vertexCount triangleCount center normal direction (local space) submeshIndex " +
                    "material smoothingGroup vertices edges; change faces with the mesh methods using index")]
    public sealed class ProBuilderFaceView
    {
        internal ProBuilderFaceView(ProBuilderMesh mesh, int index)
        {
            this.mesh = mesh;
            this.index = index;
        }

        internal static List<ProBuilderFaceView> All(ProBuilderMesh mesh) =>
            Enumerable.Range(0, mesh.faces.Count).Select(i => new ProBuilderFaceView(mesh, i)).ToList();

        [RqlReference]
        [RqlDescription("the ProBuilderMesh that owns the face")]
        public ProBuilderMesh mesh { get; }

        [RqlDescription("index of the face: what the mesh methods take, e.g. call Extrude([index], 1)")]
        public int index { get; }

        private Face Face
        {
            get
            {
                var faces = mesh.faces;
                if (index < 0 || index >= faces.Count)
                    throw new ArgumentOutOfRangeException(nameof(index), $"face {index} no longer exists: '{mesh.name}' has {faces.Count} face(s)");
                return faces[index];
            }
        }

        [RqlDescription("distinct vertices of the face")]
        public int vertexCount => Face.distinctIndexes.Count;

        public int triangleCount => Face.indexes.Count / 3;

        [RqlDescription("center of the face in the mesh's local space")]
        public Vector3 center => ProBuilderOps.Center(mesh, Face);

        [RqlDescription("face normal in the mesh's local space")]
        public Vector3 normal => ProBuilderOps.Normal(mesh, Face);

        [RqlDescription("local axis the face points along: Up, Down, Left, Right, Forward, Back or Other (within 45°)")]
        public string direction => ProBuilderOps.Direction(normal);

        [RqlDescription("index into the MeshRenderer's sharedMaterials")]
        public int submeshIndex => Face.submeshIndex;

        [RqlReference]
        [RqlDescription("the material the face renders with (sharedMaterials[submeshIndex])")]
        public Material? material
        {
            get
            {
                var renderer = mesh.GetComponent<MeshRenderer>();
                if (renderer == null) return null;
                var materials = renderer.sharedMaterials;
                return materials.Length == 0 ? null : materials[Mathf.Clamp(Face.submeshIndex, 0, materials.Length - 1)];
            }
        }

        [RqlDescription("smoothing group (0 = hard edges)")]
        public int smoothingGroup => Face.smoothingGroup;

        [RqlDescription("distinct vertex indices of the face")]
        public int[] vertices => Face.distinctIndexes.ToArray();

        [RqlDescription("edges as [a, b] vertex index pairs: what Bevel / SubdivideEdges / ConnectEdges / Bridge take")]
        public int[][] edges => ProBuilderOps.EdgeList(Face.edges);

        public override string ToString() => $"face {index} of {mesh.name}";
    }
}
