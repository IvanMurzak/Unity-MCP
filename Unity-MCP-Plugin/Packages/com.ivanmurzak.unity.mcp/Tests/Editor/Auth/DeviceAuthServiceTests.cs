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
using com.IvanMurzak.Unity.MCP.Editor.Services;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// RFC 8628 request-building + response-parsing coverage for <see cref="DeviceAuthService"/> against a
    /// mocked authorization server (canned JSON), with no live network. Exercises the exact
    /// <c>client_id</c> + <c>scope=mcp:agent</c> LOGIN form the ai-game.dev AS requires
    /// (unified-machine-auth 03 F1.2 — the plugin family is derived by token exchange, never minted
    /// directly by the device flow) and the token/refresh response shapes.
    /// </summary>
    public class DeviceAuthServiceTests
    {
        static string Value(IReadOnlyList<KeyValuePair<string, string>> form, string key)
            => form.First(kv => kv.Key == key).Value;

        [Test]
        public void DeviceAuthorizeForm_Carries_ClientId_And_AgentScope()
        {
            var form = DeviceAuthService.BuildDeviceAuthorizeForm(DeviceAuthService.DefaultClientId, DeviceAuthService.AgentScope);
            Assert.AreEqual(DeviceAuthService.DefaultClientId, Value(form, "client_id"));
            Assert.AreEqual("mcp:agent", Value(form, "scope"));
        }

        [Test]
        public void DefaultConstruction_Requests_AgentScope_With_UnityClientId()
        {
            // The wiring pin for 03 F1.2: the in-editor Authorize path constructs the service with
            // defaults, so THIS is what goes on the wire — scope=mcp:agent (the LOGIN scope; the
            // plugin family comes from RFC 8693 exchange afterwards) under client_id=unity-mcp-plugin.
            var service = new DeviceAuthService("https://ai-game.dev");
            Assert.AreEqual("mcp:agent", service.Scope);
            Assert.AreEqual("unity-mcp-plugin", service.ClientId);
        }

        [Test]
        public void DeviceTokenForm_Carries_DeviceCodeGrant_And_ClientId()
        {
            var form = DeviceAuthService.BuildDeviceTokenForm("dc-123", DeviceAuthService.DefaultClientId);
            Assert.AreEqual("urn:ietf:params:oauth:grant-type:device_code", Value(form, "grant_type"));
            Assert.AreEqual("dc-123", Value(form, "device_code"));
            Assert.AreEqual(DeviceAuthService.DefaultClientId, Value(form, "client_id"));
        }

        [Test]
        public void Endpoints_Resolve_On_The_AS_Root_Without_Double_Slash()
        {
            Assert.AreEqual("https://ai-game.dev/oauth/device_authorization", DeviceAuthService.DeviceAuthorizeUrl("https://ai-game.dev"));
            Assert.AreEqual("https://ai-game.dev/oauth/device_authorization", DeviceAuthService.DeviceAuthorizeUrl("https://ai-game.dev/"));
            Assert.AreEqual("https://ai-game.dev/oauth/token", DeviceAuthService.TokenUrl("https://ai-game.dev/"));
        }

        [Test]
        public void ParseDeviceAuthorizeResponse_Reads_RFC8628_Document()
        {
            const string json = @"{
                ""device_code"": ""DC-abc"",
                ""user_code"": ""WXYZ-1234"",
                ""verification_uri"": ""https://ai-game.dev/device"",
                ""verification_uri_complete"": ""https://ai-game.dev/device?code=WXYZ-1234"",
                ""expires_in"": 600,
                ""interval"": 5
            }";

            var parsed = DeviceAuthService.ParseDeviceAuthorizeResponse(json);
            Assert.AreEqual("DC-abc", parsed.DeviceCode);
            Assert.AreEqual("WXYZ-1234", parsed.UserCode);
            Assert.AreEqual("https://ai-game.dev/device", parsed.VerificationUri);
            Assert.AreEqual("https://ai-game.dev/device?code=WXYZ-1234", parsed.VerificationUriComplete);
            Assert.AreEqual(600, parsed.ExpiresIn);
            Assert.AreEqual(5, parsed.Interval);
        }

        [Test]
        public void ParseDeviceTokenResponse_Reads_AccessToken_RefreshToken_ExpiresIn()
        {
            const string json = @"{
                ""access_token"": ""es256.jwt.here"",
                ""token_type"": ""Bearer"",
                ""expires_in"": 3600,
                ""refresh_token"": ""rt-rotating"",
                ""scope"": ""mcp:plugin""
            }";

            var parsed = DeviceAuthService.ParseDeviceTokenResponse(json);
            Assert.AreEqual("es256.jwt.here", parsed.AccessToken);
            Assert.AreEqual("rt-rotating", parsed.RefreshToken);
            Assert.AreEqual(3600, parsed.ExpiresIn);
            Assert.AreEqual("mcp:plugin", parsed.Scope);
            Assert.IsNull(parsed.Error);
        }

        [Test]
        public void ParseDeviceTokenResponse_Reads_Pending_Error_Body()
        {
            const string json = @"{ ""error"": ""authorization_pending"", ""error_description"": ""pending"" }";
            var parsed = DeviceAuthService.ParseDeviceTokenResponse(json);
            Assert.IsNull(parsed.AccessToken);
            Assert.AreEqual("authorization_pending", parsed.Error);
        }

        [Test]
        public void Defaults_Apply_When_ClientId_Or_Scope_Omitted()
        {
            // A blank client id / scope should fall back to the login defaults rather than sending empty values.
            var service = new DeviceAuthService("https://ai-game.dev", clientId: " ", scope: "");
            Assert.AreEqual("unity-mcp-plugin", service.ClientId);
            Assert.AreEqual("mcp:agent", service.Scope);
            Assert.AreEqual("mcp:plugin", DeviceAuthService.PluginScope);
        }

        // ── JWT subject decoding (04 §1: `subject` from the JWT `sub` where available; D6/F7 guard input) ──

        static string Jwt(string payloadJson)
        {
            static string B64Url(string s) => System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{B64Url(@"{""alg"":""ES256"",""typ"":""JWT""}")}.{B64Url(payloadJson)}.sig";
        }

        [Test]
        public void DecodeJwtSubject_Reads_Sub_Claim_Unverified()
        {
            Assert.AreEqual("usr_42", DeviceAuthService.DecodeJwtSubject(Jwt(@"{""sub"":""usr_42"",""aud"":""urn:agd:hub""}")));
            // Payload lengths that exercise every base64url padding branch (0/1/2 '=' chars).
            Assert.AreEqual("u", DeviceAuthService.DecodeJwtSubject(Jwt(@"{""sub"":""u""}")));
            Assert.AreEqual("usr", DeviceAuthService.DecodeJwtSubject(Jwt(@"{""sub"":""usr""}")));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("not-a-jwt")]
        public void DecodeJwtSubject_Malformed_IsNull(string? token)
        {
            Assert.IsNull(DeviceAuthService.DecodeJwtSubject(token));
        }

        [Test]
        public void DecodeJwtSubject_BadBase64Payload_IsNull()
        {
            Assert.IsNull(DeviceAuthService.DecodeJwtSubject("a.!!!not-base64!!!.c"));
        }

        [Test]
        public void DecodeJwtSubject_NoSubClaim_IsNull()
        {
            Assert.IsNull(DeviceAuthService.DecodeJwtSubject(Jwt(@"{""aud"":""urn:agd:hub""}")));
            Assert.IsNull(DeviceAuthService.DecodeJwtSubject(Jwt(@"{""sub"":42}")));
            Assert.IsNull(DeviceAuthService.DecodeJwtSubject(Jwt(@"{""sub"":""""}")));
        }
    }
}
