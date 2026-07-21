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
using System;
using System.Text.RegularExpressions;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class MppmUtils
    {
        static readonly bool _isMppmClone;
        static readonly string? _cloneName;
        static readonly string? _cloneId;
        static readonly string _cloneSuffix;

        public static bool IsMppmClone => _isMppmClone;
        public static string? CloneName => _cloneName;
        public static string? CloneId => _cloneId;
        public static string CloneSuffix => _cloneSuffix;

        static MppmUtils()
        {
            var args = Environment.GetCommandLineArgs();
            _isMppmClone = false;
            _cloneName = null;
            _cloneId = null;
            _cloneSuffix = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];

                if (arg == "-editor-mode" && i + 1 < args.Length && args[i + 1] == "com.unity.mppm.clone")
                {
                    _isMppmClone = true;
                    i++;
                }
                else if (arg == "-name" && i + 1 < args.Length)
                {
                    _cloneName = args[i + 1];
                    i++;
                }
                else if (arg.StartsWith("-vpId=", StringComparison.Ordinal))
                {
                    _cloneId = arg.Substring("-vpId=".Length);
                }
                else if (arg == "-vpId" && i + 1 < args.Length)
                {
                    _cloneId = args[i + 1];
                    i++;
                }
            }

            if (_cloneName != null)
                _cloneSuffix = ToKebabCase(_cloneName);
        }

        static string ToKebabCase(string name)
        {
            var lower = name.Trim().ToLowerInvariant();
            var kebab = Regex.Replace(lower, @"[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrEmpty(kebab) ? string.Empty : $"-{kebab}";
        }
    }
}
