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
    /// Configurator for Visual Studio Code (Copilot) AI agent.
    /// </summary>
    public class VisualStudioCodeCopilotConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Visual Studio Code (Copilot)";
        public override string AgentId => "vscode-copilot";
        public override string DownloadUrl => "https://code.visualstudio.com/download";
        public override string TutorialUrl => "https://www.youtube.com/watch?v=ZhP7Ju91mOE";

        protected override string? IconFileName => "vs-code-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".vscode", "mcp.json"),
            transportMethod: TransportMethod.stdio,
            transportMethodValue: "stdio",
            bodyPath: "servers"
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
            configPath: Path.Combine(".vscode", "mcp.json"),
            transportMethod: TransportMethod.stdio,
            transportMethodValue: "stdio",
            bodyPath: "servers"
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
            configPath: Path.Combine(".vscode", "mcp.json"),
            transportMethod: TransportMethod.streamableHttp,
            transportMethodValue: "http",
            bodyPath: "servers"
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(".vscode", "mcp.json"),
            transportMethod: TransportMethod.streamableHttp,
            transportMethodValue: "http",
            bodyPath: "servers"
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            ContainerUnderHeader!.Add(TemplateLabelDescription("VS Code has integration of GitHub Copilot that operates as AI agent in the IDE."));

            // STDIO Configuration

            ContainerStdio!.Add(TemplateWarningLabel("IMPORTANT: Need to start 'ai-game-developer' MCP server manually in Visual Studio Code each time after Visual Studio Code restart."));

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open or create file '.vscode/mcp.json' in folder of Unity project (this folder must contain 'Assets' folder inside)."));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));
            manualStepsContainer!.Add(TemplateLabelDescription("3. Click on 'Extensions' in Visual Studio Code."));
            manualStepsContainer!.Add(TemplateLabelDescription("4. Open 'MCP SERVERS - INSTALLED' category in the extensions list."));
            manualStepsContainer!.Add(TemplateLabelDescription("5. Click on settings icon at 'ai-game-developer' in the list."));
            manualStepsContainer!.Add(TemplateLabelDescription("6. Click 'Start Server'. Done! At this point MCP is running and Unity should successfully connect."));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- '.vscode/mcp.json' file must have no json syntax errors."));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open or create file '.vscode/mcp.json' in folder of Unity project (this folder must contain 'Assets' folder inside)."));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("3. Click on 'Extensions' in Visual Studio Code."));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("4. Open 'MCP SERVERS - INSTALLED' category in the extensions list."));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("5. Click on settings icon at 'ai-game-developer' in the list."));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("6. Click 'Start Server'. Done! At this point MCP is running and Unity should successfully connect."));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- '.vscode/mcp.json' file must have no json syntax errors."));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
