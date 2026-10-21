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
    public class AnimationCurveViewTests : AnimationTestBase
    {
        private const string Y = W + " | get curves | where property == \"localPosition.y\"";

        [Test]
        public void CurvesListTheBindings()
        {
            var r = Table(Ok(Q(W + " | get curves | select path type property keyCount timeRange")));
            StringAssert.Contains("Transform", r);
            StringAssert.Contains("localPosition.y", r);
            StringAssert.Contains("(0, 1)", r);
            Assert.AreEqual(1L, Count(W + " | get curves | count"));
        }

        [Test]
        public void KeysAreReadable()
        {
            var r = Table(Ok(Q(Y + " | get keys | select index time value")));
            StringAssert.Contains("1", r);
            Assert.AreEqual(2L, Count(Y + " | get keys | count"));
        }

        [Test]
        public void AddKeyInsertsInTimeOrder()
        {
            Assert.AreEqual("1", Result(Ok(Y + " | call AddKey(0.5, 3)")));
            var curve = CurveOf(Clip, "localPosition.y");
            Assert.AreEqual(3, curve.length);
            Assert.AreEqual(3f, curve.keys[1].value, 1e-5f);
        }

        [Test]
        public void AddKeyAtAnExistingTimeReplacesIt()
        {
            Ok(Y + " | call AddKey(1, 7)");
            var curve = CurveOf(Clip, "localPosition.y");
            Assert.AreEqual(2, curve.length);
            Assert.AreEqual(7f, curve.keys[1].value, 1e-5f);
        }

        [Test]
        public void RemoveKey()
        {
            Assert.AreEqual("1", Result(Ok(Y + " | call RemoveKey(0)")));
            Assert.AreEqual(1f, CurveOf(Clip, "localPosition.y").keys[0].time, 1e-5f);
            Fails(Y + " | call RemoveKey(4)", "key index 4 is out of range");
        }

        [Test]
        public void SetKeysReplacesAllKeys()
        {
            Ok(Y + " | call SetKeys([[0, 1], [2, 3], [4, 5]])");
            var curve = CurveOf(Clip, "localPosition.y");
            Assert.AreEqual(3, curve.length);
            Assert.AreEqual(4f, Clip.length, 1e-5f);
        }

        [Test]
        public void EvaluateSamplesTheCurve()
        {
            Ok(Y + " | call SetKeys([[0, 0, 1, 1], [1, 1, 1, 1]])");
            Assert.AreEqual(0.5f, float.Parse(Result(Ok(Y + " | call Evaluate(0.5)")), System.Globalization.CultureInfo.InvariantCulture), 1e-4f);
        }

        [Test]
        public void WrapModesAreWritable()
        {
            Ok(Y + " | set preWrapMode = \"PingPong\", postWrapMode = \"Loop\"");
            var curve = CurveOf(Clip, "localPosition.y");
            Assert.AreEqual(WrapMode.PingPong, curve.preWrapMode);
            Assert.AreEqual(WrapMode.Loop, curve.postWrapMode);
        }

        [Test]
        public void RemoveDeletesTheCurve()
        {
            Ok(Y + " | call Remove()");
            Assert.AreEqual(0, AnimationUtility.GetCurveBindings(Clip).Length);
        }

        [Test]
        public void CurveEditsAreUndoable()
        {
            Undo.ClearAll();
            Ok(Y + " | call AddKey(0.5, 3)");
            Assert.AreEqual(3, CurveOf(Clip, "localPosition.y").length);
            Undo.PerformUndo();
            Assert.AreEqual(2, CurveOf(Clip, "localPosition.y").length);
        }
    }
}
