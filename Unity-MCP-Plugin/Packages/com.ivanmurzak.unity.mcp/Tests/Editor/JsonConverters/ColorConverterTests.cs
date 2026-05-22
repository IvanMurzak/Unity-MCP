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

    public class ColorConverterTests : BaseTest
    {
        #region Color

        [UnityTest]
        public IEnumerator Color_White()
        {
            TestUtils.ValidateType(Color.white);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color_Black()
        {
            TestUtils.ValidateType(Color.black);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color_Clear()
        {
            TestUtils.ValidateType(Color.clear);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color_Custom()
        {
            TestUtils.ValidateType(new Color(0.1f, 0.2f, 0.3f, 0.4f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color_HDR_Values()
        {
            // HDR colors can be > 1
            TestUtils.ValidateType(new Color(2.0f, 5.5f, 10.0f, 1.0f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color_NegativeValues()
        {
            // Colors aren't usually negative but struct allows it
            TestUtils.ValidateType(new Color(-0.5f, -1.0f, -0.1f, -0.5f));
            yield return null;
        }


        [UnityTest]
        public IEnumerator Color_PositiveInfinity()
        {
            TestUtils.ValidateType(new Color(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color_NegativeInfinity()
        {
            TestUtils.ValidateType(new Color(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color_NaN()
        {
            TestUtils.ValidateType(new Color(float.NaN, float.NaN, float.NaN, float.NaN));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color_MixedValues()
        {
            TestUtils.ValidateType(new Color(float.PositiveInfinity, float.NaN, 0.5f, 1.0f));
            yield return null;
        }

        #endregion

        #region Color32

        [UnityTest]
        public IEnumerator Color32_White()
        {
            TestUtils.ValidateType(new Color32(255, 255, 255, 255));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color32_Black()
        {
            TestUtils.ValidateType(new Color32(0, 0, 0, 255));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color32_Transparent()
        {
            TestUtils.ValidateType(new Color32(0, 0, 0, 0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Color32_Custom()
        {
            TestUtils.ValidateType(new Color32(100, 50, 200, 128));
            yield return null;
        }

        #endregion
    }
}
