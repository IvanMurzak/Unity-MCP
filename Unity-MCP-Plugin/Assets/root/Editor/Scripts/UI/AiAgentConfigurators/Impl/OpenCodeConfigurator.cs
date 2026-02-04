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
    /// Configurator for Open Code AI agent.
    /// </summary>
    public class OpenCodeConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Open Code";
        public override string AgentId => "open-code";
        public override string DownloadUrl => "https://opencode.ai/download";

        protected override string? IconFileName => "open-code-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new JsonCommandAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine("opencode.json"),
            transportMethod: TransportMethod.stdio,
            transportMethodValue: "local",
            bodyPath: "mcp"
        );

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new JsonCommandAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine("opencode.json"),
            transportMethod: TransportMethod.stdio,
            transportMethodValue: "local",
            bodyPath: "mcp"
        );

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonCommandAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine("opencode.json"),
            transportMethod: TransportMethod.streamableHttp,
            transportMethodValue: "http",
            bodyPath: "mcp"
        );

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonCommandAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine("opencode.json"),
            transportMethod: TransportMethod.streamableHttp,
            transportMethodValue: "http",
            bodyPath: "mcp"
        );

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // STDIO Configuration

            var startCliContainer = TemplateFoldoutFirst("Start Open Code CLI");

            startCliContainer!.Add(TemplateLabelDescription("1. Open a terminal and run the following command to be in the folder of the Unity project"));
            startCliContainer!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            startCliContainer!.Add(TemplateLabelDescription("2. Start Open Code"));
            startCliContainer!.Add(TemplateTextFieldReadOnly("opencode"));

            ContainerStdio!.Add(startCliContainer);

            var manualStepsContainer = TemplateFoldout("Manual Configuration Steps");

            manualStepsContainer!.Add(TemplateLabelDescription("1. Open or create file 'opencode.json' in the project root folder (the folder must contain the Assets folder inside)"));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the configuration JSON into the file."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Open Code CLI is installed and accessible from terminal"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure Open Code CLI is launched from the project root folder (the folder must contain the Assets folder inside)"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration

            var startCliContainerHttp = TemplateFoldoutFirst("Start Open Code CLI");

            startCliContainerHttp!.Add(TemplateLabelDescription("1. Open a terminal and run the following command to be in the folder of the Unity project"));
            startCliContainerHttp!.Add(TemplateTextFieldReadOnly($"cd \"{ProjectRootPath}\""));
            startCliContainerHttp!.Add(TemplateLabelDescription("2. Start Open Code"));
            startCliContainerHttp!.Add(TemplateTextFieldReadOnly("opencode"));

            ContainerHttp!.Add(startCliContainerHttp);

            var manualStepsContainerHttp = TemplateFoldout("Manual Configuration Steps");

            manualStepsContainerHttp!.Add(TemplateLabelDescription("1. Open or create file 'opencode.json' in the project root folder (the folder must contain the Assets folder inside)"));
            manualStepsContainerHttp!.Add(TemplateLabelDescription("2. Copy and paste the configuration JSON into the file."));
            manualStepsContainerHttp!.Add(TemplateTextFieldReadOnly(ConfigHttp.ExpectedFileContent));

            ContainerHttp!.Add(manualStepsContainerHttp);

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Open Code CLI is installed and accessible from terminal"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure Open Code CLI is launched from the project root folder (the folder must contain the Assets folder inside)"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }
    }
}
