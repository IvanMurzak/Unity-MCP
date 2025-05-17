#if !UNITY_5_3_OR_NEWER
using System;
using com.IvanMurzak.Unity.MCP.Common;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP
{
    internal class ConsoleLogger : ILogger
    {
        readonly string _categoryName;

        public ConsoleLogger(string categoryName)
        {
            _categoryName = categoryName.Contains('.')
                ? categoryName.Substring(categoryName.LastIndexOf('.') + 1)
                : categoryName;
        }

        IDisposable ILogger.BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));
            if (state == null) throw new ArgumentNullException(nameof(state));

            var message = $"{Consts.Log.Tag} {Consts.Log.Color.CategoryStart}{_categoryName}{Consts.Log.Color.CategoryEnd} {Consts.Log.Color.LevelStart}[{logLevel}]{Consts.Log.Color.LevelEnd} {formatter(state, exception)}";
            Console.WriteLine(message);
        }
    }
    internal class ConsoleLoggerProvider : ILoggerProvider
    {
        public void Dispose() { /* No resources to dispose of */ }
        ILogger ILoggerProvider.CreateLogger(string categoryName) => new ConsoleLogger(categoryName);
    }
}
#endif