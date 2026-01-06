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
using com.IvanMurzak.Unity.MCP;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests.JsonConverter
{
    public static class TestUtils
    {
        public static void ValidateType<T>(T sourceValue)
        {
            var reflector = UnityMcpPlugin.Instance.McpPluginInstance!.McpManager.Reflector;

            var serializedValue = reflector.JsonSerializer.Serialize(sourceValue);
            var deserializedValue = reflector.JsonSerializer.Deserialize<T>(serializedValue);

            bool areEqual = sourceValue switch
            {
                Vector2 v2 => CompareVector2(v2, (Vector2)(object)deserializedValue!),
                Vector3 v3 => CompareVector3(v3, (Vector3)(object)deserializedValue!),
                Vector4 v4 => CompareVector4(v4, (Vector4)(object)deserializedValue!),
                Quaternion q => CompareQuaternion(q, (Quaternion)(object)deserializedValue!),
                Matrix4x4 m => CompareMatrix4x4(m, (Matrix4x4)(object)deserializedValue!),
                Color c => CompareColor(c, (Color)(object)deserializedValue!),
                Rect r => CompareRect(r, (Rect)(object)deserializedValue!),
                Bounds b => CompareBounds(b, (Bounds)(object)deserializedValue!),
                _ => sourceValue!.Equals(deserializedValue)
            };

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

        static bool CompareVector2(Vector2 a, Vector2 b) => CompareFloats(a.x, b.x) && CompareFloats(a.y, b.y);
        static bool CompareVector3(Vector3 a, Vector3 b) => CompareFloats(a.x, b.x) && CompareFloats(a.y, b.y) && CompareFloats(a.z, b.z);
        static bool CompareVector4(Vector4 a, Vector4 b) => CompareFloats(a.x, b.x) && CompareFloats(a.y, b.y) && CompareFloats(a.z, b.z) && CompareFloats(a.w, b.w);
        static bool CompareQuaternion(Quaternion a, Quaternion b) => CompareFloats(a.x, b.x) && CompareFloats(a.y, b.y) && CompareFloats(a.z, b.z) && CompareFloats(a.w, b.w);
        static bool CompareColor(Color a, Color b) => CompareFloats(a.r, b.r) && CompareFloats(a.g, b.g) && CompareFloats(a.b, b.b) && CompareFloats(a.a, b.a);
        static bool CompareRect(Rect a, Rect b) => CompareFloats(a.x, b.x) && CompareFloats(a.y, b.y) && CompareFloats(a.width, b.width) && CompareFloats(a.height, b.height);
        static bool CompareBounds(Bounds a, Bounds b) => CompareVector3(a.center, b.center) && CompareVector3(a.size, b.size);

        static bool CompareMatrix4x4(Matrix4x4 a, Matrix4x4 b)
        {
            for (int i = 0; i < 16; i++)
            {
                if (!CompareFloats(a[i], b[i])) return false;
            }
            return true;
        }
    }
}
