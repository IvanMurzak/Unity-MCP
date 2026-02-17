/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Yokesh J (https://github.com/Yokesh-4040)               │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.IO;
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Rider AI agent (Junie).
    /// Uses local (project-level) configuration only.
    /// </summary>
    public class RiderConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Rider (Junie)";
        public override string AgentId => "rider-junie";
        public override string DownloadUrl => "https://www.jetbrains.com/rider/download/";
        protected override string? IconFileName => "rider-64.png";

        private static string JunieConfigPath => Path.Combine(ProjectRootPath, ".junie", "mcp", "mcp.json");

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: JunieConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("enabled", JsonValue.Create(true), requiredForConfiguration: true)
        .SetPropertyToRemove("disabled")
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: JunieConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("enabled", JsonValue.Create(true), requiredForConfiguration: true)
        .SetPropertyToRemove("disabled")
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: JunieConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("enabled", JsonValue.Create(true), requiredForConfiguration: true)
        .SetPropertyToRemove("disabled")
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: JunieConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("enabled", JsonValue.Create(true), requiredForConfiguration: true)
        .SetPropertyToRemove("disabled")
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            if (_configElementHttp != null)
            {
                _configElementHttp.Root.style.display = DisplayStyle.None;
            }

            // STDIO Configuration

            ContainerStdio!.Add(TemplateWarningLabel("After configuring, go to Rider Settings | Tools | Junie | MCP Settings and check 'ai-game-developer' to connect AI agent."));
            var manualSteps = TemplateFoldoutFirst("Manual Configuration Steps");

            var relativePath = Path.Combine(".junie", "mcp", "mcp.json");

            manualSteps.Add(TemplateLabelDescription("Option 1: Use Terminal (Recommended for CLI lovers)"));

#if UNITY_EDITOR_WIN
            manualSteps.Add(TemplateLabelDescription("Run this command in PowerShell from your project root:"));
            var terminalCmd = $"New-Item -ItemType Directory -Force -Path .junie\\mcp | Out-Null; Set-Content -Path {relativePath.Replace('/', '\\')} -Value '{ConfigStdio.ExpectedFileContent.Replace("'", "''")}'";
#elif UNITY_EDITOR_OSX
            manualSteps.Add(TemplateLabelDescription("Run this command in Terminal from your project root:"));
            var terminalCmd = $"mkdir -p .junie/mcp && printf '{ConfigStdio.ExpectedFileContent.Replace("'", "'\\''")}' > {relativePath}";
#elif UNITY_EDITOR_LINUX
            manualSteps.Add(TemplateLabelDescription("Run this command in Terminal from your project root:"));
            var terminalCmd = $"mkdir -p .junie/mcp && printf '{ConfigStdio.ExpectedFileContent.Replace("'", "'\\''")}' > {relativePath}";
#endif
            manualSteps.Add(TemplateTextFieldReadOnly(terminalCmd));

            manualSteps.Add(TemplateLabelDescription("Option 2: Manual File Creation"));
            manualSteps.Add(TemplateLabelDescription($"1. Create or open the file: {relativePath}"));
            manualSteps.Add(TemplateLabelDescription("2. Copy and paste the JSON below:"));
            manualSteps.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            manualSteps.Add(TemplateLabelDescription("Option 3: Rider Settings"));
            manualSteps.Add(TemplateLabelDescription("Open Rider settings: Settings | Tools | Junie | MCP Settings and add a new server."));

            ContainerStdio!.Add(manualSteps);

            var troubleshootingStdio = TemplateFoldout("Troubleshooting");
            troubleshootingStdio.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));
            troubleshootingStdio.Add(TemplateLabelDescription("- Restart Rider after configuration changes"));
            troubleshootingStdio.Add(TemplateLabelDescription("- If using Terminal, ensure you are in the ai-game-developer root folder."));
            ContainerStdio.Add(troubleshootingStdio);

            // HTTP Configuration

            var warning = TemplateWarningLabel("Apologies for the inconvenience. Please use Stdio to connect. Currently in Rider only Junie will be able to connect to Unity MCP, via Stdio.");
            warning.style.whiteSpace = WhiteSpace.Normal;
            warning.style.marginBottom = 10;

            ContainerHttp!.Add(warning);
            ContainerHttp.Add(TemplateLabelDescription("The standard HTTP configuration is disabled due to stability issues."));
            ContainerHttp.Add(TemplateLabelDescription("Please switch to the 'Stdio' transport method at the top to configure this agent."));
        }
    }
}
