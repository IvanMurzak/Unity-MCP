#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_TestRunner
    {
        private static class Error
        {
            public static string InvalidTestMode(string testMode)
                => $"[Error] Invalid test mode '{testMode}'. Valid modes: EditMode, PlayMode, All";

            public static string TestExecutionFailed(string reason)
                => $"[Error] Test execution failed: {reason}";

            public static string TestTimeout(int timeoutMs)
                => $"[Error] Test execution timed out after {timeoutMs} ms";

            public static string TestRunnerNotAvailable()
                => $"[Error] Unity Test Runner API is not available";

            public static string NoTestsFound(TestFilterParameters filterParams)
            {
                var filters = new List<string>();
                if (!string.IsNullOrEmpty(filterParams.TestAssembly)) filters.Add($"assembly '{filterParams.TestAssembly}'");
                if (!string.IsNullOrEmpty(filterParams.TestNamespace)) filters.Add($"namespace '{filterParams.TestNamespace}'");
                if (!string.IsNullOrEmpty(filterParams.TestClass)) filters.Add($"class '{filterParams.TestClass}'");
                if (!string.IsNullOrEmpty(filterParams.TestMethod)) filters.Add($"method '{filterParams.TestMethod}'");

                var filterText = filters.Count > 0
                    ? $" matching {string.Join(", ", filters)}"
                    : "";
                return $"[Error] No tests found{filterText}. Please check that the specified assembly, namespace, class, and method names are correct and that your Unity project contains tests.";
            }
        }
    }
}