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

        protected override void SetupAlertPanel()
        {
            if (ContainerAlert == null)
                return;

            ContainerAlert.Clear();

            _alertPanel = new AlertPanel(
                "Setup Required",
                "Skills should be configured for AI agents to work properly:"
            );
            _alertPanel.AddItem("\u2022 Enable Auto-generate Skills below", "alert-frame-item-recommended");
            _alertPanel.SetButton("Enable Skills", EnableAutoGenerateSkills);

            ContainerAlert.Add(_alertPanel.Root);
            UpdateAlertPanel();
        }

        protected override void UpdateAlertPanel()
        {
            if (_alertPanel == null)
                return;

            // Custom configurator can only validate skills (no MCP config detection)
            var hasSkills = SupportsSkills && UnityMcpPluginEditor.IsAutoGenerateSkills(AgentId);
            _alertPanel.SetVisible(!hasSkills);

            if (ContainerAlert != null)
                ContainerAlert.style.display = !hasSkills ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override void OnUICreated(VisualElement root)
        {
            SetAgentIcon();
            SetTransportMethod(UnityMcpPluginEditor.TransportMethod);
            SetAgentName(AgentName);
            DisableLinksContainer();

            // Add spacing between title and skills section
            ContainerUnderHeader!.style.marginBottom = 4;

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

            var section = TemplateSkillsSection();
            var pathLabel = section.Q<Label>("labelSkillsPath");
            var toggleAutoGenerate = section.Q<Toggle>("toggleAutoGenerateSkills");
            var btnGenerate = section.Q<Button>("btnGenerateSkills");
            var unsupportedLabel = section.Q<Label>("labelSkillsUnsupported");

            // Hide the unsupported label
            unsupportedLabel.style.display = DisplayStyle.None;

            // Replace the read-only path label with an editable TextField for custom configurator
            var headerRow = pathLabel.parent;
            var inputPath = new TextField { value = UnityMcpPluginEditor.SkillsPath };
            inputPath.style.flexGrow = 1;
            inputPath.style.flexShrink = 1;
            inputPath.style.minWidth = 0;
            inputPath.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.SkillsPath = evt.newValue;
                UnityMcpPluginEditor.Instance.Save();
            });
            headerRow.Remove(pathLabel);
            headerRow.Add(inputPath);

            // Configure toggle (per-agent)
            toggleAutoGenerate.SetValueWithoutNotify(UnityMcpPluginEditor.IsAutoGenerateSkills(AgentId));
            toggleAutoGenerate.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.SetAutoGenerateSkills(AgentId, evt.newValue);
                UnityMcpPluginEditor.Instance.Save();

                if (evt.newValue)
                    UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);

                UpdateAlertPanel();
            });

            // Configure generate button
            btnGenerate.RegisterCallback<ClickEvent>(evt =>
            {
                UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
            });

            ContainerSkills.Add(section);
        }
    }
}
