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
    /// Configurator for Visual Studio (Copilot) AI Agent.
    /// </summary>
    public class VisualStudioCopilotConfigurator2 : AiAgentConfigurator
    {
        public override string AgentName => "Visual Studio (Copilot)";
        public override string AgentId => "vs-copilot";
        public override string DownloadUrl => "https://visualstudio.microsoft.com/downloads/";
        public override string TutorialUrl => "https://www.youtube.com/watch?v=RGdak4T69mc";

        protected override string? IconFileName => "visual-studio-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.stdio,
            configPath: Path.Combine(".vs", "mcp.json"),
            bodyPath: "servers"
        );

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.stdio,
            configPath: Path.Combine(".vs", "mcp.json"),
            bodyPath: "servers"
        );

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.streamableHttp,
            configPath: Path.Combine(".vs", "mcp.json"),
            bodyPath: "servers"
        );

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            transportMethod: TransportMethod.streamableHttp,
            configPath: Path.Combine(".vs", "mcp.json"),
            bodyPath: "servers"
        );

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            ContainerUnderHeader!.Add(TemplateLabelDescription("Visual Studio has integration of GitHub Copilot that operates as AI agent in the IDE."));
            ContainerUnderHeader!.Add(TemplateLabelDescription("Visual Studio starts MCP server after the first prompt."));

            // STDIO Configuration

            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open or create file '.vs/mcp.json' in folder of Unity project (this folder must contain 'Assets' folder inside)."));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- '.vs/mcp.json' file must have no json syntax errors."));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Unity may stay 'Connecting...' until the first prompt sent is processed."));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var manualStepsContainerHttp = TemplateFoldoutFirst("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open or create file '.vs/mcp.json' in folder of Unity project (this folder must contain 'Assets' folder inside)."));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration json into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- '.vs/mcp.json' file must have no json syntax errors."));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Unity may stay 'Connecting...' until the first prompt sent is processed."));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
