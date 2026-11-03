/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using Rql.Execution;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Lighting
{
    /// <summary>
    /// LightProbeGroup: <c>PlaceGrid(size, spacing)</c> fills a box with evenly spaced probes (the usual first layout, which
    /// the Inspector can only do by hand). <c>probePositions</c> (local space) is a copy, so element writes are assigned back.
    /// </summary>
    public sealed class UnityEngine_LightProbeGroup_Adapter : RqlTypeAdapter<LightProbeGroup>
    {
        /// <summary>Most probes <see cref="PlaceGrid"/> creates in one group: beyond that a bake slows down for little gain.</summary>
        public const int MaxGridProbes = 10000;

        public override string Description =>
            "LightProbeGroup: call PlaceGrid(\"(x, y, z)\", spacing) to fill a box around the group with probes";

        protected override void Members(MemberTable<LightProbeGroup> m) => m
            .CopyOnRead("probePositions")
            .Function<Vector3, float, int>("PlaceGrid", (g, size, spacing) => PlaceGrid(g, size, spacing), "size", "spacing",
                "replace the probes with a grid filling a box of this size (meters, centered on the group) every `spacing` meters; returns the count");

        /// <summary>
        /// Probes every <paramref name="spacing"/> meters inside a box of <paramref name="size"/> centered on the group, in the
        /// group's local space. An axis of size 0 gets a single layer. The box edges always get probes, so the grid spacing
        /// is at most <paramref name="spacing"/>.
        /// </summary>
        public static int PlaceGrid(LightProbeGroup group, Vector3 size, float spacing)
        {
            if (!(spacing > 0f) || float.IsInfinity(spacing)) throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "must be > 0");
            if (!(size.x >= 0f && size.y >= 0f && size.z >= 0f) || float.IsInfinity(size.x) || float.IsInfinity(size.y) || float.IsInfinity(size.z))
                throw new ArgumentOutOfRangeException(nameof(size), size, "must be finite and not negative");

            // Counted in double: a huge box over a tiny spacing must be rejected, not overflow into a small int.
            double nx = Steps(size.x, spacing), ny = Steps(size.y, spacing), nz = Steps(size.z, spacing);
            var total = nx * ny * nz;
            if (total > MaxGridProbes)
                throw new ArgumentOutOfRangeException(nameof(spacing), spacing,
                    $"a {size.x} x {size.y} x {size.z} box every {spacing} m needs {total:0} probes, more than {MaxGridProbes}; use a larger spacing");
            var counts = new Vector3Int((int)nx, (int)ny, (int)nz);

            var positions = new List<Vector3>(counts.x * counts.y * counts.z);
            for (int x = 0; x < counts.x; x++)
                for (int y = 0; y < counts.y; y++)
                    for (int z = 0; z < counts.z; z++)
                        positions.Add(new Vector3(Coordinate(size.x, counts.x, x), Coordinate(size.y, counts.y, y), Coordinate(size.z, counts.z, z)));
            group.probePositions = positions.ToArray();
            return positions.Count;
        }

        // Probes along one axis: both ends, plus as many between as the spacing needs.
        private static double Steps(float length, float spacing) =>
            length <= 0f ? 1 : Math.Ceiling((double)length / spacing - 1e-4) + 1;

        private static float Coordinate(float length, int count, int index) =>
            count == 1 ? 0f : -length / 2f + length * index / (count - 1);
    }
}
