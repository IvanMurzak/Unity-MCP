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
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Physics3D
{
    /// <summary>
    /// Optional module: lives in an assembly that only compiles when com.unity.modules.physics is enabled (asmdef
    /// versionDefines + defineConstraints), so its adapters exist exactly when the project has 3D physics.
    /// </summary>
    public sealed class PhysicsModule : IRqlModule
    {
        public int Order => 100;

        public void Configure(RqlModuleContext ctx) => ctx.AddAdapter(new UnityEngine_Collider_Adapter());
    }
}
