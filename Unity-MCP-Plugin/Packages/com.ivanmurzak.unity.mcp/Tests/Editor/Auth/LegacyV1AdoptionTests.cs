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
using System.IO;
using System.Net.Http;
using System.Text.Json;
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using NUnit.Framework;
using static com.IvanMurzak.Unity.MCP.Editor.Tests.CredentialWiringTestKit;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// F11.1 legacy adoption (unified-machine-auth 03 F11 / 04 §1+§3.7), verified end-to-end over the
    /// PRODUCTION wiring: a machine holding a <b>v1 credentials file</b> — seeded here as raw
    /// platform-correct bytes, exactly what a pre-v2 writer left on disk — is (a) auto-adopted as
    /// signed-in (families.legacy), (b) refreshed with the component-default <c>client_id</c> and
    /// <b>no</b> <c>scope</c>, and (c) rewritten as a v2 document (legacy family + v1 compat mirror)
    /// on the first successful refresh.
    /// </summary>
    public class LegacyV1AdoptionTests
    {
        string _dir = null!;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "agd-v1-adopt-tests-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }

        static string V1Json(DateTimeOffset expiresAt) => $@"{{
  ""version"": 1,
  ""accessToken"": ""v1.jwt"",
  ""refreshToken"": ""rt-v1"",
  ""expiresAt"": ""{expiresAt:O}"",
  ""serverTarget"": ""https://ai-game.dev""
}}";

        [Test]
        public void SeededV1File_AutoAdopts_AsSignedIn_WithoutAnyWrite()
        {
            SeedRawStoreJson(_dir, V1Json(DateTimeOffset.UtcNow.AddHours(1)));
            var store = new MachineCredentialStore(_dir);
            var handler = new CapturingHandler(TokenJson("unused.jwt", null));

            using var provider = AccountCredentialService.CreateProvider(store, "https://ai-game.dev", new HttpClient(handler));

            Assert.IsTrue(provider.IsSignedIn, "a v1 file must auto-adopt as signed in (zero-button, F11.1)");
            Assert.AreEqual("v1.jwt", provider.GetAccessTokenAsync().GetAwaiter().GetResult());
            Assert.AreEqual(0, handler.Requests.Count, "a still-valid v1 token needs no refresh");

            // Reading must not rewrite: the on-disk document is still the v1 original.
            using var document = JsonDocument.Parse(ReadRawStoreJson(_dir));
            Assert.AreEqual(1, document.RootElement.GetProperty("version").GetInt32());
        }

        [Test]
        public void SeededV1File_FirstSuccessfulRefresh_RewritesV2_WithLegacyFamily_AndMirror()
        {
            // Near-expiry so the first GetAccessTokenAsync refreshes through the real wire path.
            SeedRawStoreJson(_dir, V1Json(DateTimeOffset.UtcNow.AddSeconds(5)));
            var store = new MachineCredentialStore(_dir);
            var handler = new CapturingHandler(TokenJson("rotated.jwt", "rt-rotated"));

            using var provider = AccountCredentialService.CreateProvider(store, "https://ai-game.dev", new HttpClient(handler));
            var token = provider.GetAccessTokenAsync().GetAwaiter().GetResult();

            Assert.AreEqual("rotated.jwt", token);

            // The refresh presented the v1 credential per 04 §3.7: component default id, NO scope.
            Assert.AreEqual(1, handler.Requests.Count);
            var form = handler.Requests[0].Form;
            Assert.AreEqual("rt-v1", form["refresh_token"]);
            Assert.AreEqual(DeviceAuthService.DefaultClientId, form["client_id"]);
            Assert.IsFalse(form.ContainsKey("scope"), "v1-adopted refresh must NOT send `scope` (P0-3/F11.1)");

            // F11.1: the store is now a v2 document — legacy family holding the rotated tokens, and
            // the top-level v1 compat mirror re-stamped from it so pre-v2 readers keep working.
            using var document = JsonDocument.Parse(ReadRawStoreJson(_dir));
            var root = document.RootElement;
            Assert.AreEqual(2, root.GetProperty("version").GetInt32(), "first successful refresh rewrites the file as v2");
            var legacy = root.GetProperty("families").GetProperty("legacy");
            Assert.AreEqual("rotated.jwt", legacy.GetProperty("accessToken").GetString());
            Assert.AreEqual("rt-rotated", legacy.GetProperty("refreshToken").GetString());
            Assert.AreEqual("rotated.jwt", root.GetProperty("accessToken").GetString(), "v1 mirror must track the plugin-plane family");
            Assert.AreEqual("rt-rotated", root.GetProperty("refreshToken").GetString());
        }
    }
}
