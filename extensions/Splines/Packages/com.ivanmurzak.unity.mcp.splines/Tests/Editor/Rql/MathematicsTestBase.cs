/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Splines)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Unity.Mathematics;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Splines
{
    /// <summary>Unity.Mathematics values on a plain object, reachable as the <c>probe</c> source.</summary>
    public sealed class MathematicsProbe
    {
        public float2 f2;
        public float3 f3;
        public float4 f4;
        public quaternion q = quaternion.identity;
    }

    public abstract class MathematicsTestBase : SplinesTestBase
    {
        protected MathematicsProbe Probe = null!;

        [SetUp]
        public void AddProbe()
        {
            Probe = new MathematicsProbe();
            var probe = Probe;
            RqlUnity.Engine.AddSource("probe", () => probe, "test values");
        }
    }
}
