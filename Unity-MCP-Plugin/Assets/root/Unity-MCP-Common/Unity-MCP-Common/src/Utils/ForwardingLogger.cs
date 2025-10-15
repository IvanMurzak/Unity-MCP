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

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class ForwardLoggerProvider : ILoggerProvider
    {
        private readonly ILogger _logger;
        private readonly string? _additionalErrorMessage;

        public ForwardLoggerProvider(ILogger logger, string? additionalErrorMessage = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _additionalErrorMessage = additionalErrorMessage;
        }

        public ILogger CreateLogger(string categoryName) => new ForwardingLogger(_logger, categoryName, _additionalErrorMessage);

        public void Dispose() { /* No resources to dispose */ }

        private class ForwardingLogger : ILogger
        {
            readonly ILogger _logger;
            readonly string _categoryName;
            readonly string? _additionalErrorMessage;

            public ForwardingLogger(ILogger logger, string categoryName, string? additionalErrorMessage)
            {
                _logger = logger;
                _categoryName = categoryName;
                _additionalErrorMessage = additionalErrorMessage;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);
            public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

            void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;

                // var message = $"[{_categoryName}] {formatter(state, exception)}";

                var message = $"{formatter(state, exception)}";

                _logger.Log(logLevel, eventId, (object)message, exception, (s, e) => s?.ToString() ?? string.Empty);

                if (logLevel >= LogLevel.Error)
                    _logger.Log(logLevel, _additionalErrorMessage);
            }
        }
    }
}
