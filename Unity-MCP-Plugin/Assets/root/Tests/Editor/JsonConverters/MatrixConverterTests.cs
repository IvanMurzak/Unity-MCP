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

    public class MatrixConverterTests : BaseTest
    {
        #region Matrix4x4

        [UnityTest]
        public IEnumerator Matrix4x4_Identity()
        {
            TestUtils.ValidateType(Matrix4x4.identity);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Matrix4x4_Zero()
        {
            TestUtils.ValidateType(Matrix4x4.zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Matrix4x4_TRS()
        {
            var trs = Matrix4x4.TRS(
                new Vector3(1, 2, 3),
                Quaternion.Euler(45, 90, 180),
                new Vector3(1.5f, 2.5f, 0.5f)
            );
            TestUtils.ValidateType(trs);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Matrix4x4_CustomValues()
        {
            var m = new Matrix4x4();
            for (int i = 0; i < 16; i++)
            {
                m[i] = i * 1.5f;
            }
            TestUtils.ValidateType(m);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Matrix4x4_NegativeValues()
        {
            var m = new Matrix4x4();
            for (int i = 0; i < 16; i++)
            {
                m[i] = -i * 0.5f;
            }
            TestUtils.ValidateType(m);
            yield return null;
        }


        [UnityTest]
        public IEnumerator Matrix4x4_PositiveInfinity()
        {
            var m = new Matrix4x4();
            for (int i = 0; i < 16; i++)
            {
                m[i] = float.PositiveInfinity;
            }
            TestUtils.ValidateType(m);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Matrix4x4_NegativeInfinity()
        {
            var m = new Matrix4x4();
            for (int i = 0; i < 16; i++)
            {
                m[i] = float.NegativeInfinity;
            }
            TestUtils.ValidateType(m);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Matrix4x4_NaN()
        {
            var m = new Matrix4x4();
            for (int i = 0; i < 16; i++)
            {
                m[i] = float.NaN;
            }
            TestUtils.ValidateType(m);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Matrix4x4_MixedValues()
        {
            var m = Matrix4x4.identity;
            m.m00 = float.PositiveInfinity;
            m.m11 = float.NegativeInfinity;
            m.m22 = float.NaN;
            TestUtils.ValidateType(m);
            yield return null;
        }

        #endregion
    }
}
