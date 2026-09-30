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
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public sealed class SessionStateInt
    {
        readonly string _key;
        readonly int _defaultValue;

        public SessionStateInt(string key, int defaultValue = 0)
        {
            _key = key;
            _defaultValue = defaultValue;
        }

        public int Value
        {
            get => SessionState.GetInt(_key, _defaultValue);
            set => SessionState.SetInt(_key, value);
        }
    }
}
