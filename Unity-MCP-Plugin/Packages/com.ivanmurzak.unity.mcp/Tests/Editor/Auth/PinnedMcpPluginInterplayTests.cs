/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using Microsoft.AspNetCore.SignalR.Client;
using NUnit.Framework;
using R3;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The a2/a3 interplay evidence deferred from c2 to r2 (oauth-client-error-hygiene): these
    /// tests run against the REAL pinned McpPlugin assembly (8.3.0+) and fail compiled against
    /// 8.1.0, so they pin that the NuGet bump actually carries the behavior the Unity D4 ladder
    /// (AssistedReauthService/Gate) renders and relies on:
    /// <list type="bullet">
    ///   <item>a2 — the dead-family memo blocks re-presenting a dead refresh token (no reconnect
    ///   churn against the authorization server), and a peer surface's store rotation re-arms it;</item>
    ///   <item>a3 — the ConnectionCredentialCoordinator stops the connect loop on the dead-family
    ///   verdict and resumes it by itself on the SignInRequired→SignedIn edge;</item>
    ///   <item>C2 — an invalid_target verdict's SignInRequiredReason carries the
    ///   "server configuration error" class prefix that AssistedReauthGate.ClassifyReason keys the
    ///   distinct rendering on.</item>
    /// </list>
    /// Store I/O is confined to a per-test temp directory; the refresher is scripted (no network).
    /// </summary>
    public class PinnedMcpPluginInterplayTests
    {
        const string SeededAccess = "eyJ.PLUGIN.sig";
        const string SeededRefresh = "rt-dead-aaa";
        const string ServerTarget = "https://ai-game.dev";

        /// <summary>Scripted refresher: the pending result is swapped mid-test; every request is recorded.</summary>
        sealed class ScriptedTokenRefresher : ITokenRefresher
        {
            public TokenRefreshResult Next;
            public readonly List<string> PresentedTokens = new List<string>();

            public ScriptedTokenRefresher(TokenRefreshResult first) => Next = first;

            public Task<TokenRefreshResult> RefreshAsync(string refreshToken, string? serverTarget, CancellationToken cancellationToken = default)
            {
                PresentedTokens.Add(refreshToken);
                return Task.FromResult(Next);
            }
        }

        /// <summary>
        /// Minimal <see cref="IConnection"/> fake mirroring the real ConnectionManager's stop
        /// semantics: <see cref="Disconnect"/> clears KeepConnected (the loop's intent flag) and
        /// <see cref="Connect"/> restores it. Everything completes synchronously.
        /// </summary>
        sealed class FakeConnection : IConnection
        {
            readonly ReactiveProperty<bool> _keepConnected = new ReactiveProperty<bool>(false);
            readonly ReactiveProperty<HubConnectionState> _state = new ReactiveProperty<HubConnectionState>(HubConnectionState.Disconnected);
            readonly Subject<Unit> _authRejected = new Subject<Unit>();

            public int ConnectCalls;
            public int DisconnectCalls;

            public ReadOnlyReactiveProperty<bool> KeepConnected => _keepConnected;
            public ReadOnlyReactiveProperty<HubConnectionState> ConnectionState => _state;
            public Observable<Unit> OnAuthorizationRejected => _authRejected;

            public void StartLoop() => _keepConnected.Value = true; // the consumer's KeepConnected intent

            public Task<bool> Connect(CancellationToken cancellationToken = default)
            {
                ConnectCalls++;
                _keepConnected.Value = true;
                _state.Value = HubConnectionState.Connected;
                return Task.FromResult(true);
            }

            public Task Disconnect(CancellationToken cancellationToken = default)
            {
                DisconnectCalls++;
                _keepConnected.Value = false; // Disconnect clears _continueToReconnect in production
                _state.Value = HubConnectionState.Disconnected;
                return Task.CompletedTask;
            }

            public void DisconnectImmediate() { }
            public bool WaitForImmediateTeardown(TimeSpan timeout) => true;
            public void Dispose() { }
        }

        string _dir = null!;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "agd-interplay-tests-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }

        MachineCredentialStore NewStore() => new MachineCredentialStore(_dir);

        void SeedPluginFamily()
        {
            NewStore().Write(new MachineCredentials
            {
                ServerTarget = ServerTarget,
                Subject = "usr_123",
                Families = new MachineCredentialFamilies
                {
                    Plugin = new MachineCredentialFamily
                    {
                        AccessToken = SeededAccess,
                        RefreshToken = SeededRefresh,
                        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
                        Scope = "mcp:plugin",
                    },
                },
            });
        }

        /// <summary>
        /// A peer surface (App, CLI, another editor) rotates ONLY the refresh token — the same
        /// access token — which isolates the a2 memo re-arm from the pre-existing peer-adoption
        /// path (that one keys on the access token).
        /// </summary>
        void PeerRotatesRefreshTokenOnly(string newRefreshToken)
        {
            var peer = NewStore().Read()!;
            peer.Families!.Plugin!.RefreshToken = newRefreshToken;
            NewStore().Write(peer);
        }

        static void WaitUntil(Func<bool> condition, string what)
        {
            for (var i = 0; i < 100; i++)
            {
                if (condition())
                    return;
                Thread.Sleep(20);
            }
            Assert.IsTrue(condition(), "condition not reached within 2s: " + what);
        }

        [Test]
        public void A2_DeadFamilyMemo_BlocksSecondAttempt_AndPeerRotationRearms_OneNetworkRefresh()
        {
            SeedPluginFamily();
            var refresher = new ScriptedTokenRefresher(
                TokenRefreshResult.Failure("invalid_grant: revoked", TokenRefreshFailureKind.InvalidGrant));
            using var provider = new PluginCredentialProvider(NewStore(), refresher);

            // The verdict: ONE network attempt, dead family confirmed by the post-failure re-read.
            Assert.IsFalse(provider.RefreshAsync().GetAwaiter().GetResult());
            Assert.AreEqual(AuthState.SignInRequired, provider.State.CurrentValue);
            Assert.AreEqual(1, refresher.PresentedTokens.Count);

            // a2: the memo — not any rate window (the scripted fake has none) — blocks the second
            // attempt; the dead token is never re-presented to the authorization server.
            Assert.IsFalse(provider.RefreshAsync().GetAwaiter().GetResult());
            Assert.AreEqual(1, refresher.PresentedTokens.Count);
            Assert.AreEqual(AuthState.SignInRequired, provider.State.CurrentValue);

            // Peer login re-arms (02 §C1): the in-lock store re-read sees a DIFFERENT token, so the
            // refresh goes out presenting the STORE's token — never the memoized dead one.
            PeerRotatesRefreshTokenOnly("rt-plugin-peer");
            refresher.Next = TokenRefreshResult.Success("eyJ.FRESH.sig", "rt-fresh", DateTimeOffset.UtcNow.AddHours(1));

            Assert.IsTrue(provider.RefreshAsync().GetAwaiter().GetResult());
            Assert.AreEqual(2, refresher.PresentedTokens.Count);
            Assert.AreEqual("rt-plugin-peer", refresher.PresentedTokens[1]);
            Assert.AreEqual(AuthState.SignedIn, provider.State.CurrentValue);
            Assert.IsNull(provider.SignInRequiredReason);
        }

        [Test]
        public void A3_Coordinator_StopsLoopOnDeadVerdict_AndResumesOnSignedInEdge()
        {
            SeedPluginFamily();
            var refresher = new ScriptedTokenRefresher(
                TokenRefreshResult.Failure("invalid_grant: revoked", TokenRefreshFailureKind.InvalidGrant));
            using var provider = new PluginCredentialProvider(NewStore(), refresher);
            using var connection = new FakeConnection();
            using var coordinator = new ConnectionCredentialCoordinator(connection, provider);

            connection.StartLoop(); // the consumer's connect loop is live (KeepConnected intent on)

            // a3 stop (02 §C3.1): the dead-family verdict stops the loop instead of letting it
            // re-present the dead credential every retry period.
            Assert.IsFalse(provider.RefreshAsync().GetAwaiter().GetResult());
            WaitUntil(() => connection.DisconnectCalls == 1, "coordinator Disconnect on the dead-family verdict");
            Assert.IsFalse(connection.KeepConnected.CurrentValue);
            Assert.AreEqual(0, connection.ConnectCalls);

            // a3 resume (02 §C3.2): a peer login turns the provider SignedIn — the coordinator
            // resumes the stopped loop on the SignInRequired→SignedIn edge by itself.
            PeerRotatesRefreshTokenOnly("rt-plugin-peer");
            refresher.Next = TokenRefreshResult.Success("eyJ.FRESH.sig", "rt-fresh", DateTimeOffset.UtcNow.AddHours(1));
            Assert.IsTrue(provider.RefreshAsync().GetAwaiter().GetResult());

            WaitUntil(() => connection.ConnectCalls == 1, "coordinator Connect on the SignInRequired→SignedIn edge");
            Assert.AreEqual(1, connection.DisconnectCalls); // exactly one stop pass, no churn
        }

        [Test]
        public void C2_InvalidTarget_ReasonCarriesServerConfigurationClass_AndGateRendersDistinctStatus()
        {
            SeedPluginFamily();
            var refresher = new ScriptedTokenRefresher(
                TokenRefreshResult.Failure("invalid_target: audience mismatch", TokenRefreshFailureKind.InvalidTarget));
            using var provider = new PluginCredentialProvider(NewStore(), refresher);

            Assert.IsFalse(provider.RefreshAsync().GetAwaiter().GetResult());
            Assert.AreEqual(AuthState.SignInRequired, provider.State.CurrentValue);

            // The LIVE 8.3.0 contract: invalid_target's reason carries the class prefix with the
            // raw OAuth error preserved after it (02 §C2) — the string our classifier keys on.
            var reason = provider.SignInRequiredReason;
            Assert.IsNotNull(reason);
            StringAssert.StartsWith(AssistedReauthGate.ServerConfigurationErrorReasonPrefix, reason);
            StringAssert.Contains("invalid_target", reason);

            // End-to-end: the real provider reason classifies to the server-configuration class and
            // renders the distinct status (not the generic session-expired one).
            var reasonClass = AssistedReauthGate.ClassifyReason(reason);
            Assert.AreEqual(AssistedReauthReasonClass.ServerConfiguration, reasonClass);
            Assert.AreEqual(AssistedReauthGate.StatusServerConfigurationError, AssistedReauthGate.StatusFor(reasonClass));

            // Control in the same fixture family: an invalid_grant verdict classifies generic.
            Assert.AreEqual(AssistedReauthReasonClass.SessionExpired,
                AssistedReauthGate.ClassifyReason("invalid_grant: revoked"));
        }
    }
}
