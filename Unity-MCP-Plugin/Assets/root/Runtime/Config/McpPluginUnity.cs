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
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using R3;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class McpPluginUnity
    {
        Data data = new Data();
        static event Action<Data>? onChanged;

        static volatile object instanceMutex = new();
        static McpPluginUnity instance = null!;
        static McpPluginUnity Instance
        {
            get
            {
                Init();
                lock (instanceMutex)
                {
                    return instance;
                }
            }
        }

        public static void Init()
        {
            lock (instanceMutex)
            {
                if (instance == null)
                {
                    instance = GetOrCreateInstance(out var wasCreated);
                    if (instance == null)
                    {
                        Debug.LogWarningFormat("{tag} {class}.{method}: ConnectionConfig instance is null",
                            Consts.Log.Tag, nameof(McpPluginUnity), nameof(Init));
                        return;
                    }
                    else if (wasCreated)
                    {
                        Save();
                    }
                }
            }
        }

        public static bool IsLogActive(LogLevel level)
            => (Instance.data ??= new Data()).LogLevel.IsActive(level);

        public static LogLevel LogLevel
        {
            get => Instance.data?.LogLevel ?? LogLevel.Trace;
            set
            {
                Instance.data ??= new Data();
                Instance.data.LogLevel = value;
                NotifyChanged(Instance.data);
            }
        }
        public static string Host
        {
            get => Instance.data?.Host ?? Data.DefaultHost;
            set
            {
                Instance.data ??= new Data();
                Instance.data.Host = value;
                NotifyChanged(Instance.data);
            }
        }
        public static int Port
        {
            get
            {
                if (Uri.TryCreate(Host, UriKind.Absolute, out var uri) && uri.Port > 0 && uri.Port <= Consts.Hub.MaxPort)
                    return uri.Port;

                return Consts.Hub.DefaultPort;
            }
        }
        public static bool KeepConnected
        {
            get => Instance.data?.KeepConnected ?? true;
            set
            {
                Instance.data ??= new Data();
                Instance.data.KeepConnected = value;
                NotifyChanged(Instance.data);
            }
        }
        public static int TimeoutMs
        {
            get => Instance.data?.TimeoutMs ?? Consts.Hub.DefaultTimeoutMs;
            set
            {
                Instance.data ??= new Data();
                Instance.data.TimeoutMs = value;
                NotifyChanged(Instance.data);
            }
        }
        public static ReadOnlyReactiveProperty<HubConnectionState> ConnectionState
            => McpPlugin.Instance!.ConnectionState;

        public static ReadOnlyReactiveProperty<bool> IsConnected => McpPlugin.Instance!.ConnectionState
            .Select(x => x == HubConnectionState.Connected)
            .ToReadOnlyReactiveProperty(false);

        public static async Task NotifyToolRequestCompleted(ResponseCallTool response, CancellationToken cancellationToken = default)
        {
            // wait when connection will be established
            while (McpPlugin.Instance?.ConnectionState.CurrentValue != HubConnectionState.Connected)
                await Task.Delay(100, cancellationToken);

            if (McpPlugin.Instance?.RpcRouter == null)
            {
                if (IsLogActive(LogLevel.Warning))
                    Debug.LogWarningFormat("{tag} {class}.{method}: RpcRouter is null",
                        Consts.Log.Tag, nameof(McpPluginUnity), nameof(NotifyToolRequestCompleted));
                return;
            }

            await McpPlugin.Instance.RpcRouter.NotifyToolRequestCompleted(response, cancellationToken);
        }

        public static void Validate()
        {
            var changed = false;
            var data = Instance.data ??= new Data();

            if (data.Port < 0 || data.Port > Consts.Hub.MaxPort)
            {
                data.Port = Consts.Hub.DefaultPort;
                changed = true;
            }

            if (string.IsNullOrEmpty(data.Host))
            {
                data.Host = Data.DefaultHost;
                changed = true;
            }

            if (changed)
                NotifyChanged(data);
        }

        public static void SubscribeOnChanged(Action<Data> action)
        {
            if (action == null)
                return;

            onChanged += action;
            Safe.Run(action, Instance.data, logLevel: Instance.data?.LogLevel ?? LogLevel.Trace);
        }
        public static void UnsubscribeOnChanged(Action<Data> action)
        {
            if (action == null)
                return;

            onChanged -= action;
        }

        public static async void Disconnect()
        {
            if (IsLogActive(LogLevel.Trace))
                Debug.LogFormat("{tag} {class}.{method}() called.",
                    Consts.Log.Tag, nameof(McpPluginUnity), nameof(Disconnect));

            initializedMutex.WaitOne();
            try
            {
                var instance = McpPlugin.Instance;
                if (instance == null)
                {
                    isInitialized = false;
                    if (IsLogActive(LogLevel.Debug))
                        Debug.LogFormat("{tag} {class}.{method}() isInitialized set <false>.",
                            Consts.Log.Tag, nameof(McpPluginUnity), nameof(Disconnect));

                    await McpPlugin.StaticDisposeAsync();
                    return; // ignore
                }

                await instance.Disconnect();

                // await (instance.RpcRouter?.Disconnect() ?? Task.CompletedTask);
            }
            finally
            {
                if (IsLogActive(LogLevel.Trace))
                    Debug.LogFormat("{tag} {class}.{method}() completed.",
                        Consts.Log.Tag, nameof(McpPluginUnity), nameof(Disconnect));
                initializedMutex.ReleaseMutex();
            }
        }

        static void NotifyChanged(Data data)
            => Safe.Run(onChanged, data, logLevel: data?.LogLevel ?? LogLevel.Trace);
    }
}
