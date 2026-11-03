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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.Rql.Animation
{
    public class AnimationClipSettingsViewTests : AnimationTestBase
    {
        [Test]
        public void SettingsAreReadable()
        {
            var r = Table(Ok(Q(W + " | select settings.loopTime settings.mirror settings.stopTime")));
            StringAssert.Contains("false", r);
        }

        [Test]
        public void LoopTimeDrivesIsLooping()
        {
            Ok(W + " | set settings.loopTime = true, settings.loopBlend = true, settings.cycleOffset = 0.25");
            var s = AnimationUtility.GetAnimationClipSettings(Clip);
            Assert.IsTrue(s.loopTime);
            Assert.IsTrue(s.loopBlend);
            Assert.AreEqual(0.25f, s.cycleOffset, 1e-5f);
            Assert.IsTrue(Clip.isLooping);
        }

        [Test]
        public void RootMotionFlags()
        {
            Ok(W + " | set settings.loopBlendOrientation = true, settings.keepOriginalPositionY = true, settings.orientationOffsetY = 45");
            var s = AnimationUtility.GetAnimationClipSettings(Clip);
            Assert.IsTrue(s.loopBlendOrientation);
            Assert.IsTrue(s.keepOriginalPositionY);
            Assert.AreEqual(45f, s.orientationOffsetY, 1e-5f);
        }

        [Test]
        public void SettingsWritesAreUndoable()
        {
            Undo.ClearAll();
            Ok(W + " | set settings.loopTime = true");
            Undo.PerformUndo();
            Assert.IsFalse(AnimationUtility.GetAnimationClipSettings(Clip).loopTime);
        }
    }
}
