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
using System.Net.Http;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Downloads .nupkg files from the NuGet v3 flat container API.
    /// Uses the flat container URL pattern:
    ///   https://api.nuget.org/v3-flatcontainer/{id}/{version}/{id}.{version}.nupkg
    /// </summary>
    static class NuGetDownloader
    {
        const string Tag = NuGetConfig.LogTag;

        static readonly HttpClient httpClient = CreateHttpClient();

        static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip
                                       | System.Net.DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Unity-MCP-NuGetResolver");
            client.Timeout = TimeSpan.FromMinutes(5);
            return client;
        }

        /// <summary>
        /// Downloads a .nupkg file to the cache. Returns the cached file path.
        /// If the file is already cached, returns immediately without downloading.
        /// </summary>
        public static string Download(NuGetPackage package)
        {
            NuGetCache.EnsureCacheDirectory();
            var cachedPath = NuGetCache.GetCachedPath(package);

            if (File.Exists(cachedPath))
                return cachedPath;

            var url = package.DownloadUrl;
            Debug.Log($"{Tag} Downloading {package.Id} {package.Version} from {url}");

            try
            {
                using (var response = httpClient.GetAsync(url).GetAwaiter().GetResult())
                {
                    response.EnsureSuccessStatusCode();
                    using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var fileStream = File.Create(cachedPath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
                Debug.Log($"{Tag} Downloaded {package.Id} {package.Version} ({new FileInfo(cachedPath).Length / 1024} KB)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Failed to download {package}: {ex.Message}");
                if (File.Exists(cachedPath))
                    File.Delete(cachedPath);
                throw;
            }

            return cachedPath;
        }
    }
}
