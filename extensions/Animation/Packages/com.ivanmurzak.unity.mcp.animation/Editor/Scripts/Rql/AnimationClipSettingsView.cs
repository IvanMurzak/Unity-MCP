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
using System;
using Rql;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    /// <summary>
    /// The clip's import-style settings (clip.settings: loop time, root motion bake flags, mirror …). Unity returns them as a
    /// copy (AnimationUtility.GetAnimationClipSettings); every setter writes the settings back (undoable).
    /// </summary>
    [RqlDescription("AnimationClip settings (Inspector 'Loop Time', root motion …): loopTime loopBlend cycleOffset mirror startTime stopTime " +
                    "keepOriginal* loopBlend* heightFromFeet level orientationOffsetY, all writable (undoable)")]
    public sealed class AnimationClipSettingsView
    {
        internal AnimationClipSettingsView(AnimationClip clip) { this.clip = clip; }

        [RqlReference]
        [RqlDescription("the AnimationClip")]
        public AnimationClip clip { get; }

        private AnimationClipSettings S => AnimationUtility.GetAnimationClipSettings(clip);

        private void Write(string what, Action<AnimationClipSettings> change)
        {
            var settings = S;
            change(settings);
            AnimationEdit.Edit(clip, "set clip " + what, () => AnimationUtility.SetAnimationClipSettings(clip, settings));
        }

        [RqlDescription("play the clip in a loop (the Inspector's Loop Time; clip.isLooping follows it)")]
        public bool loopTime { get => S.loopTime; set => Write("loopTime", s => s.loopTime = value); }

        [RqlDescription("Loop Pose: blend the end into the start for a seamless loop")]
        public bool loopBlend { get => S.loopBlend; set => Write("loopBlend", s => s.loopBlend = value); }

        [RqlDescription("cycle offset of the loop (normalized)")]
        public float cycleOffset { get => S.cycleOffset; set => Write("cycleOffset", s => s.cycleOffset = value); }

        [RqlDescription("mirror left / right (humanoid clips)")]
        public bool mirror { get => S.mirror; set => Write("mirror", s => s.mirror = value); }

        [RqlDescription("start time in seconds")]
        public float startTime { get => S.startTime; set => Write("startTime", s => s.startTime = value); }

        [RqlDescription("stop time in seconds")]
        public float stopTime { get => S.stopTime; set => Write("stopTime", s => s.stopTime = value); }

        [RqlDescription("root rotation: bake into pose")]
        public bool loopBlendOrientation { get => S.loopBlendOrientation; set => Write("loopBlendOrientation", s => s.loopBlendOrientation = value); }

        [RqlDescription("root position Y: bake into pose")]
        public bool loopBlendPositionY { get => S.loopBlendPositionY; set => Write("loopBlendPositionY", s => s.loopBlendPositionY = value); }

        [RqlDescription("root position XZ: bake into pose")]
        public bool loopBlendPositionXZ { get => S.loopBlendPositionXZ; set => Write("loopBlendPositionXZ", s => s.loopBlendPositionXZ = value); }

        [RqlDescription("root rotation based upon original")]
        public bool keepOriginalOrientation { get => S.keepOriginalOrientation; set => Write("keepOriginalOrientation", s => s.keepOriginalOrientation = value); }

        [RqlDescription("root position Y based upon original")]
        public bool keepOriginalPositionY { get => S.keepOriginalPositionY; set => Write("keepOriginalPositionY", s => s.keepOriginalPositionY = value); }

        [RqlDescription("root position XZ based upon original")]
        public bool keepOriginalPositionXZ { get => S.keepOriginalPositionXZ; set => Write("keepOriginalPositionXZ", s => s.keepOriginalPositionXZ = value); }

        [RqlDescription("root position Y based upon feet (humanoid)")]
        public bool heightFromFeet { get => S.heightFromFeet; set => Write("heightFromFeet", s => s.heightFromFeet = value); }

        [RqlDescription("root position Y offset")]
        public float level { get => S.level; set => Write("level", s => s.level = value); }

        [RqlDescription("root rotation offset in degrees")]
        public float orientationOffsetY { get => S.orientationOffsetY; set => Write("orientationOffsetY", s => s.orientationOffsetY = value); }

        public override string ToString() => $"settings of {clip.name}";
    }
}
