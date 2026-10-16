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
    public sealed class SessionStateBool
    {
        readonly string _key;
        readonly bool _defaultValue;

        public SessionStateBool(string key, bool defaultValue = false)
        {
            _key = key;
            _defaultValue = defaultValue;
        }

        public bool Value
        {
            get => SessionState.GetBool(_key, _defaultValue);
            set => SessionState.SetBool(_key, value);
        }
    }
}
