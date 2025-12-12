/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.ComponentModel;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    public class GameObjectData
    {
        public GameObjectRef? Reference { get; set; }
        [Description("Serialized data of the GameObject and its components.")]
        public SerializedMember? Data { get; set; }
        [Description("Bounds of the GameObject.")]
        public Bounds? Bounds { get; set; }
        [Description("Hierarchy metadata of the GameObject.")]
        public GameObjectMetadata? Hierarchy { get; set; } = null;
    }

    public static class GameObjectDataExtensions
    {
        public static GameObjectData ToGameObjectData(
            this GameObject go,
            bool includeData = false,
            bool includeBounds = false,
            bool includeHierarchy = false,
            int hierarchyDepth = 0,
            bool deepSerialization = false)
        {
            var response = new GameObjectData()
            {
                Reference = new GameObjectRef(go)
            };

            if (includeData)
            {
                var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;
                response.Data = reflector.Serialize(
                    obj: go,
                    name: go.name,
                    recursive: deepSerialization,
                    logger: McpPlugin.McpPlugin.Instance.Logger
                );
            }

            if (includeBounds)
                response.Bounds = go.CalculateBounds();

            if (includeHierarchy)
                response.Hierarchy = go.ToMetadata(hierarchyDepth);

            return response;
        }
    }
}