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
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters;
using Rql.Execution;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Splines
{
    /// <summary>
    /// SplineContainer: the raw spline storage (Splines / Spline / m_Splines / m_Knots — knot structs that would be written
    /// to copies) is replaced by the <c>splines</c> view list, and the structural operations are methods. Every knot method
    /// takes an optional leading spline index: AddKnot(position) works on spline 0, AddKnot(1, position) on spline 1.
    /// The container is the call owner, so the engine records it for Undo and marks the scene dirty.
    /// </summary>
    public sealed class UnityEngine_Splines_SplineContainer_Adapter : RqlTypeAdapter<SplineContainer>
    {
        // Raw members (m_Spline is [Obsolete] but serialized, so the generic [Obsolete] filter misses it) that expose Spline objects (lists of BezierKnot copies) or the serialized storage directly.
        private static readonly string[] Raw = { "Splines", "Spline", "m_Splines", "m_Spline", "m_Knots" };

        public override string Description =>
            "SplineContainer: splines[i] (closed, knotCount, length, knots[j] with writable position / tangents / tangentMode); " +
            "call AddSpline / RemoveSpline / AddKnot / AddKnots / InsertKnot / RemoveKnot / Evaluate (optional leading splineIndex; positions are local)";

        protected override void Members(MemberTable<SplineContainer> m)
        {
            var t = m.Untyped;
            t.HideExisting(Raw);
            m.Add("splines", c => SplineView.All(c), description: "the container's splines (views with writable closed and knots)");

            Add(t, "AddSpline", typeof(int), "append an empty spline; returns its index",
                (c, a) => AddSpline(c, (bool)a[0]!), Opt("closed", typeof(bool), false));
            Add(t, "RemoveSpline", typeof(void), "remove the spline at splineIndex (knot links are updated)",
                (c, a) => { RemoveSpline(c, (int)a[0]!); return null; }, Req("splineIndex", typeof(int)));

            Add(t, "AddKnot", typeof(int), "append an AutoSmooth knot at a local position to spline 0; returns the knot index",
                (c, a) => AddKnot(c, 0, (Vector3)a[0]!), Req("position", typeof(Vector3)));
            Add(t, "AddKnot", typeof(int), "append an AutoSmooth knot at a local position to spline splineIndex; returns the knot index",
                (c, a) => AddKnot(c, (int)a[0]!, (Vector3)a[1]!), Req("splineIndex", typeof(int)), Req("position", typeof(Vector3)));

            Add(t, "AddKnots", typeof(int), "append AutoSmooth knots at local positions to spline 0; returns the knot count",
                (c, a) => AddKnots(c, 0, (List<Vector3>)a[0]!), Req("positions", typeof(List<Vector3>)));
            Add(t, "AddKnots", typeof(int), "append AutoSmooth knots at local positions to spline splineIndex; returns the knot count",
                (c, a) => AddKnots(c, (int)a[0]!, (List<Vector3>)a[1]!), Req("splineIndex", typeof(int)), Req("positions", typeof(List<Vector3>)));

            Add(t, "InsertKnot", typeof(int), "insert an AutoSmooth knot at knotIndex (0..knotCount) of spline 0; returns knotIndex",
                (c, a) => InsertKnot(c, 0, (int)a[0]!, (Vector3)a[1]!), Req("knotIndex", typeof(int)), Req("position", typeof(Vector3)));
            Add(t, "InsertKnot", typeof(int), "insert an AutoSmooth knot at knotIndex (0..knotCount) of spline splineIndex; returns knotIndex",
                (c, a) => InsertKnot(c, (int)a[0]!, (int)a[1]!, (Vector3)a[2]!), Req("splineIndex", typeof(int)), Req("knotIndex", typeof(int)), Req("position", typeof(Vector3)));

            Add(t, "RemoveKnot", typeof(int), "remove the knot at knotIndex of spline 0; returns the remaining knot count",
                (c, a) => RemoveKnot(c, 0, (int)a[0]!), Req("knotIndex", typeof(int)));
            Add(t, "RemoveKnot", typeof(int), "remove the knot at knotIndex of spline splineIndex; returns the remaining knot count",
                (c, a) => RemoveKnot(c, (int)a[0]!, (int)a[1]!), Req("splineIndex", typeof(int)), Req("knotIndex", typeof(int)));

            Add(t, "Evaluate", typeof(SplineSample), "the point at t (0..1) on spline 0: position (world), localPosition, tangent, up",
                (c, a) => Evaluate(c, 0, (float)a[0]!), Req("t", typeof(float)));
            Add(t, "Evaluate", typeof(SplineSample), "the point at t (0..1) on spline splineIndex: position (world), localPosition, tangent, up",
                (c, a) => Evaluate(c, (int)a[0]!, (float)a[1]!), Req("splineIndex", typeof(int)), Req("t", typeof(float)));
        }

        // ------------------------------------------------------------ operations (the call stage has recorded the container for Undo)

        internal static int AddSpline(SplineContainer container, bool closed)
        {
            container.AddSpline(new Spline { Closed = closed });
            return container.Splines.Count - 1;
        }

        internal static void RemoveSpline(SplineContainer container, int splineIndex)
        {
            SplineEdit.GetSpline(container, splineIndex);
            container.RemoveSplineAt(splineIndex);
        }

        internal static int AddKnot(SplineContainer container, int splineIndex, Vector3 position)
        {
            var spline = SplineEdit.GetSpline(container, splineIndex);
            spline.Add((float3)position, TangentMode.AutoSmooth);
            return spline.Count - 1;
        }

        internal static int AddKnots(SplineContainer container, int splineIndex, List<Vector3> positions)
        {
            var spline = SplineEdit.GetSpline(container, splineIndex);
            spline.AddRange(positions.Select(p => (float3)p), TangentMode.AutoSmooth);
            return spline.Count;
        }

        internal static int InsertKnot(SplineContainer container, int splineIndex, int knotIndex, Vector3 position)
        {
            var spline = SplineEdit.GetSpline(container, splineIndex);
            SplineEdit.CheckKnot(spline, knotIndex, allowEnd: true);
            spline.Insert(knotIndex, (float3)position, TangentMode.AutoSmooth);
            return knotIndex;
        }

        internal static int RemoveKnot(SplineContainer container, int splineIndex, int knotIndex)
        {
            var spline = SplineEdit.GetSpline(container, splineIndex);
            SplineEdit.CheckKnot(spline, knotIndex);
            spline.RemoveAt(knotIndex);
            return spline.Count;
        }

        internal static SplineSample Evaluate(SplineContainer container, int splineIndex, float t)
        {
            var spline = SplineEdit.GetSpline(container, splineIndex);
            if (spline.Count < 2) throw new InvalidOperationException($"spline {splineIndex} needs at least 2 knots to be evaluated (it has {spline.Count})");
            t = Mathf.Clamp01(t);
            container.Evaluate(splineIndex, t, out var position, out var tangent, out var up);
            return new SplineSample(t, position, spline.EvaluatePosition(t), tangent, up);
        }

        // ------------------------------------------------------------ registration helpers

        private static CallableParameter Req(string name, Type type) => new CallableParameter(name, type);

        private static CallableParameter Opt(string name, Type type, object? value) => new CallableParameter(name, type, true, value);

        private static void Add(MemberTable table, string name, Type returns, string description,
            Func<SplineContainer, object?[], object?> body, params CallableParameter[] parameters) =>
            table.AddMethod(name, parameters, returns, (o, a) => body((SplineContainer)o, a), description);
    }
}
