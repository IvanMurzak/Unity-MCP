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

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    public class SceneDataShallow : SceneRef
    {
        public string Name { get; set; } = string.Empty;
        public bool IsLoaded { get; set; }
        public bool IsDirty { get; set; }
        public bool IsSubScene { get; set; } = false;

        [Description("Whether this is a valid Scene. " +
            "A Scene may be invalid if, for example, you tried to open a Scene that does not exist. " +
            "In this case, the Scene returned from EditorSceneManager.OpenScene would return False for IsValid.")]
        public bool IsValid { get; set; } = true;
        public int RootCount { get; set; } = 0;

        public SceneDataShallow() { }
        public SceneDataShallow(UnityEngine.SceneManagement.Scene scene) : base(scene)
        {
            this.Name = scene.name;
            this.IsLoaded = scene.isLoaded;
            this.IsDirty = scene.isDirty;
            this.IsSubScene = scene.isSubScene;
            this.RootCount = scene.rootCount;
            this.IsValid = scene.IsValid();
        }
    }

    public static class SceneDataShallowExtensions
    {
        public static SceneDataShallow ToSceneDataShallow(this UnityEngine.SceneManagement.Scene scene)
        {
            var sceneData = new SceneDataShallow
            {
                Name = scene.name,
                Path = scene.path,
                IsLoaded = scene.isLoaded,
                IsDirty = scene.isDirty,
                BuildIndex = scene.buildIndex,
                RootCount = scene.rootCount,
                IsSubScene = scene.isSubScene,
                IsValid = scene.IsValid()
            };
            return sceneData;
        }
    }
}