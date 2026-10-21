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
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.ProBuilder
{
    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): the <c>probuilder</c> source
    /// (meshes, CreateShape, CreatePolyShape, Merge), the ProBuilderMesh adapter (summary members, faces / materials views and the
    /// editing methods), selection functions and the agent guide rendered into the unity-rql skill.
    /// </summary>
    public sealed class ProBuilderModule : IRqlModule
    {
        public const string GuideTopic = "ProBuilder";

        private const string C = @"probuilder | get meshes | where gameObject.name == ""Crate""";
        private const string B = @"probuilder | get meshes | where gameObject.name == ""Box""";
        private const string K = @"probuilder | get meshes | where gameObject.name == ""Block""";
        private const string F = @"probuilder | get meshes | where gameObject.name == ""Floor""";

        internal const string Guide =
@"ProBuilder meshes are components: `probuilder | get meshes` (= `objects | get components | where $ is ProBuilderMesh`). A mesh
reads as faceCount vertexCount edgeCount triangleCount, `bounds` (local) / `worldBounds`, `directions` (face indices per local
direction), `materials` (slots: index material faceCount) and `faces` (read-only views: index direction center normal
submeshIndex material vertices edges); the raw vertex arrays are not members. Edit with `call` (rql-mutate): faces are index lists
`[0, 1]`, edges vertex index pairs `[[0, 1], [1, 2]]`. `probuilder.facing($, ""Up"")`, `probuilder.edges($, faces?)`,
`probuilder.openEdges($)` and `probuilder.vertices($, start?, count?)` compute selections (also as call arguments). The receipt's
result column holds the created mesh's $ref: `ref(""ProBuilderMesh#…"") | call Extrude([0], 1)`.

Create and inspect:

```
probuilder | call CreateShape(""Cube"", ""Crate"", ""(2, 1, 2)"", ""(0, 0.5, 0)"")   type, name?, size?, position?, rotation?, parent?
probuilder | call CreateShape(""Cube"", ""Box"", null, ""(3, 0.5, 0)"")   types: Cube Stair CurvedStair Prism Cylinder Plane Door Pipe Cone Sprite Arch Sphere Torus
probuilder | call CreateShape(""Cylinder"", ""Block"", ""(1, 2, 1)"", ""(6, 1, 0)"")
probuilder | call CreatePolyShape([""(0, 0)"", ""(4, 0)"", ""(4, 3)"", ""(0, 3)""], 0.5, ""Floor"", ""(-6, 0, 0)"")   points (x, z), height, name?, position?, flipNormals?, parent?
probuilder | get meshes | select $ref gameObject.name faceCount vertexCount edgeCount triangleCount bounds.size
" + C + @" | select directions materials worldBounds.center
" + C + @" | get faces | select index direction center normal vertexCount submeshIndex material.name
" + C + @" | get faces | where direction == ""Up"" | select index center edges
" + C + @" | select up=(probuilder.facing($, ""Up"")) edges=(probuilder.edges($, [0])) v=(probuilder.vertices($, 0, 4))
```

Faces (indices shift after DeleteFaces; Extrude appends the new side faces and returns their indices):

```
editor | call CreateMaterial(""Assets/Materials/Grass.mat"")
" + C + @" | call SetFaceMaterial(probuilder.facing($, ""Up""), ""Assets/Materials/Grass.mat"")   faces, material (path, unity.asset(…) or ref); returns the submesh index
" + C + @" | get materials | select index material.name faceCount
" + C + @" | call Extrude(probuilder.facing($, ""Up""), 0.5)   faces, distance (negative = inward), method? FaceNormal | IndividualFaces | VertexNormal
" + C + @" | call Extrude(probuilder.facing($, ""Forward""), 0.25, ""IndividualFaces"")
" + C + @" | call FlipNormals([0])   faces? (every face when omitted)
" + B + @" | call DeleteFaces(probuilder.facing($, ""Up""))   at least one face must remain
" + B + @" | select open=(probuilder.openEdges($))
" + B + @" | call Bridge(probuilder.openEdges($)[0], probuilder.openEdges($)[2])   two open edges [a, b], allowNonManifold?; returns the new face index
```

Edges, pivot, merge:

```
" + K + @" | call Bevel(probuilder.edges($, probuilder.facing($, ""Down"")), 0.1)   edges, amount 0 … 1 (exclusive); returns the new face indices
" + K + @" | call SubdivideEdges(probuilder.edges($, [0]), 1)   edges, subdivisions (1 = split in half); returns the vertices added
" + K + @" | call ConnectEdges(probuilder.edges($, [1]))   at least 2 edges sharing faces; returns the new face indices
" + F + @" | call SetPivot(""Center"")   ""Center"" (bounds) or ""FirstVertex""; the geometry stays in place
" + F + @" | call SetPivotAt(""(-6, 0, 0)"")   a world position
probuilder | call Merge([meshes.first(gameObject.name == ""Crate""), meshes.first(gameObject.name == ""Box"")])   refs to meshes or GameObjects (first = target), deleteSources? (default true)
```

Directions are in the mesh's local space (normal within 45° of the axis; the rest is Other). Invalid indices, edges that are not
edges of the mesh, amounts outside the range and deleting every face fail without changing anything. Every call is undoable
(the mesh is rebuilt after Undo / Redo); a created material asset stays.";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            ctx.AddSource("probuilder", () => ProBuilderEditorApi.Instance,
                   "ProBuilder: meshes (every ProBuilderMesh in the loaded scenes), CreateShape(type, name, size, position, rotation, parent), " +
                   "CreatePolyShape(points, height, name, position, flipNormals, parent), Merge(meshes, deleteSources)")
               .AddFunction("probuilder.facing", ProBuilderFunctions.Facing, ProBuilderFunctions.FacingDescription)
               .AddFunction("probuilder.edges", ProBuilderFunctions.Edges, ProBuilderFunctions.EdgesDescription)
               .AddFunction("probuilder.openEdges", ProBuilderFunctions.OpenEdges, ProBuilderFunctions.OpenEdgesDescription)
               .AddFunction("probuilder.vertices", ProBuilderFunctions.Vertices, ProBuilderFunctions.VerticesDescription)
               .AddAdapter(new UnityEngine_ProBuilder_ProBuilderMesh_Adapter())
               .AddGuide(GuideTopic, Guide);
        }
    }
}
