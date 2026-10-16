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
using System.Linq;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Cinemachine
{
    /// <summary>The cinemachine source: cameras / brains / names, CreateCamera, EnsureBrain.</summary>
    public class CinemachineApiTests : CinemachineTestBase
    {
        [Test]
        public void CreateCameraWithDefaultsAddsABrainToANewMainCamera()
        {
            Assert.IsNull(Camera.main);
            StringAssert.Contains("CinemachineCamera#", Cell(M("cinemachine | call CreateCamera()"), 2));

            var camera = All<CinemachineCamera>().Single();
            Assert.AreEqual("CinemachineCamera", camera.name);
            Assert.AreEqual(Vector3.zero, camera.transform.position);
            Assert.IsNull(camera.Follow);
            Assert.AreEqual(0, camera.Priority.Value);
            Assert.IsNotNull(Camera.main, "a Main Camera was created");
            Assert.AreEqual("Main Camera", Camera.main!.name);
            Assert.IsNotNull(Camera.main.GetComponent<CinemachineBrain>());
        }

        [Test]
        public void CreateCameraTakesTargetsByNameOrRefAndTheTransform()
        {
            CreateTarget("Player");
            var boss = CreateTarget("Boss");
            Ok(M("cinemachine | call CreateCamera(\"Cam\", \"Player\", ref(\"" + Ref(boss) + "\"), \"(1, 2, 3)\", \"(10, 20, 0)\", 7)"));

            var camera = GameObject.Find("Cam").GetComponent<CinemachineCamera>();
            Assert.AreEqual("Player", camera.Follow.name);
            Assert.AreEqual("Boss", camera.LookAt.name);
            Assert.Less(Vector3.Distance(new Vector3(1, 2, 3), camera.transform.position), 1e-4f);
            Assert.Less(Quaternion.Angle(Quaternion.Euler(10, 20, 0), camera.transform.rotation), 1e-3f);
            Assert.AreEqual(7, camera.Priority.Value);
        }

        [Test]
        public void CreateCameraReusesTheExistingMainCamera()
        {
            var main = new GameObject("Cam Main") { tag = "MainCamera" }.AddComponent<Camera>();
            Ok(M("cinemachine | call CreateCamera(\"A\")"));
            Ok(M("cinemachine | call CreateCamera(\"B\")"));
            Assert.AreEqual(1, All<Camera>().Count);
            Assert.AreEqual(1, main.GetComponents<CinemachineBrain>().Length);
        }

        [Test]
        public void CreateCameraFailsOnAnUnknownTargetWithoutCreatingAnything()
        {
            var r = Fails(M("cinemachine | call CreateCamera(\"Cam\", \"Nobody\")"));
            StringAssert.Contains("no GameObject named 'Nobody'", Table(r));
            Assert.AreEqual(0, All<CinemachineCamera>().Count);
            Assert.IsNull(Camera.main);
        }

        [Test]
        public void CreateCameraIsUndoneInOneStep()
        {
            Ok(M("cinemachine | call CreateCamera(\"Cam\")"));
            Assert.IsNotNull(GameObject.Find("Cam"));
            Undo.PerformUndo();
            Assert.IsNull(GameObject.Find("Cam"));
            Assert.IsNull(GameObject.Find("Main Camera"), "the Main Camera created with it is undone too");
        }

        [Test]
        public void EnsureBrainIsIdempotent()
        {
            var main = new GameObject("Main") { tag = "MainCamera" }.AddComponent<Camera>();
            var first = Cell(M("cinemachine | call EnsureBrain()"), 2);
            var second = Cell(M("cinemachine | call EnsureBrain()"), 2);
            Assert.AreEqual(first, second);
            Assert.AreEqual(1, main.GetComponents<CinemachineBrain>().Length);
        }

        [Test]
        public void EnsureBrainTargetsANamedCamera()
        {
            new GameObject("Main") { tag = "MainCamera" }.AddComponent<Camera>();
            var side = new GameObject("Side").AddComponent<Camera>();
            Ok(M("cinemachine | call EnsureBrain(\"Side\")"));
            Assert.IsNotNull(side.GetComponent<CinemachineBrain>());
            Assert.IsNull(Camera.main!.GetComponent<CinemachineBrain>());

            CreateTarget("NoCamera");
            StringAssert.Contains("has no Camera", Table(Fails(M("cinemachine | call EnsureBrain(\"NoCamera\")"))));
        }

        [Test]
        public void CamerasAndBrainsListTheLoadedScenes()
        {
            CreateCamera("A");
            var inactive = CreateCamera("B");
            inactive.gameObject.SetActive(false);
            new GameObject("Main").AddComponent<Camera>().gameObject.AddComponent<CinemachineBrain>();

            Assert.AreEqual(2, Ok(Q(Cameras + " | count")).Value!.GetValue<long>());
            Assert.AreEqual("Main", Cell(Q("cinemachine | get brains | select gameObject.name")));
            Assert.AreEqual(1, Ok(Q(Cameras + " | where name == \"B\" and not isActiveAndEnabled | count")).Value!.GetValue<long>());
        }

        [Test]
        public void TheNameListsMatchWhatTheMethodsAccept()
        {
            var body = Table(Ok(Q("cinemachine | get bodyTypes")));
            foreach (var name in new[] { "Follow", "OrbitalFollow", "ThirdPersonFollow", "PositionComposer", "HardLockToTarget" })
                StringAssert.Contains(name, body);
            StringAssert.DoesNotContain("Transposer", body, "deprecated CM2 components are not offered");
            var aim = Table(Ok(Q("cinemachine | get aimTypes")));
            foreach (var name in new[] { "RotationComposer", "HardLookAt", "PanTilt" })
                StringAssert.Contains(name, aim);
            var extensions = Table(Ok(Q("cinemachine | get extensionTypes")));
            StringAssert.Contains("CinemachineDeoccluder", extensions);
            StringAssert.Contains("CinemachineConfiner3D", extensions);
        }

        [Test]
        public void NoiseProfilesListThePresets()
        {
            var profiles = Table(Ok(Q("cinemachine | get noiseProfiles | select name")));
            StringAssert.Contains("6D Shake", profiles);
            StringAssert.Contains("Handheld_normal_mild", profiles);
        }
    }
}
