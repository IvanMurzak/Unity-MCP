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

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Utility class for loading Unity Editor assets with fallback paths.
    /// </summary>
    public static class EditorUtils
    {
        public static void RepaintAllEditorWindows()
        {
            UnityEditor.EditorApplication.RepaintProjectWindow();
            UnityEditor.EditorApplication.RepaintHierarchyWindow();
            UnityEditor.EditorApplication.RepaintAnimationWindow();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
