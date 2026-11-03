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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Cinemachine
{
    /// <summary>Every Cinemachine camera: runtime caches and upgrade bookkeeping hidden, CameraState explicit-only.</summary>
    public class Unity_Cinemachine_CinemachineVirtualCameraBase_AdapterTests : CinemachineTestBase
    {
        private static readonly string[] Hidden =
        {
            "m_StreamingVersion", "m_LegacyPriority", "FollowTargetAsGroup", "FollowTargetAsVcam", "LookAtTargetAsGroup",
            "LookAtTargetAsVcam", "FollowTargetChanged", "LookAtTargetChanged", "PreviousStateIsValid", "ParentCamera",
        };

        [Test]
        public void InternalMembersAreHidden()
        {
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "CinemachineCamera");
            foreach (var member in Hidden)
                StringAssert.DoesNotContain(member, schema);
            StringAssert.Contains("Priority PrioritySettings", schema);
            StringAssert.Contains("StandbyUpdate", schema);
        }

        [Test]
        public void OtherCameraTypesAreCoveredToo()
        {
            var schema = RqlUnitySchema.Render(RqlUnity.Engine, "CinemachineClearShot");
            StringAssert.DoesNotContain("m_StreamingVersion", schema);
            StringAssert.DoesNotContain("FollowTargetAsVcam", schema);
            StringAssert.Contains("Priority", schema);
        }

        [Test]
        public void StateIsReadOnlyByExplicitPath()
        {
            var camera = CreateCamera();
            camera.transform.position = new Vector3(0, 5, 0);
            var r = Ok(Q(Cam() + " | select State.Lens.FieldOfView"));
            StringAssert.DoesNotContain("unknown", Table(r));
            StringAssert.Contains("readonly-member", Table(Fails(M(Cam() + " | set State = null"))));
        }

        [Test]
        public void ReadingCamerasWholeDoesNotFail()
        {
            var camera = CreateCamera();
            camera.gameObject.AddComponent<CinemachineFollow>();
            camera.gameObject.AddComponent<CinemachineRotationComposer>();
            camera.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
            Ok(Q(Cameras));
            Ok(Q(Cameras + " | describe"));
            Ok(Q("objects | get components | where $ is CinemachineComponentBase | describe"));
        }
    }
}
