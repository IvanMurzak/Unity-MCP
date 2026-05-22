/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.   |
+------------------------------------------------------------------+
*/

#nullable enable
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Windows-only pre-flight check that aborts a NuGet install before any DLL
    /// is written to disk if the resulting path would exceed the legacy
    /// <c>MAX_PATH</c> limit (260 chars).
    ///
    /// Why this is necessary: Unity's bundled Mono runtime opens NuGet DLLs via
    /// <c>System.IO.FileStream</c> → legacy <c>CreateFileW</c> *without* the
    /// <c>\\?\</c> long-path prefix. Even with Windows' "Enable Win32 long
    /// paths" group policy enabled, Unity's runtime does not honor it, so a DLL
    /// at a long path appears "not found" to <c>AssemblyValidation</c> with an
    /// opaque <c>DirectoryNotFoundException</c> stack trace.
    ///
    /// We surface the error pre-extraction so the user gets an actionable
    /// message instead of a confusing post-hoc validation failure (see issue
    /// #733).
    ///
    /// macOS / Linux: this check is a no-op. Linux's <c>PATH_MAX</c> is much
    /// higher (~4096) and macOS handles long paths transparently for our
    /// purposes.
    /// </summary>
    static class NuGetLongPathPreflight
    {
        const string Tag = NuGetConfig.LogTag;

        /// <summary>
        /// Threshold below the OS hard limit. The companion <c>.meta</c> file
        /// Unity creates next to each DLL adds <c>".meta"</c> = 5 chars to the
        /// path, so we leave 5 chars of slack — meaning a DLL whose full path
        /// is exactly 255 chars is allowed; one at 256 chars is rejected.
        /// </summary>
        internal const int MaxPath = 260;
        internal const int MetaSuffixSlack = 5;
        internal const int DefaultMaxAllowedPathLength = MaxPath - MetaSuffixSlack;

        /// <summary>
        /// Runs the pre-flight check against a single planned DLL path. Throws
        /// <see cref="InstallPathTooLongException"/> when the path would exceed
        /// the threshold on Windows.
        ///
        /// On non-Windows platforms this method returns without throwing.
        /// </summary>
        public static void Check(string plannedDllPath, string packageId)
        {
            CheckWith(plannedDllPath, packageId, IsWindows(), DefaultMaxAllowedPathLength);
        }

        /// <summary>
        /// Test seam: same as <see cref="Check"/> but with the OS check and the
        /// threshold injected. Lets EditMode tests exercise the rejection path
        /// deterministically without needing a real 250-char temp directory.
        /// </summary>
        internal static void CheckWith(string plannedDllPath, string packageId, bool isWindows, int maxAllowedPathLength)
        {
            if (!isWindows)
                return;

            // We compare against the absolute path the OS will see, not the
            // (possibly project-relative) path the caller passed in, because
            // CreateFileW operates on the absolute form.
            //
            // Path.GetFullPath itself can throw PathTooLongException on legacy
            // .NET Framework / Mono when the resolved path exceeds the runtime's
            // own buffer ceiling — convert that into the same actionable
            // InstallPathTooLongException so callers see one consistent error
            // class instead of falling into the generic catch (Exception) higher
            // up the stack.
            string absolutePath;
            try
            {
                absolutePath = Path.GetFullPath(plannedDllPath);
            }
            catch (PathTooLongException ex)
            {
                throw new InstallPathTooLongException(
                    BuildLongPathMessage(packageId, plannedDllPath, plannedDllPath.Length, ex.Message),
                    plannedDllPath,
                    plannedDllPath.Length);
            }

            if (absolutePath.Length <= maxAllowedPathLength)
                return;

            throw new InstallPathTooLongException(
                BuildLongPathMessage(packageId, absolutePath, absolutePath.Length, innerDetail: null),
                absolutePath,
                absolutePath.Length);
        }

        static string BuildLongPathMessage(string packageId, string path, int pathLength, string? innerDetail)
        {
            var detail = innerDetail == null
                ? string.Empty
                : $"\n\nUnderlying error: {innerDetail}";

            return
                $"{Tag} Cannot install '{packageId}' — the DLL plus its Unity .meta companion would exceed " +
                $"Windows' 260-character path limit (the .meta adds {MetaSuffixSlack} chars, so DLLs are " +
                $"capped at {DefaultMaxAllowedPathLength} chars):\n\n" +
                $"  {path}  ({pathLength} chars; max {DefaultMaxAllowedPathLength})\n\n" +
                "Unity's bundled Mono runtime (used by the assembly validator) does NOT honor Windows' " +
                "\"Enable Win32 long paths\" registry/group-policy setting, so DLLs at long paths appear " +
                "missing even though they are on disk. This is a known limitation of Unity, not of this plugin.\n\n" +
                "Move your Unity project to a shorter path and reopen it. For example:\n" +
                $"  C:\\src\\<project-name>\\\n\n" +
                $"Project root currently: {TryGetProjectRoot()}" + detail;
        }

        static string TryGetProjectRoot()
        {
            try
            {
                // Application.dataPath is "<project>/Assets". Its parent is the project root.
                var dataPath = UnityEngine.Application.dataPath;
                return Path.GetFullPath(Path.Combine(dataPath, ".."));
            }
            catch
            {
                return "(unable to resolve project root)";
            }
        }

        static bool IsWindows() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    /// <summary>
    /// Raised by <see cref="NuGetLongPathPreflight.Check"/> when the planned
    /// install path would exceed the Windows MAX_PATH limit.
    ///
    /// The full <c>Message</c> is also surfaced via <c>Debug.LogError</c> by
    /// the caller so the user sees it in the Unity Console without expanding
    /// the stack trace.
    /// </summary>
    sealed class InstallPathTooLongException : Exception
    {
        public string PlannedPath { get; }
        public int PlannedPathLength { get; }

        public InstallPathTooLongException(string message, string plannedPath, int plannedPathLength)
            : base(message)
        {
            PlannedPath = plannedPath;
            PlannedPathLength = plannedPathLength;
        }
    }
}
