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

    public class RectConverterTests : BaseTest
    {
        #region Rect

        [UnityTest]
        public IEnumerator Rect_Zero()
        {
            TestUtils.ValidateType(Rect.zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rect_Standard()
        {
            TestUtils.ValidateType(new Rect(10, 20, 100, 200));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rect_NegativeValues()
        {
            TestUtils.ValidateType(new Rect(-10, -20, -100, -200));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rect_LargeValues()
        {
            TestUtils.ValidateType(new Rect(float.MaxValue / 4, float.MinValue / 4, float.MaxValue / 2, float.MaxValue / 2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rect_PositiveInfinity()
        {
            TestUtils.ValidateType(new Rect(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rect_NegativeInfinity()
        {
            TestUtils.ValidateType(new Rect(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rect_NaN()
        {
            TestUtils.ValidateType(new Rect(float.NaN, float.NaN, float.NaN, float.NaN));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Rect_MixedValues()
        {
            TestUtils.ValidateType(new Rect(float.PositiveInfinity, float.NaN, 100, 200));
            yield return null;
        }

        #endregion

        #region RectInt

        [UnityTest]
        public IEnumerator RectInt_Zero()
        {
            TestUtils.ValidateType(new RectInt(0, 0, 0, 0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RectInt_Standard()
        {
            TestUtils.ValidateType(new RectInt(10, 20, 100, 200));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RectInt_NegativeValues()
        {
            TestUtils.ValidateType(new RectInt(-10, -20, -100, -200));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RectInt_LargeValues()
        {
            TestUtils.ValidateType(new RectInt(int.MaxValue / 4, int.MinValue / 4, int.MaxValue / 2, int.MaxValue / 2));
            yield return null;
        }

        #endregion
    }
}
