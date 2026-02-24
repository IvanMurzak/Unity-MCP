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
using System.IO;
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Kilo Code AI agent.
    /// </summary>
    public class KiloCodeConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Kilo Code";
        public override string AgentId => "kilo-code";
        public override string DownloadUrl => "https://app.kilo.ai/get-started";

        protected override string? IconFileName => "kilo-code-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Code",
                "User",
                "globalStorage",
                "kilocode.kilo-code",
                "settings",
                "mcp_settings.json"
            ),
            bodyPath: "mcpServers"
        )
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("enabled")
        .SetPropertyToRemove("type");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Code",
                "User",
                "globalStorage",
                "kilocode.kilo-code",
                "settings",
                "mcp_settings.json"
            ),
            bodyPath: "mcpServers"
        )
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("enabled")
        .SetPropertyToRemove("type");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Code",
                "User",
                "globalStorage",
                "kilocode.kilo-code",
                "settings",
                "mcp_settings.json"
            ),
            bodyPath: "mcpServers"
        )
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args")
        .SetPropertyToRemove("enabled")
        .SetPropertyToRemove("type");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Code",
                "User",
                "globalStorage",
                "kilocode.kilo-code",
                "settings",
                "mcp_settings.json"
            ),
            bodyPath: "mcpServers"
        )
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args")
        .SetPropertyToRemove("enabled")
        .SetPropertyToRemove("type");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // STDIO Configuration

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open the file 'mcp_settings.json' in Kilo Code global settings folder."));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));
            manualStepsContainer!.Add(TemplateLabelDescription("3. Restart Kilo Code if it was running."));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure the JSON file has no syntax errors."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Check that the executable path is correct."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Verify Kilo Code has MCP support enabled."));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open the file 'mcp_settings.json' in Kilo Code global settings folder."));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("3. Restart Kilo Code if it was running."));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure the JSON file has no syntax errors."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Verify Kilo Code has MCP support enabled."));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
