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
using com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Adapters;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Particles
{
    using ParticleSystem = UnityEngine.ParticleSystem;

    /// <summary>A fresh scene with one particle system ("Sparks") per test; query helpers come from Unity-MCP's AdapterTestBase.</summary>
    public abstract class ParticleTestBase : AdapterTestBase
    {
        protected ParticleSystem Ps = null!;
        protected const string P = "objects | get components | where $ is ParticleSystem";

        // Runs after AdapterTestBase.NewScene (NUnit runs base-class SetUp methods first).
        [SetUp]
        public void AddParticleSystem() => Ps = new GameObject("Sparks").AddComponent<ParticleSystem>();
    }
}
