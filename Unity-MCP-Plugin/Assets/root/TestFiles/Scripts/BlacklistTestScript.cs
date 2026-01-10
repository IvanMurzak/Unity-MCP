/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.TestFiles
{
    /// <summary>
    /// A type that will be blacklisted during serialization tests.
    /// </summary>
    [System.Serializable]
    public class BlacklistedType
    {
        public int value;
        public string name;
    }

    /// <summary>
    /// Another type that will be blacklisted during serialization tests.
    /// </summary>
    [System.Serializable]
    public struct BlacklistedStruct
    {
        public float x;
        public float y;
    }

    /// <summary>
    /// Test component containing fields and properties of blacklisted types.
    /// Used to verify that blacklisted types are excluded from serialization.
    /// </summary>
    public class BlacklistTestScript : MonoBehaviour
    {
        // Non-blacklisted fields (should be serialized)
        public int normalIntField;
        public string normalStringField;
        public Vector3 normalVectorField;

        // Fields of types that will be blacklisted (should NOT be serialized when blacklisted)
        public BlacklistedType blacklistedTypeField;
        public BlacklistedStruct blacklistedStructField;

        // Property of blacklisted type (should NOT be serialized when blacklisted)
        public BlacklistedType BlacklistedTypeProperty { get; set; }
    }
}
