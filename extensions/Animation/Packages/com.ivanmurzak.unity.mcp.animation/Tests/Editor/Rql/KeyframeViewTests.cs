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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    public class KeyframeViewTests : AnimationTestBase
    {
        private const string Y = W + " | get curves | where property == \"localPosition.y\"";

        [Test]
        public void KeyMembersAreWritable()
        {
            Ok(Y + " | set keys[1].value = 4, keys[1].inTangent = 2, keys[0].outTangent = -1");
            var keys = CurveOf(Clip, "localPosition.y").keys;
            Assert.AreEqual(4f, keys[1].value, 1e-5f);
            Assert.AreEqual(2f, keys[1].inTangent, 1e-5f);
            Assert.AreEqual(-1f, keys[0].outTangent, 1e-5f);
        }

        [Test]
        public void WeightsAndWeightedMode()
        {
            Ok(Y + " | set keys[0].weightedMode = \"Out\", keys[0].outWeight = 0.5");
            var key = CurveOf(Clip, "localPosition.y").keys[0];
            Assert.AreEqual(WeightedMode.Out, key.weightedMode);
            Assert.AreEqual(0.5f, key.outWeight, 1e-5f);
        }

        [Test]
        public void EveryKeyAtOnce()
        {
            Ok(Y + " | get keys | set value = value + 10");
            var keys = CurveOf(Clip, "localPosition.y").keys;
            Assert.AreEqual(10f, keys[0].value, 1e-5f);
            Assert.AreEqual(11f, keys[1].value, 1e-5f);
        }

        [Test]
        public void MovingAKeyInTime()
        {
            Ok(Y + " | set keys[1].time = 2");
            Assert.AreEqual(2f, Clip.length, 1e-5f);
        }

        [Test]
        public void KeyWritesAreUndoable()
        {
            Undo.ClearAll();
            Ok(Y + " | set keys[1].value = 9");
            Undo.PerformUndo();
            Assert.AreEqual(1f, CurveOf(Clip, "localPosition.y").keys[1].value, 1e-5f);
        }
    }
}
