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
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Editor.Services;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Drives the <see cref="DeviceAuthFlow"/> state machine against a mocked authorization server
    /// (<see cref="FakeDeviceAuthClient"/>) with a no-op browser + zero delay, so it runs fully in-process
    /// with no live network. Verifies the terminal states and that a successful flow yields a
    /// <see cref="MachineCredentials"/> carrying the access token, refresh token, expiry, and server target.
    /// </summary>
    public class DeviceAuthFlowTests
    {
        /// <summary>An <see cref="IDeviceAuthClient"/> stub returning canned responses (all completed tasks).</summary>
        sealed class FakeDeviceAuthClient : IDeviceAuthClient
        {
            readonly DeviceAuthorizeResponse _authorize;
            readonly Queue<DeviceTokenResponse> _tokens;
            readonly Exception? _authorizeError;
            readonly Exception? _pollError;

            public int RequestCount { get; private set; }
            public int PollCount { get; private set; }

            public FakeDeviceAuthClient(DeviceAuthorizeResponse authorize, IEnumerable<DeviceTokenResponse> tokens,
                Exception? authorizeError = null, Exception? pollError = null)
            {
                _authorize = authorize;
                _tokens = new Queue<DeviceTokenResponse>(tokens);
                _authorizeError = authorizeError;
                _pollError = pollError;
            }

            public Task<DeviceAuthorizeResponse> RequestDeviceCodeAsync(CancellationToken ct = default)
            {
                RequestCount++;
                if (_authorizeError != null)
                    return Task.FromException<DeviceAuthorizeResponse>(_authorizeError);
                return Task.FromResult(_authorize);
            }

            public Task<DeviceTokenResponse> PollTokenAsync(string deviceCode, CancellationToken ct = default)
            {
                PollCount++;
                if (_pollError != null)
                    return Task.FromException<DeviceTokenResponse>(_pollError);
                return Task.FromResult(_tokens.Count > 0 ? _tokens.Dequeue() : Pending());
            }
        }

        static DeviceAuthorizeResponse Authorize() => new DeviceAuthorizeResponse
        {
            DeviceCode = "DC-1",
            UserCode = "WXYZ-1234",
            VerificationUri = "https://ai-game.dev/device",
            VerificationUriComplete = "https://ai-game.dev/device?code=WXYZ-1234",
            ExpiresIn = 600,
            Interval = 5,
        };

        static DeviceTokenResponse Pending() => new DeviceTokenResponse { Error = "authorization_pending" };
        static DeviceTokenResponse SlowDown() => new DeviceTokenResponse { Error = "slow_down" };
        static DeviceTokenResponse Denied() => new DeviceTokenResponse { Error = "access_denied" };
        static DeviceTokenResponse Expired() => new DeviceTokenResponse { Error = "expired_token" };
        static DeviceTokenResponse Success() => new DeviceTokenResponse
        {
            AccessToken = "es256.jwt",
            RefreshToken = "rt-1",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            Scope = "mcp:plugin",
        };

        static DeviceAuthFlow NewFlow(IDeviceAuthClient client, Action<MachineCredentials> onAuthorized,
            Func<TimeSpan, CancellationToken, Task>? delay = null, Action<string>? openBrowser = null)
            => new DeviceAuthFlow(
                client,
                onAuthorized,
                serverTarget: "https://ai-game.dev",
                openBrowser: openBrowser ?? (_ => { }),
                delay: delay ?? ((_, __) => Task.CompletedTask));

        static void Run(DeviceAuthFlow flow) => flow.StartAsync().GetAwaiter().GetResult();

        [Test]
        public void HappyPath_Reaches_Authorized_And_Adopts_Credential()
        {
            MachineCredentials? adopted = null;
            var states = new List<DeviceAuthFlowState>();
            var client = new FakeDeviceAuthClient(Authorize(), new[] { Pending(), Success() });
            var flow = NewFlow(client, c => adopted = c);
            flow.OnStateChanged += states.Add;

            Run(flow);

            Assert.AreEqual(DeviceAuthFlowState.Authorized, flow.State);
            Assert.AreEqual("WXYZ-1234", flow.UserCode);
            CollectionAssert.AreEqual(
                new[]
                {
                    DeviceAuthFlowState.Initiating,
                    DeviceAuthFlowState.WaitingForUser,
                    DeviceAuthFlowState.Polling,
                    DeviceAuthFlowState.Authorized,
                },
                states);

            Assert.IsNotNull(adopted);
            Assert.AreEqual("es256.jwt", adopted!.AccessToken);
            Assert.AreEqual("rt-1", adopted.RefreshToken);
            Assert.AreEqual("https://ai-game.dev", adopted.ServerTarget);
            Assert.IsNotNull(adopted.ExpiresAt);
            Assert.Greater(adopted.ExpiresAt!.Value, DateTimeOffset.UtcNow);
        }

        [Test]
        public void OpensVerificationUrl_ForTheUser()
        {
            string? opened = null;
            var client = new FakeDeviceAuthClient(Authorize(), new[] { Success() });
            var flow = NewFlow(client, _ => { }, openBrowser: url => opened = url);

            Run(flow);

            Assert.AreEqual("https://ai-game.dev/device?code=WXYZ-1234", opened);
        }

        [Test]
        public void SlowDown_KeepsPolling_UntilSuccess()
        {
            MachineCredentials? adopted = null;
            var client = new FakeDeviceAuthClient(Authorize(), new[] { SlowDown(), Pending(), Success() });
            var flow = NewFlow(client, c => adopted = c);

            Run(flow);

            Assert.AreEqual(DeviceAuthFlowState.Authorized, flow.State);
            Assert.AreEqual(3, client.PollCount);
            Assert.IsNotNull(adopted);
        }

        [Test]
        public void AccessDenied_Reaches_Failed_WithMessage_AndNoCredential()
        {
            var adopted = false;
            var client = new FakeDeviceAuthClient(Authorize(), new[] { Denied() });
            var flow = NewFlow(client, _ => adopted = true);

            Run(flow);

            Assert.AreEqual(DeviceAuthFlowState.Failed, flow.State);
            Assert.AreEqual("Authorization was denied.", flow.ErrorMessage);
            Assert.IsFalse(adopted);
        }

        [Test]
        public void ExpiredToken_Reaches_Expired()
        {
            var client = new FakeDeviceAuthClient(Authorize(), new[] { Expired() });
            var flow = NewFlow(client, _ => { });

            Run(flow);

            Assert.AreEqual(DeviceAuthFlowState.Expired, flow.State);
        }

        [Test]
        public void Cancellation_DuringPoll_Reaches_Cancelled()
        {
            var client = new FakeDeviceAuthClient(Authorize(), Array.Empty<DeviceTokenResponse>(),
                pollError: new OperationCanceledException());
            var flow = NewFlow(client, _ => { });

            Run(flow);

            Assert.AreEqual(DeviceAuthFlowState.Cancelled, flow.State);
        }

        [Test]
        public void TransportError_Reaches_Failed_WithMessage()
        {
            var client = new FakeDeviceAuthClient(Authorize(), Array.Empty<DeviceTokenResponse>(),
                authorizeError: new HttpRequestException("network down"));
            var flow = NewFlow(client, _ => { });

            // The transport-error path intentionally logs an error ("Device auth flow failed")
            // before transitioning to Failed; tell the Unity test runner that error log is expected
            // so it doesn't fail this negative-path test on the unhandled error message.
            LogAssert.ignoreFailingMessages = true;

            Run(flow);

            Assert.AreEqual(DeviceAuthFlowState.Failed, flow.State);
            Assert.AreEqual("network down", flow.ErrorMessage);
        }
    }
}
