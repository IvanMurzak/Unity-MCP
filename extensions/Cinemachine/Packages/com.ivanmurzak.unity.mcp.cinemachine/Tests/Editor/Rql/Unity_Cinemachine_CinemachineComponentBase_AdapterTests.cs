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
    /// <summary>Pipeline components: caches and bookkeeping hidden, VirtualCamera a reference, settings plain members.</summary>
    public class Unity_Cinemachine_CinemachineComponentBase_AdapterTests : CinemachineTestBase
    {
        private const string Noise = "objects | get components | where $ is CinemachineBasicMultiChannelPerlin";

        [Test]
        public void InternalMembersAreHidden()
        {
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "CinemachineBasicMultiChannelPerlin");
            StringAssert.DoesNotContain("m_NoiseOffsets", schema);
            StringAssert.DoesNotContain("FollowTargetAsGroup", schema);
            StringAssert.DoesNotContain("LookAtTargetAsGroup", schema);
            foreach (var member in new[] { "NoiseProfile", "AmplitudeGain", "FrequencyGain", "PivotOffset", "Stage", "VirtualCamera" })
                StringAssert.Contains(member, schema);
        }

        [Test]
        public void ComponentsAreFoundAndWrittenDirectly()
        {
            var camera = CreateCamera();
            var noise = camera.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
            Assert.AreEqual("Noise", Cell(Q(Noise + " | select Stage")));
            Assert.AreEqual("Cam", Cell(Q(Noise + " | select VirtualCamera.name")));
            Ok(M(Noise + " | set FrequencyGain = 3, PivotOffset = \"(0, 1, 0)\""));
            Assert.AreEqual(3f, noise.FrequencyGain, 1e-5f);
            Assert.AreEqual(Vector3.up, noise.PivotOffset);
            Undo.PerformUndo();
            Assert.AreEqual(1f, noise.FrequencyGain, 1e-5f);
        }

        [Test]
        public void TheNoiseProfileIsAnAssetReference()
        {
            var camera = CreateCamera();
            var noise = camera.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
            Ok(M(Noise + " | set NoiseProfile = unity.asset(\"Packages/com.unity.cinemachine/Presets/Noise/6D Wobble.asset\")"));
            Assert.AreEqual("6D Wobble", noise.NoiseProfile.name);
            Assert.AreEqual("6D Wobble", Cell(Q(Noise + " | select NoiseProfile.name")));
        }

        [Test]
        public void VcamStateIsExplicitOnly()
        {
            var camera = CreateCamera();
            camera.gameObject.AddComponent<CinemachineFollow>();
            var describe = Table(Ok(Q("objects | get components | where $ is CinemachineFollow | describe")));
            StringAssert.Contains("FollowOffset", describe);
            Ok(Q("objects | get components | where $ is CinemachineFollow | select VcamState.Lens.FieldOfView"));
        }
    }
}
