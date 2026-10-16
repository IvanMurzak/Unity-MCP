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
using System.Collections.Generic;
using System.Linq;
using Rql;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ProBuilder
{
    using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;

    /// <summary>One material slot of a ProBuilderMesh (renderer.sharedMaterials[i]) with the number of faces using it.</summary>
    [RqlDescription("ProBuilder material slot (read-only): index material faceCount; assign with call SetFaceMaterial(faces, material)")]
    public sealed class ProBuilderMaterialView
    {
        internal ProBuilderMaterialView(int index, Material? material, int faceCount)
        {
            this.index = index;
            this.material = material;
            this.faceCount = faceCount;
        }

        internal static List<ProBuilderMaterialView> All(ProBuilderMesh mesh)
        {
            var renderer = mesh.GetComponent<MeshRenderer>();
            var materials = renderer != null ? renderer.sharedMaterials : new Material[0];
            if (materials.Length == 0) return new List<ProBuilderMaterialView>();
            var counts = new int[materials.Length];
            foreach (var face in mesh.faces) counts[Mathf.Clamp(face.submeshIndex, 0, materials.Length - 1)]++;
            return Enumerable.Range(0, materials.Length).Select(i => new ProBuilderMaterialView(i, materials[i], counts[i])).ToList();
        }

        [RqlDescription("submesh index (face submeshIndex)")]
        public int index { get; }

        [RqlReference]
        public Material? material { get; }

        [RqlDescription("faces rendered with this material")]
        public int faceCount { get; }

        public override string ToString() => $"material {index}: {(material != null ? material.name : "none")}";
    }
}
