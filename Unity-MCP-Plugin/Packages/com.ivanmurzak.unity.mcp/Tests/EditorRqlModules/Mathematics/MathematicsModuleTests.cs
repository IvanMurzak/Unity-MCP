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
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Mathematics;
using NUnit.Framework;
using Rql;
using Rql.Execution;
using Unity.Mathematics;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Mathematics
{
    public class MathematicsModuleTests
    {
        public sealed class Holder { public float3 position; public quaternion rotation = quaternion.identity; public float2 uv; }

        private static RqlEngine Engine(Holder h)
        {
            RqlUnity.Reset();
            var e = RqlUnity.Create();
            e.AddSource("h", () => h);
            return e;
        }

        private static readonly QueryOptions Mutate = new QueryOptions { AllowMutation = true };

        [Test]
        public void TheModuleIsDiscovered() =>
            CollectionAssert.Contains(RqlUnity.Engine.Modules.Select(m => m.Module).ToList(), typeof(MathematicsModule).FullName);

        [Test]
        public void VectorsAndQuaternionsAreCompactLiterals()
        {
            var h = new Holder();
            var e = Engine(h);
            Assert.IsTrue(e.Execute("h | set position = \"(1, 2, 3)\", uv = [0.5, 1]", null, Mutate).Ok);
            Assert.AreEqual(new float3(1, 2, 3), h.position);
            Assert.AreEqual(new float2(0.5f, 1), h.uv);
            var r = e.Execute("h | select position uv");
            StringAssert.Contains("(1, 2, 3)|(0.5, 1)", AgentFormat.Render(r, OutputFormat.Table));
        }

        [Test]
        public void QuaternionsAcceptEulerDegrees()
        {
            var h = new Holder();
            var e = Engine(h);
            Assert.IsTrue(e.Execute("h | set rotation = \"(0, 90, 0)\"", null, Mutate).Ok);
            var euler = ((UnityEngine.Quaternion)h.rotation).eulerAngles;
            Assert.AreEqual(90f, euler.y, 1e-3f);
        }
    }
}
