/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.JsonConverter
{
    using com.IvanMurzak.Unity.MCP.Editor.Tests;

    public class BoundsConverterTests : BaseTest
    {
        #region Bounds

        [UnityTest]
        public IEnumerator Bounds_Zero()
        {
            TestUtils.ValidateType(new Bounds(Vector3.zero, Vector3.zero));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bounds_Standard()
        {
            TestUtils.ValidateType(new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bounds_NegativeValues()
        {
            TestUtils.ValidateType(new Bounds(new Vector3(-1, -2, -3), new Vector3(10, 10, 10)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bounds_LargeValues()
        {
            Vector3 large = new Vector3(float.MaxValue / 2, float.MaxValue / 2, float.MaxValue / 2);
            TestUtils.ValidateType(new Bounds(Vector3.zero, large));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bounds_PositiveInfinity()
        {
            Vector3 inf = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            TestUtils.ValidateType(new Bounds(inf, inf));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bounds_NegativeInfinity()
        {
            Vector3 nInf = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            TestUtils.ValidateType(new Bounds(nInf, nInf));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bounds_NaN()
        {
            Vector3 nan = new Vector3(float.NaN, float.NaN, float.NaN);
            TestUtils.ValidateType(new Bounds(nan, nan));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bounds_MixedValues()
        {
            TestUtils.ValidateType(new Bounds(new Vector3(1, float.PositiveInfinity, float.NaN), new Vector3(float.NegativeInfinity, 0, 1)));
            yield return null;
        }

        #endregion

        #region BoundsInt

        [UnityTest]
        public IEnumerator BoundsInt_Zero()
        {
            TestUtils.ValidateType(new BoundsInt(Vector3Int.zero, Vector3Int.zero));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoundsInt_Standard()
        {
            TestUtils.ValidateType(new BoundsInt(new Vector3Int(1, 2, 3), new Vector3Int(4, 5, 6)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoundsInt_NegativeValues()
        {
            TestUtils.ValidateType(new BoundsInt(new Vector3Int(-5, -5, -5), new Vector3Int(10, 10, 10)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoundsInt_LargeValues()
        {
            Vector3Int large = new Vector3Int(int.MaxValue / 2, int.MaxValue / 2, int.MaxValue / 2);
            TestUtils.ValidateType(new BoundsInt(Vector3Int.zero, large));
            yield return null;
        }

        #endregion
    }
}
