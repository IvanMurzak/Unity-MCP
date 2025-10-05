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
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityEditor;
using UnityEditor.Compilation;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    [InitializeOnLoad]
    public static partial class ScriptUtils
    {
        private const string PendingNotificationKeysKey = "MCP_PendingNotificationKeys";
        private const string NotificationDataSeparator = "<MCP_SEP>";

        private static bool _processPendingScheduled = false;

        // Store compilation messages (thread-safe with lock)
        private static readonly List<CompilerMessage> _lastCompilationMessages = new List<CompilerMessage>();
        private static readonly object _compilationMessagesLock = new object();

        static ScriptUtils()
        {
            // Process any pending notifications after domain reload (successful compilation)
            ScheduleProcessPendingNotifications();

            // Also listen for compilation event to handle both success and failure cases
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            lock (_compilationMessagesLock)
            {
                _lastCompilationMessages.Clear();
                _lastCompilationMessages.AddRange(messages);
            }
            ScheduleProcessPendingNotifications();
        }

        private static void ScheduleProcessPendingNotifications()
        {
            if (_processPendingScheduled)
                return;

            _processPendingScheduled = true;
            EditorApplication.update += ProcessPendingNotificationsOnce;
        }

        private static void ProcessPendingNotificationsOnce()
        {
            EditorApplication.update -= ProcessPendingNotificationsOnce;

            if (!_processPendingScheduled)
                return;

            _processPendingScheduled = false;
            ProcessPendingNotifications();
        }

        /// <summary>
        /// Checks if the provided C# code has valid syntax.
        /// This method uses Roslyn to parse the code and check for syntax errors.
        /// </summary>
        /// <param name="code">
        /// <param name="errors"></param>
        /// <returns>True if the code has valid syntax; otherwise, false.</returns>
        public static bool IsValidCSharpSyntax(string code, out IEnumerable<Diagnostic> errors)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = syntaxTree.GetDiagnostics();

            errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
            return !errors.Any();
        }

        /// <summary>
        /// Schedules a notification to be sent after Unity compilation completes.
        /// Uses SessionState to persist across domain reloads.
        /// </summary>
        /// <param name="requestId">The request ID to track</param>
        /// <param name="filePath">The file path that was modified</param>
        /// <param name="operationType">The type of operation performed</param>
        public static void SchedulePostCompilationNotification(string requestId, string filePath, string operationType)
        {
            var notificationKey = $"MCP_PendingNotification_{requestId}";
            var notificationData = $"{requestId}{NotificationDataSeparator}{filePath}{NotificationDataSeparator}{operationType}";

            // Store the notification data
            SessionState.SetString(notificationKey, notificationData);

            // Add to the key list
            var existingKeys = SessionState.GetString(PendingNotificationKeysKey, string.Empty);
            var keyList = string.IsNullOrEmpty(existingKeys)
                ? new List<string>()
                : existingKeys.Split(',').Where(k => !string.IsNullOrEmpty(k)).ToList();

            if (!keyList.Contains(notificationKey))
            {
                keyList.Add(notificationKey);
                SessionState.SetString(PendingNotificationKeysKey, string.Join(",", keyList));
            }
        }

        /// <summary>
        /// Called by InitializeOnLoad to process any pending notifications after compilation.
        /// </summary>
        public static void ProcessPendingNotifications()
        {
            // Get the list of pending notification keys from SessionState
            var pendingKeys = SessionState.GetString(PendingNotificationKeysKey, string.Empty);
            if (string.IsNullOrEmpty(pendingKeys))
                return;

            var keys = pendingKeys.Split(',').Where(k => !string.IsNullOrEmpty(k)).ToList();
            if (keys.Count == 0)
                return;

            var processedKeys = new List<string>();

            foreach (var key in keys)
            {
                var notificationData = SessionState.GetString(key, string.Empty);
                if (string.IsNullOrEmpty(notificationData))
                {
                    processedKeys.Add(key);
                    continue;
                }

                var parts = notificationData.Split(NotificationDataSeparator);
                if (parts.Length != 3)
                {
                    processedKeys.Add(key);
                    continue;
                }

                var requestId = parts[0];
                var filePath = parts[1];
                var operationType = parts[2];

                // Check for compilation errors
                var hasErrors = HasCompilationErrors();

                ResponseCallTool response;
                if (hasErrors)
                {
                    var errorDetails = GetCompilationErrorDetails();
                    var message = $"[Warning] {operationType} completed: {filePath}, but compilation errors occurred. Details:\n{errorDetails}";
                    response = ResponseCallTool.Success(message).SetRequestID(requestId);
                }
                else
                {
                    var message = $"[Success] {operationType} completed: {filePath}";
                    response = ResponseCallTool.Success(message).SetRequestID(requestId);
                }

                // Send notification and mark for cleanup
                _ = UnityMcpPlugin.NotifyToolRequestCompleted(response);
                processedKeys.Add(key);
            }

            // Clean up processed keys
            foreach (var key in processedKeys)
                SessionState.EraseString(key);

            // Update the key list
            var remainingKeys = keys.Except(processedKeys).ToList();
            if (remainingKeys.Count > 0)
            {
                SessionState.SetString(PendingNotificationKeysKey, string.Join(",", remainingKeys));
            }
            else
            {
                SessionState.EraseString(PendingNotificationKeysKey);
            }
        }

        /// <summary>
        /// Checks if there are any compilation errors by examining Unity's console and compilation pipeline.
        /// </summary>
        /// <returns>True if compilation errors exist</returns>
        public static bool HasCompilationErrors() => EditorUtility.scriptCompilationFailed;

        /// <summary>
        /// Retrieves detailed compilation error messages from the last compilation.
        /// </summary>
        /// <returns>Detailed compilation error messages</returns>
        public static string GetCompilationErrorDetails()
        {
            var errors = new List<string>();

            lock (_compilationMessagesLock)
            {
                foreach (var message in _lastCompilationMessages)
                {
                    if (message.type == CompilerMessageType.Error)
                        errors.Add($"[{message.file}] {message.message} (Line: {message.line})");
                }
            }

            if (errors.Count == 0)
            {
                // Since we can reliably check if compilation failed with EditorUtility.scriptCompilationFailed,
                // but getting detailed messages is complex, provide a simple fallback
                if (EditorUtility.scriptCompilationFailed)
                    return "Compilation errors detected. Please check the Unity Console window for detailed error messages.";

                errors.Add("No detailed compilation errors found.");
            }

            return string.Join("\n", errors);
        }
    }
}