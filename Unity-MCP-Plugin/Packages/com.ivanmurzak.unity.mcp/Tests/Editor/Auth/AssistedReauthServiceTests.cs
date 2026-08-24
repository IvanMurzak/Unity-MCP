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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// The service-level assisted sign-in runner (<see cref="AssistedReauthService.RunSignInCoreAsync"/>,
    /// oauth-client-error-hygiene 02 §C4 / D4): browser-open stays behind the injected seam (no real
    /// browser in CI), polling stops on expiry and on cancellation/disposal, and is NEVER
    /// re-initiated unattended. Runs against a mocked authorization server; pending awaits run on
    /// the thread pool (via <see cref="Task.Run(Func{Task})"/>) so the flow's continuations never
    /// deadlock against the blocked Unity main thread.
    /// </summary>
    public class AssistedReauthServiceTests
    {
        sealed class FakeDeviceAuthClient : IDeviceAuthClient
        {
            readonly DeviceAuthorizeResponse _authorize;
            readonly Queue<DeviceTokenResponse> _tokens;
            int _requestCount;
            int _pollCount;

            public string ClientId => "unity-mcp-plugin";
            public int RequestCount => Volatile.Read(ref _requestCount);
            public int PollCount => Volatile.Read(ref _pollCount);

            public FakeDeviceAuthClient(DeviceAuthorizeResponse authorize, IEnumerable<DeviceTokenResponse> tokens)
            {
                _authorize = authorize;
                _tokens = new Queue<DeviceTokenResponse>(tokens);
            }

            public Task<DeviceAuthorizeResponse> RequestDeviceCodeAsync(CancellationToken ct = default)
            {
                Interlocked.Increment(ref _requestCount);
                return Task.FromResult(_authorize);
            }

            public Task<DeviceTokenResponse> PollTokenAsync(string deviceCode, CancellationToken ct = default)
            {
                Interlocked.Increment(ref _pollCount);
                lock (_tokens)
                    return Task.FromResult(_tokens.Count > 0 ? _tokens.Dequeue() : Pending());
            }
        }

        static DeviceAuthorizeResponse Authorize(int expiresIn = 600) => new DeviceAuthorizeResponse
        {
            DeviceCode = "DC-1",
            UserCode = "WXYZ-1234",
            VerificationUri = "https://ai-game.dev/device",
            VerificationUriComplete = "https://ai-game.dev/device?code=WXYZ-1234",
            ExpiresIn = expiresIn,
            Interval = 5,
        };

        static DeviceTokenResponse Pending() => new DeviceTokenResponse { Error = "authorization_pending" };
        static DeviceTokenResponse ServerExpired() => new DeviceTokenResponse { Error = "expired_token" };

        /// <summary>A structurally valid (unverified) JWT whose payload carries the given JSON.</summary>
        static string Jwt(string payloadJson)
        {
            static string B64Url(string s) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{B64Url(@"{""alg"":""ES256"",""typ"":""JWT""}")}.{B64Url(payloadJson)}.sig";
        }

        static DeviceTokenResponse Success() => new DeviceTokenResponse
        {
            AccessToken = Jwt(@"{""sub"":""usr_42"",""aud"":""urn:agd:hub""}"),
            RefreshToken = "rt-1",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            Scope = "mcp:agent",
        };

        /// <summary>Run the core off the Unity main thread so pending awaits resume on the pool.</summary>
        static AssistedReauthOutcome Run(Func<Task<AssistedReauthOutcome>> flow)
            => Task.Run(flow).GetAwaiter().GetResult();

        [Test]
        public void Approval_OpensBrowserThroughTheInjectedSeam_AndCommitsExactlyOnce()
        {
            string? opened = null;
            var commits = 0;
            var client = new FakeDeviceAuthClient(Authorize(), new[] { Pending(), Success() });

            var outcome = Run(() => AssistedReauthService.RunSignInCoreAsync(
                client,
                serverTarget: "https://ai-game.dev",
                openBrowser: url => opened = url, // the D4 browser-open seam — no real browser in CI
                delay: (_, __) => Task.CompletedTask,
                commit: (login, ct) =>
                {
                    Interlocked.Increment(ref commits);
                    Assert.AreEqual("rt-1", login.AgentFamily.RefreshToken);
                    return Task.FromResult(AssistedReauthOutcome.Committed);
                }));

            Assert.AreEqual(AssistedReauthOutcome.Committed, outcome);
            Assert.AreEqual("https://ai-game.dev/device?code=WXYZ-1234", opened);
            Assert.AreEqual(1, commits);
        }

        [Test]
        public void ServerExpiry_StopsThePoll_NoReinitiation_NoCommit()
        {
            var commits = 0;
            var client = new FakeDeviceAuthClient(Authorize(), new[] { Pending(), ServerExpired() });

            var outcome = Run(() => AssistedReauthService.RunSignInCoreAsync(
                client,
                serverTarget: "https://ai-game.dev",
                openBrowser: _ => { },
                delay: (_, __) => Task.CompletedTask,
                commit: (login, ct) => { Interlocked.Increment(ref commits); return Task.FromResult(AssistedReauthOutcome.Committed); }));

            Assert.AreEqual(AssistedReauthOutcome.Expired, outcome);
            Assert.AreEqual(0, commits);
            // Never re-initiated unattended (D4): exactly ONE device-code request went out, and the
            // poll stopped at the server's expired_token verdict.
            Assert.AreEqual(1, client.RequestCount);
            Assert.AreEqual(2, client.PollCount);
        }

        [Test]
        public void DeadlineExpiry_ShortFixtureExpiry_StopsThePoll_AndNeverReinitiates()
        {
            var commits = 0;
            // 1-second device-code lifetime (the short fixture expiry), pending forever, a real but
            // tiny poll delay: the RFC 8628 deadline is what ends the flow.
            var client = new FakeDeviceAuthClient(Authorize(expiresIn: 1), Array.Empty<DeviceTokenResponse>());

            var outcome = Run(() => AssistedReauthService.RunSignInCoreAsync(
                client,
                serverTarget: "https://ai-game.dev",
                openBrowser: _ => { },
                delay: (_, ct) => Task.Delay(TimeSpan.FromMilliseconds(25), ct),
                commit: (login, ct) => { Interlocked.Increment(ref commits); return Task.FromResult(AssistedReauthOutcome.Committed); }));

            Assert.AreEqual(AssistedReauthOutcome.Expired, outcome);
            Assert.AreEqual(0, commits);
            Assert.AreEqual(1, client.RequestCount); // no unattended re-initiation

            // The poll loop is dead after the deadline: no further polls arrive.
            var pollsAtCompletion = client.PollCount;
            Thread.Sleep(200);
            Assert.AreEqual(pollsAtCompletion, client.PollCount);
        }

        [Test]
        public void Cancellation_StopsThePoll_NoCommit_NoReinitiation()
        {
            var commits = 0;
            using var cts = new CancellationTokenSource();
            // Pending forever + a delay that parks until cancelled: only disposal/cancellation can
            // end this flow (within the device-code lifetime).
            var client = new FakeDeviceAuthClient(Authorize(), Array.Empty<DeviceTokenResponse>());

            var task = Task.Run(() => AssistedReauthService.RunSignInCoreAsync(
                client,
                serverTarget: "https://ai-game.dev",
                openBrowser: _ => { },
                delay: (_, ct) => Task.Delay(Timeout.InfiniteTimeSpan, ct),
                commit: (login, ct) => { Interlocked.Increment(ref commits); return Task.FromResult(AssistedReauthOutcome.Committed); },
                onFlow: null,
                cancellationToken: cts.Token));

            // Wait for the flow to actually start (the device-code request is its first step),
            // then cancel while it is parked in the poll delay.
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (client.RequestCount == 0 && DateTime.UtcNow < deadline)
                Thread.Sleep(10);
            Assert.AreEqual(1, client.RequestCount, "flow never started");

            cts.Cancel();
            var outcome = task.GetAwaiter().GetResult();

            Assert.AreEqual(AssistedReauthOutcome.Cancelled, outcome);
            Assert.AreEqual(0, commits);
            Assert.AreEqual(1, client.RequestCount); // cancellation never re-initiates

            // The poll loop is dead after cancellation: no further polls arrive.
            var pollsAtCompletion = client.PollCount;
            Thread.Sleep(200);
            Assert.AreEqual(pollsAtCompletion, client.PollCount);
        }

        [Test]
        public void CancelledBeforeStart_DoesNothing()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var client = new FakeDeviceAuthClient(Authorize(), Array.Empty<DeviceTokenResponse>());

            var outcome = Run(() => AssistedReauthService.RunSignInCoreAsync(
                client,
                serverTarget: "https://ai-game.dev",
                openBrowser: _ => { },
                delay: (_, __) => Task.CompletedTask,
                commit: (login, ct) => Task.FromResult(AssistedReauthOutcome.Committed),
                onFlow: null,
                cancellationToken: cts.Token));

            Assert.AreEqual(AssistedReauthOutcome.Cancelled, outcome);
            Assert.AreEqual(0, client.RequestCount); // nothing went out on the wire
        }
    }
}
