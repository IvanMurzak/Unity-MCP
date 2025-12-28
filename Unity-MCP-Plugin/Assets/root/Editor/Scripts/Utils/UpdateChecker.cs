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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Extensions.Unity.PlayerPrefsEx;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine.Networking;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Utility class for checking if a new version of the package is available on GitHub.
    /// </summary>
    public static class UpdateChecker
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/IvanMurzak/Unity-MCP/tags";
        private const string GitHubReleasesUrl = "https://github.com/IvanMurzak/Unity-MCP/releases";

        private static PlayerPrefsBool DoNotShowAgain = new("Unity-MCP.UpdateChecker.DoNotShowAgain");
        private static PlayerPrefsString NextCheckTime = new("Unity-MCP.UpdateChecker.NextCheckTime");
        private static PlayerPrefsString SkippedVersion = new("Unity-MCP.UpdateChecker.SkippedVersion");

        private static bool isChecking = false;
        private static string? latestVersion = null;
        private static ILogger? logger = null;

        /// <summary>
        /// Gets whether the user has chosen to never show the update popup again.
        /// </summary>
        public static bool IsDoNotShowAgain
        {
            get => DoNotShowAgain.Value;
            set => DoNotShowAgain.Value = value;
        }

        /// <summary>
        /// Gets the latest version that was found during the last check.
        /// </summary>
        public static string? LatestVersion => latestVersion;

        /// <summary>
        /// Gets the GitHub releases URL for the user to manually check updates.
        /// </summary>
        public static string ReleasesUrl => GitHubReleasesUrl;

        public static void Init(ILogger? initLogger = null)
        {
            logger = initLogger;

            // Check for updates after Unity finishes loading
            EditorApplication.delayCall += CheckForUpdatesOnStartup;
        }

        private static void CheckForUpdatesOnStartup()
        {
            EditorApplication.delayCall -= CheckForUpdatesOnStartup;

            if (!ShouldCheckForUpdates())
                return;

            _ = CheckForUpdatesAsync();
        }

        /// <summary>
        /// Determines if we should check for updates based on user preferences and cooldown.
        /// </summary>
        public static bool ShouldCheckForUpdates()
        {
            // Don't check if user opted out
            if (DoNotShowAgain.Value)
                return false;

            // Check if we're still in cooldown period
            var nextCheckTimeStr = NextCheckTime.Value;
            if (!string.IsNullOrEmpty(nextCheckTimeStr))
            {
                if (DateTime.TryParse(nextCheckTimeStr, out var nextCheckDateTime))
                {
                    if (DateTime.UtcNow < nextCheckDateTime)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Skips a specific version (user doesn't want to be notified about it again).
        /// </summary>
        public static void SkipVersion(string version)
        {
            SkippedVersion.Value = version;
        }

        /// <summary>
        /// Clears all update checker preferences (useful for testing).
        /// </summary>
        public static void ClearPreferences()
        {
            DoNotShowAgain.Value = false;
            NextCheckTime.Value = string.Empty;
            SkippedVersion.Value = string.Empty;
        }

        /// <summary>
        /// Asynchronously checks for updates from GitHub.
        /// </summary>
        /// <param name="forceCheck">If true, ignores cooldown and skipped version settings.</param>
        public static async Task CheckForUpdatesAsync(bool forceCheck = false)
        {
            if (isChecking)
            {
                if (forceCheck)
                    logger?.LogWarning("Already checking for updates...");
                return;
            }

            if (!forceCheck && !ShouldCheckForUpdates())
                return;

            isChecking = true;

            try
            {
                var latestVersion = await FetchLatestVersionAsync();
                if (string.IsNullOrEmpty(latestVersion))
                {
                    if (forceCheck)
                        logger?.LogWarning("Unable to check for updates. Please check your internet connection.");
                    return;
                }

                UpdateChecker.latestVersion = latestVersion;

                // Check if this version was skipped
                var skippedVersion = SkippedVersion.Value;
                if (!string.IsNullOrEmpty(skippedVersion) && skippedVersion == latestVersion && !forceCheck)
                {
                    return;
                }
                // Update the next check time only on successful check
                if (!forceCheck)
                {
                    NextCheckTime.Value = DateTime.UtcNow.AddDays(1).ToString("O");
                }
                // Compare versions
                var currentVersion = UnityMcpPlugin.Version;
                if (IsNewerVersion(latestVersion!, currentVersion))
                {
                    // Show the update popup on the main thread
                    EditorApplication.delayCall += () =>
                    {
                        UpdatePopupWindow.ShowWindow(currentVersion, latestVersion!);
                    };
                }
                else if (forceCheck)
                {
                    // User manually checked - inform them they're up to date
                    logger?.LogDebug("You are using the latest version ({currentVersion}).", currentVersion);
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to check for updates");
            }
            finally
            {
                // Set next allowed check time to enforce cooldown
                NextCheckTime.Value = DateTime.UtcNow.AddDays(1).ToString("O");
                isChecking = false;
            }
        }

        /// <summary>
        /// Fetches the latest version tag from GitHub API.
        /// </summary>
        private static Task<string?> FetchLatestVersionAsync()
        {
            var tcs = new TaskCompletionSource<string?>();

            var request = UnityWebRequest.Get(GitHubApiUrl);
            request.SetRequestHeader("User-Agent", "AI-Game-Developer-UpdateChecker");

            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        logger?.LogWarning("Failed to fetch tags: {error}", request.error);
                        tcs.SetResult(null);
                        return;
                    }

                    var json = request.downloadHandler.text;
                    var latestVersion = ParseLatestVersionFromJson(json);
                    tcs.SetResult(latestVersion);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to parse response");
                    tcs.SetResult(null);
                }
                finally
                {
                    request.Dispose();
                }
            };

            return tcs.Task;
        }

        /// <summary>
        /// Parses the latest version from GitHub tags API JSON response.
        /// </summary>
        internal static string? ParseLatestVersionFromJson(string json)
        {
            // Simple JSON parsing for the tags array
            // Response format: [{"name": "v0.33.1", ...}, {"name": "v0.33.0", ...}, ...]

            var tags = new List<string>();
            var tagMatches = Regex.Matches(json, @"""name""\s*:\s*""([^""]+)""");

            foreach (Match match in tagMatches)
            {
                if (match.Groups.Count > 1)
                {
                    var tagName = match.Groups[1].Value;
                    // Remove 'v' prefix if present
                    if (tagName.StartsWith("v") || tagName.StartsWith("V"))
                        tagName = tagName.Substring(1);

                    // Validate it looks like a version number
                    if (Regex.IsMatch(tagName, @"^\d+\.\d+(\.\d+)?"))
                        tags.Add(tagName);
                }
            }

            if (tags.Count == 0)
                return null;

            // Sort versions and get the latest
            tags.Sort((a, b) => CompareVersions(b, a)); // Descending order
            return tags[0];
        }

        /// <summary>
        /// Compares two version strings.
        /// </summary>
        internal static int CompareVersions(string v1, string v2)
        {
            var parts1 = v1.Split('.');
            var parts2 = v2.Split('.');

            var maxLength = Math.Max(parts1.Length, parts2.Length);
            for (int i = 0; i < maxLength; i++)
            {
                var num1 = i < parts1.Length && int.TryParse(parts1[i], out var n1) ? n1 : 0;
                var num2 = i < parts2.Length && int.TryParse(parts2[i], out var n2) ? n2 : 0;

                if (num1 != num2)
                    return num1.CompareTo(num2);
            }

            return 0;
        }

        /// <summary>
        /// Determines if the remote version is newer than the current version.
        /// </summary>
        public static bool IsNewerVersion(string remoteVersion, string currentVersion)
        {
            return CompareVersions(remoteVersion, currentVersion) > 0;
        }
    }
}
