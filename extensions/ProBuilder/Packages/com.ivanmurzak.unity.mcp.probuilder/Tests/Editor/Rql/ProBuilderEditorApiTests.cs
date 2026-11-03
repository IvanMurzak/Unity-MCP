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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ProBuilder
{
    using ProBuilderMesh = UnityEngine.ProBuilder.ProBuilderMesh;

    public class ProBuilderEditorApiTests : ProBuilderTestBase
    {
        private static ProBuilderMesh? Find(string name)
        {
            var go = RqlUnity.AllGameObjects().FirstOrDefault(g => g.name == name);
            return go != null ? go.GetComponent<ProBuilderMesh>() : null;
        }

        [Test]
        public void MeshesListsEveryProBuilderMesh()
        {
            CreateCube("Second");
            new GameObject("Plain");
            var r = Ok(Q(Meshes + " | select gameObject.name faceCount"));
            CollectionAssert.AreEquivalent(new[] { "Cube", "Second" }, r.Rows!.Select(row => row[0]!.ToJsonString().Trim('"')).ToArray());
        }

        // ------------------------------------------------------------ CreateShape

        [Test]
        public void CreateShapeMakesAnEditableMesh()
        {
            var r = Ok("probuilder | call CreateShape(\"Cylinder\", \"Pillar\", null, \"(4, 1, 0)\", \"(0, 90, 0)\")");
            var mesh = Find("Pillar")!;
            Assert.IsNotNull(mesh);
            StringAssert.Contains("ProBuilderMesh#", Result(r));
            Assert.AreEqual(new Vector3(4f, 1f, 0f), mesh.transform.position);
            Assert.AreEqual(90f, mesh.transform.eulerAngles.y, 1e-3f);
            Assert.Greater(mesh.faceCount, 6);
            Assert.AreEqual(mesh.vertexCount, SharedVertexCount(mesh));
        }

        [Test]
        public void CreateShapeScalesToSize()
        {
            Ok("probuilder | call CreateShape(\"Cube\", \"Crate\", \"(2, 1, 4)\")");
            var mesh = Find("Crate")!;
            var size = mesh.GetComponent<MeshFilter>().sharedMesh.bounds.size;
            Assert.AreEqual(2f, size.x, 1e-4f);
            Assert.AreEqual(1f, size.y, 1e-4f);
            Assert.AreEqual(4f, size.z, 1e-4f);
        }

        [Test]
        public void CreateShapeUnderAParent()
        {
            var parent = new GameObject("Level");
            parent.transform.position = new Vector3(10f, 0f, 0f);
            Ok("probuilder | call CreateShape(\"Prism\", \"Roof\", null, \"(0, 2, 0)\", null, " + Ref(parent) + ")");
            var mesh = Find("Roof")!;
            Assert.AreEqual(parent.transform, mesh.transform.parent);
            Assert.AreEqual(new Vector3(10f, 2f, 0f), mesh.transform.position);
        }

        [Test]
        public void CreateShapeRejectsBadInput()
        {
            Fails("probuilder | call CreateShape(\"Blob\")", "unknown shape type");
            Fails("probuilder | call CreateShape(\"Cube\", \"X\", \"(0, 1, 1)\")", "size must be positive");
            Assert.IsNull(Find("X"));
        }

        [Test]
        public void CreateShapeIsUndoable()
        {
            Ok("probuilder | call CreateShape(\"Sphere\", \"Ball\")");
            Assert.IsNotNull(Find("Ball"));
            Undo.PerformUndo();
            Assert.IsNull(Find("Ball"));
        }

        // ------------------------------------------------------------ CreatePolyShape

        [Test]
        public void CreatePolyShapeExtrudesTheOutline()
        {
            Ok("probuilder | call CreatePolyShape([\"(0, 0)\", \"(4, 0)\", \"(4, 3)\", \"(0, 3)\"], 2, \"Floor\", \"(-6, 0, 0)\")");
            var mesh = Find("Floor")!;
            Assert.IsNotNull(mesh);
            Assert.AreEqual(6, mesh.faceCount);
            var bounds = mesh.GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.AreEqual(new Vector3(4f, 2f, 3f), bounds.size);
            Assert.AreEqual(-6f, mesh.transform.position.x);
            Assert.IsNotNull(mesh.GetComponent<MeshRenderer>().sharedMaterial);
        }

        [Test]
        public void CreatePolyShapeTakesListPoints()
        {
            Ok("probuilder | call CreatePolyShape([[0, 0], [1, 0], [0, 1]], 1, \"Wedge\")");
            Assert.AreEqual(5, Find("Wedge")!.faceCount);
        }

        [Test]
        public void CreatePolyShapeRejectsBadInput()
        {
            Fails("probuilder | call CreatePolyShape([\"(0, 0)\", \"(1, 0)\"], 1, \"Line\")", "at least 3 points");
            Fails("probuilder | call CreatePolyShape([\"(0, 0)\", \"(1, 0)\", \"(0, 1)\"], 0, \"Flat\")", "height is 0");
            Assert.IsNull(RqlUnity.AllGameObjects().FirstOrDefault(g => g.name == "Line" || g.name == "Flat"));
        }

        [Test]
        public void CreatePolyShapeIsUndoable()
        {
            Ok("probuilder | call CreatePolyShape([\"(0, 0)\", \"(1, 0)\", \"(0, 1)\"], 1, \"Wedge\")");
            Undo.PerformUndo();
            Assert.IsNull(Find("Wedge"));
        }

        // ------------------------------------------------------------ Merge

        private static string Ref(Object o) => "ref(\"" + RqlUnity.Engine.RefOf(o) + "\")";

        [Test]
        public void MergeCombinesIntoTheFirstMesh()
        {
            var other = CreateCube("Other", new Vector3(3f, 0f, 0f));
            var r = Ok($"probuilder | call Merge([{Ref(Cube)}, {Ref(other.gameObject)}])");
            StringAssert.Contains(RqlUnity.Engine.RefOf(Cube), Result(r));
            Assert.AreEqual(12, Cube.faceCount);
            Assert.IsNull(Find("Other"));
            Assert.AreEqual(3.5f, Cube.positions.Max(p => p.x), 1e-4f);
        }

        [Test]
        public void MergeCanKeepTheSources()
        {
            var other = CreateCube("Other", new Vector3(3f, 0f, 0f));
            Ok($"probuilder | call Merge([{Ref(Cube)}, {Ref(other)}], false)");
            Assert.AreEqual(12, Cube.faceCount);
            Assert.IsNotNull(Find("Other"));
            Assert.AreEqual(6, other.faceCount);
        }

        [Test]
        public void MergeDeletesNestedSources()
        {
            var parent = CreateCube("Parent", new Vector3(3f, 0f, 0f));
            var child = CreateCube("Child", new Vector3(6f, 0f, 0f));
            child.transform.SetParent(parent.transform, true);
            Ok($"probuilder | call Merge([{Ref(Cube)}, {Ref(parent)}, {Ref(child)}])");
            Assert.AreEqual(18, Cube.faceCount);
            Assert.IsNull(Find("Parent"));
            Assert.IsNull(Find("Child"));
        }

        [Test]
        public void MergeRejectsBadInput()
        {
            Fails($"probuilder | call Merge([{Ref(Cube)}])", "at least 2 meshes");
            Fails($"probuilder | call Merge([{Ref(Cube)}, {Ref(Cube)}])", "twice");
            var plain = new GameObject("Plain");
            Fails($"probuilder | call Merge([{Ref(Cube)}, {Ref(plain)}])", "ProBuilderMesh");
            Assert.AreEqual(6, Cube.faceCount);
        }

        [Test]
        public void MergeIsUndoable()
        {
            var other = CreateCube("Other", new Vector3(3f, 0f, 0f));
            Ok($"probuilder | call Merge([{Ref(Cube)}, {Ref(other)}])");
            Undo.PerformUndo();
            Assert.AreEqual(6, Cube.faceCount);
            Assert.AreEqual(24, SharedVertexCount(Cube));
            Assert.IsNotNull(Find("Other"));
        }
    }
}
