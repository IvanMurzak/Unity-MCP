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
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    public class UnityEngine_Animator_AdapterTests : AnimationTestBase
    {
        private const string A = "objects | get components | where $ is Animator";

        private Animator Animator = null!;

        [SetUp]
        public void AddAnimator() => Animator = new GameObject("Hero").AddComponent<Animator>();

        [Test]
        public void ReadingAWholeAnimatorLogsNothing()
        {
            Ok(Q(A));
            Animator.runtimeAnimatorController = Controller;
            Ok(Q(A));
            Ok(Q("objects | where name == \"Hero\" | get components"));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnlyConfigurationMembersAreExposed()
        {
            var described = Table(Ok(Q(A + " | describe")));
            StringAssert.Contains("runtimeAnimatorController", described);
            StringAssert.Contains("applyRootMotion", described);
            StringAssert.DoesNotContain("playbackTime", described);
            StringAssert.DoesNotContain("bodyPosition", described);
        }

        [Test]
        public void AssignTheController()
        {
            Ok(A + " | set runtimeAnimatorController = " + P + ", applyRootMotion = true, cullingMode = \"CullCompletely\", speed = 2");
            Assert.AreEqual(Controller, Animator.runtimeAnimatorController);
            Assert.IsTrue(Animator.applyRootMotion);
            Assert.AreEqual(AnimatorCullingMode.CullCompletely, Animator.cullingMode);
            Assert.AreEqual(2f, Animator.speed, 1e-5f);
            StringAssert.Contains("Player", Table(Ok(Q("animation | get animators | select runtimeAnimatorController.name"))));
        }

        [Test]
        public void PlayModeMethodsFailInEditMode()
        {
            Animator.runtimeAnimatorController = Controller;
            Fails(A + " | call SetParameter(\"Speed\", 1)", "not playing");
            Fails(A + " | call Play(\"Walk\")", "not playing");
        }
    }
}
