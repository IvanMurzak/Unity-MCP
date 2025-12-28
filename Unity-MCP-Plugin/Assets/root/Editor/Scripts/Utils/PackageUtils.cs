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
using com.IvanMurzak.McpPlugin.Common.Model;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Utility class for handling package operations that may trigger domain reloads.
    /// Uses SessionState to persist notification data across domain reloads.
    /// </summary>
    public static class PackageUtils
    {
        private const string PendingNotificationKeysKey = "MCP_PendingPackageNotificationKeys";
        private const string NotificationDataSeparator = "<MCP_PKG_SEP>";

        private static bool _processPendingScheduled = false;

        public static void Init()
        {
            // Process any pending notifications after domain reload
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
        /// Schedules a notification to be sent after Unity domain reload completes.
        /// Uses SessionState to persist across domain reloads.
        /// </summary>
        /// <param name="requestId">The request ID to track</param>
        /// <param name="packageIdentifier">The package identifier that was operated on</param>
        /// <param name="operationType">The type of operation performed (Add, Remove)</param>
        /// <param name="expectedResult">Whether the operation was expected to succeed before domain reload</param>
        public static void SchedulePostDomainReloadNotification(string requestId, string packageIdentifier, string operationType, bool expectedResult)
        {
            var notificationKey = $"MCP_PendingPackageNotification_{requestId}";
            var notificationData = $"{requestId}{NotificationDataSeparator}{packageIdentifier}{NotificationDataSeparator}{operationType}{NotificationDataSeparator}{expectedResult}";

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
        /// Called by InitializeOnLoad to process any pending notifications after domain reload.
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
                if (parts.Length != 4)
                {
                    processedKeys.Add(key);
                    continue;
                }

                var requestId = parts[0];
                var packageIdentifier = parts[1];
                var operationType = parts[2];
                var expectedResult = bool.Parse(parts[3]);

                // Check for compilation errors after package operation
                var hasCompilationErrors = ScriptUtils.HasCompilationErrors();

                ResponseCallTool response;
                if (hasCompilationErrors)
                {
                    var errorDetails = ScriptUtils.GetCompilationErrorDetails();
                    var message = $"[Warning] Package {operationType} completed: {packageIdentifier}, but compilation errors occurred after domain reload. Details:\n{errorDetails}";
                    response = ResponseCallTool.Success(message).SetRequestID(requestId);
                }
                else if (expectedResult)
                {
                    var message = $"[Success] Package {operationType} completed: {packageIdentifier}. Domain reload finished successfully.";
                    response = ResponseCallTool.Success(message).SetRequestID(requestId);
                }
                else
                {
                    var message = $"[Error] Package {operationType} failed: {packageIdentifier}";
                    response = ResponseCallTool.Error(message).SetRequestID(requestId);
                }

                // Send notification and mark for cleanup
                _ = UnityMcpPlugin.NotifyToolRequestCompleted(new RequestToolCompletedData
                {
                    RequestId = requestId,
                    Result = response
                });
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
    }
}
