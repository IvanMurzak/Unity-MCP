/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Kieran Hannigan (https://github.com/KaiStarkk)          │
│  Project: Ivan Murzak (https://github.com/IvanMurzak/Unity-MCP)  │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#if PROBUILDER_ENABLED
#nullable enable
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_ProBuilder
    {
        public static class Error
        {
            public static string ProBuilderMeshNotFound(int instanceID)
                => $"[Error] ProBuilderMesh component not found on GameObject with instanceID '{instanceID}'. " +
                   "Make sure the GameObject has a ProBuilderMesh component attached.";

            public static string ProBuilderMeshNotFoundAtPath(string path)
                => $"[Error] ProBuilderMesh component not found on GameObject at path '{path}'. " +
                   "Make sure the GameObject has a ProBuilderMesh component attached.";

            public static string GameObjectNotFound()
                => "[Error] GameObject not found. Provide a valid reference to an existing GameObject.";

            public static string InvalidFaceIndex(int index, int faceCount)
                => $"[Error] Face index '{index}' is out of range. Valid range: 0 to {faceCount - 1}.";

            public static string InvalidEdgeIndex(int index, int edgeCount)
                => $"[Error] Edge index '{index}' is out of range. Valid range: 0 to {edgeCount - 1}.";

            public static string NoFacesProvided()
                => "[Error] No face indices provided. Please specify at least one face index to operate on.";

            public static string NoEdgesProvided()
                => "[Error] No edge indices provided. Please specify at least one edge index to operate on.";

            public static string ExtrusionFailed(string reason)
                => $"[Error] Extrusion failed: {reason}";

            public static string BevelFailed(string reason)
                => $"[Error] Bevel operation failed: {reason}";

            public static string MeshHasNoFaces()
                => "[Error] The ProBuilderMesh has no faces to operate on.";

            public static string MeshImportFailed(string reason)
                => $"[Error] Failed to convert mesh to ProBuilder: {reason}";
        }
    }
}
#endif
