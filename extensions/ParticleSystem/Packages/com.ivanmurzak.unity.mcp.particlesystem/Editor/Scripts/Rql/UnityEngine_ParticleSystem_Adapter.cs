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
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Particles
{
    using ParticleSystem = UnityEngine.ParticleSystem;

    /// <summary>ParticleSystem: playback in the Editor (Play / Pause / Stop / Clear / Simulate); modules are members.</summary>
    public sealed class UnityEngine_ParticleSystem_Adapter : RqlTypeAdapter<ParticleSystem>
    {
        public override string Description =>
            "ParticleSystem: modules main / emission / shape / … are writable members; call Play() / Pause() / Stop() / Clear() / Simulate(seconds)";

        protected override void Members(MemberTable<ParticleSystem> m) =>
            m.Method("Play", ps => ps.Play(true), "start emitting (children included)")
             .Method("Pause", ps => ps.Pause(true), "pause the simulation")
             .Method("Stop", ps => ps.Stop(true, ParticleSystemStopBehavior.StopEmitting), "stop emitting; live particles finish")
             .Method("Clear", ps => ps.Clear(true), "remove every live particle")
             .Method<float>("Simulate", (ps, seconds) => ps.Simulate(seconds, true, true), "seconds", "restart and fast-forward the simulation by seconds");
    }
}
