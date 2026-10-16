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
    /// One float curve of an AnimationClip (clip.curves[i]), addressed by its binding (path, type, property). Unity hands out
    /// curve copies (AnimationUtility.GetEditorCurve), so every write here changes a copy and stores it back with
    /// SetEditorCurve; writes are undoable.
    /// </summary>
    [RqlDescription("Float curve of an AnimationClip: path (child path, \"\" = root) type property (binding), keyCount, keys (writable " +
                    "keyframe views), preWrapMode / postWrapMode; call AddKey(time, value), RemoveKey(index), SetKeys(keys), Evaluate(time), Remove()")]
    public sealed class AnimationCurveView
    {
        private readonly EditorCurveBinding _binding;

        internal AnimationCurveView(AnimationClip clip, EditorCurveBinding binding)
        {
            this.clip = clip;
            _binding = binding;
        }

        internal static List<AnimationCurveView> All(AnimationClip clip) =>
            AnimationUtility.GetCurveBindings(clip).Select(b => new AnimationCurveView(clip, b)).ToList();

        [RqlReference]
        [RqlDescription("the AnimationClip that owns the curve")]
        public AnimationClip clip { get; }

        [RqlDescription("transform path of the animated object relative to the Animator (\"\" = the root, \"Body/Arm\" = a child)")]
        public string path => _binding.path;

        [RqlDescription("animated component type, e.g. Transform, SpriteRenderer, GameObject")]
        public string type => _binding.type.Name;

        [RqlDescription("animated property, e.g. localPosition.x, m_Color.a, m_IsActive")]
        public string property => _binding.propertyName;

        internal AnimationCurve Curve =>
            AnimationUtility.GetEditorCurve(clip, _binding)
            ?? throw new InvalidOperationException($"the curve {this} no longer exists on {clip.name}");

        [RqlDescription("number of keyframes")]
        public int keyCount => Curve.length;

        [RqlDescription("first to last key time in seconds")]
        public Vector2 timeRange
        {
            get
            {
                var keys = Curve.keys;
                return keys.Length == 0 ? Vector2.zero : new Vector2(keys[0].time, keys[keys.Length - 1].time);
            }
        }

        [RqlDescription("the keyframes in time order; time value inTangent outTangent inWeight outWeight weightedMode are writable")]
        public List<KeyframeView> keys => Enumerable.Range(0, Curve.length).Select(i => new KeyframeView(this, i)).ToList();

        [RqlDescription("how the curve continues before its first key: Clamp / ClampForever, Loop, PingPong")]
        public WrapMode preWrapMode
        {
            get => Curve.preWrapMode;
            set => Change("set curve preWrapMode", c => c.preWrapMode = value);
        }

        [RqlDescription("how the curve continues after its last key: Clamp / ClampForever, Loop, PingPong")]
        public WrapMode postWrapMode
        {
            get => Curve.postWrapMode;
            set => Change("set curve postWrapMode", c => c.postWrapMode = value);
        }

        [RqlCallable("add a key at time (seconds) with value (flat tangents); an existing key at that time is replaced; returns the key index")]
        public int AddKey(float time, float value)
        {
            int index = -1;
            Change("add key", c =>
            {
                var existing = Array.FindIndex(c.keys, k => Mathf.Approximately(k.time, time));
                if (existing >= 0) c.RemoveKey(existing);
                index = c.AddKey(new Keyframe(time, value));
            });
            return index;
        }

        [RqlCallable("remove the key at index; returns the remaining key count")]
        public int RemoveKey(int index)
        {
            int count = 0;
            Change("remove key", c =>
            {
                CheckKey(c, index);
                c.RemoveKey(index);
                count = c.length;
            });
            return count;
        }

        [RqlCallable("replace every key: [[time, value], …], [[time, value, inTangent, outTangent], …] or [{time, value, …}, …]; returns the key count")]
        public int SetKeys(List<Keyframe> keys)
        {
            Change("set keys", c => c.keys = keys.ToArray());
            return keys.Count;
        }

        [RqlCallable("the curve value at time (seconds)")]
        public float Evaluate(float time) => Curve.Evaluate(time);

        [RqlCallable("remove this curve from the clip")]
        public void Remove() => AnimationEdit.Edit(clip, "remove curve", () => AnimationUtility.SetEditorCurve(clip, _binding, null));

        internal void Change(string what, Action<AnimationCurve> change)
        {
            var curve = Curve;
            change(curve);
            AnimationEdit.Edit(clip, what, () => AnimationUtility.SetEditorCurve(clip, _binding, curve));
        }

        internal static void CheckKey(AnimationCurve curve, int index) => AnimationEdit.CheckIndex(index, curve.length, "key", "the curve");

        public override string ToString() => $"{(path.Length == 0 ? "" : path + ":")}{type}.{property}";
    }
}
