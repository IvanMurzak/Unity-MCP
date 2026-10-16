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

// Name-clash rule: code in com.IvanMurzak.Unity.MCP.Editor.Rql.* resolves a simple name against every enclosing namespace
// BEFORE the using directives at the top of the file. Sibling extensions add namespaces such as
// com.IvanMurzak.Unity.MCP.Editor.Rql.Animation, so a plain `Animation` / `Terrain` / `Light` may bind to a namespace or
// to another package's type. Declare aliases for the engine types you use INSIDE the namespace block (as below): an alias
// there wins over enclosing namespace members. Also never name the feature namespace after a Unity type you reference.
namespace com.IvanMurzak.Unity.MCP.Editor.Rql.YOUR_FEATURE_NAME
{
    /// <summary>
    /// The extension's RQL module. Unity-MCP discovers every public <see cref="IRqlModule"/> with a parameterless
    /// constructor through TypeCache, so installing this package is enough: no registration call, no MCP tool. Agents reach
    /// everything registered here through the rql-query / rql-mutate / rql-schema tools, and the guide below is rendered
    /// into Unity-MCP's unity-rql skill.
    /// <para>
    /// Template example (replace it with your feature): an adapter that adds a virtual member and a method to the built-in
    /// Light component, a source with an [RqlCallable] operation that has no owner object, and a guide with example
    /// queries. Every guide example is executed by the EveryGuideExampleRuns test, so keep them runnable.
    /// </para>
    /// </summary>
    public sealed class YOUR_FEATURE_NAMEModule : IRqlModule
    {
        /// <summary>The guide's heading in the unity-rql skill (<c>## YOUR_PACKAGE_NAME</c>).</summary>
        public const string GuideTopic = "YOUR_PACKAGE_NAME";

        /// <summary>The source name agents type at the start of a query: <c>YOUR_FEATURE_NAME_LOWERCASE | call …</c>.</summary>
        public const string SourceName = "YOUR_FEATURE_NAME_LOWERCASE";

        private const string L = "objects | get components | where $ is Light";

        // Markdown. One query per line inside ``` blocks; a comment after three or more spaces is allowed.
        private const string Guide =
@"Lights are components: `" + L + @"`. `castsShadows` is a virtual bool member (on = soft shadows), `ScaleIntensity`
multiplies the intensity and returns the new value. `call` needs rql-mutate; every mutation is one Undo step.

```
" + SourceName + @" | call CreateLight(""Lamp"", ""Spot"", ""(0, 3, 0)"")   name, type (Point / Spot / Directional …), position
" + L + @" | select $ref gameObject.name type intensity castsShadows
" + L + @" | where gameObject.name == ""Lamp"" | set intensity = 2, color = ""#ffcc88""
" + L + @" | where gameObject.name == ""Lamp"" | set castsShadows = true
" + L + @" | where gameObject.name == ""Lamp"" | call ScaleIntensity(0.5)   returns the new intensity
```";

        /// <summary>Unity-MCP's core module uses a lower order; extensions use 100 so they can build on the core adapters.</summary>
        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            ctx.AddSource(SourceName, () => YOUR_FEATURE_NAMEEditorApi.Instance,
                   "YOUR_PACKAGE_NAME operations without an owner object: CreateLight (a GameObject with a Light)")
               .AddAdapter(new UnityEngine_Light_Adapter())
               .AddGuide(GuideTopic, Guide);
            // Optional: expression functions usable in rql-query (no mutation), e.g.
            // ctx.AddFunction("YOUR_FEATURE_NAME_LOWERCASE.something", args => …, "YOUR_FEATURE_NAME_LOWERCASE.something(x): what it returns");
        }
    }
}
