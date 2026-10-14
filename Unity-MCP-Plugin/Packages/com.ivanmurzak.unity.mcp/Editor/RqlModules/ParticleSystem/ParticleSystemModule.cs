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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Particles
{
    /// <summary>
    /// Optional module: lives in an assembly that only compiles when com.unity.modules.particlesystem is enabled
    /// (asmdef versionDefines + defineConstraints), so its adapters exist exactly when the project has particle systems.
    /// </summary>
    public sealed class ParticleSystemModule : IRqlModule
    {
        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            // MainModule, EmissionModule, ShapeModule …: handles whose setters write to the native particle system.
            foreach (var module in typeof(ParticleSystem).GetNestedTypes().Where(t => t.IsValueType && t.Name.EndsWith("Module")))
                ctx.AddAdapter(new UnityEngine_ParticleSystem_ModuleStruct_Adapter(module));
            ctx.AddAdapter(new UnityEngine_MinMaxCurve_Adapter())
               .AddAdapter(new UnityEngine_MinMaxGradient_Adapter());
        }
    }
}
