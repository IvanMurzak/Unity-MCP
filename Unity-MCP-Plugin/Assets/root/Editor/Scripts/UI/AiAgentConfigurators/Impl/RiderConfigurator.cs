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
using System;
using System.IO;
using System.Text.Json.Nodes;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Rider AI agent.
    /// </summary>
    public class RiderConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Rider (junie)";
        public override string AgentId => "rider-junie";
        public override string DownloadUrl => "https://www.jetbrains.com/rider/download/";

        protected override string? IconFileName => "rider-64.png";

        private string JunieConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".junie",
            "mcp",
            "mcp.json"
        );

        protected override AiAgentConfig CreateConfigStdioWindows() => CreateJunieConfig(TransportMethod.stdio);
        protected override AiAgentConfig CreateConfigStdioMacLinux() => CreateJunieConfig(TransportMethod.stdio);
        protected override AiAgentConfig CreateConfigHttpWindows() => CreateJunieConfig(TransportMethod.streamableHttp);
        protected override AiAgentConfig CreateConfigHttpMacLinux() => CreateJunieConfig(TransportMethod.streamableHttp);

        private AiAgentConfig CreateJunieConfig(TransportMethod transportMethod)
        {
            var config = new JsonAiAgentConfig(
                name: "Unity Project",
                configPath: JunieConfigPath,
                bodyPath: Consts.MCP.Server.DefaultBodyPath
            )
            .SetProperty("enabled", JsonValue.Create(true), requiredForConfiguration: true)
            .SetPropertyToRemove("disabled");

            if (transportMethod == TransportMethod.stdio)
            {
                config.SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
                      .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
                      .SetProperty("args", new JsonArray {
                          $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
                          $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
                          $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
                      }, requiredForConfiguration: true)
                      .SetPropertyToRemove("url");
            }
            else
            {
                config.SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
                      .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
                      .SetPropertyToRemove("command")
                      .SetPropertyToRemove("args");
            }

            return config;
        }

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // STDIO Configuration
            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");
            
            var relativePath = Path.Combine(".junie", "mcp", "mcp.json");
            var terminalCommandStdio = $"mkdir -p .junie/mcp && printf '{ConfigStdio.ExpectedFileContent.Replace("'", "'\\''")}' > {relativePath}";

            manualStepsContainer!.Add(TemplateLabelDescription("Option 1: Use Terminal (Recommended for CLI lovers)"));
            manualStepsContainer!.Add(TemplateLabelDescription("Run this command in your project root terminal:"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(terminalCommandStdio));

            manualStepsContainer!.Add(TemplateLabelDescription("Option 2: Manual File Creation"));
            manualStepsContainer!.Add(TemplateLabelDescription($"1. Create or open the file: {relativePath}"));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Copy and paste the JSON below:"));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));
            
            manualStepsContainer!.Add(TemplateLabelDescription("Option 3: Rider Settings"));
            manualStepsContainer!.Add(TemplateLabelDescription("Open Rider settings: Settings | Tools | Junie | MCP Settings and add a new server."));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Restart Rider after configuration changes"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- If using Terminal, ensure you are in the Unity project root folder."));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration
            ContainerHttp!.Clear();
            
            var warningLabel = TemplateWarningLabel("Apologies for inconvenience. Please use Stdio to connect. Currently in Rider only Junie will be able to connect to Unity MCP, via Stdio.");
            warningLabel.style.color = new StyleColor(UnityEngine.Color.yellow);
            warningLabel.style.whiteSpace = WhiteSpace.Normal;
            warningLabel.style.marginBottom = 10;
            
            ContainerHttp!.Add(warningLabel);
            ContainerHttp!.Add(TemplateLabelDescription("The standard HTTP configuration is disabled due to stability issues."));
            ContainerHttp!.Add(TemplateLabelDescription("Please switch to the 'Stdio' transport method at the top to configure this agent."));
        }
    }
}
