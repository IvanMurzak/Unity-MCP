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
using System.Collections.Generic;
using System.Linq;
using Rql;
using UnityEngine.Splines;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Splines
{
    /// <summary>
    /// One spline of a SplineContainer (container.splines[i]). Spline itself enumerates its knots, so generic reflection
    /// would see a read-only list of BezierKnot copies without Closed; this view exposes the spline as an object whose
    /// writes go through the Spline API and are undoable.
    /// </summary>
    [RqlDescription("Spline of a SplineContainer: closed (writable), knotCount, length (world units), knots (writable knot views)")]
    public sealed class SplineView
    {
        internal SplineView(SplineContainer container, int index)
        {
            this.container = container;
            this.index = index;
        }

        internal static List<SplineView> All(SplineContainer container) =>
            Enumerable.Range(0, container.Splines.Count).Select(i => new SplineView(container, i)).ToList();

        [RqlReference]
        [RqlDescription("the SplineContainer that owns the spline")]
        public SplineContainer container { get; }

        [RqlDescription("index of the spline inside the container")]
        public int index { get; }

        private Spline Spline => SplineEdit.GetSpline(container, index);

        [RqlDescription("closed loop (the last knot connects to the first)")]
        public bool closed
        {
            get => Spline.Closed;
            set
            {
                var spline = Spline;
                SplineEdit.Edit(container, "set spline closed", () => spline.Closed = value);
            }
        }

        [RqlDescription("number of knots")]
        public int knotCount => Spline.Count;

        [RqlDescription("length in world units")]
        public float length => container.CalculateLength(index);

        [RqlDescription("the knots, in order; their members are writable")]
        public List<KnotView> knots => Enumerable.Range(0, Spline.Count).Select(i => new KnotView(container, index, i)).ToList();

        public override string ToString() => $"spline {index}";
    }
}
