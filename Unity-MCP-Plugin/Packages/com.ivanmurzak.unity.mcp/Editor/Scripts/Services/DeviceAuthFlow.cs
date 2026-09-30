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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.AgentConfig;
using Microsoft.Extensions.Logging;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    public enum DeviceAuthFlowState
    {
        Idle,
        Initiating,
        WaitingForUser,
        Polling,
        Authorized,
        Failed,
        Expired,
        Cancelled
    }

    /// <summary>
    /// What a successful device sign-in yields (unified-machine-auth 03 F1.3): the freshly minted
    /// AGENT credential family — stamped with the <c>client_id</c> actually presented on the wire (D8)
    /// and the granted scope — plus the account subject decoded from the JWT <c>sub</c> (D6/F7 guard
    /// input) and the server target the credential was issued for. The editor hands this to
    /// <see cref="AccountCredentialService.CommitLoginAsync"/>, which runs the two-lock-hold login
    /// commit (agent family → RFC 8693 exchange → plugin family + v1 mirror).
    /// </summary>
    public sealed class DeviceAuthLoginResult
    {
        public DeviceAuthLoginResult(MachineCredentialFamily agentFamily, string? subject, string? serverTarget)
        {
            AgentFamily = agentFamily ?? throw new ArgumentNullException(nameof(agentFamily));
            Subject = subject;
            ServerTarget = serverTarget;
        }

        /// <summary>The minted agent family (access + refresh token, expiry, presented clientId, granted scope).</summary>
        public MachineCredentialFamily AgentFamily { get; }

        /// <summary>The account id from the mint's JWT <c>sub</c> claim; null when the server issued none.</summary>
        public string? Subject { get; }

        /// <summary>The AS/hub target the credential was issued for.</summary>
        public string? ServerTarget { get; }
    }

    /// <summary>
    /// Drives the RFC 8628 device-authorization state machine (unified-machine-auth 03 F1): request a
    /// device/user code, open the verification URL, poll <c>/oauth/token</c> until the user approves, then
    /// hand the resulting <see cref="DeviceAuthLoginResult"/> (the minted AGENT family + subject) to
    /// <paramref name="onAuthorized"/> — the editor wires that to the F1 login commit
    /// (<see cref="AccountCredentialService.CommitLoginAsync"/>), which persists it into the shared
    /// machine store under the cross-process lock. The device client, browser-open action, and poll delay
    /// are all injectable so the state machine runs against a mocked authorization server with no live
    /// network in CI.
    /// </summary>
    public class DeviceAuthFlow
    {
        private static readonly ILogger _logger = MCP.Utils.UnityLoggerFactory.LoggerFactory.CreateLogger<DeviceAuthFlow>();

        readonly IDeviceAuthClient _client;
        readonly Action<DeviceAuthLoginResult> _onAuthorized;
        readonly Action<string> _openBrowser;
        readonly Func<TimeSpan, CancellationToken, Task> _delay;
        readonly string? _serverTarget;

        private CancellationTokenSource? _cts;

        public DeviceAuthFlowState State { get; private set; } = DeviceAuthFlowState.Idle;
        public string? UserCode { get; private set; }
        public string? ErrorMessage { get; private set; }

        public event Action<DeviceAuthFlowState>? OnStateChanged;

        /// <param name="client">Device-authorization transport (real <see cref="DeviceAuthService"/> or a mock).</param>
        /// <param name="onAuthorized">Sink for the minted agent family (editor: run the F1 login commit).</param>
        /// <param name="serverTarget">The AS/hub target recorded on the credential (hosted vs local).</param>
        /// <param name="openBrowser">Verification-URL opener; defaults to <see cref="Application.OpenURL"/>.</param>
        /// <param name="delay">Poll delay; defaults to <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</param>
        public DeviceAuthFlow(
            IDeviceAuthClient client,
            Action<DeviceAuthLoginResult> onAuthorized,
            string? serverTarget = null,
            Action<string>? openBrowser = null,
            Func<TimeSpan, CancellationToken, Task>? delay = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _onAuthorized = onAuthorized ?? throw new ArgumentNullException(nameof(onAuthorized));
            _serverTarget = serverTarget;
            _openBrowser = openBrowser ?? (url => Application.OpenURL(url));
            _delay = delay ?? ((ts, ct) => Task.Delay(ts, ct));
        }

        public async Task StartAsync()
        {
            Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            try
            {
                SetState(DeviceAuthFlowState.Initiating);

                var authResponse = await _client.RequestDeviceCodeAsync(ct);
                UserCode = authResponse.UserCode;

                SetState(DeviceAuthFlowState.WaitingForUser);

                var verificationUrl = !string.IsNullOrEmpty(authResponse.VerificationUriComplete)
                    ? authResponse.VerificationUriComplete
                    : authResponse.VerificationUri;
                if (!string.IsNullOrEmpty(verificationUrl))
                    _openBrowser(verificationUrl);

                SetState(DeviceAuthFlowState.Polling);

                // RFC 8628: honour the server's interval (floor 5s), and slow_down bumps it by 5s.
                var interval = TimeSpan.FromSeconds(Math.Max(authResponse.Interval, 5));
                var lifetimeSeconds = authResponse.ExpiresIn > 0 ? authResponse.ExpiresIn : 900;
                var deadline = DateTime.UtcNow.AddSeconds(lifetimeSeconds);

                while (DateTime.UtcNow < deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    await _delay(interval, ct);

                    var tokenResponse = await _client.PollTokenAsync(authResponse.DeviceCode, ct);

                    if (!string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        // The minted AGENT family (03 F1.3). ClientId is the id actually presented on
                        // the wire (D8 — read from the client, never re-inferred); Scope prefers the
                        // server's granted scope over the requested default.
                        var agentFamily = new MachineCredentialFamily
                        {
                            AccessToken = tokenResponse.AccessToken,
                            RefreshToken = tokenResponse.RefreshToken,
                            ExpiresAt = tokenResponse.ExpiresIn > 0
                                ? DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
                                : (DateTimeOffset?)null,
                            ClientId = _client.ClientId,
                            Scope = string.IsNullOrEmpty(tokenResponse.Scope)
                                ? DeviceAuthService.AgentScope
                                : tokenResponse.Scope,
                        };
                        var subject = DeviceAuthService.DecodeJwtSubject(tokenResponse.AccessToken);
                        _onAuthorized(new DeviceAuthLoginResult(agentFamily, subject, _serverTarget));
                        SetState(DeviceAuthFlowState.Authorized);
                        return;
                    }

                    switch (tokenResponse.Error)
                    {
                        case "access_denied":
                            ErrorMessage = "Authorization was denied.";
                            SetState(DeviceAuthFlowState.Failed);
                            return;
                        case "expired_token":
                            SetState(DeviceAuthFlowState.Expired);
                            return;
                        case "slow_down":
                            interval += TimeSpan.FromSeconds(5);
                            break;
                        // "authorization_pending" (or no error) — keep polling.
                    }
                }

                SetState(DeviceAuthFlowState.Expired);
            }
            catch (OperationCanceledException)
            {
                SetState(DeviceAuthFlowState.Cancelled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device auth flow failed");
                ErrorMessage = ex.Message;
                SetState(DeviceAuthFlowState.Failed);
            }
        }

        public void Cancel()
        {
            var cts = _cts;
            _cts = null;
            if (cts != null)
            {
                try { cts.Cancel(); } catch (ObjectDisposedException) { }
                cts.Dispose();
            }
        }

        private void SetState(DeviceAuthFlowState state)
        {
            State = state;
            OnStateChanged?.Invoke(state);
        }
    }
}
