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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.TestFiles
{
    /// <summary>A plain serializable class inside a component (writes into it are recorded on the component for Undo).</summary>
    [System.Serializable]
    public class RqlTestSettings
    {
        public float speed = 1;
    }

    /// <summary>A script with the member kinds the adapters handle (in its own file so Unity can serialize it).</summary>
    public class RqlTestBehaviour : MonoBehaviour
    {
        [SerializeField] private int secret = 7;
        [System.NonSerialized] public int transient = 3;
        [System.Obsolete("use speed")] public int legacy;
        public float speed = 1;
        public Light? lamp;
        public Transform? anchor;
        public Vector2 offset;
        public Vector4 weights;
        public Color tint = Color.white;
        public Gradient gradient = new Gradient();
        public LayerMask layers;
        public Vector3Int cell;
        public RqlTestSettings settings = new RqlTestSettings();
    }
}
