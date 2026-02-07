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
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Gemini AI agent.
    /// </summary>
    public class GeminiConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Gemini";
        public override string AgentId => "gemini";
        public override string DownloadUrl => "https://geminicli.com/docs/get-started/installation/";

        protected override string? IconFileName => "gemini-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".gemini", "settings.json"),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(Startup.Server.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true)
        .SetProperty("args", new JsonArray {
            $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
            $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".gemini", "settings.json"),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(Startup.Server.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true)
        .SetProperty("args", new JsonArray {
            $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
            $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".gemini", "settings.json"),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".gemini", "settings.json"),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            var addMcpServerCommandStdio = $"gemini mcp add {AiAgentConfig.DefaultMcpServerName} \"{Startup.Server.ExecutableFullPath}\" port={UnityMcpPlugin.Port} plugin-timeout={UnityMcpPlugin.TimeoutMs} client-transport=stdio";
            var addMcpServerCommandHttp = $"gemini mcp add --transport http {AiAgentConfig.DefaultMcpServerName} {UnityMcpPlugin.Host}";

            // STDIO Configuration

            var manualStepsOption1 = TemplateFoldoutFirst("Manual Configuration Steps - Option 1");

            manualStepsOption1!.Add(TemplateLabelDescription("1. Open a terminal and run the following command to be in the folder of the Unity project"));
            manualStepsOption1!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            manualStepsOption1!.Add(TemplateLabelDescription("2. Run the following command in the folder of the Unity project to configure Gemini"));
            manualStepsOption1!.Add(TemplateTextFieldReadOnly(addMcpServerCommandStdio));
            manualStepsOption1!.Add(TemplateLabelDescription("3. Start Gemini"));
            manualStepsOption1!.Add(TemplateTextFieldReadOnly("gemini"));

            ContainerStdio!.Add(manualStepsOption1);

            var manualStepsOption2 = TemplateFoldout("Manual Configuration Steps - Option 2");

            manualStepsOption2!.Add(TemplateLabelDescription("1. Open or create file '.gemini/settings.json'"));
            manualStepsOption2!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsOption2!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            ContainerStdio!.Add(manualStepsOption2);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Gemini CLI is installed and accessible from terminal"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsOption1Http = TemplateFoldoutFirst("Manual Configuration Steps - Option 1");

            manualStepsOption1Http!.Add(TemplateLabelDescription("1. Open a terminal and run the following command to be in the folder of the Unity project"));
            manualStepsOption1Http!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            manualStepsOption1Http!.Add(TemplateLabelDescription("2. Run the following command in the folder of the Unity project to configure Gemini"));
            manualStepsOption1Http!.Add(TemplateTextFieldReadOnly(addMcpServerCommandHttp));
            manualStepsOption1Http!.Add(TemplateLabelDescription("3. Start Gemini"));
            manualStepsOption1Http!.Add(TemplateTextFieldReadOnly("gemini"));

            ContainerHttp!.Add(manualStepsOption1Http);

            var manualStepsOption2Http = TemplateFoldout("Manual Configuration Steps - Option 2");

            manualStepsOption2Http!.Add(TemplateLabelDescription("1. Open or create file '.gemini/settings.json'"));
            manualStepsOption2Http!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsOption2Http!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));

            ContainerHttp!.Add(manualStepsOption2Http);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Gemini CLI is installed and accessible from terminal"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
