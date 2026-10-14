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

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Mathematics
{
    /// <summary>
    /// Optional module: Unity.Mathematics vectors and quaternions (used by Splines, Entities, Burst code) as compact literals.
    /// Its assembly only compiles when com.unity.mathematics is installed (asmdef versionDefines + defineConstraints).
    /// </summary>
    public sealed class MathematicsModule : IRqlModule
    {
        public int Order => 50;

        public void Configure(RqlModuleContext ctx) =>
            ctx.AddAdapter(new Unity_Mathematics_float2_Adapter())
               .AddAdapter(new Unity_Mathematics_float3_Adapter())
               .AddAdapter(new Unity_Mathematics_float4_Adapter())
               .AddAdapter(new Unity_Mathematics_quaternion_Adapter());
    }
}
