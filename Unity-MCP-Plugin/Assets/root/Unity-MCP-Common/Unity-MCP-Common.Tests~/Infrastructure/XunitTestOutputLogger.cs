using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Infrastructure
{
    /// <summary>
    /// ILogger implementation that writes to xUnit's ITestOutputHelper.
    /// </summary>
    internal sealed class XunitTestOutputLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ITestOutputHelper _output;

        public XunitTestOutputLogger(string categoryName, ITestOutputHelper output)
        {
            _categoryName = categoryName;
            _output = output;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            if (formatter is null) throw new ArgumentNullException(nameof(formatter));

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception is null) return;

            try
            {
                var line = $"[{DateTime.UtcNow:O}] {_categoryName} {logLevel}: {message}";
                _output.WriteLine(line);
                if (exception is not null)
                {
                    _output.WriteLine(exception.ToString());
                }
            }
            catch (InvalidOperationException)
            {
                // ITestOutputHelper may throw when used after test completion; ignore.
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();
            private NullScope() { }
            public void Dispose() { }
        }
    }
}
