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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    /// <summary>
    /// Device-authorization client contract (mcp-authorize design 03 Flow B / design 06). Split from the
    /// concrete <see cref="DeviceAuthService"/> so the <see cref="DeviceAuthFlow"/> state machine can be
    /// exercised against a mocked authorization server with no live network dependency.
    /// </summary>
    public interface IDeviceAuthClient
    {
        /// <summary>Begin the flow: <c>POST /oauth/device_authorization</c> → device/user code document.</summary>
        Task<DeviceAuthorizeResponse> RequestDeviceCodeAsync(CancellationToken ct = default);

        /// <summary>Poll for the token: <c>POST /oauth/token</c> (device-code grant). Pending is a soft error.</summary>
        Task<DeviceTokenResponse> PollTokenAsync(string deviceCode, CancellationToken ct = default);
    }

    /// <summary>RFC 8628 <c>device_authorization</c> response document.</summary>
    public class DeviceAuthorizeResponse
    {
        [JsonPropertyName("device_code")] public string DeviceCode { get; set; } = "";
        [JsonPropertyName("user_code")] public string UserCode { get; set; } = "";
        [JsonPropertyName("verification_uri")] public string VerificationUri { get; set; } = "";
        [JsonPropertyName("verification_uri_complete")] public string VerificationUriComplete { get; set; } = "";
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("interval")] public int Interval { get; set; }
    }

    /// <summary>
    /// OAuth 2.1 token response for the device-code grant (design 03 Flow B). On success it carries the
    /// ES256 MCP JWT (<c>access_token</c>) + the rotating <c>refresh_token</c> + <c>expires_in</c>; while
    /// authorization is pending it carries an RFC 6749 §5.2 <c>error</c> (<c>authorization_pending</c> /
    /// <c>slow_down</c> / <c>access_denied</c> / <c>expired_token</c>) with HTTP 400.
    /// </summary>
    public class DeviceTokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("token_type")] public string? TokenType { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("scope")] public string? Scope { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    /// <summary>
    /// RFC 8628-conformant device-authorization client for the ai-game.dev authorization server
    /// (mcp-authorize design 03 Flow B / design 05). It POSTs <c>client_id</c> + <c>scope=mcp:plugin</c>
    /// (form-encoded) to <c>{base}/oauth/device_authorization</c>, then redeems the grant at
    /// <c>{base}/oauth/token</c> with the device-code URN — yielding an ES256 hub JWT
    /// (<c>aud=urn:agd:hub</c>) plus a rotating refresh token. This replaces the legacy
    /// <c>/api/auth/device/*</c> JSON flow. It never persists anything itself — the caller
    /// (<see cref="DeviceAuthFlow"/>) stores the credential in the shared machine credential store (D12).
    /// </summary>
    public sealed class DeviceAuthService : IDeviceAuthClient
    {
        /// <summary>Public device client id (audit only; the AS accepts any non-empty value for the device grant).</summary>
        public const string DefaultClientId = "unity-mcp-plugin";

        /// <summary>Scope selecting the ES256 MCP-plugin JWT + refresh-token response (design 08 §roles).</summary>
        public const string PluginScope = "mcp:plugin";

        /// <summary>RFC 8628 device-code grant type redeemed at <c>/oauth/token</c>.</summary>
        public const string DeviceCodeGrantType = "urn:ietf:params:oauth:grant-type:device_code";

        static readonly HttpClient _sharedHttpClient = new HttpClient();
        static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        readonly string _serverBaseUrl;
        readonly string _clientId;
        readonly string _scope;
        readonly HttpClient _httpClient;

        /// <param name="serverBaseUrl">The AS root (e.g. <c>https://ai-game.dev</c>) — NOT the <c>/mcp</c> hub URL.</param>
        public DeviceAuthService(string serverBaseUrl, string? clientId = null, string? scope = null, HttpClient? httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(serverBaseUrl))
                throw new ArgumentException("serverBaseUrl is required", nameof(serverBaseUrl));
            _serverBaseUrl = serverBaseUrl.TrimEnd('/');
            _clientId = string.IsNullOrWhiteSpace(clientId) ? DefaultClientId : clientId!;
            _scope = string.IsNullOrWhiteSpace(scope) ? PluginScope : scope!;
            _httpClient = httpClient ?? _sharedHttpClient;
        }

        public async Task<DeviceAuthorizeResponse> RequestDeviceCodeAsync(CancellationToken ct = default)
        {
            using var content = new FormUrlEncodedContent(BuildDeviceAuthorizeForm(_clientId, _scope));
            using var response = await _httpClient.PostAsync(DeviceAuthorizeUrl(_serverBaseUrl), content, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return ParseDeviceAuthorizeResponse(json);
        }

        public async Task<DeviceTokenResponse> PollTokenAsync(string deviceCode, CancellationToken ct = default)
        {
            using var content = new FormUrlEncodedContent(BuildDeviceTokenForm(deviceCode, _clientId));
            // Do NOT EnsureSuccessStatusCode: authorization_pending / slow_down come back as HTTP 400 with
            // an RFC 6749 §5.2 JSON error body that DeviceAuthFlow inspects to decide whether to keep polling.
            using var response = await _httpClient.PostAsync(TokenUrl(_serverBaseUrl), content, ct);
            var json = await response.Content.ReadAsStringAsync();
            return ParseDeviceTokenResponse(json);
        }

        // ── Pure, unit-testable request/response helpers (mocked-AS coverage without an HttpMessageHandler) ──

        internal static string DeviceAuthorizeUrl(string serverBaseUrl) => $"{serverBaseUrl.TrimEnd('/')}/oauth/device_authorization";
        internal static string TokenUrl(string serverBaseUrl) => $"{serverBaseUrl.TrimEnd('/')}/oauth/token";

        internal static IReadOnlyList<KeyValuePair<string, string>> BuildDeviceAuthorizeForm(string clientId, string scope)
            => new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("scope", scope),
            };

        internal static IReadOnlyList<KeyValuePair<string, string>> BuildDeviceTokenForm(string deviceCode, string clientId)
            => new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", DeviceCodeGrantType),
                new KeyValuePair<string, string>("device_code", deviceCode),
                new KeyValuePair<string, string>("client_id", clientId),
            };

        internal static DeviceAuthorizeResponse ParseDeviceAuthorizeResponse(string json)
            => JsonSerializer.Deserialize<DeviceAuthorizeResponse>(json, _jsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialize device authorization response");

        internal static DeviceTokenResponse ParseDeviceTokenResponse(string json)
            => JsonSerializer.Deserialize<DeviceTokenResponse>(json, _jsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialize device token response");
    }
}
