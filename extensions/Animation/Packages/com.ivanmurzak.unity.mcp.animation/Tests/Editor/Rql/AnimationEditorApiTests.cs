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
    using AnimatorController = UnityEditor.Animations.AnimatorController;

    public class AnimationEditorApiTests : AnimationTestBase
    {
        [Test]
        public void CreateClipWithDefaults()
        {
            var r = Ok("animation | call CreateClip(\"" + Folder + "/New/Run.anim\")");
            StringAssert.Contains("AnimationClip#", Table(r));
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/New/Run.anim");
            Assert.IsNotNull(clip);
            Assert.AreEqual("Run", clip.name);
            Assert.AreEqual(60f, clip.frameRate);
            Assert.IsFalse(clip.isLooping);
        }

        [Test]
        public void CreateClipWithFrameRateAndLoop()
        {
            Ok("animation | call CreateClip(\"" + Folder + "/Idle.anim\", 24, true)");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/Idle.anim");
            Assert.AreEqual(24f, clip.frameRate);
            Assert.IsTrue(AnimationUtility.GetAnimationClipSettings(clip).loopTime);
        }

        [Test]
        public void CreateClipRejectsBadPaths()
        {
            Fails("animation | call CreateClip(\"Temp/Run.anim\")", "must start with Assets/");
            Fails("animation | call CreateClip(\"" + Folder + "/Run.controller\")", "must end with .anim");
            Fails("animation | call CreateClip(\"" + ClipPath + "\")", "already exists");
            Fails("animation | call CreateClip(\"" + Folder + "/Zero.anim\", 0)", "frameRate");
        }

        [Test]
        public void CreateController()
        {
            Ok("animation | call CreateController(\"" + Folder + "/Enemy.controller\")");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "/Enemy.controller");
            Assert.IsNotNull(controller);
            Assert.AreEqual(1, controller.layers.Length);
            Assert.AreEqual(0, controller.layers[0].stateMachine.states.Length);
        }

        [Test]
        public void CreateControllerWithAMotionAddsTheDefaultState()
        {
            Ok("animation | call CreateController(\"" + Folder + "/Walker.controller\", " + W + ")");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "/Walker.controller");
            var sm = controller.layers[0].stateMachine;
            Assert.AreEqual(1, sm.states.Length);
            Assert.AreEqual(Clip, sm.defaultState.motion);
        }

        [Test]
        public void CreateControllerRejectsBadPaths()
        {
            Fails("animation | call CreateController(\"" + Folder + "/X.anim\")", "must end with .controller");
            Fails("animation | call CreateController(\"" + ControllerPath + "\")", "already exists");
        }

        [Test]
        public void AnimatorsListsEveryAnimator()
        {
            new GameObject("A").AddComponent<Animator>();
            var hidden = new GameObject("B");
            hidden.AddComponent<Animator>();
            hidden.SetActive(false);
            new GameObject("C");
            Assert.AreEqual(2L, Count("animation | get animators | count"));
            StringAssert.Contains("B", Table(Ok(Q("animation | get animators | select gameObject.name"))));
        }

        [Test]
        public void CreatedAssetsAreFoundByUnityAssets()
        {
            Ok("animation | call CreateClip(\"" + Folder + "/Jump.anim\")");
            var names = Table(Ok(Q("unity.assets(\"t:AnimationClip\", \"" + Folder + "\") | select name")));
            StringAssert.Contains("Jump", names);
            StringAssert.Contains("Walk", names);
        }
    }
}
