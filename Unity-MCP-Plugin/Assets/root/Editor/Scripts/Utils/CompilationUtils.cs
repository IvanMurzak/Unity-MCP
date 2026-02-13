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
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public static class CompilationUtils
    {
        private static TaskCompletionSource<bool>? _compilationCompletionSource;
        private static readonly object _compilationLock = new object();

        static CompilationUtils()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private static void OnCompilationFinished(object obj)
        {
            lock (_compilationLock)
            {
                if (_compilationCompletionSource != null && !_compilationCompletionSource.Task.IsCompleted)
                {
                    _compilationCompletionSource.SetResult(!EditorUtility.scriptCompilationFailed);
                }
            }
        }

        /// <summary>
        /// Checks if Unity is currently compiling scripts.
        /// </summary>
        /// <returns>True if compilation is in progress</returns>
        public static bool IsCompiling() => EditorApplication.isCompiling;

        /// <summary>
        /// Waits for any ongoing compilation to complete or triggers compilation if needed.
        /// </summary>
        /// <param name="timeoutSeconds">Maximum time to wait in seconds (default: 300 = 5 minutes)</param>
        /// <returns>True if compilation completed successfully, false if it failed or timed out</returns>
        public static async Task<bool> WaitForCompilationAsync(int timeoutSeconds = 300)
        {
            // If no compilation is happening and no compilation errors exist, return immediately
            if (!EditorApplication.isCompiling && !EditorUtility.scriptCompilationFailed)
                return true;

            // If compilation already failed, return false immediately
            if (!EditorApplication.isCompiling && EditorUtility.scriptCompilationFailed)
                return false;

            TaskCompletionSource<bool> currentCompletionSource;
            lock (_compilationLock)
            {
                // Create a new TaskCompletionSource if one doesn't exist or is completed
                if (_compilationCompletionSource == null || _compilationCompletionSource.Task.IsCompleted)
                {
                    _compilationCompletionSource = new TaskCompletionSource<bool>();
                }
                currentCompletionSource = _compilationCompletionSource;
            }

            // Wait for compilation to complete with timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var completedTask = await Task.WhenAny(currentCompletionSource.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timeout occurred
                lock (_compilationLock)
                {
                    if (!currentCompletionSource.Task.IsCompleted)
                    {
                        currentCompletionSource.SetResult(false);
                    }
                }
                return false;
            }

            // Return the compilation result
            return await currentCompletionSource.Task;
        }

        /// <summary>
        /// Ensures Unity is in a compiled, ready state before proceeding.
        /// If compilation is needed, triggers it and waits for completion.
        /// </summary>
        /// <param name="timeoutSeconds">Maximum time to wait in seconds</param>
        /// <returns>True if Unity is ready (compiled successfully), false if compilation failed or timed out</returns>
        public static async Task<bool> EnsureCompilationReadyAsync(int timeoutSeconds = 300)
        {
            // Check if compilation is in progress or failed
            if (EditorApplication.isCompiling)
            {
                // Compilation is already running, just wait
                return await WaitForCompilationAsync(timeoutSeconds);
            }

            if (EditorUtility.scriptCompilationFailed)
            {
                // Compilation has failed, cannot proceed
                return false;
            }

            // No compilation in progress and no errors - ready to proceed
            return true;
        }

        /// <summary>
        /// Gets a summary of compilation errors suitable for user feedback.
        /// </summary>
        /// <param name="maxErrors">Maximum number of errors to include in summary (default: 10)</param>
        /// <returns>Formatted error summary</returns>
        public static string GetCompilationErrorSummary(int maxErrors = 10)
        {
            var errorDetails = ScriptUtils.GetCompilationErrorDetails();
            
            if (string.IsNullOrEmpty(errorDetails))
                return "Compilation errors detected. See Unity console for full log.";

            var lines = errorDetails.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length <= maxErrors)
                return $"{errorDetails}\n\nSee Unity console for full log.";

            var summary = string.Join("\n", lines, 0, Math.Min(maxErrors, lines.Length));
            var remaining = lines.Length - maxErrors;
            return $"{summary}\n\n...and {remaining} more error(s). See Unity console for full log.";
        }
    }
}
