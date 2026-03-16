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
using UnityEngine;
using UnityEngine.UIElements;

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
        public override string? SkillsPath => UnityMcpPluginEditor.SkillsPath;

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
            SetTransportMethod(UnityMcpPluginEditor.TransportMethod);
            SetAgentName(AgentName);
            DisableLinksContainer();

            // STDIO Configuration

            ContainerStdio!.Add(TemplateLabelDescription("Copy paste the json into your MCP Client to configure it."));
            ContainerStdio!.Add(TemplateTextFieldReadOnly(McpServerManager.RawJsonConfigurationStdio(
                port: UnityMcpPluginEditor.Port,
                bodyPath: "mcpServers",
                timeoutMs: UnityMcpPluginEditor.TimeoutMs,
                type: "stdio").ToString()));

            // HTTP Configuration

            ContainerHttp!.Add(TemplateLabelDescription("1. (First time or after port/version changes) Setup and start the MCP server using Docker."));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.DockerSetupRunCommand()));

            ContainerHttp!.Add(TemplateLabelDescription("2. (Next time) Start the MCP server using Docker."));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.DockerRunCommand()));

            ContainerHttp!.Add(TemplateLabelDescription("3. Copy paste the json into your MCP Client to configure it."));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.RawJsonConfigurationHttp(
                url: UnityMcpPluginEditor.Host,
                bodyPath: "mcpServers",
                type: null).ToString()));

            ContainerHttp!.Add(TemplateLabelDescription("4. (Optional) Stop and remove the MCP server using Docker when you are done."));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.DockerStopCommand()));
            ContainerHttp!.Add(TemplateTextFieldReadOnly(McpServerManager.DockerRemoveCommand()));
        }

        protected override void SetupSkillsUI()
        {
            if (ContainerSkills == null)
                return;

            // "SKILLS" header
            var headerLabel = new Label("SKILLS (procedural)");
            headerLabel.AddToClassList("timeline-label");
            headerLabel.style.alignSelf = Align.FlexStart;
            headerLabel.tooltip = "Skills give the AI specialized procedural knowledge to handle complex tasks. " +
                "They work by providing structured guides (Markdown files) that the AI reads to understand exactly " +
                "how to execute workflows.";
            ContainerSkills.Add(headerLabel);

            // Editable output path
            var pathRow = new VisualElement();
            pathRow.style.flexDirection = FlexDirection.Row;
            pathRow.style.alignItems = Align.Center;
            pathRow.style.marginTop = 4;
            pathRow.style.marginBottom = 2;

            var pathLabel = new Label("Output Path");
            pathLabel.AddToClassList("section-desc");
            pathLabel.style.marginBottom = 0;
            pathLabel.style.flexShrink = 0;
            pathLabel.tooltip = "Root folder path where skill markdown files will be generated.";
            pathRow.Add(pathLabel);

            var pathField = new TextField { value = UnityMcpPluginEditor.SkillsPath };
            pathField.style.flexGrow = 1;
            pathField.style.flexShrink = 1;
            pathField.style.minWidth = 0;
            pathField.style.marginLeft = 47;
            pathField.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.SkillsPath = evt.newValue;
                UnityMcpPluginEditor.Instance.Save();
            });
            pathRow.Add(pathField);

            ContainerSkills.Add(pathRow);

            // Auto-generate toggle + Generate button row
            var toggleRow = new VisualElement();
            toggleRow.style.flexDirection = FlexDirection.Row;
            toggleRow.style.alignItems = Align.Center;
            toggleRow.style.justifyContent = Justify.SpaceBetween;
            toggleRow.style.marginTop = 2;

            var toggleLabelContainer = new VisualElement();
            toggleLabelContainer.style.flexDirection = FlexDirection.Row;
            toggleLabelContainer.style.alignItems = Align.Center;

            var toggleLabel = new Label("Automatic generate");
            toggleLabel.AddToClassList("section-desc");
            toggleLabel.style.marginBottom = 0;
            toggleLabel.tooltip = "Automatically regenerate skill files each time the Unity Editor reloads domain.";
            toggleLabelContainer.Add(toggleLabel);

            var toggle = new Toggle();
            toggle.style.marginLeft = 4;
            toggle.tooltip = "Automatically regenerate skill files each time the Unity Editor reloads domain.";
            toggle.SetValueWithoutNotify(UnityMcpPluginEditor.GenerateSkillFiles);
            toggleLabelContainer.Add(toggle);
            toggleRow.Add(toggleLabelContainer);

            var btnGenerate = new Button { text = "Generate" };
            btnGenerate.AddToClassList("btn-compact");
            btnGenerate.tooltip = "Manually generate skill files.";
            btnGenerate.RegisterCallback<ClickEvent>(evt =>
            {
                UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
            });
            toggleRow.Add(btnGenerate);

            toggle.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.GenerateSkillFiles = evt.newValue;
                UnityMcpPluginEditor.Instance.Save();

                if (evt.newValue)
                    UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
            });

            ContainerSkills.Add(toggleRow);
        }
    }
}
