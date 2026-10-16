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
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Particles
{
    using ParticleSystem = UnityEngine.ParticleSystem;

    public class UnityEngine_ParticleSystem_ModuleStruct_AdapterTests : ParticleTestBase
    {
        [Test]
        public void ModuleHandlesWriteThroughGetOnlyProperties()
        {
            var r = M(P + " | set main.loop = false, main.gravityModifier = 0.5, emission.enabled = false, shape.radius = 2");
            Assert.IsTrue(r.Ok, Table(r));
            Assert.IsFalse(Ps.main.loop);
            Assert.AreEqual(0.5f, Ps.main.gravityModifier.constant, 1e-6f);
            Assert.IsFalse(Ps.emission.enabled);
            Assert.AreEqual(2f, Ps.shape.radius, 1e-6f);
        }

        [Test]
        public void ModuleMembersAreReadable()
        {
            var main = Ps.main;
            main.maxParticles = 321;
            Assert.AreEqual("321", Cell(Q(P + " | select main.maxParticles")));
        }
    }
}
