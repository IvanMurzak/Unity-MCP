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

    public class QuaternionConverterTests : BaseTest
    {
        #region Quaternion

        [UnityTest]
        public IEnumerator Quaternion_Identity()
        {
            TestUtils.ValidateType(Quaternion.identity);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Quaternion_Euler()
        {
            TestUtils.ValidateType(Quaternion.Euler(45, 90, 180));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Quaternion_AngleAxis()
        {
            TestUtils.ValidateType(Quaternion.AngleAxis(30, Vector3.up));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Quaternion_CustomValues()
        {
            // Note: Quaternions are usually normalized, but the struct can hold arbitrary values
            TestUtils.ValidateType(new Quaternion(0.1f, 0.2f, 0.3f, 0.4f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Quaternion_NegativeValues()
        {
            TestUtils.ValidateType(new Quaternion(-0.5f, -0.6f, -0.7f, -0.8f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Quaternion_PositiveInfinity()
        {
            TestUtils.ValidateType(new Quaternion(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Quaternion_NegativeInfinity()
        {
            TestUtils.ValidateType(new Quaternion(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Quaternion_NaN()
        {
            TestUtils.ValidateType(new Quaternion(float.NaN, float.NaN, float.NaN, float.NaN));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Quaternion_MixedValues()
        {
            TestUtils.ValidateType(new Quaternion(float.PositiveInfinity, float.NaN, 0, 1));
            yield return null;
        }

        #endregion
    }
}
