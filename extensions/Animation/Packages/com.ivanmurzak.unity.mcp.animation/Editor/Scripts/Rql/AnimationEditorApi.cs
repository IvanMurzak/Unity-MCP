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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rql;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    using AnimatorController = UnityEditor.Animations.AnimatorController;

    /// <summary>
    /// The <c>animation</c> source: the operations without an owner object (creating AnimationClip and AnimatorController assets)
    /// and the Animators of the loaded scenes, e.g. <c>animation | call CreateClip("Assets/Animations/Walk.anim")</c>. Everything
    /// else is a member or method of the clip / controller (<see cref="UnityEngine_AnimationClip_Adapter"/>,
    /// <see cref="UnityEditor_Animations_AnimatorController_Adapter"/>).
    /// </summary>
    [RqlDescription("Animation: animators (every Animator in the loaded scenes); CreateClip(path, frameRate, loop), CreateController(path, motion)")]
    public sealed class AnimationEditorApi
    {
        public static readonly AnimationEditorApi Instance = new AnimationEditorApi();

        [RqlDescription("every Animator component in the loaded scenes, inactive ones included")]
        public List<Animator> animators =>
            RqlUnity.RootGameObjects().SelectMany(root => root.GetComponentsInChildren<Animator>(true)).ToList();

        [RqlCallable("create an empty AnimationClip asset at path (\"Assets/…/Name.anim\", folders are created) with frameRate (default 60) " +
                     "and loop (Loop Time); returns the clip — then call SetCurve / AddEvent on unity.asset(path). Asset creation is not undone by Undo")]
        public AnimationClip CreateClip(string path, float frameRate = 60f, bool loop = false)
        {
            if (frameRate <= 0f) throw new ArgumentException($"frameRate {frameRate} must be positive");
            path = AnimationEdit.NewAssetPath(path, ".anim");
            var clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path), frameRate = frameRate };
            if (loop)
            {
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        [RqlCallable("create an AnimatorController asset at path (\"Assets/…/Name.controller\", folders are created) with a Base Layer; " +
                     "with a motion (unity.asset(\"….anim\")) the layer gets a default state playing it; returns the controller. " +
                     "Asset creation is not undone by Undo")]
        public AnimatorController CreateController(string path, Motion? motion = null)
        {
            path = AnimationEdit.NewAssetPath(path, ".controller");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            if (motion != null) controller.AddMotion(motion);
            return controller;
        }
    }
}
