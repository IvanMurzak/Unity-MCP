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
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Particles
{
    using ParticleSystem = UnityEngine.ParticleSystem;

    public class UnityEngine_ParticleSystem_AdapterTests : ParticleTestBase
    {
        [Test]
        public void PlaySimulateAndStop()
        {
            Assert.IsTrue(M(P + " | call Play()").Ok);
            Assert.IsTrue(Ps.isPlaying);
            var sim = M(P + " | call Simulate(1.5)");
            Assert.IsTrue(sim.Ok, Table(sim));
            Assert.AreEqual(1.5f, Ps.time, 0.05f);
            Assert.IsTrue(M(P + " | call Stop()").Ok);
            Assert.IsFalse(Ps.isEmitting);
            Assert.IsTrue(M(P + " | call Clear()").Ok);
            Assert.AreEqual(0, Ps.particleCount);
        }

        [Test]
        public void PlaybackMethodsAreInTheSchema()
        {
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "ParticleSystem");
            StringAssert.Contains("Play()", schema);
            StringAssert.Contains("Simulate(", schema);
        }

        [Test]
        public void TheRqlToolsReachTheExtensionThroughTheMcpPipeline()
        {
            UnityMcpPluginEditor.InitSingletonIfNeeded();
            var args = new System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>
            {
                ["query"] = System.Text.Json.JsonDocument.Parse("\"objects | get components | where $ is ParticleSystem | set main.startLifetime = 4\"").RootElement.Clone(),
            };
            var response = UnityMcpPluginEditor.Instance.Tools!.RunCallTool(new com.IvanMurzak.McpPlugin.Common.Model.RequestCallTool("rql-mutate", args)).Result;
            Assert.AreEqual(com.IvanMurzak.McpPlugin.Common.Model.ResponseStatus.Success, response.Value!.Status);
            Assert.AreEqual(4f, Ps.main.startLifetime.constant, 1e-6f);
        }
    }
}
