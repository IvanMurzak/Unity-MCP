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
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    public class UnityEngine_AnimationClip_AdapterTests : AnimationTestBase
    {
        [Test]
        public void SetCurveFromTimeValuePairs()
        {
            Assert.AreEqual("3", Result(Ok(W + " | call SetCurve(\"\", \"Transform\", \"localPosition.x\", [[0, 0], [0.5, 2], [1, 0]])")));
            var curve = CurveOf(Clip, "localPosition.x");
            Assert.AreEqual(3, curve.length);
            Assert.AreEqual(2f, curve.keys[1].value, 1e-5f);
            Assert.AreEqual(1f, Clip.length, 1e-5f);
        }

        [Test]
        public void SetCurveWithTangentsAndObjectLiterals()
        {
            Ok(W + " | call SetCurve(\"Arm\", \"UnityEngine.Transform\", \"localEulerAngles.z\", [[0, 0, 1, 2], {time: 1, value: 90, inTangent: 3}])");
            var curve = CurveOf(Clip, "localEulerAngles.z", "Arm");
            Assert.AreEqual(2f, curve.keys[0].outTangent, 1e-5f);
            Assert.AreEqual(3f, curve.keys[1].inTangent, 1e-5f);
            Assert.AreEqual(90f, curve.keys[1].value, 1e-5f);
        }

        [Test]
        public void SetCurveReplacesAnExistingCurve()
        {
            Ok(W + " | call SetCurve(\"\", \"Transform\", \"localPosition.y\", [[0, 5]])");
            Assert.AreEqual(1, CurveOf(Clip, "localPosition.y").length);
            Assert.AreEqual(1, AnimationUtility.GetCurveBindings(Clip).Length);
        }

        [Test]
        public void SetCurveOnOtherComponentTypes()
        {
            Ok(W + " | call SetCurve(\"Body\", \"SpriteRenderer\", \"m_Color.a\", [[0, 1], [1, 0]])");
            Ok(W + " | call SetCurve(\"\", \"GameObject\", \"m_IsActive\", [[0, 1], [0.5, 0]])");
            var types = AnimationUtility.GetCurveBindings(Clip).Select(b => b.type).ToList();
            CollectionAssert.Contains(types, typeof(SpriteRenderer));
            CollectionAssert.Contains(types, typeof(GameObject));
        }

        [Test]
        public void SetCurveRejectsBadInput()
        {
            Fails(W + " | call SetCurve(\"\", \"NoSuchComponent\", \"x\", [[0, 1]])", "no Unity object type named 'NoSuchComponent'");
            Fails(W + " | call SetCurve(\"\", \"Transform\", \"localPosition.x\", [])", "keys is empty");
            Fails(W + " | call SetCurve(\"\", \"Transform\", \"localPosition.x\", [[0, 1, 2]])", "expected a keyframe");
            Assert.AreEqual(1, AnimationUtility.GetCurveBindings(Clip).Length);
        }

        [Test]
        public void SetCurveIsUndoable()
        {
            Undo.ClearAll();
            Ok(W + " | call SetCurve(\"\", \"Transform\", \"localPosition.z\", [[0, 0], [1, 3]])");
            Assert.AreEqual(2, AnimationUtility.GetCurveBindings(Clip).Length);
            Undo.PerformUndo();
            Assert.AreEqual(1, AnimationUtility.GetCurveBindings(Clip).Length);
        }

        [Test]
        public void RemoveCurve()
        {
            Assert.AreEqual("true", Result(Ok(W + " | call RemoveCurve(\"\", \"Transform\", \"localPosition.y\")")));
            Assert.AreEqual(0, AnimationUtility.GetCurveBindings(Clip).Length);
            Assert.AreEqual("false", Result(Ok(W + " | call RemoveCurve(\"\", \"Transform\", \"localPosition.y\")")));
        }

        [Test]
        public void ObjectCurvesSetReadAndRemove()
        {
            EnsureFolder();
            var a = new Material(Shader.Find("Hidden/InternalErrorShader")) { name = "A" };
            var b = new Material(Shader.Find("Hidden/InternalErrorShader")) { name = "B" };
            AssetDatabase.CreateAsset(a, Folder + "/A.mat");
            AssetDatabase.CreateAsset(b, Folder + "/B.mat");
            Assert.AreEqual("2", Result(Ok(W + " | call SetObjectCurve(\"\", \"MeshRenderer\", \"m_Materials.Array.data[0]\", " +
                                              "[{time: 0, value: unity.asset(\"" + Folder + "/A.mat\")}, {time: 0.5, value: unity.asset(\"" + Folder + "/B.mat\")}])")));
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(Clip).Single();
            Assert.AreEqual(b, AnimationUtility.GetObjectReferenceCurve(Clip, binding)[1].value);

            var read = Table(Ok(Q(W + " | get objectCurves | select type property keyCount")));
            StringAssert.Contains("MeshRenderer", read);
            StringAssert.Contains("m_Materials.Array.data[0]", read);

            Ok(W + " | get objectCurves | call SetKeys([{time: 0, value: unity.asset(\"" + Folder + "/B.mat\")}])");
            Assert.AreEqual(1, AnimationUtility.GetObjectReferenceCurve(Clip, binding).Length);

            Assert.AreEqual("true", Result(Ok(W + " | call RemoveCurve(\"\", \"MeshRenderer\", \"m_Materials.Array.data[0]\")")));
            Assert.AreEqual(0, AnimationUtility.GetObjectReferenceCurveBindings(Clip).Length);
        }

        [Test]
        public void ClearCurves()
        {
            Ok(W + " | call SetCurve(\"\", \"Transform\", \"localPosition.x\", [[0, 0]])");
            Assert.AreEqual("2", Result(Ok(W + " | call ClearCurves()")));
            Assert.AreEqual(0, AnimationUtility.GetCurveBindings(Clip).Length);
        }

        [Test]
        public void AddEventsInTimeOrder()
        {
            Assert.AreEqual("0", Result(Ok(W + " | call AddEvent(0.5, \"Step\")")));
            Assert.AreEqual("0", Result(Ok(W + " | call AddEvent(0.25, \"Shout\", \"hey\", 1.5, 3)")));
            var events = AnimationUtility.GetAnimationEvents(Clip);
            CollectionAssert.AreEqual(new[] { "Shout", "Step" }, events.Select(e => e.functionName).ToArray());
            Assert.AreEqual("hey", events[0].stringParameter);
            Assert.AreEqual(1.5f, events[0].floatParameter, 1e-5f);
            Assert.AreEqual(3, events[0].intParameter);
        }

        [Test]
        public void AddEventRejectsBadInput()
        {
            Fails(W + " | call AddEvent(0.5, \"\")", "functionName is empty");
            Fails(W + " | call AddEvent(-1, \"Step\")", "must not be negative");
            Assert.AreEqual(0, AnimationUtility.GetAnimationEvents(Clip).Length);
        }

        [Test]
        public void RemoveAndClearEvents()
        {
            Ok(W + " | call AddEvent(0.1, \"A\")");
            Ok(W + " | call AddEvent(0.2, \"B\")");
            Ok(W + " | call AddEvent(0.3, \"C\")");
            Assert.AreEqual("2", Result(Ok(W + " | call RemoveEvent(1)")));
            CollectionAssert.AreEqual(new[] { "A", "C" }, AnimationUtility.GetAnimationEvents(Clip).Select(e => e.functionName).ToArray());
            Fails(W + " | call RemoveEvent(5)", "event index 5 is out of range");
            Assert.AreEqual("2", Result(Ok(W + " | call ClearEvents()")));
            Assert.AreEqual(0, AnimationUtility.GetAnimationEvents(Clip).Length);
        }

        [Test]
        public void EventsAreUndoable()
        {
            Undo.ClearAll();
            Ok(W + " | call AddEvent(0.5, \"Step\")");
            Undo.PerformUndo();
            Assert.AreEqual(0, AnimationUtility.GetAnimationEvents(Clip).Length);
        }

        [Test]
        public void PlainMembersAreWritable()
        {
            Ok(W + " | set frameRate = 24, wrapMode = \"Loop\"");
            Assert.AreEqual(24f, Clip.frameRate);
            Assert.AreEqual(WrapMode.Loop, Clip.wrapMode);
            var r = Table(Ok(Q(W + " | select name length frameRate isLooping n=(curves.count())")));
            StringAssert.Contains("Walk", r);
            StringAssert.Contains("24", r);
        }
    }
}
