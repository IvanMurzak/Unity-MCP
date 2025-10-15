/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#if UNITY_EDITOR
namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class Consts
    {
        public static partial class Log
        {
            public const string Tag = "<color=#B4FF32>[AI]</color>";

            public const string Trce = "<color=#777776>trce</color>: ";
            public const string Dbug = "<color=#777776>dbug</color>: ";
            public const string Info = "<color=#16C60C>info</color>: ";
            public const string Warn = "<color=#FFC107>warn</color>: ";
            public const string Fail = "<color=#E74856>fail</color>: ";
            public const string Crit = "<color=#E50016>crit</color>: ";

            public static partial class Color
            {
                public const string TagStart = "<color=#B4FF32>";
                public const string TagEnd = "</color>";

                public const string LevelStart = "<color=#777776>";
                public const string LevelEnd = "</color>";

                public const string CategoryStart = "<color=#007575><b>";
                public const string CategoryEnd = "</b></color>";
            }
        }
    }
}
#endif
