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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Configurator for Custom MCP client.
    /// </summary>
    public class CustomConfigurator : AiAgentConfigurator
    {
        public override string AgentName => "Other - Custom";
        public override string AgentId => "other-custom";
        public override string DownloadUrl => "NA";

        protected override string? IconFileName => null;

        protected override AiAgentConfig CreateConfigStdioWindows()
        {
            throw new System.NotImplementedException();
        }

        protected override AiAgentConfig CreateConfigStdioMacLinux()
        {
            throw new System.NotImplementedException();
        }

        protected override AiAgentConfig CreateConfigHttpWindows()
        {
            throw new System.NotImplementedException();
        }

        protected override AiAgentConfig CreateConfigHttpMacLinux()
        {
            throw new System.NotImplementedException();
        }

        protected override AiAgentConfigurator SetConfigureStatusIndicator()
        {
            // Custom configurator doesn't have configure status indicator
            return this;
        }

        protected override void UpdateRemoveButton()
        {
            // Custom configurator doesn't have a remove button, so we can skip this
        }

        protected override void OnUICreated(VisualElement root)
        {
            SetAgentIcon();
            SetTransportMethod(UnityMcpPlugin.TransportMethod);
            SetAgentName(AgentName);
            DisableLinksContainer();

            // STDIO Configuration

            ContainerStdio!.Add(TemplateLabelDescription("Copy paste the json into your MCP Client to configure it."));
            ContainerStdio!.Add(TemplateTextFieldReadOnly(McpServerManager.RawJsonConfigurationStdio(
                port: UnityMcpPlugin.Port,
                bodyPath: "mcpServers",
                timeoutMs: UnityMcpPlugin.TimeoutMs,
                type: "stdio").ToString()));

            // HTTP Configuration

            ContainerHttp!.Add(TemplateLabelDescription("1. (First time or after port/version changes) Setup and start the MCP server using Docker."));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.DockerSetupRunCommand()));

            ContainerHttp!.Add(TemplateLabelDescription("2. (Next time) Start the MCP server using Docker."));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.DockerRunCommand()));

            ContainerHttp!.Add(TemplateLabelDescription("3. Copy paste the json into your MCP Client to configure it."));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.RawJsonConfigurationHttp(
                url: UnityMcpPlugin.Host,
                bodyPath: "mcpServers",
                type: null).ToString()));

            ContainerHttp!.Add(TemplateLabelDescription("4. (Optional) Stop and remove the MCP server using Docker when you are done."));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.DockerStopCommand()));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.DockerRemoveCommand()));
        }
    }
}
