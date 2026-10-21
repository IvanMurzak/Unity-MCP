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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Animation
{
    /// <summary>
    /// One key of a clip curve (clip.curves[i].keys[j]). Keyframe is a struct inside a curve copy, so a plain reflected value
    /// would lose writes; every setter moves the key through AnimationCurve.MoveKey and stores the curve back (undoable).
    /// </summary>
    [RqlDescription("Curve keyframe: time (seconds) value inTangent outTangent inWeight outWeight weightedMode, all writable (undoable)")]
    public sealed class KeyframeView
    {
        private readonly AnimationCurveView _curve;

        internal KeyframeView(AnimationCurveView curve, int index)
        {
            _curve = curve;
            this.index = index;
        }

        [RqlDescription("index of the key inside the curve (keys are in time order)")]
        public int index { get; }

        private Keyframe Key
        {
            get
            {
                var curve = _curve.Curve;
                AnimationCurveView.CheckKey(curve, index);
                return curve[index];
            }
        }

        [RqlDescription("time in seconds; moving a key past a neighbour reorders the keys")]
        public float time { get => Key.time; set => Write(k => { k.time = value; return k; }); }

        [RqlDescription("value at the key")]
        public float value { get => Key.value; set => Write(k => { k.value = value; return k; }); }

        [RqlDescription("incoming tangent (slope)")]
        public float inTangent { get => Key.inTangent; set => Write(k => { k.inTangent = value; return k; }); }

        [RqlDescription("outgoing tangent (slope)")]
        public float outTangent { get => Key.outTangent; set => Write(k => { k.outTangent = value; return k; }); }

        [RqlDescription("incoming tangent weight (used when weightedMode is In or Both)")]
        public float inWeight { get => Key.inWeight; set => Write(k => { k.inWeight = value; return k; }); }

        [RqlDescription("outgoing tangent weight (used when weightedMode is Out or Both)")]
        public float outWeight { get => Key.outWeight; set => Write(k => { k.outWeight = value; return k; }); }

        [RqlDescription("None, In, Out or Both")]
        public WeightedMode weightedMode { get => Key.weightedMode; set => Write(k => { k.weightedMode = value; return k; }); }

        private void Write(Func<Keyframe, Keyframe> change) =>
            _curve.Change("set key", c =>
            {
                AnimationCurveView.CheckKey(c, index);
                if (c.MoveKey(index, change(c[index])) < 0)
                    throw new InvalidOperationException($"another key of {_curve} already sits at that time");
            });

        public override string ToString() => $"key {index} of {_curve}";
    }
}
