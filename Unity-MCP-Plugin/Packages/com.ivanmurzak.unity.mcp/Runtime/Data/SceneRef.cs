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
#if UNITY_6000_5_OR_NEWER
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    [Description("Scene reference. Used to find a Scene.")]
    public class SceneRef : ObjectRef
    {
        public static partial class SceneRefProperty
        {
            public const string Path = "path";
            public const string BuildIndex = "buildIndex";

            public static IEnumerable<string> All => new[] { Path, BuildIndex };
        }

        [JsonInclude, JsonPropertyName(SceneRefProperty.Path)]
        [Description("Path to the Scene within the project. Starts with 'Assets/'")]
        public string Path { get; set; } = string.Empty;

        [JsonInclude, JsonPropertyName(SceneRefProperty.BuildIndex)]
        [Description("Build index of the Scene in the Build Settings.")]
        public int BuildIndex { get; set; } = -1;

        public SceneRef() { }
        public SceneRef(UnityEngine.EntityId entityId)
        {
            this.InstanceID = entityId;
        }
        public SceneRef(UnityEngine.SceneManagement.Scene scene)
        {
            this.InstanceID = UnityEngine.EntityId.FromULong((ulong)scene.GetHashCode());
            this.Path = scene.path;
            this.BuildIndex = scene.buildIndex;
        }
    }
}
#endif
