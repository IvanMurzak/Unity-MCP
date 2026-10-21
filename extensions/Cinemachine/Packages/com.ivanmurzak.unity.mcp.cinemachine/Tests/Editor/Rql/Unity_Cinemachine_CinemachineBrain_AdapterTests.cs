/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Cinemachine)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Cinemachine
{
    /// <summary>CinemachineBrain: default blend writes, activeCamera, hidden duplicates.</summary>
    public class Unity_Cinemachine_CinemachineBrain_AdapterTests : CinemachineTestBase
    {
        private const string Brains = "cinemachine | get brains";

        private static CinemachineBrain CreateBrain() =>
            new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>().gameObject.AddComponent<CinemachineBrain>();

        [Test]
        public void DefaultBlendIsWritableAndUndoable()
        {
            var brain = CreateBrain();
            Ok(M(Brains + " | set DefaultBlend.Style = \"Cut\", DefaultBlend.Time = 0.25"));
            Assert.AreEqual(CinemachineBlendDefinition.Styles.Cut, brain.DefaultBlend.Style);
            Assert.AreEqual(0.25f, brain.DefaultBlend.Time, 1e-5f);
            Undo.PerformUndo();
            Assert.AreEqual(CinemachineBlendDefinition.Styles.EaseInOut, brain.DefaultBlend.Style);
            Assert.AreEqual(2f, brain.DefaultBlend.Time, 1e-5f);
        }

        [Test]
        public void DefaultBlendTakesAnObjectLiteral()
        {
            var brain = CreateBrain();
            Ok(M(Brains + " | set DefaultBlend = {Style: \"Linear\", Time: 3}"));
            Assert.AreEqual(CinemachineBlendDefinition.Styles.Linear, brain.DefaultBlend.Style);
            Assert.AreEqual(3f, brain.DefaultBlend.Time, 1e-5f);
        }

        [Test]
        public void UnknownBlendStylesAreRejected()
        {
            var brain = CreateBrain();
            Fails(M(Brains + " | set DefaultBlend.Style = \"Teleport\""));
            Assert.AreEqual(CinemachineBlendDefinition.Styles.EaseInOut, brain.DefaultBlend.Style);
        }

        [Test]
        public void ActiveCameraReadsWithoutFailing()
        {
            CreateBrain();
            CreateCamera();
            var cell = Cell(Q(Brains + " | select activeCamera"));
            Assert.IsTrue(cell == "null" || cell.Contains("CinemachineCamera#"), cell);
        }

        [Test]
        public void InternalMembersAreHidden()
        {
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "CinemachineBrain");
            foreach (var member in new[] { "ActiveVirtualCamera", "ParentCamera", "IsValid" })
                StringAssert.DoesNotContain(member, schema);
            foreach (var member in new[] { "DefaultBlend", "CustomBlends", "UpdateMethod", "activeCamera", "OutputCamera" })
                StringAssert.Contains(member, schema);
        }
    }
}
