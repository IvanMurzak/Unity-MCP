/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Linq;
using Rql.Execution;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters
{
    /// <summary>Scene (struct): a whitelist of safe members plus its root GameObjects.</summary>
    public sealed class UnityEngine_Scene_Adapter : RqlTypeAdapter<Scene>
    {
        protected override void Members(MemberTable<Scene> m)
        {
            m.Untyped.Only("name", "path", "buildIndex", "isLoaded", "isDirty", "rootCount");
            m.Add("roots", s => s.GetRootGameObjects().ToList(), description: "root GameObjects", owning: true);
        }
    }
}
