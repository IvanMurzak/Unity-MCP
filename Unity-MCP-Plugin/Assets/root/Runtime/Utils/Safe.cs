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
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Utils
{
    internal static class Safe
    {
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger("Safe");

        public static bool Run(Action action, LogLevel? logLevel = null)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                if (logLevel?.IsEnabled(LogLevel.Error) ?? true)
                    _logger.LogError(eventId: default, message: e.Message, exception: e);
                return false;
            }
        }
        public static bool Run<T>(Action<T> action, T value, LogLevel? logLevel = null)
        {
            try
            {
                action?.Invoke(value);
                return true;
            }
            catch (Exception e)
            {
                if (logLevel?.IsEnabled(LogLevel.Error) ?? true)
                    _logger.LogError(eventId: default, message: e.Message, exception: e);
                return false;
            }
        }
        public static bool Run<T1, T2>(Action<T1, T2> action, T1 value1, T2 value2, LogLevel? logLevel = null)
        {
            try
            {
                action?.Invoke(value1, value2);
                return true;
            }
            catch (Exception e)
            {
                if (logLevel?.IsEnabled(LogLevel.Error) ?? true)
                    _logger.LogError(eventId: default, message: e.Message, exception: e);
                return false;
            }
        }
        public static TResult? Run<TInput, TResult>(Func<TInput, TResult> action, TInput input, LogLevel? logLevel = null)
        {
            try
            {
                return action.Invoke(input);
            }
            catch (Exception e)
            {
                if (logLevel?.IsEnabled(LogLevel.Error) ?? true)
                    _logger.LogError(eventId: default, message: e.Message, exception: e);
                return default;
            }
        }
        public static bool Run(WeakAction action, LogLevel? logLevel = null)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                if (logLevel?.IsEnabled(LogLevel.Error) ?? true)
                    _logger.LogError(eventId: default, message: e.Message, exception: e);
                return false;
            }
        }
        public static bool Run<T>(WeakAction<T> action, T value, LogLevel? logLevel = null)
        {
            try
            {
                action?.Invoke(value);
                return true;
            }
            catch (Exception e)
            {
                if (logLevel?.IsEnabled(LogLevel.Error) ?? true)
                    _logger.LogError(eventId: default, message: e.Message, exception: e);
                return false;
            }
        }
        public static bool RunCancel(CancellationTokenSource cts, LogLevel? logLevel = null)
        {
            try
            {
                if (cts == null)
                    return false;

                if (cts.IsCancellationRequested)
                    return false;

                cts.Cancel();
                return true;
            }
            catch (Exception e)
            {
                if (logLevel?.IsEnabled(LogLevel.Error) ?? true)
                    _logger.LogError(eventId: default, message: e.Message, exception: e);
                return false;
            }
        }
    }
}
