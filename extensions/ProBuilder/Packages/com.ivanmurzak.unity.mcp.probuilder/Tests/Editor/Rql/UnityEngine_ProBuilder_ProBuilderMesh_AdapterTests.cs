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
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.ProBuilder
{
    using Edge = UnityEngine.ProBuilder.Edge;

    public class UnityEngine_ProBuilder_ProBuilderMesh_AdapterTests : ProBuilderTestBase
    {
        private int Up => Facing(Cube, Vector3.up).Single();
        private int Down => Facing(Cube, Vector3.down).Single();

        private string EdgesOf(int face) =>
            "[" + string.Join(", ", Cube.faces[face].edges.Select(e => $"[{e.a}, {e.b}]")) + "]";

        // ------------------------------------------------------------ members

        [Test]
        public void SummaryMembersReadTheMesh()
        {
            var r = Ok(Q(M + " | select faceCount vertexCount edgeCount triangleCount bounds.size"));
            Assert.AreEqual("6", Cell(r, 0));
            Assert.AreEqual(Cube.vertexCount.ToString(), Cell(r, 1));
            Assert.AreEqual(Cube.edgeCount.ToString(), Cell(r, 2));
            Assert.AreEqual("12", Cell(r, 3));
            StringAssert.Contains("1", Cell(r, 4));
        }

        [Test]
        public void DirectionsGroupTheFacesByLocalAxis()
        {
            var r = Ok(Q(M + " | select up=(directions[\"Up\"]) down=(directions[\"Down\"])"));
            Assert.AreEqual($"[{Up}]", Cell(r, 0));
            Assert.AreEqual($"[{Down}]", Cell(r, 1));
        }

        [Test]
        public void RawGeometryArraysAreNotMembers()
        {
            var r = Q(M + " | select positions");
            Assert.IsTrue(r.Rows == null || r.Rows.Count == 0 || r.Rows[0][0] == null, Table(r));
            var whole = Ok(Q(M));
            var table = Table(whole);
            StringAssert.DoesNotContain("m_Positions", table);
            StringAssert.DoesNotContain("sharedVertices", table);
            Fails(M + " | set faceCount = 2", "faceCount");
        }

        [Test]
        public void ReadingTheMeshWholeDoesNotFail()
        {
            Ok(Q(M));
            Ok(Q(M + " | get faces"));
            Ok(Q(M + " | get materials"));
            Ok(Q("objects | get components | where $ is ProBuilderMesh | select $ref faceCount"));
        }

        // ------------------------------------------------------------ Extrude

        [Test]
        public void ExtrudeAddsSideFacesAndMovesTheFace()
        {
            var r = Ok(M + $" | call Extrude({Ints(Up)}, 1)");
            Assert.AreEqual(10, Cube.faceCount);
            StringAssert.Contains("[", Result(r));
            Assert.AreEqual(1.5f, Cube.positions.Max(p => p.y), 1e-4f);
            Assert.AreEqual(Cube.vertexCount, SharedVertexCount(Cube), "the Unity mesh is rebuilt");
        }

        [Test]
        public void ExtrudeIndividualFacesTakesTheMethodName()
        {
            Ok(M + " | call Extrude([0, 1], 0.25, \"IndividualFaces\")");
            Assert.AreEqual(14, Cube.faceCount);
        }

        [Test]
        public void ExtrudeRejectsBadFacesWithoutChanges()
        {
            Fails(M + " | call Extrude([7], 1)", "does not exist");
            Fails(M + " | call Extrude([], 1)", "empty");
            Fails(M + " | call Extrude([0], 0)", "distance is 0");
            Assert.AreEqual(6, Cube.faceCount);
        }

        [Test]
        public void ExtrudeIsUndoable()
        {
            Ok(M + $" | call Extrude({Ints(Up)}, 1)");
            Assert.AreEqual(10, Cube.faceCount);
            Undo.PerformUndo();
            Assert.AreEqual(6, Cube.faceCount);
            Assert.AreEqual(0.5f, Cube.positions.Max(p => p.y), 1e-4f);
            Assert.AreEqual(24, SharedVertexCount(Cube), "the Unity mesh is rebuilt after Undo");
        }

        // ------------------------------------------------------------ DeleteFaces

        [Test]
        public void DeleteFacesRemovesThem()
        {
            Assert.AreEqual("4", Result(Ok(M + $" | call DeleteFaces({Ints(Up, Down)})")));
            Assert.AreEqual(4, Cube.faceCount);
            Assert.AreEqual(0, Facing(Cube, Vector3.up).Length);
        }

        [Test]
        public void DeleteFacesKeepsAtLeastOneFace()
        {
            Fails(M + " | call DeleteFaces([0, 1, 2, 3, 4, 5])", "at least one face");
            Fails(M + " | call DeleteFaces([-1])", "does not exist");
            Assert.AreEqual(6, Cube.faceCount);
        }

        [Test]
        public void DeleteFacesIsUndoable()
        {
            Ok(M + " | call DeleteFaces([0])");
            Undo.PerformUndo();
            Assert.AreEqual(6, Cube.faceCount);
            Assert.AreEqual(24, SharedVertexCount(Cube));
        }

        // ------------------------------------------------------------ SetFaceMaterial

        [Test]
        public void SetFaceMaterialAddsASlotAndAssignsTheFaces()
        {
            var grass = MaterialAsset("Grass");
            Assert.AreEqual("1", Result(Ok(M + $" | call SetFaceMaterial({Ints(Up)}, \"{Folder}/Grass.mat\")")));
            var renderer = Cube.GetComponent<MeshRenderer>();
            Assert.AreEqual(2, renderer.sharedMaterials.Length);
            Assert.AreEqual(grass, renderer.sharedMaterials[1]);
            Assert.AreEqual(1, Cube.faces[Up].submeshIndex);
            Assert.AreEqual(0, Cube.faces[Down].submeshIndex);
            var r = Ok(Q(M + " | get materials | select index faceCount"));
            Assert.AreEqual("5", Cell(r, 1, 0));
            Assert.AreEqual("1", Cell(r, 1, 1));
        }

        [Test]
        public void SetFaceMaterialTakesAnAssetFunctionOrFails()
        {
            MaterialAsset("Dirt");
            Ok(M + $" | call SetFaceMaterial([0], unity.asset(\"{Folder}/Dirt.mat\"))");
            Assert.AreEqual(1, Cube.faces[0].submeshIndex);
            Fails(M + " | call SetFaceMaterial([0], \"Assets/Nope/Missing.mat\")", "no asset");
            Fails(M + " | call SetFaceMaterial([0], null)", "material");
        }

        [Test]
        public void SetFaceMaterialIsUndoable()
        {
            MaterialAsset("Grass");
            var renderer = Cube.GetComponent<MeshRenderer>();
            var before = renderer.sharedMaterials;
            Ok(M + $" | call SetFaceMaterial({Ints(Up)}, \"{Folder}/Grass.mat\")");
            Undo.PerformUndo();
            CollectionAssert.AreEqual(before, renderer.sharedMaterials);
            Assert.AreEqual(0, Cube.faces[Up].submeshIndex);
        }

        // ------------------------------------------------------------ FlipNormals

        [Test]
        public void FlipNormalsReversesTheFaces()
        {
            var up = Up;
            Assert.AreEqual("1", Result(Ok(M + $" | call FlipNormals({Ints(up)})")));
            CollectionAssert.AreEqual(new[] { up }, Facing(Cube, Vector3.down).Where(i => i == up).ToArray());
            Assert.AreEqual("6", Result(Ok(M + " | call FlipNormals()")));
        }

        [Test]
        public void FlipNormalsIsUndoable()
        {
            var up = Up;
            Ok(M + $" | call FlipNormals({Ints(up)})");
            Undo.PerformUndo();
            CollectionAssert.Contains(Facing(Cube, Vector3.up), up);
        }

        // ------------------------------------------------------------ edges

        [Test]
        public void BevelCreatesFaces()
        {
            var r = Ok(M + $" | call Bevel({EdgesOf(Up)}, 0.1)");
            Assert.Greater(Cube.faceCount, 6);
            StringAssert.Contains("[", Result(r));
        }

        [Test]
        public void BevelRejectsBadInput()
        {
            Fails(M + $" | call Bevel({EdgesOf(Up)}, 1.5)", "between 0 and 1");
            Fails(M + " | call Bevel([[0, 99]], 0.1)", "is not an edge");
            Fails(M + " | call Bevel([[0, 1, 2]], 0.1)", "pair of vertex indices");
            Assert.AreEqual(6, Cube.faceCount);
        }

        [Test]
        public void BevelIsUndoable()
        {
            Ok(M + $" | call Bevel({EdgesOf(Up)}, 0.1)");
            Undo.PerformUndo();
            Assert.AreEqual(6, Cube.faceCount);
            Assert.AreEqual(24, Cube.vertexCount);
        }

        [Test]
        public void SubdivideEdgesAddsVertices()
        {
            var e = Cube.faces[Up].edges[0];
            Assert.AreEqual("2", Result(Ok(M + $" | call SubdivideEdges([[{e.a}, {e.b}]], 1)")));
            Fails(M + $" | call SubdivideEdges([[{e.a}, {e.b}]], 0)", "subdivisions");
        }

        [Test]
        public void SubdivideEdgesIsUndoable()
        {
            var e = Cube.faces[Up].edges[0];
            Ok(M + $" | call SubdivideEdges([[{e.a}, {e.b}]], 2)");
            Assert.Greater(Cube.vertexCount, 24);
            Undo.PerformUndo();
            Assert.AreEqual(24, Cube.vertexCount);
        }

        [Test]
        public void ConnectEdgesSplitsTheFace()
        {
            Ok(M + $" | call ConnectEdges({EdgesOf(Up)})");
            Assert.Greater(Cube.faceCount, 6);
            var e = Cube.faces[0].edges[0];
            Fails(M + $" | call ConnectEdges([[{e.a}, {e.b}]])", "at least 2 edges");
        }

        [Test]
        public void ConnectEdgesIsUndoable()
        {
            Ok(M + $" | call ConnectEdges({EdgesOf(Up)})");
            Undo.PerformUndo();
            Assert.AreEqual(6, Cube.faceCount);
        }

        [Test]
        public void BridgeClosesAHole()
        {
            var up = Up;
            Ok(M + $" | call DeleteFaces({Ints(up)})");
            var open = UnityEngine.ProBuilder.WingedEdge.GetWingedEdges(Cube).Where(w => w.opposite == null).Select(w => w.edge.local).ToList();
            Assert.AreEqual(4, open.Count);
            // Two opposite edges of the hole: no shared corner.
            var a = open[0];
            Edge b = open.Skip(1).First(o => !Shares(Cube, a, o));
            var r = Ok(M + $" | call Bridge([{a.a}, {a.b}], [{b.a}, {b.b}])");
            Assert.AreEqual(6, Cube.faceCount);
            Assert.AreEqual("5", Result(r));
            Fails(M + $" | call Bridge([{a.a}, {a.b}], [{a.a}, {a.b}])", "same edge");
        }

        [Test]
        public void BridgeIsUndoable()
        {
            Ok(M + $" | call DeleteFaces({Ints(Up)})");
            var open = UnityEngine.ProBuilder.WingedEdge.GetWingedEdges(Cube).Where(w => w.opposite == null).Select(w => w.edge.local).ToList();
            Ok(M + $" | call Bridge([{open[0].a}, {open[0].b}], [{open[2].a}, {open[2].b}])");
            Assert.AreEqual(6, Cube.faceCount);
            Undo.PerformUndo();
            Assert.AreEqual(5, Cube.faceCount);
        }

        private static bool Shares(UnityEngine.ProBuilder.ProBuilderMesh mesh, Edge a, Edge b)
        {
            var p = mesh.positions;
            return new[] { p[a.a], p[a.b] }.Any(x => (x - p[b.a]).sqrMagnitude < 1e-8f || (x - p[b.b]).sqrMagnitude < 1e-8f);
        }

        // ------------------------------------------------------------ pivot

        [Test]
        public void SetPivotKeepsTheGeometryInTheWorld()
        {
            Cube.transform.position = new Vector3(5f, 0f, 0f);
            Ok(M + " | call SetPivot(\"FirstVertex\")");
            var first = Cube.transform.TransformPoint(Cube.positions[0]);
            Assert.AreEqual(Cube.transform.position, first);
            Ok(M + " | call SetPivot(\"Center\")");
            Assert.AreEqual(new Vector3(5f, 0f, 0f), Cube.transform.position);
            Assert.AreEqual(Vector3.zero, Cube.positions.Aggregate(Vector3.zero, (s, p) => s + p) / Cube.vertexCount);
            Fails(M + " | call SetPivot(\"Top\")", "unknown pivot location");
        }

        [Test]
        public void SetPivotAtMovesThePivotToAWorldPosition()
        {
            var r = Ok(M + " | call SetPivotAt(\"(0, -0.5, 0)\")");
            Assert.AreEqual(new Vector3(0f, -0.5f, 0f), Cube.transform.position);
            Assert.AreEqual(0f, Cube.positions.Min(p => p.y), 1e-4f);
            StringAssert.Contains("-0.5", Result(r));
        }

        [Test]
        public void SetPivotIsUndoable()
        {
            Ok(M + " | call SetPivotAt(\"(1, 2, 3)\")");
            Undo.PerformUndo();
            Assert.AreEqual(Vector3.zero, Cube.transform.position);
            Assert.AreEqual(-0.5f, Cube.positions.Min(p => p.y), 1e-4f);
        }
    }
}
