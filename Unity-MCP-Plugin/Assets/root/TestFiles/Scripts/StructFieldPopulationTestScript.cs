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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.TestFiles
{
    /// <summary>
    /// Test script with struct fields for testing partial population of value types.
    /// </summary>
    public class StructFieldPopulationTestScript : MonoBehaviour
    {
        public Vector3 vector3Field;
        public Color colorField;
        public Quaternion quaternionField;
        public Bounds boundsField;
        public Rect rectField;
        public Vector2 vector2Field;
        public Vector4 vector4Field;

        // Custom struct for testing
        public TestStruct customStructField;
    }

    /// <summary>
    /// Custom struct for testing partial population.
    /// </summary>
    [System.Serializable]
    public struct TestStruct
    {
        public float floatValue;
        public int intValue;
        public string stringValue;
        public Vector3 nestedVector;
    }
}
