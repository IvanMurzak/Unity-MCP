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

    public class DeviceAuthFlow
    {
        private static readonly ILogger _logger = MCP.Utils.UnityLoggerFactory.LoggerFactory.CreateLogger<DeviceAuthFlow>();

        private CancellationTokenSource? _cts;

        public DeviceAuthFlowState State { get; private set; } = DeviceAuthFlowState.Idle;
        public string? UserCode { get; private set; }
        public string? ErrorMessage { get; private set; }

        public event Action<DeviceAuthFlowState>? OnStateChanged;

        public async Task StartAsync(string serverUrl, string? clientLabel = null)
        {
            Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            try
            {
                SetState(DeviceAuthFlowState.Initiating);

                var authResponse = await DeviceAuthService.InitiateDeviceAuthAsync(serverUrl, clientLabel, ct);
                UserCode = authResponse.UserCode;

                SetState(DeviceAuthFlowState.WaitingForUser);

                Application.OpenURL(authResponse.VerificationUriComplete);

                SetState(DeviceAuthFlowState.Polling);

                var interval = Math.Max(authResponse.Interval, 5) * 1000;
                var deadline = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn);

                while (DateTime.UtcNow < deadline)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(interval, ct);

                    var tokenResponse = await DeviceAuthService.PollDeviceTokenAsync(serverUrl, authResponse.DeviceCode, ct);

                    if (tokenResponse.AccessToken != null)
                    {
                        UnityMcpPluginEditor.CloudToken = tokenResponse.AccessToken;
                        UnityMcpPluginEditor.Instance.Save();
                        SetState(DeviceAuthFlowState.Authorized);
                        return;
                    }

                    if (tokenResponse.Error == "access_denied")
                    {
                        ErrorMessage = "Authorization was denied.";
                        SetState(DeviceAuthFlowState.Failed);
                        return;
                    }

                    if (tokenResponse.Error == "expired_token")
                    {
                        SetState(DeviceAuthFlowState.Expired);
                        return;
                    }

                    // authorization_pending or slow_down — keep polling
                    if (tokenResponse.Error == "slow_down")
                    {
                        interval = Math.Min(interval + 5000, 30000);
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
