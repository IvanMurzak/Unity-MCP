/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Splines)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using Rql;
using UnityEngine;
using UnityEngine.Splines;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Splines
{
    /// <summary>
    /// One knot of a spline, addressed by container, spline index and knot index. BezierKnot is a struct stored inside the
    /// spline, so a plain reflected copy would lose writes; every setter here writes the knot back through the Spline API
    /// (which keeps the spline's caches and tangent modes consistent) and is undoable.
    /// </summary>
    [RqlDescription("Spline knot (local space of its SplineContainer): position worldPosition tangentIn tangentOut eulerAngles " +
                    "tangentMode tension are writable; writes are undoable")]
    public sealed class KnotView
    {
        internal KnotView(SplineContainer container, int splineIndex, int index)
        {
            this.container = container;
            this.splineIndex = splineIndex;
            this.index = index;
        }

        [RqlReference]
        [RqlDescription("the SplineContainer that owns the knot")]
        public SplineContainer container { get; }

        [RqlDescription("index of the spline inside the container")]
        public int splineIndex { get; }

        [RqlDescription("index of the knot inside the spline")]
        public int index { get; }

        private Spline Spline
        {
            get
            {
                var spline = SplineEdit.GetSpline(container, splineIndex);
                SplineEdit.CheckKnot(spline, index);
                return spline;
            }
        }

        private BezierKnot Knot => Spline[index];

        [RqlDescription("position in the container's local space")]
        public Vector3 position
        {
            get => Knot.Position;
            set => Write(k => { k.Position = value; return k; }, BezierTangent.Out);
        }

        [RqlDescription("position in world space")]
        public Vector3 worldPosition
        {
            get => container.transform.TransformPoint(position);
            set => position = container.transform.InverseTransformPoint(value);
        }

        [RqlDescription("in tangent, relative to the knot; writing it on an AutoSmooth / Linear knot switches the knot to Broken")]
        public Vector3 tangentIn
        {
            get => Knot.TangentIn;
            set => WriteTangent(k => { k.TangentIn = value; return k; }, BezierTangent.In);
        }

        [RqlDescription("out tangent, relative to the knot; writing it on an AutoSmooth / Linear knot switches the knot to Broken")]
        public Vector3 tangentOut
        {
            get => Knot.TangentOut;
            set => WriteTangent(k => { k.TangentOut = value; return k; }, BezierTangent.Out);
        }

        [RqlDescription("knot rotation as euler angles (degrees)")]
        public Vector3 eulerAngles
        {
            get => ((Quaternion)Knot.Rotation).eulerAngles;
            set => Write(k => { k.Rotation = Quaternion.Euler(value); return k; }, BezierTangent.Out);
        }

        [RqlDescription("AutoSmooth, Linear, Mirrored, Continuous or Broken")]
        public TangentMode tangentMode
        {
            get => Spline.GetTangentMode(index);
            set
            {
                var spline = Spline;
                SplineEdit.Edit(container, "set tangent mode", () => spline.SetTangentMode(index, value));
            }
        }

        [RqlDescription("AutoSmooth tension (0..1, default 1/3); used by AutoSmooth knots")]
        public float tension
        {
            get => Spline.GetAutoSmoothTension(index);
            set
            {
                var spline = Spline;
                SplineEdit.Edit(container, "set knot tension", () => spline.SetAutoSmoothTension(index, value));
            }
        }

        private void Write(Func<BezierKnot, BezierKnot> change, BezierTangent main)
        {
            var spline = Spline;
            SplineEdit.Edit(container, "set knot", () => spline.SetKnot(index, change(spline[index]), main));
        }

        // AutoSmooth and Linear knots compute their tangents: an explicit tangent would be overwritten at once, so the knot
        // becomes Broken first (what the Spline editor does when a computed tangent is dragged) and the written value sticks.
        private void WriteTangent(Func<BezierKnot, BezierKnot> change, BezierTangent main)
        {
            var spline = Spline;
            SplineEdit.Edit(container, "set knot tangent", () =>
            {
                var mode = spline.GetTangentMode(index);
                if (mode == TangentMode.AutoSmooth || mode == TangentMode.Linear) spline.SetTangentModeNoNotify(index, TangentMode.Broken, main);
                spline.SetKnot(index, change(spline[index]), main);
            });
        }

        public override string ToString() => $"knot {index} of spline {splineIndex}";
    }
}
