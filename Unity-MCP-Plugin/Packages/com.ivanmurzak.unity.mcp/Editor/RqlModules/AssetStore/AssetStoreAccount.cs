/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    /// <summary>One package of the signed-in user's "My Assets" list.</summary>
    public sealed class AssetStorePurchase
    {
        public long productId;
        public string displayName = "";
        public string? purchasedAt;
        public List<string> tags = new List<string>();
        public bool hidden;
    }

    /// <summary>Where the Editor's own download of a product lands in the cache: publisher / category / package name.</summary>
    public sealed class AssetStoreDownloadTarget
    {
        public string publisher = "";
        public string category = "";
        public string packageName = "";
    }

    /// <summary>
    /// The Asset Store REST API the Package Manager window's "My Assets" page uses (packages-v2.unity.com), called with the
    /// access token of the user signed in to the Editor (<c>CloudProjectSettings.accessToken</c>). Read-only: the list of owned
    /// packages and where a product's download is stored. Blocking calls are made off the main thread.
    /// </summary>
    public static class AssetStoreAccount
    {
        public const string Host = "https://packages-v2.unity.com";
        const int PageSize = 100;
        const int MaxPackages = 5000;

        static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        /// <summary>Every package the user owns (hidden ones included), newest purchase first.</summary>
        public static List<AssetStorePurchase> FetchPurchases(string accessToken)
            => Task.Run(() => FetchPurchasesAsync(accessToken)).GetAwaiter().GetResult();

        /// <summary>The cache location of a product's download (Package Manager's download info).</summary>
        public static AssetStoreDownloadTarget FetchDownloadTarget(string accessToken, long productId)
            => Task.Run(() => FetchDownloadTargetAsync(accessToken, productId)).GetAwaiter().GetResult();

        static async Task<List<AssetStorePurchase>> FetchPurchasesAsync(string accessToken)
        {
            var result = new List<AssetStorePurchase>();
            for (var offset = 0; offset < MaxPackages; offset += PageSize)
            {
                using var doc = await GetJsonAsync($"/-/api/purchases?offset={offset}&limit={PageSize}&orderBy=purchased_date&order=desc", accessToken).ConfigureAwait(false);
                var root = doc.RootElement;
                var page = ParsePurchases(root);
                result.AddRange(page);
                var total = root.TryGetProperty("total", out var t) && t.TryGetInt64(out var n) ? n : 0;
                if (page.Count < PageSize || result.Count >= total)
                    break;
            }
            return result;
        }

        /// <summary>Parses one page of <c>/-/api/purchases</c> (exposed for tests).</summary>
        public static List<AssetStorePurchase> ParsePurchases(JsonElement root)
        {
            var list = new List<AssetStorePurchase>();
            if (!root.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return list;
            foreach (var item in results.EnumerateArray())
            {
                if (!item.TryGetProperty("packageId", out var idElement)) continue;
                var id = idElement.ValueKind == JsonValueKind.Number ? idElement.GetInt64()
                    : long.TryParse(idElement.GetString(), out var parsed) ? parsed : 0;
                if (id <= 0) continue;
                var purchase = new AssetStorePurchase
                {
                    productId = id,
                    displayName = String(item, "displayName") ?? id.ToString(),
                    purchasedAt = String(item, "grantTime"),
                    hidden = item.TryGetProperty("isHidden", out var h) && h.ValueKind == JsonValueKind.True
                };
                if (item.TryGetProperty("tagging", out var tags) && tags.ValueKind == JsonValueKind.Array)
                    foreach (var tag in tags.EnumerateArray())
                        if (tag.ValueKind == JsonValueKind.String) purchase.tags.Add(tag.GetString()!);
                list.Add(purchase);
            }
            return list;
        }

        static async Task<AssetStoreDownloadTarget> FetchDownloadTargetAsync(string accessToken, long productId)
        {
            using var doc = await GetJsonAsync($"/-/api/legacy-package-download-info/{productId}", accessToken).ConfigureAwait(false);
            return ParseDownloadTarget(doc.RootElement)
                ?? throw new InvalidOperationException($"the Asset Store returned no download info for product {productId} (not owned, or not an asset package)");
        }

        /// <summary>Parses <c>/-/api/legacy-package-download-info/{id}</c> (exposed for tests).</summary>
        public static AssetStoreDownloadTarget? ParseDownloadTarget(JsonElement root)
        {
            if (!root.TryGetProperty("result", out var result) || !result.TryGetProperty("download", out var download))
                return null;
            var target = new AssetStoreDownloadTarget
            {
                // The Editor drops dots from each segment (AssetStoreDownloadInfo.destination).
                publisher = (String(download, "filename_safe_publisher_name") ?? "").Replace(".", ""),
                category = (String(download, "filename_safe_category_name") ?? "").Replace(".", ""),
                packageName = (String(download, "filename_safe_package_name") ?? "").Replace(".", "")
            };
            return target.packageName.Length == 0 ? null : target;
        }

        static async Task<JsonDocument> GetJsonAsync(string pathAndQuery, string accessToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Host + pathAndQuery);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await _http.SendAsync(request).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if ((int)response.StatusCode == 401 || (int)response.StatusCode == 403)
                throw new InvalidOperationException($"the Asset Store refused the Editor's sign-in ({(int)response.StatusCode}): sign in again: assetStore | call SignIn()");
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"the Asset Store answered {(int)response.StatusCode} for {pathAndQuery}");
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("errorMessage", out var error))
            {
                doc.Dispose();
                throw new InvalidOperationException($"the Asset Store answered: {error}");
            }
            return doc;
        }

        static string? String(JsonElement element, string name)
            => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
