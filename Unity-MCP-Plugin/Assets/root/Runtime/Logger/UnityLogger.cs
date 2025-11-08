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
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Utils
{
    using LogLevel = Runtime.Utils.LogLevel;
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
            // Prevent infinite loop during UnityMcpPlugin initialization by checking if instance exists
            if (!UnityMcpPlugin.HasInstance)
            {
                // During initialization, allow all log levels
                return true;
            }

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
#if UNITY_EDITOR
            string logLevelShort = logLevel switch
            {
                LogLevelMicrosoft.Critical => "<color=#ff0000>crit: </color>",
                LogLevelMicrosoft.Error => "<color=#ff6b6b>fail: </color>",
                LogLevelMicrosoft.Warning => "<color=#ffaa00>warn: </color>",
                LogLevelMicrosoft.Information => "<color=#00ff00>info: </color>",
                LogLevelMicrosoft.Debug => "<color=#00ffff>dbug: </color>",
                LogLevelMicrosoft.Trace => "<color=#aaaaaa>trce: </color>",
                _ => "<color=#ffffff>none</color>"
            };
            var message = $"{logLevelShort} [{DateTime.Now:HH:mm:ss:ffff}] <color=#B4FF32>[AI]</color> <color=#007575><b>{_categoryName}</b></color> {formatter(state, exception)}";
#else
            string logLevelShort = logLevel switch
            {
                LogLevelMicrosoft.Critical => "crit: ",
                LogLevelMicrosoft.Error => "fail: ",
                LogLevelMicrosoft.Warning => "warn: ",
                LogLevelMicrosoft.Information => "info: ",
                LogLevelMicrosoft.Debug => "dbug: ",
                LogLevelMicrosoft.Trace => "trce: ",
                _ => "none"
            };
            var message = $"{logLevelShort} [{DateTime.Now:HH:mm:ss:ffff}] [AI] {_categoryName} {formatter(state, exception)}";
#endif

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
