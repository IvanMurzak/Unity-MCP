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
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ProBuilder
{
    using BuiltinMaterials = UnityEngine.ProBuilder.BuiltinMaterials;
    using PivotLocation = UnityEngine.ProBuilder.PivotLocation;
    using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;
    using RqlUnity = global::com.IvanMurzak.Unity.MCP.Editor.Rql.RqlUnity;
    using ShapeGenerator = UnityEngine.ProBuilder.ShapeGenerator;
    using ShapeType = UnityEngine.ProBuilder.ShapeType;

    /// <summary>
    /// The <c>probuilder</c> source: every ProBuilderMesh of the loaded scenes and the operations without an owner mesh (creating
    /// shapes, merging meshes), e.g. <c>probuilder | call CreateShape("Cube", "Crate", "(2, 1, 2)")</c>. Operations on one mesh are
    /// methods of the ProBuilderMesh component (<see cref="UnityEngine_ProBuilder_ProBuilderMesh_Adapter"/>).
    /// </summary>
    public sealed class ProBuilderEditorApi
    {
        public static readonly ProBuilderEditorApi Instance = new ProBuilderEditorApi();

        private static string ShapeTypes => string.Join(", ", Enum.GetNames(typeof(ShapeType)));

        [RqlDescription("every ProBuilderMesh in the loaded scenes, inactive ones included")]
        public List<ProBuilderMesh> meshes
        {
            get
            {
                // One native call per root (depth-first, inactive included) instead of one GetComponent per GameObject.
                var result = new List<ProBuilderMesh>();
                foreach (var root in RqlUnity.RootGameObjects()) result.AddRange(root.GetComponentsInChildren<ProBuilderMesh>(true));
                return result;
            }
        }

        [RqlCallable("create an editable ProBuilder shape: type Cube, Stair, CurvedStair, Prism, Cylinder, Plane, Door, Pipe, Cone, Sprite, Arch, " +
                     "Sphere, Torus; size (x, y, z) rescales the generated geometry; position / rotation (euler) are local to parent (world without one); " +
                     "returns the ProBuilderMesh (undoable)")]
        public ProBuilderMesh CreateShape(string type = "Cube", string? name = null, Vector3? size = null, Vector3? position = null,
            Vector3? rotation = null, GameObject? parent = null)
        {
            if (!Enum.TryParse<ShapeType>(type, ignoreCase: true, out var shape) || !Enum.IsDefined(typeof(ShapeType), shape))
                throw new ArgumentException($"unknown shape type '{type}' ({ShapeTypes})");
            if (size.HasValue && (size.Value.x <= 0f || size.Value.y <= 0f || size.Value.z <= 0f))
                throw new ArgumentException($"size must be positive, got ({size.Value.x}, {size.Value.y}, {size.Value.z})");

            var mesh = ShapeGenerator.CreateShape(shape, PivotLocation.Center);
            if (mesh == null) throw new InvalidOperationException($"ProBuilder could not create a {shape}");
            var go = mesh.gameObject;
            go.name = string.IsNullOrWhiteSpace(name) ? shape.ToString() : name!;

            if (size.HasValue)
            {
                var current = ProBuilderOps.LocalBounds(mesh).size;
                var scale = new Vector3(
                    current.x > 0f ? size.Value.x / current.x : 1f,
                    current.y > 0f ? size.Value.y / current.y : 1f,
                    current.z > 0f ? size.Value.z / current.z : 1f);
                mesh.positions = mesh.positions.Select(p => Vector3.Scale(p, scale)).ToArray();
            }
            Place(go, parent, position, rotation);
            ProBuilderOps.Rebuild(mesh);
            Undo.RegisterCreatedObjectUndo(go, "RQL ProBuilder create " + go.name);
            return mesh;
        }

        [RqlCallable("create a ProBuilder mesh by extruding a polygon: points are (x, z) outline corners in order, e.g. " +
                     "[\"(0, 0)\", \"(4, 0)\", \"(4, 3)\", \"(0, 3)\"] (at least 3), height is the extrusion upwards; returns the ProBuilderMesh (undoable)")]
        public ProBuilderMesh CreatePolyShape(Vector2[] points, float height = 1f, string? name = null, Vector3? position = null,
            bool flipNormals = false, GameObject? parent = null)
        {
            if (points == null || points.Length < 3) throw new ArgumentException($"a polygon needs at least 3 points, got {points?.Length ?? 0}");
            if (height == 0f) throw new ArgumentException("height is 0: the shape would be flat");

            var go = new GameObject(string.IsNullOrWhiteSpace(name) ? "PolyShape" : name);
            try
            {
                var mesh = go.AddComponent<ProBuilderMesh>();
                var result = mesh.CreateShapeFromPolygon(points.Select(p => new Vector3(p.x, 0f, p.y)).ToList(), height, flipNormals);
                if (result.status != UnityEngine.ProBuilder.ActionResult.Status.Success)
                    throw new InvalidOperationException("ProBuilder could not build the polygon: " + result.notification);
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial == null) renderer.sharedMaterial = BuiltinMaterials.defaultMaterial;
                Place(go, parent, position, null);
                ProBuilderOps.Rebuild(mesh);
                Undo.RegisterCreatedObjectUndo(go, "RQL ProBuilder create " + go.name);
                return mesh;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(go);
                throw;
            }
        }

        [RqlCallable("merge ProBuilder meshes (refs to the meshes or their GameObjects, at least 2) into the first one; deleteSources removes " +
                     "the other GameObjects; returns the merged ProBuilderMesh (undoable)")]
        public ProBuilderMesh Merge(ProBuilderMesh[] meshes, bool deleteSources = true)
        {
            if (meshes == null || meshes.Length < 2) throw new ArgumentException($"Merge needs at least 2 meshes, got {meshes?.Length ?? 0}");
            if (meshes.Any(m => m == null)) throw new ArgumentException("a mesh in the list is null or destroyed");
            if (meshes.Distinct().Count() != meshes.Length) throw new ArgumentException("a mesh appears twice in the list");
            var target = meshes[0];
            var sources = meshes.Skip(1).ToList();
            if (deleteSources)
                foreach (var source in sources)
                    if (target.transform.IsChildOf(source.transform))
                        throw new ArgumentException($"the target '{target.name}' is under '{source.name}', which deleteSources would destroy");

            Undo.RecordObject(target, "RQL ProBuilder merge");
            Undo.RecordObject(ProBuilderOps.Renderer(target), "RQL ProBuilder merge");
            var existing = new HashSet<ProBuilderMesh>(meshes);
            var results = CombineMeshes.Combine(meshes, target);
            if (results == null || results.Count == 0) throw new InvalidOperationException("ProBuilder returned no merged mesh");
            foreach (var mesh in results)
            {
                ProBuilderOps.Rebuild(mesh);
                // Above 65535 vertices ProBuilder splits the result into extra GameObjects.
                if (!existing.Contains(mesh)) Undo.RegisterCreatedObjectUndo(mesh.gameObject, "RQL ProBuilder merge");
            }
            if (deleteSources)
                foreach (var source in sources)
                    if (source != null) // a source under an earlier one is already destroyed with it
                        Undo.DestroyObjectImmediate(source.gameObject);
            return target;
        }

        private static void Place(GameObject go, GameObject? parent, Vector3? position, Vector3? rotation)
        {
            if (parent != null) go.transform.SetParent(parent.transform, false);
            if (position.HasValue) go.transform.localPosition = position.Value;
            if (rotation.HasValue) go.transform.localEulerAngles = rotation.Value;
        }
    }
}
