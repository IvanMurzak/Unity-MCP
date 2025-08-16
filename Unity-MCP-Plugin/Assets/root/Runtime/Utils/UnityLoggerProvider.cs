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

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevelMicrosoft logLevel) => true;

        public void Log<TState>(LogLevelMicrosoft logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
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
    public class UnityLoggerProvider : ILoggerProvider
    {
        public void Dispose() { /* No resources to dispose of */ }
        ILogger ILoggerProvider.CreateLogger(string categoryName) => new UnityLogger(categoryName);
    }
}