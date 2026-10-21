/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-ParticleSystem) │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Particles
{
    using ParticleSystem = UnityEngine.ParticleSystem;

    /// <summary>
    /// A ParticleSystem module struct (ps.main, ps.emission …) is a handle: writing a member of a copy changes the particle
    /// system. Marked writes-through, so `set main.startLifetime = 3` works although `main` is a get-only property.
    /// </summary>
    public sealed class UnityEngine_ParticleSystem_ModuleStruct_Adapter : RqlTypeAdapter
    {
        public UnityEngine_ParticleSystem_ModuleStruct_Adapter(Type moduleStruct) : base(moduleStruct) { }

        public override string Name => "ParticleSystem." + Target.Name;

        public override void ConfigureMembers(MemberTable members) => members.WritesThrough();
    }
}
