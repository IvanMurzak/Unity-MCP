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
    /// Lighting: the <c>lighting</c> source (the active scene's environment, its Lighting Settings asset and the bake), plus
    /// LightProbeGroup grid placement and ReflectionProbe baking. Lights are built into Unity, so this module ships with the
    /// core package in its own assembly, like Physics. It adds no guide to the unity-rql skill: the skill lists the source in
    /// one line and agents read the members and calls on demand with <c>rql-schema "lighting"</c> (as for <c>editor</c> and
    /// <c>profiler</c>), so projects that never touch lighting pay no tokens for it.
    /// </summary>
    public sealed class LightingModule : IRqlModule
    {
        /// <summary>The source name agents type at the start of a query: <c>lighting | …</c>.</summary>
        public const string SourceName = "lighting";

        /// <summary>After the core module (0), like the other optional modules.</summary>
        public int Order => 100;

        public void Configure(RqlModuleContext ctx) =>
            ctx.AddAdapter(new UnityEngine_LightProbeGroup_Adapter())
               .AddAdapter(new UnityEngine_ReflectionProbe_Adapter())
               .AddSource(SourceName, () => LightingEditorApi.Instance,
                   "the active scene's lighting: environment (ambient, fog, skybox, sun), Lighting Settings asset, bake " +
                   "(rql-schema \"lighting\" for members and calls)");
    }
}
