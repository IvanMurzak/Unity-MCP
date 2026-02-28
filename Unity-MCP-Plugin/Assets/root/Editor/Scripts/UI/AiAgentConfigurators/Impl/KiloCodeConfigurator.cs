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

        private static string LocalConfigPath => Path.Combine(ProjectRootPath, ".kilocode", "mcp.json");

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray
        {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray
        {
            $"{Args.Port}={UnityMcpPluginEditor.Port}",
            $"{Args.PluginTimeout}={UnityMcpPluginEditor.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Authorization}={UnityMcpPluginEditor.AuthOption}",
            $"{Args.Token}={UnityMcpPluginEditor.Token}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("streamable-http"), requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: LocalConfigPath,
            bodyPath: DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("streamable-http"), requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPluginEditor.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            var relativePath = Path.Combine(".kilocode", "mcp.json");

            // STDIO Configuration

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Create or open the file in your project:"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(relativePath));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));
            manualStepsContainer!.Add(TemplateLabelDescription("3. Restart Kilo Code if it was running."));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure the JSON file has no syntax errors."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Check that the executable path is correct."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Verify Kilo Code has MCP support enabled."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- The configuration file should be in your Unity project root, next to Assets folder."));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Create or open the file in your project:"));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(relativePath));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("3. Restart Kilo Code if it was running."));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure the JSON file has no syntax errors."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Verify Kilo Code has MCP support enabled."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- The configuration file should be in your Unity project root, next to Assets folder."));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
