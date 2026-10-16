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
using Rql.Execution;
using Unity.Mathematics;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Mathematics
{
    /// <summary>
    /// quaternion (Unity.Mathematics): compact "(x, y, z, w)" output and input like UnityEngine.Quaternion; three numbers
    /// "(x, y, z)" are euler angles in degrees, which is how rotations are usually written.
    /// </summary>
    public sealed class Unity_Mathematics_quaternion_Adapter : NumericTupleAdapter<quaternion>
    {
        public override string Description => "quaternion (Unity.Mathematics): (x, y, z, w); write euler degrees as \"(x, y, z)\" or a quaternion as \"(x, y, z, w)\"";
        protected override string Expected => "(x, y, z, w), [x, y, z, w] or euler degrees (x, y, z)";
        protected override int Width => 4;
        protected override float Part(quaternion q, int i) => q.value[i];

        protected override quaternion? Make(float[] f) => f.Length switch
        {
            4 => new quaternion(f[0], f[1], f[2], f[3]),
            3 => (quaternion)UnityEngine.Quaternion.Euler(f[0], f[1], f[2]),
            _ => (quaternion?)null,
        };
    }
}
