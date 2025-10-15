using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Infrastructure
{
    internal sealed class XunitTestOutputLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;

        public XunitTestOutputLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName) => new XunitTestOutputLogger(categoryName, _output);

        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    public static class XunitTestOutputLoggerExtensions
    {
        public static ILoggingBuilder AddXunitTestOutput(this ILoggingBuilder builder, ITestOutputHelper output)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (output is null) throw new ArgumentNullException(nameof(output));
            builder.AddProvider(new XunitTestOutputLoggerProvider(output));
            return builder;
        }

        public static ILoggerFactory AddXunitTestOutput(this ILoggerFactory factory, ITestOutputHelper output)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            if (output is null) throw new ArgumentNullException(nameof(output));
            factory.AddProvider(new XunitTestOutputLoggerProvider(output));
            return factory;
        }
    }
}
