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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using NUnit.Framework;
using R3;
using static com.IvanMurzak.Unity.MCP.Editor.Tests.CredentialWiringTestKit;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// WIRING-level refresh contract (unified-machine-auth d1 / 04 §3): these tests build the
    /// credential stack through the SAME factory the production editor uses
    /// (<see cref="AccountCredentialService.CreateProvider"/> → shared <c>HttpTokenRefresher</c>)
    /// and assert the bytes that actually leave over HTTP. They redden if the wiring regresses to a
    /// scope-sending refresher (the retired <c>UnityTokenRefresher</c> always sent
    /// <c>scope=mcp:plugin</c> — P0-3, the scope-narrowing hazard) or to presenting the component
    /// default <c>client_id</c> instead of the family's stored one (the cross-mint hazard: a
    /// refresh presented under the wrong client id re-stamps the family server-side).
    /// </summary>
    public class HttpRefresherWiringTests
    {
        string _dir = null!;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "agd-wiring-tests-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }

        MachineCredentialStore NewStore() => new MachineCredentialStore(_dir);

        [Test]
        public void Refresh_OmitsScopeAndResource_AndPresentsTheStoredClientId_NotTheComponentDefault()
        {
            var store = NewStore();
            store.Write(new MachineCredentials
            {
                ServerTarget = "https://ai-game.dev",
                Families = new MachineCredentialFamilies
                {
                    Plugin = new MachineCredentialFamily
                    {
                        AccessToken = "old.jwt",
                        RefreshToken = "rt-stored-1",
                        ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(5), // inside the 60 s proactive skew
                        // A DCR-minted id ≠ Unity's component default — the cross-mint discriminator.
                        // If the wiring presented "unity-mcp-plugin" here, the AS would treat the
                        // refresh as coming from a different client than the family was minted under.
                        ClientId = "app-dcr-6f2a1b",
                        Scope = "mcp:plugin",
                    },
                },
            });

            var handler = new CapturingHandler(TokenJson("new.jwt", "rt-2"));
            using var provider = AccountCredentialService.CreateProvider(store, "https://ai-game.dev", new HttpClient(handler));

            var token = provider.GetAccessTokenAsync().GetAwaiter().GetResult();

            Assert.AreEqual("new.jwt", token, "the proactive refresh must have run over the wire");
            Assert.AreEqual(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.AreEqual("https://ai-game.dev/oauth/token", request.Uri!.ToString());
            Assert.AreEqual("refresh_token", request.Form["grant_type"]);
            Assert.AreEqual("rt-stored-1", request.Form["refresh_token"]);

            // P0-3 (04 §3.3): NO scope on the wire — a component-default scope permanently narrows
            // an agent family server-side. NO resource either; the server falls back to the stored grant.
            Assert.IsFalse(request.Form.ContainsKey("scope"), "refresh must NOT send `scope` (P0-3)");
            Assert.IsFalse(request.Form.ContainsKey("resource"), "refresh must NOT send `resource` (04 §3.3)");

            // 04 §3.2: the family's STORED clientId is presented — never Unity's component default.
            Assert.AreEqual("app-dcr-6f2a1b", request.Form["client_id"]);

            // The rotation was persisted through the same wiring (survives an editor restart).
            var persisted = store.Read();
            Assert.AreEqual("new.jwt", persisted!.AccessToken);
            Assert.AreEqual("rt-2", persisted.RefreshToken);
        }

        [Test]
        public void Refresh_OfALegacyFamilyOfUnknownId_PresentsTheComponentDefault_AndStillOmitsScope()
        {
            var store = NewStore();
            // Top-level-only token material: the write contract adopts it as families.legacy — a
            // family whose clientId is unknown by definition (04 §3.7).
            store.Write(new MachineCredentials
            {
                AccessToken = "old.jwt",
                RefreshToken = "rt-legacy-1",
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(5),
                ServerTarget = "https://ai-game.dev",
            });

            var handler = new CapturingHandler(TokenJson("new.jwt", "rt-2"));
            using var provider = AccountCredentialService.CreateProvider(store, "https://ai-game.dev", new HttpClient(handler));

            var token = provider.GetAccessTokenAsync().GetAwaiter().GetResult();

            Assert.AreEqual("new.jwt", token);
            Assert.AreEqual(1, handler.Requests.Count);
            var form = handler.Requests[0].Form;
            // 04 §3.7: unknown stored id ⇒ the component default is presented...
            Assert.AreEqual(DeviceAuthService.DefaultClientId, form["client_id"]);
            // ...but — unlike the retired pre-b3 Unity refresher — scope is STILL omitted.
            Assert.IsFalse(form.ContainsKey("scope"), "legacy refresh must NOT send `scope` (P0-3)");
            Assert.IsFalse(form.ContainsKey("resource"));
        }

        [Test]
        public void TokenExpiry_SelfHeals_WithoutSignInPrompt()
        {
            // A1 (DoD): an expired-token editor session self-heals through the shared refresher —
            // the provider stays SignedIn throughout and never surfaces a re-auth prompt.
            var store = NewStore();
            store.Write(new MachineCredentials
            {
                ServerTarget = "https://ai-game.dev",
                Families = new MachineCredentialFamilies
                {
                    Plugin = new MachineCredentialFamily
                    {
                        AccessToken = "expired.jwt",
                        RefreshToken = "rt-expired-1",
                        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5), // already past expiry
                        ClientId = "unity-mcp-plugin",
                        Scope = "mcp:plugin",
                    },
                },
            });

            var signInRequiredFired = false;
            var handler = new CapturingHandler(TokenJson("healed.jwt", "rt-healed"));
            using var provider = AccountCredentialService.CreateProvider(store, "https://ai-game.dev", new HttpClient(handler));
            using var subscription = provider.OnSignInRequired.Subscribe(_ => signInRequiredFired = true);

            var token = provider.GetAccessTokenAsync().GetAwaiter().GetResult();

            Assert.AreEqual("healed.jwt", token);
            Assert.IsTrue(provider.IsSignedIn);
            Assert.IsFalse(signInRequiredFired, "self-healing must not surface a re-auth prompt (A1)");
        }
    }
}
