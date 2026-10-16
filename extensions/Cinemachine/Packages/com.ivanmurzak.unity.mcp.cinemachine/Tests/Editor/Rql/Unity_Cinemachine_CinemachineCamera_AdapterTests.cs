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
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Cinemachine
{
    /// <summary>CinemachineCamera: pipeline methods and members, lens / priority / target writes, undo.</summary>
    public class Unity_Cinemachine_CinemachineCamera_AdapterTests : CinemachineTestBase
    {
        [Test]
        public void SetBodyAddsAndReplacesThePositionControl()
        {
            var camera = CreateCamera();
            StringAssert.Contains("CinemachineFollow#", Cell(M(Cam() + " | call SetBody(\"Follow\")"), 2));
            Assert.IsNotNull(camera.GetComponent<CinemachineFollow>());

            Ok(M(Cam() + " | call SetBody(\"CinemachinePositionComposer\")"));   // full type names work too
            Assert.IsNull(camera.GetComponent<CinemachineFollow>());
            Assert.IsNotNull(camera.GetComponent<CinemachinePositionComposer>());
            Assert.AreEqual("CinemachinePositionComposer", Cell(Q(Cam() + " | select body.$type")));

            Ok(M(Cam() + " | call SetBody(\"None\")"));
            Assert.IsNull(camera.GetComponent<CinemachinePositionComposer>());
            Assert.AreEqual("null", Cell(Q(Cam() + " | select body")));
        }

        [Test]
        public void SetBodyKeepsTheSettingsOfTheSameType()
        {
            var camera = CreateCamera();
            var follow = camera.gameObject.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(1, 2, 3);
            Ok(M(Cam() + " | call SetBody(\"Follow\")"));
            Assert.AreSame(follow, camera.GetComponent<CinemachineFollow>());
            Assert.AreEqual(new Vector3(1, 2, 3), follow.FollowOffset);
        }

        [Test]
        public void SetBodyRejectsUnknownAndOtherStageTypes()
        {
            var camera = CreateCamera();
            var r = Table(Fails(M(Cam() + " | call SetBody(\"Teleport\")")));
            StringAssert.Contains("no Body component named 'Teleport'", r);
            StringAssert.Contains("OrbitalFollow", r, "the error lists the valid names");
            Fails(M(Cam() + " | call SetBody(\"RotationComposer\")"));   // an Aim component
            Fails(M(Cam() + " | call SetBody(\"Transposer\")"));         // deprecated CM2 component
            Assert.AreEqual(2, camera.GetComponents<Component>().Length, "nothing was added (Transform, CinemachineCamera)");
        }

        [Test]
        public void SetBodyIsUndoable()
        {
            var camera = CreateCamera();
            camera.gameObject.AddComponent<CinemachineFollow>();
            Ok(M(Cam() + " | call SetBody(\"ThirdPersonFollow\")"));
            Undo.PerformUndo();
            Assert.IsNotNull(camera.GetComponent<CinemachineFollow>(), "the replaced body is restored");
            Assert.IsNull(camera.GetComponent<CinemachineThirdPersonFollow>());
        }

        [Test]
        public void SetAimAddsAndReplacesTheRotationControl()
        {
            var camera = CreateCamera();
            Ok(M(Cam() + " | call SetAim(\"RotationComposer\")"));
            Assert.IsNotNull(camera.GetComponent<CinemachineRotationComposer>());
            Ok(M(Cam() + " | call SetAim(\"HardLookAt\")"));
            Assert.IsNull(camera.GetComponent<CinemachineRotationComposer>());
            Assert.IsNotNull(camera.GetComponent<CinemachineHardLookAt>());
            Assert.AreEqual("CinemachineHardLookAt", Cell(Q(Cam() + " | select aim.$type")));
            Ok(M(Cam() + " | call SetAim(\"None\")"));
            Assert.IsNull(camera.GetComponent<CinemachineHardLookAt>());
            StringAssert.Contains("no Aim component named 'Follow'", Table(Fails(M(Cam() + " | call SetAim(\"Follow\")"))));
        }

        [Test]
        public void BodyAndAimAreKeptApart()
        {
            var camera = CreateCamera();
            Ok(M(Cam() + " | call SetBody(\"Follow\")"));
            Ok(M(Cam() + " | call SetAim(\"PanTilt\")"));
            Ok(M(Cam() + " | call SetBody(\"OrbitalFollow\")"));
            Assert.IsNotNull(camera.GetComponent<CinemachinePanTilt>(), "replacing the Body leaves the Aim");
        }

        [Test]
        public void SetNoiseUsesAProfileByName()
        {
            var camera = CreateCamera();
            Ok(M(Cam() + " | call SetNoise(\"6D Shake\", 2, 0.5)"));
            var noise = camera.GetComponent<CinemachineBasicMultiChannelPerlin>();
            Assert.AreEqual("6D Shake", noise.NoiseProfile.name);
            Assert.AreEqual(2f, noise.AmplitudeGain, 1e-5f);
            Assert.AreEqual(0.5f, noise.FrequencyGain, 1e-5f);

            Ok(M(Cam() + " | call SetNoise(\"handheld_normal_mild\")"));   // case-insensitive, gains default to 1
            Assert.AreSame(noise, camera.GetComponent<CinemachineBasicMultiChannelPerlin>(), "the noise component is reused");
            Assert.AreEqual("Handheld_normal_mild", noise.NoiseProfile.name);
            Assert.AreEqual(1f, noise.AmplitudeGain, 1e-5f);

            Assert.AreEqual(0.5, double.Parse(Cell(Ok(M(Cam() + " | set noise.AmplitudeGain = 0.5")), 3), System.Globalization.CultureInfo.InvariantCulture), 1e-5);
            Assert.AreEqual(0.5f, noise.AmplitudeGain, 1e-5f);

            Ok(M(Cam() + " | call SetNoise(\"None\")"));
            Assert.IsNull(camera.GetComponent<CinemachineBasicMultiChannelPerlin>());
        }

        [Test]
        public void SetNoiseRejectsAnUnknownProfile()
        {
            var camera = CreateCamera();
            var r = Table(Fails(M(Cam() + " | call SetNoise(\"Earthquake\")")));
            StringAssert.Contains("no NoiseSettings profile named 'Earthquake'", r);
            StringAssert.Contains("6D Shake", r);
            Assert.IsNull(camera.GetComponent<CinemachineBasicMultiChannelPerlin>());
        }

        [Test]
        public void SetNoiseIsUndoable()
        {
            var camera = CreateCamera();
            Ok(M(Cam() + " | call SetNoise(\"6D Shake\", 3)"));
            Undo.PerformUndo();
            Assert.IsNull(camera.GetComponent<CinemachineBasicMultiChannelPerlin>());
        }

        [Test]
        public void SetTargetsByNameOrRef()
        {
            var camera = CreateCamera();
            var player = CreateTarget("Player");
            var boss = CreateTarget("Boss");

            Ok(M(Cam() + " | call SetTargets(\"Player\")"));
            Assert.AreEqual(player.transform, camera.Follow);
            Assert.AreEqual(player.transform, camera.LookAt, "without lookAt the camera looks at the Follow target");
            Assert.IsFalse(camera.Target.CustomLookAtTarget);

            Ok(M(Cam() + " | call SetTargets(\"Player\", ref(\"" + Ref(boss) + "\"))"));
            Assert.AreEqual(boss.transform, camera.LookAt);

            Ok(M(Cam() + " | call SetTargets(null)"));
            Assert.IsNull(camera.Follow);
            Assert.IsNull(camera.LookAt);
        }

        [Test]
        public void SetTargetsRejectsAmbiguousNames()
        {
            var camera = CreateCamera();
            CreateTarget("Twin");
            CreateTarget("Twin");
            StringAssert.Contains("2 GameObjects are named 'Twin'", Table(Fails(M(Cam() + " | call SetTargets(\"Twin\")"))));
            Assert.IsNull(camera.Follow);
        }

        [Test]
        public void SetTargetsIsUndoable()
        {
            var camera = CreateCamera();
            CreateTarget("Player");
            Ok(M(Cam() + " | call SetTargets(\"Player\")"));
            Undo.PerformUndo();
            Assert.IsNull(camera.Follow);
        }

        [Test]
        public void FollowAndLookAtAreWritableMembers()
        {
            var camera = CreateCamera();
            var player = CreateTarget("Player");
            Ok(M(Cam() + " | set Follow = ref(\"" + Ref(player) + "\"), LookAt = ref(\"" + Ref(player) + "\")"));
            Assert.AreEqual(player.transform, camera.Follow);
            Assert.AreEqual(player.transform, camera.LookAt);
            Ok(M(Cam() + " | set LookAt = null"));
            Assert.IsNull(camera.LookAt);
            Assert.AreEqual("Player", Cell(Q(Cam() + " | select Follow.name")));
        }

        [Test]
        public void LensIsWrittenInPlaceAndUndoable()
        {
            var camera = CreateCamera();
            Ok(M(Cam() + " | set Lens.FieldOfView = 55, Lens.FarClipPlane = 500, Lens.Dutch = -10"));
            Assert.AreEqual(55f, camera.Lens.FieldOfView, 1e-4f);
            Assert.AreEqual(500f, camera.Lens.FarClipPlane, 1e-4f);
            Assert.AreEqual(-10f, camera.Lens.Dutch, 1e-4f);
            Ok(M(Cam() + " | set Lens.ModeOverride = \"Orthographic\", Lens.OrthographicSize = 8"));
            Assert.AreEqual(LensSettings.OverrideModes.Orthographic, camera.Lens.ModeOverride);
            Assert.AreEqual(8f, camera.Lens.OrthographicSize, 1e-4f);
            Undo.PerformUndo();
            Assert.AreEqual(LensSettings.OverrideModes.None, camera.Lens.ModeOverride);
            Assert.AreEqual(55f, camera.Lens.FieldOfView, 1e-4f);
        }

        [Test]
        public void PriorityIsWritable()
        {
            var camera = CreateCamera();
            Ok(M(Cam() + " | set Priority.Value = 12"));
            Assert.AreEqual(12, camera.Priority.Value);
            Assert.IsTrue(camera.Priority.Enabled, "setting Value enables the priority");
            Assert.AreEqual("12", Cell(Q(Cam() + " | select Priority.Value")));
        }

        [Test]
        public void PipelineComponentMembersAreWrittenThroughTheCameraAndUndoable()
        {
            var camera = CreateCamera();
            var follow = camera.gameObject.AddComponent<CinemachineFollow>();
            Ok(M(Cam() + " | set body.FollowOffset = \"(0, 4, -12)\", body.TrackerSettings.PositionDamping = \"(2, 2, 2)\""));
            Assert.AreEqual(new Vector3(0, 4, -12), follow.FollowOffset);
            Assert.AreEqual(new Vector3(2, 2, 2), follow.TrackerSettings.PositionDamping);
            Undo.PerformUndo();
            Assert.AreEqual(new Vector3(0, 0, -10), follow.FollowOffset, "recorded on the CinemachineFollow, so undo restores it");
            Assert.AreEqual(Vector3.one, follow.TrackerSettings.PositionDamping);
        }

        [Test]
        public void ExtensionsListTheCameraExtensions()
        {
            var camera = CreateCamera();
            Ok(M("objects | where name == \"Cam\" | call AddComponent(\"CinemachineDeoccluder\")"));
            Assert.IsNotNull(camera.GetComponent<CinemachineDeoccluder>());
            StringAssert.Contains("CinemachineDeoccluder", Cell(Q(Cam() + " | select extensions.$type")));
        }

        [Test]
        public void IsLiveIsReadOnly()
        {
            CreateCamera();
            Assert.AreEqual("false", Cell(Q(Cam() + " | select IsLive")));
            StringAssert.Contains("readonly-member", Table(Fails(M(Cam() + " | set IsLive = true"))));
        }
    }
}
