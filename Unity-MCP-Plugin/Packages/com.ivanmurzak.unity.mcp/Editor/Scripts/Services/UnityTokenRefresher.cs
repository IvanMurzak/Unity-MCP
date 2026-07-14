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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    /// <summary>
    /// Unity's concrete <see cref="ITokenRefresher"/> (mcp-authorize design 03 Flow B): exchanges a stored
    /// refresh token for a fresh ES256 access token at <c>{serverTarget}/oauth/token</c>
    /// (<c>grant_type=refresh_token</c>). The shared <see cref="PluginCredentialProvider"/> owns the
    /// machine store and the refresh scheduling; this class is only the HTTP seam. It fails closed — any
    /// non-success or exception returns <see cref="TokenRefreshResult.Failure"/> — and never logs token
    /// material.
    /// </summary>
    public sealed class UnityTokenRefresher : ITokenRefresher
    {
        static readonly HttpClient _sharedHttpClient = new HttpClient();

        readonly string _defaultServerBaseUrl;
        readonly string _clientId;
        readonly string _scope;
        readonly HttpClient _httpClient;

        public UnityTokenRefresher(string defaultServerBaseUrl, string? clientId = null, string? scope = null, HttpClient? httpClient = null)
        {
            _defaultServerBaseUrl = NormalizeBase(defaultServerBaseUrl) ?? "";
            _clientId = string.IsNullOrWhiteSpace(clientId) ? DeviceAuthService.DefaultClientId : clientId!;
            _scope = string.IsNullOrWhiteSpace(scope) ? DeviceAuthService.PluginScope : scope!;
            _httpClient = httpClient ?? _sharedHttpClient;
        }

        public async Task<TokenRefreshResult> RefreshAsync(string refreshToken, string? serverTarget, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return TokenRefreshResult.Failure("no refresh token");

            var baseUrl = NormalizeBase(serverTarget) ?? _defaultServerBaseUrl;
            if (string.IsNullOrEmpty(baseUrl))
                return TokenRefreshResult.Failure("no server target");

            try
            {
                using var content = new FormUrlEncodedContent(BuildRefreshForm(refreshToken, _clientId, _scope));
                using var response = await _httpClient.PostAsync(DeviceAuthService.TokenUrl(baseUrl), content, cancellationToken);
                var json = await response.Content.ReadAsStringAsync();
                var parsed = DeviceAuthService.ParseDeviceTokenResponse(json);
                return BuildResult(response.IsSuccessStatusCode, (int)response.StatusCode, parsed);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TokenRefreshResult.Failure(ex.Message);
            }
        }

        // ── Pure, unit-testable helpers ──────────────────────────────────────────────────────────────

        internal static IReadOnlyList<KeyValuePair<string, string>> BuildRefreshForm(string refreshToken, string clientId, string scope)
            => new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("scope", scope),
            };

        internal static TokenRefreshResult BuildResult(bool isSuccessStatusCode, int statusCode, DeviceTokenResponse? parsed)
        {
            if (parsed == null)
                return TokenRefreshResult.Failure("empty token response");
            if (!isSuccessStatusCode || string.IsNullOrEmpty(parsed.AccessToken))
                return TokenRefreshResult.Failure(parsed.Error ?? $"refresh failed (HTTP {statusCode})");

            DateTimeOffset? expiresAt = parsed.ExpiresIn > 0
                ? DateTimeOffset.UtcNow.AddSeconds(parsed.ExpiresIn)
                : (DateTimeOffset?)null;
            return TokenRefreshResult.Success(parsed.AccessToken!, parsed.RefreshToken, expiresAt);
        }

        /// <summary>
        /// Normalize a stored server target to the AS root: trim a trailing slash and a trailing
        /// <c>/mcp</c> hub segment so <c>/oauth/token</c> resolves on the authorization-server root.
        /// </summary>
        internal static string? NormalizeBase(string? serverTarget)
        {
            if (string.IsNullOrWhiteSpace(serverTarget))
                return null;
            var s = serverTarget!.Trim().TrimEnd('/');
            if (s.EndsWith("/mcp", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(0, s.Length - "/mcp".Length);
            return s;
        }
    }
}
