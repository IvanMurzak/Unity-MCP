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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    public static class DeviceAuthService
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        public class DeviceAuthorizeResponse
        {
            [JsonPropertyName("device_code")]
            public string DeviceCode { get; set; } = "";
            [JsonPropertyName("user_code")]
            public string UserCode { get; set; } = "";
            [JsonPropertyName("verification_uri")]
            public string VerificationUri { get; set; } = "";
            [JsonPropertyName("verification_uri_complete")]
            public string VerificationUriComplete { get; set; } = "";
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
            [JsonPropertyName("interval")]
            public int Interval { get; set; }
        }

        public class DeviceTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }
            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }
            [JsonPropertyName("error")]
            public string? Error { get; set; }
            [JsonPropertyName("error_description")]
            public string? ErrorDescription { get; set; }
        }

        public static async Task<DeviceAuthorizeResponse> InitiateDeviceAuthAsync(
            string serverUrl, string? clientLabel, CancellationToken ct = default)
        {
            var body = clientLabel != null
                ? JsonSerializer.Serialize(new { client_label = clientLabel }, _jsonOptions)
                : "{}";
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{serverUrl.TrimEnd('/')}/api/auth/device/authorize", content, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DeviceAuthorizeResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize device auth response");
        }

        public static async Task<DeviceTokenResponse> PollDeviceTokenAsync(
            string serverUrl, string deviceCode, CancellationToken ct = default)
        {
            var body = JsonSerializer.Serialize(new { device_code = deviceCode, grant_type = "urn:ietf:params:oauth:grant-type:device_code" }, _jsonOptions);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{serverUrl.TrimEnd('/')}/api/auth/device/token", content, ct);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DeviceTokenResponse>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize device token response");
        }
    }
}
