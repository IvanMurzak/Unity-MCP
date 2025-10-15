using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Infrastructure
{
    public static class TestLoggerFactory
    {
        public static ILoggerFactory Create(ITestOutputHelper output, LogLevel minLevel = LogLevel.Information)
        {
            return LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(minLevel);
                builder.AddXunitTestOutput(output);
            });
        }
    }
}
