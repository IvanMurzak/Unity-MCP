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
    public class UnityEngine_AnimationCurve_AdapterTests : AnimationTestBase
    {
        private const string HolderPath = Folder + "/Holder.asset";
        private const string H = "unity.asset(\"" + HolderPath + "\")";

        private CurveHolder Holder = null!;

        [SetUp]
        public void CreateHolder()
        {
            Holder = ScriptableObject.CreateInstance<CurveHolder>();
            AssetDatabase.CreateAsset(Holder, HolderPath);
        }

        [Test]
        public void ACurveFromAKeyframeList()
        {
            Ok(H + " | set curve = [[0, 1], [0.5, 2], [2, 0]]");
            Assert.AreEqual(3, Holder.curve.length);
            Assert.AreEqual(2f, Holder.curve.keys[1].value, 1e-5f);
        }

        [Test]
        public void KeyElementWritesAreAssignedBack()
        {
            Ok(H + " | set curve.keys[1].value = 5");
            Assert.AreEqual(5f, Holder.curve.keys[1].value, 1e-5f);
        }

        [Test]
        public void CurveObjectLiteral()
        {
            Ok(H + " | set curve = {keys: [[0, 0], [1, 1]], postWrapMode: \"Loop\"}");
            Assert.AreEqual(WrapMode.Loop, Holder.curve.postWrapMode);
        }

        [Test]
        public void ABadKeyframeFails()
        {
            Fails(H + " | set curve = [[0, 1], \"x\"]", "expected a keyframe");
            Assert.AreEqual(2, Holder.curve.length);
        }

        [Test]
        public void KeysAreReadable()
        {
            var r = Table(Ok(Q(H + " | select curve.keys")));
            StringAssert.Contains("time", r);
            StringAssert.DoesNotContain("tangentMode", r);
        }
    }
}
