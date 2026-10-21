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
using System;
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.YOUR_FEATURE_NAME
{
    // Aliases inside the namespace block: see the name-clash rule in YOUR_FEATURE_NAMEModule.cs.
    using Light = UnityEngine.Light;
    using LightShadows = UnityEngine.LightShadows;

    /// <summary>
    /// Template example adapter (one adapter per file, named after the full type name): Light gets a virtual member and a
    /// method. Ordinary members (intensity, color, range …) need no adapter: RQL reflects them already. Adapters compose with
    /// Unity-MCP's adapters for the base types (Component, Object), so only add what is specific to your type.
    /// </summary>
    public sealed class UnityEngine_Light_Adapter : RqlTypeAdapter<Light>
    {
        public override string Description =>
            "Light: castsShadows (bool, on = soft shadows); call ScaleIntensity(factor) multiplies intensity and returns it";

        protected override void Members(MemberTable<Light> m) =>
            m.Add("castsShadows",
                  light => light.shadows != LightShadows.None,
                  (light, on) => light.shadows = on ? LightShadows.Soft : LightShadows.None,
                  description: "true when the light casts shadows; setting true selects soft shadows")
             .Function<float, float>("ScaleIntensity", ScaleIntensity, "factor",
                  "multiply intensity by factor (>= 0); returns the new intensity");

        // Validate and throw: the error reaches the agent as the query error. The call's target (the Light) is recorded for
        // Undo by Unity-MCP before the method runs, so the method itself does not call Undo.RecordObject.
        private static float ScaleIntensity(Light light, float factor)
        {
            if (factor < 0f || float.IsNaN(factor) || float.IsInfinity(factor))
                throw new ArgumentException($"factor must be a finite number >= 0, got {factor}");
            light.intensity *= factor;
            return light.intensity;
        }
    }
}
