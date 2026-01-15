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
using System.ComponentModel;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    [Description("Scene reference. " +
        "Used to find a Scene.")]
    public class SceneRef : ObjectRef
    {
        public string Path { get; set; } = string.Empty;
        public int BuildIndex { get; set; } = -1;

        public SceneRef() { }
        public SceneRef(int instanceID)
        {
            this.InstanceID = instanceID;
        }
        public SceneRef(UnityEngine.SceneManagement.Scene scene)
        {
            this.InstanceID = scene.GetHashCode();
            this.Path = scene.path;
            this.BuildIndex = scene.buildIndex;
        }
    }
}
