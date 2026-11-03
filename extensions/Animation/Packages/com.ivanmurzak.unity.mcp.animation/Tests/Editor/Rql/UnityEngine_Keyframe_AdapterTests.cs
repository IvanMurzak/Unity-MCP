/*
┌─────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                    │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Animation)  │
│  Copyright (c) 2026 Ivan Murzak                                         │
│  Licensed under the Apache License, Version 2.0.                        │
│  See the LICENSE file in the project root for more information.         │
└─────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    public class UnityEngine_Keyframe_AdapterTests : AnimationTestBase
    {
        private const string Set = W + " | call SetCurve(\"\", \"Transform\", \"localScale.x\", ";

        [Test]
        public void TimeValuePair()
        {
            Ok(Set + "[[0.25, 2]])");
            var key = CurveOf(Clip, "localScale.x").keys[0];
            Assert.AreEqual(0.25f, key.time, 1e-5f);
            Assert.AreEqual(2f, key.value, 1e-5f);
        }

        [Test]
        public void WithTangents()
        {
            Ok(Set + "[[0, 1, 0.5, -0.5]])");
            var key = CurveOf(Clip, "localScale.x").keys[0];
            Assert.AreEqual(0.5f, key.inTangent, 1e-5f);
            Assert.AreEqual(-0.5f, key.outTangent, 1e-5f);
        }

        [Test]
        public void TupleText()
        {
            Ok(Set + "[\"(1, 3)\"])");
            Assert.AreEqual(3f, CurveOf(Clip, "localScale.x").keys[0].value, 1e-5f);
        }

        [Test]
        public void ObjectLiteral()
        {
            Ok(Set + "[{time: 0, value: 1, weightedMode: \"Both\", inWeight: 0.2}])");
            var key = CurveOf(Clip, "localScale.x").keys[0];
            Assert.AreEqual(UnityEngine.WeightedMode.Both, key.weightedMode);
            Assert.AreEqual(0.2f, key.inWeight, 1e-5f);
        }

        [Test]
        public void WrongArity()
        {
            Fails(Set + "[[0]])", "expected a keyframe");
            Fails(Set + "[[0, \"a\"]])", "not a number");
        }
    }
}
