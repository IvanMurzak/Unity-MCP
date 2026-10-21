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

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting
{
    /// <summary>
    /// Lighting: the <c>lighting</c> source (the active scene's environment and Lighting Settings asset, the bake, every light
    /// and probe, stats), LightProbeGroup grid placement, ReflectionProbe baking, and the guide rendered into the unity-rql
    /// skill. Lights are built into Unity, so this module ships with the core package in its own assembly, like Physics.
    /// </summary>
    public sealed class LightingModule : IRqlModule
    {
        /// <summary>The guide's heading in the unity-rql skill (<c>## Lighting</c>).</summary>
        public const string GuideTopic = "Lighting";

        /// <summary>The source name agents type at the start of a query: <c>lighting | …</c>.</summary>
        public const string SourceName = "lighting";

        private const string L = "objects | get components | where $ is Light";

        // Markdown. One query per line inside ``` blocks; a comment after three or more spaces is allowed. Every example runs,
        // in order, in LightingModuleTests.EveryGuideExampleRuns, so keep them runnable.
        private const string Guide =
@"Lights, light probe groups and reflection probes are components: add them with AddComponent and set any member
(`" + L + @"`). Realtime lights cost every frame; Baked lights cost nothing at run time but only light static
geometry (`objects | where name == ""Ground"" | set isStatic = true`) and light probes, after a bake.

```
editor | call CreateGameObject(""Lamp"")
objects | where name == ""Lamp"" | call AddComponent(""Light"")
" + L + @" | where gameObject.name == ""Lamp"" | set type = ""Spot"", intensity = 2, range = 15, spotAngle = 60, color = ""#ffd8a8"", shadows = ""Soft""
" + L + @" | where gameObject.name == ""Lamp"" | set lightmapBakeType = ""Baked""   Realtime / Mixed / Baked
" + SourceName + @" | get lights | select gameObject.name type lightmapBakeType intensity shadows
" + SourceName + @" | select stats   lights per bake type, real-time shadow casters, GI contributors, probes
```

The active scene's environment (ambient, fog, skybox, sun, default reflections); writes are undoable:

```
" + SourceName + @" | get environment | select ambientMode ambientIntensity fog fogMode skybox sun
" + SourceName + @" | get environment | set ambientMode = ""Trilight"", ambientSkyColor = ""#9ab8e0"", ambientEquatorColor = ""#6b7a8a"", ambientGroundColor = ""#3a3228""
" + SourceName + @" | get environment | set fog = true, fogMode = ""ExponentialSquared"", fogColor = ""#8090a0"", fogDensity = 0.02
" + SourceName + @" | get environment | set sun = unity.find(""Sun""), reflectionIntensity = 0.8
```

Bake settings live in the scene's Lighting Settings asset (null until one is assigned). A bake needs saved scenes; it runs
in the background, so poll `isBaking` / `bakeProgress` (`call Bake(true)` blocks until done: small scenes only):

```
" + SourceName + @" | call CreateSettings(""Assets/Lighting/Outdoor.lighting"")   new asset, assigned to the active scene
" + SourceName + @" | call UseSettings(""Assets/Lighting/Outdoor.lighting"")   assign an existing one
" + SourceName + @" | get settings | set lightmapper = ""ProgressiveCPU"", lightmapResolution = 10, lightmapMaxSize = 512, ao = true
" + SourceName + @" | get settings | select lightmapper lightmapResolution lightmapMaxSize bakedGI realtimeGI mixedBakeMode
" + SourceName + @" | call Bake()   start baking lightmaps, light probes and baked reflection probes
" + SourceName + @" | select isBaking bakeProgress hasBakedData lightmapCount
" + SourceName + @" | call CancelBake()
" + SourceName + @" | call ClearBakedData()
```

Probes light dynamic objects (light probes) and reflections (reflection probes):

```
editor | call CreateGameObject(""Probes"")
objects | where name == ""Probes"" | call AddComponent(""LightProbeGroup"")
objects | get components | where $ is LightProbeGroup | call PlaceGrid(""(10, 4, 10)"", 2)   a probe every 2 m in a 10 x 4 x 10 m box around it; returns the count
" + SourceName + @" | get probeGroups | select gameObject.name probeCount
editor | call CreateGameObject(""Mirror"")
objects | where name == ""Mirror"" | call AddComponent(""ReflectionProbe"")
objects | get components | where $ is ReflectionProbe | set mode = ""Baked"", size = ""(10, 5, 10)"", resolution = 128
objects | get components | where $ is ReflectionProbe | call Bake()   returns the baked texture's path
" + SourceName + @" | call BakeReflectionProbes()   every reflection probe of the loaded scenes
```";

        /// <summary>After the core module (0), like the other optional modules.</summary>
        public int Order => 100;

        public void Configure(RqlModuleContext ctx) =>
            ctx.AddAdapter(new UnityEngine_LightProbeGroup_Adapter())
               .AddAdapter(new UnityEngine_ReflectionProbe_Adapter())
               .AddSource(SourceName, () => LightingEditorApi.Instance,
                   "the active scene's lighting: environment (ambient, fog, skybox, sun), settings (Lighting Settings asset), lights, " +
                   "probeGroups, reflectionProbes, stats, isBaking / bakeProgress; calls CreateSettings, UseSettings, Bake, CancelBake, " +
                   "ClearBakedData, BakeReflectionProbes (rql-schema \"lighting\" for signatures)")
               // A Windows checkout (core.autocrlf) puts CRLF into the verbatim string; the skill uses LF.
               .AddGuide(GuideTopic, Guide.Replace("\r\n", "\n"));
    }
}
