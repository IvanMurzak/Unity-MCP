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
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using Extensions.Unity.PlayerPrefsEx;
using Microsoft.Extensions.Logging;
using UnityEditor;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Utility class for checking if a new version of the package is available on OpenUPM.
    /// </summary>
    /// <remarks>
    /// OpenUPM is the source of truth for the version that Unity Package Manager will actually
    /// install for end users. GitHub releases are published before the OpenUPM build pipeline
    /// finishes, so polling GitHub releases would prompt users to update to a version that is
    /// not yet installable. See https://github.com/IvanMurzak/Unity-MCP/issues/694.
    /// </remarks>
    public static class UpdateChecker
    {
        private const string PackageId = "com.ivanmurzak.unity.mcp";
        // package.openupm.com is the npm-style metadata registry (machine-readable JSON);
        // openupm.com is the human-readable package page the popup links users to.
        private const string OpenUpmPackageMetadataUrl = "https://package.openupm.com/" + PackageId;
        private const string OpenUpmPackageUrl = "https://openupm.com/packages/" + PackageId + "/";

        // Anchored at both ends: accepts only complete "N.N" or "N.N.N" strings. Without the
        // trailing anchor, "1.0.0-preview" would match and CompareVersions would silently
        // treat the "0-preview" segment as 0, making pre-release tags look equal to the
        // final release. See https://github.com/IvanMurzak/Unity-MCP/issues/694 review.
        private static readonly Regex VersionPattern = new(@"^\d+\.\d+(\.\d+)?$", RegexOptions.Compiled);

        // Hoisted to a single static instance to avoid socket exhaustion (TIME_WAIT) under
        // repeated update checks. Same pattern used by NuGetDownloader and DeviceAuthService
        // elsewhere in this package. The 10s timeout covers OpenUPM's slowest realistic responses.
        private static readonly HttpClient HttpClient = CreateHttpClient();

        private static PlayerPrefsBool DoNotShowAgain = new("Unity-MCP.UpdateChecker.DoNotShowAgain");
        private static PlayerPrefsString NextCheckTime = new("Unity-MCP.UpdateChecker.NextCheckTime");
        private static PlayerPrefsString SkippedVersion = new("Unity-MCP.UpdateChecker.SkippedVersion");

        private static bool isChecking = false;
        private static string? latestVersion = null;
        private static ILogger? logger = null;

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.Add("User-Agent", "AI-Game-Developer-UpdateChecker");
            return client;
        }

        /// <summary>
        /// Gets whether the user has chosen to never show the update popup again.
        /// </summary>
        public static bool IsDoNotShowAgain
        {
            get => DoNotShowAgain.Value;
            set
            {
                DoNotShowAgain.Value = value;
                PlayerPrefsEx.Save();
            }
        }

        /// <summary>
        /// Gets the latest version that was found during the last check.
        /// </summary>
        public static string? LatestVersion => latestVersion;

        /// <summary>
        /// Gets the OpenUPM package URL for the user to manually view available versions.
        /// </summary>
        /// <remarks>
        /// Points at OpenUPM rather than GitHub releases because OpenUPM is the registry
        /// Unity Package Manager actually pulls from — the version visible there is the
        /// version users can actually install at this moment.
        /// </remarks>
        public static string ReleasesUrl => OpenUpmPackageUrl;

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
            PlayerPrefsEx.Save();
        }

        /// <summary>
        /// Clears all update checker preferences (useful for testing).
        /// </summary>
        public static void ClearPreferences()
        {
            DoNotShowAgain.Value = false;
            NextCheckTime.Value = string.Empty;
            SkippedVersion.Value = string.Empty;
            PlayerPrefsEx.Save();
        }

        /// <summary>
        /// Asynchronously checks for updates from OpenUPM.
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
                var fetched = await FetchLatestVersionAsync();
                if (string.IsNullOrEmpty(fetched))
                {
                    if (forceCheck)
                        logger?.LogWarning("Unable to check for updates. Please check your internet connection.");
                    return;
                }

                latestVersion = fetched;

                // Check if this version was skipped
                var skippedVersion = SkippedVersion.Value;
                if (!string.IsNullOrEmpty(skippedVersion) && skippedVersion == fetched && !forceCheck)
                {
                    return;
                }

                // Compare versions
                var currentVersion = UnityMcpPlugin.Version;
                if (IsNewerVersion(fetched, currentVersion))
                {
                    // Show the update popup on the main thread
                    EditorApplication.delayCall += () =>
                    {
                        UpdatePopupWindow.ShowWindow(currentVersion, fetched);
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
                // Set next allowed check time to enforce cooldown (only for automatic checks)
                if (!forceCheck)
                {
                    NextCheckTime.Value = DateTime.UtcNow.AddHours(1).ToString("O");
                    PlayerPrefsEx.Save();
                }
                isChecking = false;
            }
        }

        /// <summary>
        /// Fetches the latest version from the OpenUPM registry.
        /// </summary>
        /// <remarks>
        /// Uses the npm-style registry endpoint <c>https://package.openupm.com/{packageId}</c>,
        /// which returns a JSON document whose <c>dist-tags.latest</c> field is the version
        /// currently installable via Unity Package Manager. On any network or parsing failure
        /// the method returns <c>null</c> so callers fall back gracefully without prompting.
        /// </remarks>
        private static async Task<string?> FetchLatestVersionAsync()
        {
            try
            {
                var json = await HttpClient.GetStringAsync(OpenUpmPackageMetadataUrl);
                return ParseLatestVersionFromJson(json);
            }
            catch (HttpRequestException ex)
            {
                logger?.LogWarning("Failed to fetch package metadata: {error}", ex.Message);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                logger?.LogWarning("OpenUPM request timed out: {error}", ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                // ParseLatestVersionFromJson swallows JsonException internally, so anything
                // reaching here is an unexpected transport / URI / IO failure rather than a
                // parse error — message must reflect that.
                logger?.LogWarning(ex, "Unexpected error during OpenUPM update check");
                return null;
            }
        }

        /// <summary>
        /// Parses the latest version from an OpenUPM registry JSON response.
        /// </summary>
        /// <remarks>
        /// The OpenUPM registry follows the npm registry shape; the latest published version
        /// is at <c>dist-tags.latest</c>. Returns <c>null</c> if the JSON is empty, malformed,
        /// or does not contain a <c>dist-tags.latest</c> string.
        /// </remarks>
        internal static string? ParseLatestVersionFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("dist-tags", out var distTags))
                    return null;

                if (distTags.ValueKind != JsonValueKind.Object)
                    return null;

                if (!distTags.TryGetProperty("latest", out var latest))
                    return null;

                if (latest.ValueKind != JsonValueKind.String)
                    return null;

                var version = latest.GetString();
                if (string.IsNullOrEmpty(version))
                    return null;

                // Defensive: only accept numeric semver-shaped strings. Without this guard
                // CompareVersions would silently treat non-numeric parts as 0, producing a
                // misleading "version" in the popup if the registry ever returns garbage.
                return VersionPattern.IsMatch(version) ? version : null;
            }
            catch (JsonException)
            {
                return null;
            }
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
