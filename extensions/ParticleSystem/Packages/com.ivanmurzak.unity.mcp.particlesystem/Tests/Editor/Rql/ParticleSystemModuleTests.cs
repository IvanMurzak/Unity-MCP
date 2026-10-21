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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Particles;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Particles
{
    using ParticleSystem = UnityEngine.ParticleSystem;

    public class ParticleSystemModuleTests : ParticleTestBase
    {
        [Test]
        public void TheOptionalModuleIsDiscovered()
        {
            var modules = RqlUnity.Engine.Modules.Select(m => m.Module).ToList();
            CollectionAssert.Contains(modules, typeof(ParticleSystemModule).FullName);
            CollectionAssert.Contains(modules, typeof(global::com.IvanMurzak.Unity.MCP.Editor.Rql.Modules.UnityCoreModule).FullName);
            Assert.IsTrue(RqlUnity.Engine.Modules.All(m => m.Ok), string.Join("\n", RqlUnity.Engine.Modules));
        }

        [Test]
        public void TheModuleGuideIsPartOfTheRqlSkill() =>
            StringAssert.Contains("## Particle systems", global::com.IvanMurzak.Unity.MCP.Editor.API.Skill_Rql.Render());

        [Test]
        public void SchemaFindsNestedParticleTypes()
        {
            // ParticleSystem.MinMaxCurve / MainModule are nested types: the type index must include them.
            var curve = RqlUnitySchema.Render(RqlUnity.Engine, "MinMaxCurve");
            StringAssert.DoesNotContain("unknown-type", curve);
            StringAssert.Contains("constantMin", curve);
            StringAssert.DoesNotContain("unknown-type", RqlUnitySchema.Render(RqlUnity.Engine, "MainModule"));
        }
    }
}
