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
using Rql;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Splines
{
    /// <summary>The result of SplineContainer.Evaluate(t): a point on the spline.</summary>
    [RqlDescription("A point on a spline: t, position (world), localPosition, tangent and up (world)")]
    public sealed class SplineSample
    {
        public SplineSample(float t, Vector3 position, Vector3 localPosition, Vector3 tangent, Vector3 up)
        {
            this.t = t;
            this.position = position;
            this.localPosition = localPosition;
            this.tangent = tangent;
            this.up = up;
        }

        [RqlDescription("normalized position along the spline (0..1)")]
        public float t { get; }

        [RqlDescription("world-space position")]
        public Vector3 position { get; }

        [RqlDescription("position in the container's local space")]
        public Vector3 localPosition { get; }

        [RqlDescription("world-space tangent (direction of travel; not normalized)")]
        public Vector3 tangent { get; }

        [RqlDescription("world-space up vector")]
        public Vector3 up { get; }
    }
}
