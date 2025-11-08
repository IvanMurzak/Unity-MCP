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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPlugin : IDisposable
    {
        public const string Version = "0.21.0";

        protected readonly CompositeDisposable _disposables = new();

        protected UnityMcpPlugin(UnityConnectionConfig? config = null)
        {
            if (config == null)
            {
                config = GetOrCreateConfig(out var wasCreated);
                this.unityConnectionConfig = config ?? throw new InvalidOperationException("ConnectionConfig is null");
                if (wasCreated)
                    Save();
            }
            else
            {
                this.unityConnectionConfig = config ?? throw new InvalidOperationException("ConnectionConfig is null");
            }
        }

        public void Validate()
        {
            var changed = false;
            var data = unityConnectionConfig ??= new UnityConnectionConfig();

            if (string.IsNullOrEmpty(data.Host))
            {
                data.Host = UnityConnectionConfig.DefaultHost;
                changed = true;
            }

            // Data was changed during validation, need to notify subscribers
            if (changed)
                NotifyChanged(data);
        }

        public void LogTrace(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogTrace(message, args);
        }
        public void LogDebug(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogDebug(message, args);
        }
        public void LogInfo(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogInformation(message, args);
        }
        public void LogWarn(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogWarning(message, args);
        }
        public void LogError(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogError(message, args);
        }
        public void LogException(string message, Type sourceClass, params object?[] args)
        {
            UnityLoggerFactory.LoggerFactory
                .CreateLogger(sourceClass.GetTypeShortName())
                .LogCritical(message, args);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            lock (buildMutex)
            {
                mcpPluginInstance?.Dispose();
                mcpPluginInstance = null;
            }
        }
    }
}
