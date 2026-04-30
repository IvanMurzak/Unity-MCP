/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Model;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    [System.Serializable]
    public class ComponentData : ComponentDataShallow
    {
        public List<SerializedMember?>? fields { get; set; }
        public List<SerializedMember?>? properties { get; set; }

        public ComponentData() { }
        public ComponentData(Component component) : base(component) { }
    }

    // public static class ComponentDataExtensions
    // {
    //     public static ComponentData ToComponentData(
    //         this Component component,
    //         bool includeFields = false,
    //         bool includeProperties = false)
    //     {
    //         var result = new ComponentData(component);

    //         if (includeData)
    //         {
    //             var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;
    //             response.Data = reflector.Serialize(
    //                 obj: go,
    //                 name: go.name,
    //                 recursive: deepSerialization,
    //                 logger: UnityLoggerFactory.CreateLogger("GameObjectData")
    //             );
    //         }

    //         if (includeBounds)
    //             response.Bounds = go.CalculateBounds();

    //         if (includeHierarchy)
    //             response.Hierarchy = go.ToMetadata(hierarchyDepth);

    //         return response;
    //     }
    // }
}
