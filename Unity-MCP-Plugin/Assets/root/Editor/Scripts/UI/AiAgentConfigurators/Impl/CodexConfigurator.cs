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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Codex AI agent.
    /// </summary>
    public class CodexConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Codex";
        public override string AgentId => "codex";
        public override string DownloadUrl => "https://openai.com/codex/";

        protected override string? IconFileName => "codex-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new TomlAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                ".codex",
                "config.toml"
            ),
            bodyPath: "mcp_servers"
        )
        .SetProperty("enabled", true, requiredForConfiguration: true) // Codex requires an "enabled" property
        .SetProperty("command", McpServerManager.ExecutableFullPath.Replace('\\', '/'), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new[] {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Token}={UnityMcpPlugin.Token}"
        }, requiredForConfiguration: true)
        .SetProperty("tool_timeout_sec", 300, requiredForConfiguration: false) // Optional: Set a longer tool timeout for Codex
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("type")
        .SetPropertyToRemove("bearer_token_env_var");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new TomlAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                ".codex",
                "config.toml"
            ),
            bodyPath: "mcp_servers"
        )
        .SetProperty("enabled", true, requiredForConfiguration: true) // Codex requires an "enabled" property
        .SetProperty("command", McpServerManager.ExecutableFullPath.Replace('\\', '/'), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new[] {
            $"{Args.Port}={UnityMcpPlugin.Port}",
            $"{Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Args.ClientTransportMethod}={TransportMethod.stdio}",
            $"{Args.Token}={UnityMcpPlugin.Token}"
        }, requiredForConfiguration: true)
        .SetProperty("tool_timeout_sec", 300, requiredForConfiguration: false) // Optional: Set a longer tool timeout for Codex
        .SetPropertyToRemove("url")
        .SetPropertyToRemove("type")
        .SetPropertyToRemove("bearer_token_env_var");

        protected override AiAgentConfig CreateConfigHttpWindows() => new TomlAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                ".codex",
                "config.toml"
            ),
            bodyPath: "mcp_servers"
        )
        .SetProperty("enabled", true, requiredForConfiguration: true) // Codex requires an "enabled" property
        .SetProperty("url", UnityMcpPlugin.Host, requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetProperty("tool_timeout_sec", 300, requiredForConfiguration: false) // Optional: Set a longer tool timeout for Codex
        .SetProperty("bearer_token_env_var", $"Bearer {UnityMcpPlugin.Token}", requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args")
        .SetPropertyToRemove("type");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new TomlAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                ".codex",
                "config.toml"
            ),
            bodyPath: "mcp_servers"
        )
        .SetProperty("enabled", true, requiredForConfiguration: true) // Codex requires an "enabled" property
        .SetProperty("url", UnityMcpPlugin.Host, requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetProperty("tool_timeout_sec", 300, requiredForConfiguration: false) // Optional: Set a longer tool timeout for Codex
        .SetProperty("bearer_token_env_var", $"Bearer {UnityMcpPlugin.Token}", requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args")
        .SetPropertyToRemove("type");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            var addMcpServerCommandStdio = $"codex mcp add {AiAgentConfig.DefaultMcpServerName} \"{McpServerManager.ExecutableFullPath}\" port={UnityMcpPlugin.Port} plugin-timeout={UnityMcpPlugin.TimeoutMs} client-transport=stdio";
            var addMcpServerCommandHttp = $"codex mcp add {AiAgentConfig.DefaultMcpServerName} --url {UnityMcpPlugin.Host}";

            // STDIO Configuration

            var manualStepsOption1 = TemplateFoldoutFirst("Manual Configuration Steps - Option 1");

            manualStepsOption1!.Add(TemplateLabelDescription("1. Open a terminal and run the following command to be in the folder of the Unity project"));
            manualStepsOption1!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            manualStepsOption1!.Add(TemplateLabelDescription("2. Run the following command in the folder of the Unity project to configure Codex"));
            manualStepsOption1!.Add(TemplateTextFieldReadOnly(addMcpServerCommandStdio));
            manualStepsOption1!.Add(TemplateLabelDescription("3. Start Codex"));
            manualStepsOption1!.Add(TemplateTextFieldReadOnly("codex"));

            ContainerStdio!.Add(manualStepsOption1);

            var manualStepsOption2 = TemplateFoldout("Manual Configuration Steps - Option 2");

            manualStepsOption2!.Add(TemplateLabelDescription("1. Open or create file '.codex/config.toml'"));
            manualStepsOption2!.Add(TemplateLabelDescription("2. Copy and paste the configuration TOML into the file."));
            manualStepsOption2!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            ContainerStdio!.Add(manualStepsOption2);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Codex CLI is installed and accessible from terminal"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsOption1Http = TemplateFoldoutFirst("Manual Configuration Steps - Option 1");

            manualStepsOption1Http!.Add(TemplateLabelDescription("1. Open a terminal and run the following command to be in the folder of the Unity project"));
            manualStepsOption1Http!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            manualStepsOption1Http!.Add(TemplateLabelDescription("2. Run the following command in the folder of the Unity project to configure Codex"));
            manualStepsOption1Http!.Add(TemplateTextFieldReadOnly(addMcpServerCommandHttp));
            manualStepsOption1Http!.Add(TemplateLabelDescription("3. Start Codex"));
            manualStepsOption1Http!.Add(TemplateTextFieldReadOnly("codex"));

            ContainerHttp!.Add(manualStepsOption1Http);

            var manualStepsOption2Http = TemplateFoldout("Manual Configuration Steps - Option 2");

            manualStepsOption2Http!.Add(TemplateLabelDescription("1. Open or create file '.codex/config.toml'"));
            manualStepsOption2Http!.Add(TemplateLabelDescription("2. Copy and paste the configuration TOML into the file."));
            manualStepsOption2Http!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));

            ContainerHttp!.Add(manualStepsOption2Http);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Codex CLI is installed and accessible from terminal"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
