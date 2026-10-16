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
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Particles
{
    using ParticleSystem = UnityEngine.ParticleSystem;

    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): particle system playback,
    /// writable module handles, MinMaxCurve / MinMaxGradient literals, and the agent guide rendered into the unity-rql skill.
    /// </summary>
    public sealed class ParticleSystemModule : IRqlModule
    {
        private const string Guide =
@"Particle systems are components: `objects | get components | where $ is ParticleSystem`. Module structs (`main`,
`emission`, `shape` …) are written through, even though they are get-only properties:

```
objects | get components | where $ is ParticleSystem | set main.startLifetime = 3, main.loop = false
objects | get components | where $ is ParticleSystem | set emission.rateOverTime = [5, 25]          random between two
objects | get components | where $ is ParticleSystem | set main.startColor = ""#ff8800""
objects | get components | where $ is ParticleSystem | set main.startColor = [[""#ff0000"", 0], [""#0000ff"", 1]]   gradient
```

`MinMaxCurve` takes a number or `[min, max]`; `MinMaxGradient` a color, `[color, color]` or gradient keys.
Playback in the Editor: `objects | get components | where $ is ParticleSystem | call Play()` (also Pause, Stop, Clear,
Simulate(seconds)).";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            // MainModule, EmissionModule, ShapeModule …: handles whose setters write to the native particle system.
            foreach (var module in typeof(ParticleSystem).GetNestedTypes().Where(t => t.IsValueType && t.Name.EndsWith("Module")))
                ctx.AddAdapter(new UnityEngine_ParticleSystem_ModuleStruct_Adapter(module));
            ctx.AddAdapter(new UnityEngine_ParticleSystem_Adapter())
               .AddAdapter(new UnityEngine_MinMaxCurve_Adapter())
               .AddAdapter(new UnityEngine_MinMaxGradient_Adapter())
               .AddGuide("Particle systems", Guide);
        }
    }
}
