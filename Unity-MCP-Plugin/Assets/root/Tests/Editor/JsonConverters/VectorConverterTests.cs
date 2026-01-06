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

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class VectorConverterTests : BaseTest
    {
        static void ValidateType<T>(T sourceValue)
        {
            var reflector = UnityMcpPlugin.Instance.McpPluginInstance!.McpManager.Reflector;

            var serializedValue = reflector.JsonSerializer.Serialize(sourceValue);
            var deserializedValue = reflector.JsonSerializer.Deserialize<T>(serializedValue);

            var areEqual = sourceValue!.Equals(deserializedValue);

            Assert.IsTrue(areEqual, $"Serialized and deserialized values do not match for type '{typeof(T).Name}'.\n" +
                $"Source: {sourceValue}\n" +
                $"Serialized: {serializedValue}\n" +
                $"Deserialized: {deserializedValue}");
        }

        static bool CompareFloats(float a, float b)
        {
            if (float.IsNaN(a) && float.IsNaN(b)) return true;
            if (float.IsPositiveInfinity(a) && float.IsPositiveInfinity(b)) return true;
            if (float.IsNegativeInfinity(a) && float.IsNegativeInfinity(b)) return true;
            return Mathf.Approximately(a, b) || a == b;
        }

        #region Vector2

        [UnityTest]
        public IEnumerator Vector2_Zero()
        {
            ValidateType(Vector2.zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_One()
        {
            ValidateType(Vector2.one);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_Up()
        {
            ValidateType(Vector2.up);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_Down()
        {
            ValidateType(Vector2.down);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_Left()
        {
            ValidateType(Vector2.left);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_Right()
        {
            ValidateType(Vector2.right);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_NegativeValues()
        {
            ValidateType(new Vector2(-5.5f, -10.25f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_LargeValues()
        {
            ValidateType(new Vector2(float.MaxValue / 2, float.MinValue / 2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_SmallValues()
        {
            ValidateType(new Vector2(float.Epsilon, -float.Epsilon));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_PositiveInfinity()
        {
            ValidateType(new Vector2(float.PositiveInfinity, float.PositiveInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_NegativeInfinity()
        {
            ValidateType(new Vector2(float.NegativeInfinity, float.NegativeInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_NaN()
        {
            ValidateType(new Vector2(float.NaN, float.NaN));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2_MixedInfinityAndNaN()
        {
            ValidateType(new Vector2(float.PositiveInfinity, float.NaN));
            yield return null;
        }

        #endregion

        #region Vector3

        [UnityTest]
        public IEnumerator Vector3_Zero()
        {
            ValidateType(Vector3.zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_One()
        {
            ValidateType(Vector3.one);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_Up()
        {
            ValidateType(Vector3.up);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_Down()
        {
            ValidateType(Vector3.down);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_Left()
        {
            ValidateType(Vector3.left);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_Right()
        {
            ValidateType(Vector3.right);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_Forward()
        {
            ValidateType(Vector3.forward);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_Back()
        {
            ValidateType(Vector3.back);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_NegativeValues()
        {
            ValidateType(new Vector3(-5.5f, -10.25f, -100.333f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_LargeValues()
        {
            ValidateType(new Vector3(float.MaxValue / 2, float.MinValue / 2, float.MaxValue / 3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_SmallValues()
        {
            ValidateType(new Vector3(float.Epsilon, -float.Epsilon, float.Epsilon * 2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_PositiveInfinity()
        {
            ValidateType(new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_NegativeInfinity()
        {
            ValidateType(new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_NaN()
        {
            ValidateType(new Vector3(float.NaN, float.NaN, float.NaN));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3_MixedInfinityAndNaN()
        {
            ValidateType(new Vector3(float.PositiveInfinity, float.NegativeInfinity, float.NaN));
            yield return null;
        }

        #endregion

        #region Vector4

        [UnityTest]
        public IEnumerator Vector4_Zero()
        {
            ValidateType(Vector4.zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector4_One()
        {
            ValidateType(Vector4.one);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector4_CustomValues()
        {
            ValidateType(new Vector4(1.5f, 2.5f, 3.5f, 4.5f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector4_NegativeValues()
        {
            ValidateType(new Vector4(-5.5f, -10.25f, -100.333f, -0.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector4_LargeValues()
        {
            ValidateType(new Vector4(float.MaxValue / 2, float.MinValue / 2, float.MaxValue / 3, float.MinValue / 3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector4_SmallValues()
        {
            ValidateType(new Vector4(float.Epsilon, -float.Epsilon, float.Epsilon * 2, -float.Epsilon * 2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector4_PositiveInfinity()
        {
            ValidateType(new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector4_NegativeInfinity()
        {
            ValidateType(new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector4_NaN()
        {
            ValidateType(new Vector4(float.NaN, float.NaN, float.NaN, float.NaN));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector4_MixedInfinityAndNaN()
        {
            ValidateType(new Vector4(float.PositiveInfinity, float.NegativeInfinity, float.NaN, 0f));
            yield return null;
        }

        #endregion

        #region Vector2Int

        [UnityTest]
        public IEnumerator Vector2Int_Zero()
        {
            ValidateType(Vector2Int.zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2Int_One()
        {
            ValidateType(Vector2Int.one);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2Int_Up()
        {
            ValidateType(Vector2Int.up);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2Int_Down()
        {
            ValidateType(Vector2Int.down);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2Int_Left()
        {
            ValidateType(Vector2Int.left);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2Int_Right()
        {
            ValidateType(Vector2Int.right);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2Int_NegativeValues()
        {
            ValidateType(new Vector2Int(-5, -10));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2Int_LargeValues()
        {
            ValidateType(new Vector2Int(int.MaxValue, int.MinValue));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector2Int_CustomValues()
        {
            ValidateType(new Vector2Int(123, 456));
            yield return null;
        }

        #endregion

        #region Vector3Int

        [UnityTest]
        public IEnumerator Vector3Int_Zero()
        {
            ValidateType(Vector3Int.zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_One()
        {
            ValidateType(Vector3Int.one);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_Up()
        {
            ValidateType(Vector3Int.up);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_Down()
        {
            ValidateType(Vector3Int.down);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_Left()
        {
            ValidateType(Vector3Int.left);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_Right()
        {
            ValidateType(Vector3Int.right);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_Forward()
        {
            ValidateType(Vector3Int.forward);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_Back()
        {
            ValidateType(Vector3Int.back);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_NegativeValues()
        {
            ValidateType(new Vector3Int(-5, -10, -100));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_LargeValues()
        {
            ValidateType(new Vector3Int(int.MaxValue, int.MinValue, int.MaxValue / 2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Vector3Int_CustomValues()
        {
            ValidateType(new Vector3Int(123, 456, 789));
            yield return null;
        }

        #endregion
    }
}
