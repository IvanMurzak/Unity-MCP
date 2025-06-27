using System;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using R3;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class McpPluginUnity
    {
        Data data = new Data();
        static event Action<Data> onChanged;

        static McpPluginUnity instance;
        static McpPluginUnity Instance
        {
            get
            {
                Init();
                return instance;
            }
        }

        public static void Init()
        {
            instance ??= GetOrCreateInstance();
            if (instance == null)
                Debug.LogWarning("[McpPluginUnity] ConnectionConfig instance is null");
        }

        public static bool IsLogActive(LogLevel level)
            => (Instance.data ??= new Data()).logLevel.IsActive(level);

        public static LogLevel LogLevel
        {
            get => Instance.data?.logLevel ?? LogLevel.Trace;
            set
            {
                Instance.data ??= new Data();
                Instance.data.logLevel = value;
                NotifyChanged(Instance.data);
            }
        }
        public static string Host
        {
            get => Instance.data?.host ?? Data.DefaultHost;
            set
            {
                Instance.data ??= new Data();
                Instance.data.host = value;
                NotifyChanged(Instance.data);
            }
        }
        public static int Port
        {
            get
            {
                if (Uri.TryCreate(Host, UriKind.Absolute, out var uri) && uri.Port > 0)
                    return uri.Port;

                return Consts.Hub.DefaultPort;
            }
        }
        public static bool KeepConnected
        {
            get => Instance.data?.keepConnected ?? true;
            set
            {
                Instance.data ??= new Data();
                Instance.data.keepConnected = value;
                NotifyChanged(Instance.data);
            }
        }
        public static int TimeoutMs
        {
            get => Instance.data?.timeoutMs ?? Consts.Hub.DefaultTimeoutMs;
            set
            {
                Instance.data ??= new Data();
                Instance.data.timeoutMs = value;
                NotifyChanged(Instance.data);
            }
        }
        public static ReadOnlyReactiveProperty<HubConnectionState> ConnectionState
            => McpPlugin.Instance.ConnectionState;

        public static ReadOnlyReactiveProperty<bool> IsConnected => McpPlugin.Instance.ConnectionState
            .Select(x => x == HubConnectionState.Connected)
            .ToReadOnlyReactiveProperty(false);

        public static void Validate()
        {
            var changed = false;
            var data = Instance.data ??= new Data();

            if (data.port < 0 || data.port > Consts.Hub.MaxPort)
            {
                data.port = Consts.Hub.DefaultPort;
                changed = true;
            }

            if (string.IsNullOrEmpty(data.host))
            {
                data.host = Data.DefaultHost;
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
            Safe.Run(action, Instance.data, logLevel: Instance.data?.logLevel ?? LogLevel.Trace);
        }
        public static void UnsubscribeOnChanged(Action<Data> action)
        {
            if (action == null)
                return;

            onChanged -= action;
        }

        static void NotifyChanged(Data data)
            => Safe.Run(onChanged, data, logLevel: data?.logLevel ?? LogLevel.Trace);
    }
}