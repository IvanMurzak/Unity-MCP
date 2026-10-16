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
using NUnit.Framework;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.YOUR_FEATURE_NAME
{
    using LightShadows = UnityEngine.LightShadows;

    public class UnityEngine_Light_AdapterTests : YOUR_FEATURE_NAMETestBase
    {
        [Test]
        public void CastsShadowsIsAVirtualMember()
        {
            Bulb.shadows = LightShadows.None;
            Assert.AreEqual("false", Cell(Q(L + " | select castsShadows")));

            Ok(L + " | set castsShadows = true");
            Assert.AreEqual(LightShadows.Soft, Bulb.shadows);
            Assert.AreEqual("true", Cell(Q(L + " | select castsShadows")));
        }

        [Test]
        public void ScaleIntensityReturnsTheNewIntensityAndUndoes()
        {
            var r = Ok(L + " | call ScaleIntensity(2.5)");
            Assert.AreEqual(2.5f, float.Parse(Result(r), System.Globalization.CultureInfo.InvariantCulture), 1e-5f);
            Assert.AreEqual(2.5f, Bulb.intensity, 1e-5f);

            Undo.PerformUndo();
            Assert.AreEqual(1f, Bulb.intensity, 1e-5f);
        }

        [Test]
        public void ScaleIntensityRejectsNegativeFactors()
        {
            Fails(L + " | call ScaleIntensity(-1)", "factor must be");
            Assert.AreEqual(1f, Bulb.intensity, 1e-5f);
        }

        [Test]
        public void TheReadOnlyToolCannotCall() => Assert.IsFalse(Q(L + " | call ScaleIntensity(2)").Ok);

        [Test]
        public void TheSchemaDescribesTheAdapter()
        {
            var light = Schema("Light");
            StringAssert.Contains("castsShadows", light);
            StringAssert.Contains("ScaleIntensity(", light);
        }
    }
}
