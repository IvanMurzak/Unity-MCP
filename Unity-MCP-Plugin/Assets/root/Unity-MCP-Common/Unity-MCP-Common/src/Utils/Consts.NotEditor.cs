/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#if !UNITY_EDITOR
namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class Consts
    {
        public static partial class Log
        {
            public const string Tag = "[AI]";

            public const string Trce = "trce: ";
            public const string Dbug = "dbug: ";
            public const string Info = "info: ";
            public const string Warn = "warn: ";
            public const string Fail = "fail: ";
            public const string Crit = "crit: ";

            public static partial class Color
            {
                public const string TagStart = "";
                public const string TagEnd = "";

                public const string LevelStart = "";
                public const string LevelEnd = "";

                public const string CategoryStart = "";
                public const string CategoryEnd = "";
            }
        }
    }
}
#endif
