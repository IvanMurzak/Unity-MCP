# Changelog

## [1.0.0] - 2025-12-09

### Added
- Initial release of **AI ProBuilder** (`com.ivanmurzak.unity.mcp.probuilder`).
- Integration with Model Context Protocol (MCP) to enable AI agents to interact with ProBuilder.
- **Shape Creation**:
  - `CreateShape`: Generate standard ProBuilder primitives.
  - `PolyShape`: Create custom polygonal shapes.
- **Mesh Editing**:
  - `Extrude`: Extrude selected faces.
  - `Bevel`: Bevel edges for rounded corners.
  - `Bridge`: Create geometry between edges or faces.
  - `ConnectEdges`: Insert new edges between selected ones.
  - `SubdivideEdges`: Divide edges to increase geometry density.
  - `DeleteFaces`: Remove faces from the mesh.
  - `FlipNormals`: Invert the direction of face normals.
  - `MergeObjects`: Combine multiple ProBuilder objects into one.
  - `SetPivot`: Adjust the pivot point of the mesh.
- **Material & Info**:
  - `SetFaceMaterial`: Assign materials to specific faces.
  - `GetMeshInfo`: Retrieve detailed mesh data for AI analysis.
