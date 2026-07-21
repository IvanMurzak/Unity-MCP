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
    public sealed class SessionStateString
    {
        readonly string _key;
        readonly string _defaultValue;

        public SessionStateString(string key, string defaultValue = "")
        {
            _key = key;
            _defaultValue = defaultValue;
        }

        public string Value
        {
            get => SessionState.GetString(_key, _defaultValue);
            set
            {
                if (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(_defaultValue))
                    SessionState.EraseString(_key);
                else
                    SessionState.SetString(_key, value);
            }
        }
    }
}
