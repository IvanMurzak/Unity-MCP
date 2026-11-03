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
using System.Linq;
using Rql;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    /// <summary>
    /// One object-reference curve of an AnimationClip (e.g. a sprite swap: SpriteRenderer.m_Sprite), clip.objectCurves[i]. Keys
    /// are replaced whole with AnimationClip.SetObjectCurve(path, type, property, keys) or SetKeys.
    /// </summary>
    [RqlDescription("Object-reference curve of an AnimationClip (sprite swaps …): path type property keyCount keys ({time, value}); " +
                    "call SetKeys([{time, value}, …]), Remove()")]
    public sealed class ObjectCurveView
    {
        private readonly EditorCurveBinding _binding;

        internal ObjectCurveView(AnimationClip clip, EditorCurveBinding binding)
        {
            this.clip = clip;
            _binding = binding;
        }

        internal static List<ObjectCurveView> All(AnimationClip clip) =>
            AnimationUtility.GetObjectReferenceCurveBindings(clip).Select(b => new ObjectCurveView(clip, b)).ToList();

        [RqlReference]
        [RqlDescription("the AnimationClip that owns the curve")]
        public AnimationClip clip { get; }

        [RqlDescription("transform path of the animated object relative to the Animator (\"\" = the root)")]
        public string path => _binding.path;

        [RqlDescription("animated component type, e.g. SpriteRenderer")]
        public string type => _binding.type.Name;

        [RqlDescription("animated property, e.g. m_Sprite")]
        public string property => _binding.propertyName;

        [RqlDescription("number of keyframes")]
        public int keyCount => Keys.Length;

        [RqlDescription("the keyframes: time (seconds) and value (the referenced object)")]
        public List<ObjectReferenceKeyframe> keys => Keys.ToList();

        private ObjectReferenceKeyframe[] Keys => AnimationUtility.GetObjectReferenceCurve(clip, _binding) ?? Array.Empty<ObjectReferenceKeyframe>();

        [RqlCallable("replace every key: [{time: 0, value: unity.asset(\"….png\")}, …]; returns the key count")]
        public int SetKeys(List<ObjectReferenceKeyframe> keys)
        {
            AnimationEdit.Edit(clip, "set object curve", () => AnimationUtility.SetObjectReferenceCurve(clip, _binding, keys.ToArray()));
            return keys.Count;
        }

        [RqlCallable("remove this curve from the clip")]
        public void Remove() => AnimationEdit.Edit(clip, "remove object curve", () => AnimationUtility.SetObjectReferenceCurve(clip, _binding, null));

        public override string ToString() => $"{(path.Length == 0 ? "" : path + ":")}{type}.{property}";
    }
}
