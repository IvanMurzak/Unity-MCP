/*
┌────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                       │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-Package-Template) │
│  Copyright (c) 2025 Ivan Murzak                                            │
│  Licensed under the MIT License.                                           │
│  See the LICENSE file in the project root for more information.            │
└────────────────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using System;
using System.Net.Http;
using UnityEngine;
using com.IvanMurzak.Unity.MCP.ParticleSystem.Installer.SimpleJSON;

namespace com.IvanMurzak.Unity.MCP.ParticleSystem.Installer
{
    public static partial class Installer
    {
        const int OpenUpmTimeoutMs = 5000;

        static string OpenUpmPackageUrl => $"https://package.openupm.com/{PackageId}";

        /// <summary>
        /// Queries OpenUPM for the latest available version of the package.
        /// Returns the highest version that is less than or equal to the hardcoded Version constant,
        /// or null on failure.
        /// </summary>
        internal static string? GetLatestAvailableVersion()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMilliseconds(OpenUpmTimeoutMs);

                var response = client.GetStringAsync(OpenUpmPackageUrl).Result;
                var result = ParseBestVersion(response, Version);

                if (result != null)
                    Debug.Log($"[Installer] OpenUPM latest available version: {result}");
                else
                    Debug.LogWarning($"[Installer] No suitable version found on OpenUPM, using hardcoded version {Version}");

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Installer] Failed to query OpenUPM, using hardcoded version {Version}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses an OpenUPM registry JSON response and returns the highest available version
        /// that is less than or equal to <paramref name="maxVersion"/>.
        /// Returns null if no suitable version is found or if parsing fails.
        /// </summary>
        internal static string? ParseBestVersion(string jsonResponse, string maxVersion)
        {
            if (string.IsNullOrEmpty(jsonResponse) || string.IsNullOrEmpty(maxVersion))
                return null;

            try
            {
                var json = JSONObject.Parse(jsonResponse);
                if (json == null)
                    return null;

                var versions = json["versions"];
                if (versions == null)
                    return null;

                var targetVersion = new System.Version(maxVersion);
                string? bestVersion = null;
                System.Version? bestParsed = null;

                foreach (var kvp in versions.Linq)
                {
                    var key = kvp.Key;
                    if (string.IsNullOrEmpty(key))
                        continue;

                    System.Version parsed;
                    try { parsed = new System.Version(key); }
                    catch { continue; }

                    if (parsed > targetVersion)
                        continue;

                    if (bestParsed == null || parsed > bestParsed)
                    {
                        bestParsed = parsed;
                        bestVersion = key;
                    }
                }

                return bestVersion;
            }
            catch
            {
                return null;
            }
        }
    }
}
