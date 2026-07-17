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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.AgentConfig;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Shared machine credential store round-trip + the zero-button auto-adopt behaviour (mcp-authorize
    /// design 06 / D12), exercised over a temporary store directory so nothing touches the real
    /// <c>~/.ai-game-dev/</c>. The refresh path (proactive + reactive) is driven by a mocked authorization
    /// server (<see cref="FakeTokenRefresher"/>), so there is no live network.
    /// </summary>
    public class MachineCredentialTests
    {
        sealed class FakeTokenRefresher : ITokenRefresher
        {
            readonly TokenRefreshResult _result;
            public int Calls { get; private set; }
            public string? LastRefreshToken { get; private set; }

            public FakeTokenRefresher(TokenRefreshResult result) => _result = result;

            public Task<TokenRefreshResult> RefreshAsync(string refreshToken, string? serverTarget, CancellationToken cancellationToken = default)
            {
                Calls++;
                LastRefreshToken = refreshToken;
                return Task.FromResult(_result);
            }
        }

        string _dir = null!;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "agd-cred-tests-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }

        MachineCredentialStore NewStore() => new MachineCredentialStore(_dir);

        [Test]
        public void Store_RoundTrips_Credentials()
        {
            var store = NewStore();
            Assert.IsFalse(store.Exists);

            var expiry = DateTimeOffset.UtcNow.AddHours(1);
            store.Write(new MachineCredentials
            {
                AccessToken = "es256.jwt",
                RefreshToken = "rt-1",
                ExpiresAt = expiry,
                ServerTarget = "https://ai-game.dev",
                Subject = "acct-123",
            });

            Assert.IsTrue(store.Exists);
            var read = store.Read();
            Assert.IsNotNull(read);
            Assert.AreEqual("es256.jwt", read!.AccessToken);
            Assert.AreEqual("rt-1", read.RefreshToken);
            Assert.AreEqual("https://ai-game.dev", read.ServerTarget);
            Assert.AreEqual("acct-123", read.Subject);
            Assert.AreEqual(expiry.ToUnixTimeSeconds(), read.ExpiresAt!.Value.ToUnixTimeSeconds());
        }

        [Test]
        public void EmptyStore_AutoAdopt_IsSignedOut()
        {
            using var provider = new PluginCredentialProvider(NewStore());
            Assert.IsFalse(provider.IsSignedIn);
            Assert.IsNull(provider.AsAccessTokenProvider()().GetAwaiter().GetResult());
        }

        [Test]
        public void PopulatedStore_AutoAdopts_SignedIn_ZeroButton()
        {
            var store = NewStore();
            store.Write(new MachineCredentials
            {
                AccessToken = "es256.jwt",
                RefreshToken = "rt-1",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                ServerTarget = "https://ai-game.dev",
            });

            // Construction reads the store — a present credential means SignedIn with no UI interaction.
            using var provider = new PluginCredentialProvider(store);
            Assert.IsTrue(provider.IsSignedIn);
            Assert.AreEqual("es256.jwt", provider.AsAccessTokenProvider()().GetAwaiter().GetResult());
            Assert.AreEqual("https://ai-game.dev", provider.ServerTarget);
        }

        [Test]
        public void ProactiveRefresh_NearExpiry_MintsAndPersists_NewToken()
        {
            var store = NewStore();
            store.Write(new MachineCredentials
            {
                AccessToken = "old.jwt",
                RefreshToken = "rt-old",
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(5), // within the 60s proactive-refresh skew
                ServerTarget = "https://ai-game.dev",
            });

            var refresher = new FakeTokenRefresher(TokenRefreshResult.Success(
                "new.jwt", "rt-new", DateTimeOffset.UtcNow.AddHours(1)));
            using var provider = new PluginCredentialProvider(store, refresher);

            var token = provider.AsAccessTokenProvider()().GetAwaiter().GetResult();

            Assert.AreEqual("new.jwt", token);
            Assert.AreEqual(1, refresher.Calls);
            Assert.AreEqual("rt-old", refresher.LastRefreshToken);
            // The rotated credential is persisted back to the store (survives a restart).
            var persisted = store.Read();
            Assert.AreEqual("new.jwt", persisted!.AccessToken);
            Assert.AreEqual("rt-new", persisted.RefreshToken);
        }

        [Test]
        public void ReactiveRefresh_Failure_SurfacesSignInRequired()
        {
            var store = NewStore();
            store.Write(new MachineCredentials
            {
                AccessToken = "old.jwt",
                RefreshToken = "rt-old",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                ServerTarget = "https://ai-game.dev",
            });

            var refresher = new FakeTokenRefresher(TokenRefreshResult.Failure("refresh token expired"));
            using var provider = new PluginCredentialProvider(store, refresher);

            var refreshed = provider.RefreshAsync().GetAwaiter().GetResult();

            Assert.IsFalse(refreshed);
            Assert.AreEqual(AuthState.SignInRequired, provider.State.CurrentValue);
        }

        [Test]
        public void SignOut_Deletes_StoredCredential()
        {
            var store = NewStore();
            store.Write(new MachineCredentials { AccessToken = "es256.jwt", RefreshToken = "rt-1" });

            using var provider = new PluginCredentialProvider(store);
            Assert.IsTrue(provider.IsSignedIn);

            provider.SignOut();

            Assert.IsFalse(provider.IsSignedIn);
            Assert.IsFalse(store.Exists);
        }
    }
}
