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
    }
}
