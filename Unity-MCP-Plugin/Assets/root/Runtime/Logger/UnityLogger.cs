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
using com.IvanMurzak.Unity.MCP.Common;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Utils
{
    using LogLevelMicrosoft = Microsoft.Extensions.Logging.LogLevel;

    public class UnityLogger : ILogger
    {
        readonly string _categoryName;

        public UnityLogger(string categoryName)
        {
            _categoryName = categoryName.Contains('.')
                ? categoryName.Substring(categoryName.LastIndexOf('.') + 1)
                : categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null!;

        public bool IsEnabled(LogLevelMicrosoft logLevel)
        {
            return UnityMcpPlugin.IsLogEnabled(logLevel switch
            {
                LogLevelMicrosoft.Critical => LogLevel.Exception,
                LogLevelMicrosoft.Error => LogLevel.Error,
                LogLevelMicrosoft.Warning => LogLevel.Warning,
                LogLevelMicrosoft.Information => LogLevel.Info,
                LogLevelMicrosoft.Debug => LogLevel.Debug,
                LogLevelMicrosoft.Trace => LogLevel.Trace,
                _ => LogLevel.None
            });
        }

        public void Log<TState>(LogLevelMicrosoft logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            if (state == null) throw new ArgumentNullException(nameof(state));

            // Map LogLevel to short names
            string logLevelShort = logLevel switch
            {
                LogLevelMicrosoft.Critical => Consts.Log.Crit,
                LogLevelMicrosoft.Error => Consts.Log.Fail,
                LogLevelMicrosoft.Warning => Consts.Log.Warn,
                LogLevelMicrosoft.Information => Consts.Log.Info,
                LogLevelMicrosoft.Debug => Consts.Log.Dbug,
                LogLevelMicrosoft.Trace => Consts.Log.Trce,
                _ => "none: "
            };

            var message = $"{Consts.Log.Color.LevelStart}{logLevelShort}{Consts.Log.Color.LevelEnd}{Consts.Log.Tag} {Consts.Log.Color.CategoryStart}{_categoryName}{Consts.Log.Color.CategoryEnd} {formatter(state, exception)}";
            switch (logLevel)
            {
                case LogLevelMicrosoft.Critical:
                case LogLevelMicrosoft.Error:
                    if (exception != null)
                        UnityEngine.Debug.LogException(exception);
                    UnityEngine.Debug.LogError(message);
                    break;

                case LogLevelMicrosoft.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;

                default:
                    UnityEngine.Debug.Log(message);
                    break;
            }
        }
    }
}
