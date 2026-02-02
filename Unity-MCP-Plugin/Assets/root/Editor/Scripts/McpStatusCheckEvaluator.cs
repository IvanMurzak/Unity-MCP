#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR.Client;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    public class McpStatusCheckEvaluator
    {
        public class CheckResult
        {
            public bool IsPassed { get; set; }
            public bool CanCountAsPassed { get; set; }
        }

        public static List<CheckResult> EvaluateAllChecks()
        {
            var results = new List<CheckResult>();

            results.Add(EvaluateMcpClientConfiguredCheck());
            results.Add(EvaluateUnityConnectedCheck());
            results.Add(EvaluateVersionHandshakeCheck());
            results.Add(EvaluateServerToClientCheck());
            results.Add(EvaluateClientLocationCheck());
            results.Add(EvaluateEnabledToolsCheck());
            results.Add(EvaluateToolExecutedCheck());

            return results;
        }

        public static int GetPassedCount()
        {
            var results = EvaluateAllChecks();
            return results.Count(r => r.IsPassed && r.CanCountAsPassed);
        }

        private static CheckResult EvaluateMcpClientConfiguredCheck()
        {
            var configuredClients = MainWindowEditor.GetConfiguredClients();
            var isPassed = configuredClients.Count > 0;

            return new CheckResult
            {
                IsPassed = isPassed,
                CanCountAsPassed = true
            };
        }

        private static CheckResult EvaluateUnityConnectedCheck()
        {
            var isConnected = UnityMcpPlugin.IsConnected.CurrentValue;

            return new CheckResult
            {
                IsPassed = isConnected,
                CanCountAsPassed = true
            };
        }

        private static CheckResult EvaluateVersionHandshakeCheck()
        {
            var isConnected = UnityMcpPlugin.IsConnected.CurrentValue;
            var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance;
            var isPassed = isConnected && mcpPlugin != null && mcpPlugin.VersionHandshakeStatus?.Compatible == true;

            return new CheckResult
            {
                IsPassed = isPassed,
                CanCountAsPassed = true
            };
        }

        private static CheckResult EvaluateServerToClientCheck()
        {
            var isConnected = UnityMcpPlugin.IsConnected.CurrentValue;
            var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance;
            var handshake = mcpPlugin?.VersionHandshakeStatus;
            var isPassed = isConnected && handshake != null;

            return new CheckResult
            {
                IsPassed = isPassed,
                CanCountAsPassed = false
            };
        }

        private static CheckResult EvaluateClientLocationCheck()
        {
            var configuredClients = MainWindowEditor.GetConfiguredClients();
            var isConnected = UnityMcpPlugin.IsConnected.CurrentValue;
            var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance;
            var serverBasePath = mcpPlugin?.CurrentBaseDirectory ?? string.Empty;
            var isPassed = configuredClients.Count > 0 && isConnected && !string.IsNullOrEmpty(serverBasePath);

            if (isPassed)
            {
                var unityProjectDir = System.Environment.CurrentDirectory;
                var normServerPath = System.IO.Path.GetFullPath(serverBasePath).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
                var normProjectPath = System.IO.Path.GetFullPath(unityProjectDir).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
                isPassed = normServerPath.StartsWith(normProjectPath, System.StringComparison.OrdinalIgnoreCase);
            }

            return new CheckResult
            {
                IsPassed = isPassed,
                CanCountAsPassed = true
            };
        }

        private static CheckResult EvaluateEnabledToolsCheck()
        {
            var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance;
            var toolManager = mcpPlugin?.McpManager.ToolManager;
            var isPassed = false;

            if (toolManager != null)
            {
                var allTools = toolManager.GetAllTools();
                var enabledCount = allTools.Count(t => toolManager.IsToolEnabled(t.Name));
                isPassed = enabledCount > 0;
            }

            return new CheckResult
            {
                IsPassed = isPassed,
                CanCountAsPassed = true
            };
        }

        private static CheckResult EvaluateToolExecutedCheck()
        {
            var isPassed = McpStatusChecksWindow.ToolExecutionCount > 0;

            return new CheckResult
            {
                IsPassed = isPassed,
                CanCountAsPassed = false
            };
        }
    }
}
