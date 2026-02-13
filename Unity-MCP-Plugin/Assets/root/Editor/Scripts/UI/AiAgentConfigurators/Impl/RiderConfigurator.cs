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
        public override string AgentName => "Rider";
        public override string AgentId => "rider";
        public override string DownloadUrl => "https://www.jetbrains.com/rider/download/";

        protected override string? IconFileName => "rider-64.png";

        protected override AiAgentConfig CreateConfigStdioWindows() => new RiderJsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JetBrains",
                "Rider",
                "mcp.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
            $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        protected override AiAgentConfig CreateConfigStdioMacLinux() => new RiderJsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "JetBrains",
                "Rider",
                "mcp.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
        .SetProperty("args", new JsonArray {
            $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
            $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
            $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
        }, requiredForConfiguration: true)
        .SetPropertyToRemove("url");

        // ... (HTTP Configs remain unchanged) ...

        protected override AiAgentConfig CreateConfigHttpWindows() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "JetBrains",
                "Rider",
                "mcp.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");

        protected override AiAgentConfig CreateConfigHttpMacLinux() => new JsonAiAgentConfig(
            name: AgentName,
            configPath: Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "JetBrains",
                "Rider",
                "mcp.json"
            ),
            bodyPath: Consts.MCP.Server.DefaultBodyPath
        )
        .SetProperty("type", JsonValue.Create("http"), requiredForConfiguration: true)
        .SetProperty("disabled", JsonValue.Create(false), requiredForConfiguration: true)
        .SetProperty("url", JsonValue.Create(UnityMcpPlugin.Host), requiredForConfiguration: true, comparison: ValueComparisonMode.Url)
        .SetPropertyToRemove("command")
        .SetPropertyToRemove("args");


        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // STDIO Configuration
            var manualStepsContainer = TemplateFoldoutFirst("Manual Configuration Steps");
            manualStepsContainer!.Add(TemplateLabelDescription("Rider typically uses HTTP for MCP, but can be configured with a JSON file."));
            manualStepsContainer!.Add(TemplateLabelDescription("1. Open Rider settings: Settings | Tools | Junie | MCP Settings."));
            manualStepsContainer!.Add(TemplateLabelDescription("2. Add a new server with the following details."));
            manualStepsContainer!.Add(TemplateTextFieldReadOnly(ConfigStdio.ExpectedFileContent));

            ContainerStdio!.Add(manualStepsContainer);

            var troubleshootingContainerStdio = TemplateFoldout("Troubleshooting");

            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));
            troubleshootingContainerStdio.Add(TemplateLabelDescription("- Restart Rider after configuration changes"));

            ContainerStdio!.Add(troubleshootingContainerStdio);

            // HTTP Configuration
            ContainerHttp!.Clear();
            
            var warningLabel = TemplateWarningLabel("Apologies for inconvenience. Please use Stdio to connect. Currently in Rider only Junie will be able to connect to Unity MCP, via Stdio.");
            warningLabel.style.color = new StyleColor(UnityEngine.Color.yellow);
            warningLabel.style.whiteSpace = WhiteSpace.Normal;
            warningLabel.style.marginBottom = 10;
            
            ContainerHttp!.Add(warningLabel);
            ContainerHttp!.Add(TemplateLabelDescription("The standard HTTP configuration is disabled due to stability issues."));

            var troubleshootingContainerHttp = TemplateFoldout("Troubleshooting");

            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Ensure MCP configuration file doesn't have syntax errors"));
            troubleshootingContainerHttp.Add(TemplateLabelDescription("- Restart Rider after configuration changes"));

            ContainerHttp!.Add(troubleshootingContainerHttp);
        }

        private class RiderJsonAiAgentConfig : JsonAiAgentConfig
        {
            public RiderJsonAiAgentConfig(string name, string configPath, string bodyPath) : base(name, configPath, bodyPath)
            {
            }

            public override bool Configure()
            {
                var result = base.Configure();
                if (result)
                {
                    ConfigureJunie();
                }
                return result;
            }

            private void ConfigureJunie()
            {
                try
                {
                    var junieConfigPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".junie",
                        "mcp",
                        "mcp.json"
                    );

                    var junieConfig = new JsonAiAgentConfig(
                        name: "Unity Project",
                        configPath: junieConfigPath,
                        bodyPath: Consts.MCP.Server.DefaultBodyPath
                    )
                    .SetProperty("type", JsonValue.Create("stdio"), requiredForConfiguration: true)
                    .SetProperty("command", JsonValue.Create(McpServerManager.ExecutableFullPath.Replace('\\', '/')), requiredForConfiguration: true, comparison: ValueComparisonMode.Path)
                    .SetProperty("args", new JsonArray {
                        $"{Consts.MCP.Server.Args.Port}={UnityMcpPlugin.Port}",
                        $"{Consts.MCP.Server.Args.PluginTimeout}={UnityMcpPlugin.TimeoutMs}",
                        $"{Consts.MCP.Server.Args.ClientTransportMethod}={TransportMethod.stdio}"
                    }, requiredForConfiguration: true)
                    .SetPropertyToRemove("url");

                    junieConfig.Configure();
                    UnityEngine.Debug.Log($"Also configured Junie MCP at: {junieConfigPath}");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to configure Junie MCP: {ex.Message}");
                }
            }
        }
    }
}
