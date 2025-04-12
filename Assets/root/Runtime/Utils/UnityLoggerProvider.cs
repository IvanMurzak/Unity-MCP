using System;
using com.IvanMurzak.Unity.MCP.Common;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public class UnityLogger : ILogger
    {
        readonly string _categoryName;

        public UnityLogger(string categoryName)
        {
            _categoryName = categoryName.Contains('.')
                ? categoryName.Substring(categoryName.LastIndexOf('.') + 1)
                : categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            if (state == null) throw new ArgumentNullException(nameof(state));

            // Map LogLevel to short names
            string logLevelShort = logLevel switch
            {
                LogLevel.Critical => "crit",
                LogLevel.Error => "fail",
                LogLevel.Warning => "warn",
                LogLevel.Information => "info",
                LogLevel.Debug => "dbug",
                LogLevel.Trace => "trce",
                _ => "none"
            };

            var message = $"{Consts.Log.Color.LevelStart}[{logLevelShort}]{Consts.Log.Color.LevelEnd}: {Consts.Log.Tag} {Consts.Log.Color.CategoryStart}{_categoryName}{Consts.Log.Color.CategoryEnd} {formatter(state, exception)}";
            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(message);
                    if (exception != null)
                        UnityEngine.Debug.LogException(exception);
                    break;

                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;

                default:
                    UnityEngine.Debug.Log(message);
                    break;
            }
        }
    }
    public class UnityLoggerProvider : ILoggerProvider
    {
        public void Dispose() { /* No resources to dispose of */ }
        ILogger ILoggerProvider.CreateLogger(string categoryName) => new UnityLogger(categoryName);
    }
}