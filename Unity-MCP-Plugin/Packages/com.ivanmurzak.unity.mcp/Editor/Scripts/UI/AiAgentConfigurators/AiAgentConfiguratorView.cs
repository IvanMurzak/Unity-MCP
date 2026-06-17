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
using System;
using System.IO;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using AgentConfig = com.IvanMurzak.McpPlugin.AgentConfig;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Thin UIToolkit adapter over a shared <see cref="AgentConfig.AiAgentConfigurator"/>.
    /// The shared library owns ALL configurator logic (config-file building, detection, the
    /// three-state status, and the per-transport UI content as an engine-agnostic
    /// <see cref="AgentConfig.AgentConfiguratorDescription"/> DTO). This view maps that DTO onto
    /// Unity's existing <c>Template*</c> UIToolkit elements and wires the Configure / Remove /
    /// Reconfigure actions back to the shared config's <c>Configure()</c> / <c>Unconfigure()</c>.
    /// No per-agent logic lives here — every agent renders through the same DTO walk.
    /// </summary>
    /// <remarks>
    /// Replaces the former Unity-local <c>AiAgentConfigurator</c> base + <c>ConfigurationElements</c>
    /// (both retired). The element vocabularies match 1:1 with the shared DTO's
    /// <see cref="AgentConfig.ConfigurationItemKind"/>; this view adds the Link element (open-URL)
    /// and the reconfigure-needed alert rendering required for parity with the DTO.
    /// </remarks>
    public sealed class AiAgentConfiguratorView
    {
        private readonly AgentConfig.AiAgentConfigurator _configurator;

        private VisualElement? Root { get; set; }
        private VisualElement? ContainerUnderHeader { get; set; }
        private VisualElement? ContainerAlert { get; set; }
        private VisualElement? ContainerHttp { get; set; }
        private VisualElement? ContainerStdio { get; set; }
        private VisualElement? ContainerSkills { get; set; }

        private AlertPanel? _alertPanel;
        private AlertPanel? _reconfigureAlertPanel;

        public AiAgentConfiguratorView(AgentConfig.AiAgentConfigurator configurator)
        {
            _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        }

        #region Public surface consumed by the window

        public string AgentId => _configurator.AgentId;
        public string AgentName => _configurator.AgentName;
        public bool SupportsSkills => _configurator.SupportsSkills;
        public string? SkillsPath => _configurator.SkillsPath;

        /// <summary>
        /// Invalidates cached state. The shared configurator is stateless (settings are passed
        /// per call), so there is nothing to clear here — kept for call-site parity with the
        /// former Unity configurator and as a future hook.
        /// </summary>
        public void Invalidate() { }

        #endregion

        #region Settings snapshot

        private AgentConfig.AgentConfiguratorSettings CurrentSettings() => AgentConfiguratorSettingsFactory.Create();

        private TransportMethod ActiveTransport => UnityMcpPluginEditor.TransportMethod;

        #endregion

        #region UI Templates (mirror the former AiAgentConfigurator template helpers)

        private static Label TemplateLabelDescription(string? text = null)
        {
            var result = new UITemplate<Label>("Editor/UI/uxml/agents/elements/TemplateLabelDescription.uxml").Value;
            if (text != null) result.text = text;
            return result;
        }
        private static Label TemplateWarningLabel(string? text = null)
        {
            var result = new UITemplate<Label>("Editor/UI/uxml/agents/elements/TemplateWarningLabel.uxml").Value;
            if (text != null) result.text = text;
            return result;
        }
        private static Label TemplateAlertLabel(string? text = null)
        {
            var result = new UITemplate<Label>("Editor/UI/uxml/agents/elements/TemplateAlertLabel.uxml").Value;
            if (text != null) result.text = text;
            return result;
        }
        private static TextField TemplateTextFieldReadOnly(string? value = null)
        {
            var result = new UITemplate<TextField>("Editor/UI/uxml/agents/elements/TemplateTextFieldReadOnly.uxml").Value;
            if (value != null) result.value = value;
            return result;
        }
        private static Foldout TemplateFoldoutFirst(string? text = null)
        {
            var result = new UITemplate<Foldout>("Editor/UI/uxml/agents/elements/TemplateFoldoutFirst.uxml").Value;
            if (text != null) result.text = text;
            return result;
        }
        private static Foldout TemplateFoldout(string? text = null)
        {
            var result = new UITemplate<Foldout>("Editor/UI/uxml/agents/elements/TemplateFoldout.uxml").Value;
            if (text != null) result.text = text;
            return result;
        }
        private static VisualElement TemplateSkillsSection() =>
            new UITemplate<VisualElement>("Editor/UI/uxml/agents/elements/TemplateSkillsSection.uxml").Value;

        #endregion

        #region Path helpers

        private static string ProjectRootPath => UnityMcpPluginEditor.ProjectRootPath;

        /// <summary>Shortens an absolute path to a project-relative display string.</summary>
        public static string ToDisplayPath(string fullPath)
        {
            var root = ProjectRootPath.Replace('\\', '/').TrimEnd('/') + '/';
            var normalized = fullPath.Replace('\\', '/');
            return normalized.StartsWith(root) ? normalized[root.Length..] : normalized;
        }

        private static string ResolveAbsoluteSkillsPath(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return folder;
            return Path.IsPathRooted(folder)
                ? folder
                : Path.GetFullPath(Path.Combine(ProjectRootPath, folder));
        }

        #endregion

        #region UI Creation

        /// <summary>
        /// Creates and returns the visual element containing the configuration UI for this agent.
        /// </summary>
        public VisualElement? CreateUI(VisualElement container)
        {
            var root = new UITemplate<VisualElement>("Editor/UI/uxml/agents/AiAgentTemplateConfig.uxml").Value;

            Root = root;
            ContainerUnderHeader = root.Q<VisualElement>("containerUnderHeader") ?? throw new NullReferenceException("VisualElement 'containerUnderHeader' not found in UI.");
            ContainerAlert = root.Q<VisualElement>("containerAlert") ?? throw new NullReferenceException("VisualElement 'containerAlert' not found in UI.");
            ContainerHttp = root.Q<VisualElement>("containerHttp") ?? throw new NullReferenceException("VisualElement 'containerHttp' not found in UI.");
            ContainerStdio = root.Q<VisualElement>("containerStdio") ?? throw new NullReferenceException("VisualElement 'containerStdio' not found in UI.");
            ContainerSkills = root.Q<VisualElement>("containerSkills") ?? throw new NullReferenceException("VisualElement 'containerSkills' not found in UI.");

            SetAgentName(_configurator.AgentName);
            SetAgentIcon();
            SetupHeaderLinks();
            BuildTransportSections();
            SetupSkillsUI();
            SetupAlertPanel();
            SetTransportMethod(UnityMcpPluginEditor.TransportMethod);

            McpWindowBase.EnableSmoothFoldoutTransitions(root);
            return root;
        }

        private void SetAgentName(string name)
        {
            var nameLabel = Root!.Q<Label>("agentName") ?? throw new NullReferenceException("Label 'agentName' not found in UI.");
            nameLabel.text = name;
        }

        private void SetAgentIcon()
        {
            var agentIcon = Root!.Q<VisualElement>("agentIcon") ?? throw new NullReferenceException("VisualElement 'agentIcon' not found in UI.");

            var iconName = _configurator.IconName;
            if (string.IsNullOrEmpty(iconName))
            {
                agentIcon.style.display = DisplayStyle.None;
                return;
            }

            var iconPaths = EditorAssetLoader.GetEditorAssetPaths($"Editor/Gizmos/ai-agents/{iconName}");
            var icon = EditorAssetLoader.LoadAssetAtPath<Texture2D>(iconPaths);

            agentIcon.style.backgroundImage = icon == null ? null : new StyleBackground(icon);
            agentIcon.style.display = icon == null ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// Renders the header open-URL links from the shared DTO's <see cref="AgentConfig.AgentConfiguratorDescription.Links"/>
        /// onto the existing download / tutorial label slots. Each link is a
        /// <see cref="AgentConfig.ConfigurationItemKind.Link"/> carrying a display label + URL — this is the
        /// Link-element rendering added for DTO parity. When the configurator emits no links
        /// (e.g. the Custom agent) the whole links row is hidden, mirroring the old
        /// <c>DisableLinksContainer()</c> behaviour.
        /// </summary>
        private void SetupHeaderLinks()
        {
            var linksContainer = Root!.Q<VisualElement>("linksContainer");
            var downloadLink = Root!.Q<Label>("downloadLink");
            var tutorialSeparator = Root!.Q<Label>("tutorialSeparator");
            var tutorialLink = Root!.Q<Label>("tutorialLink");

            // Describe() carries the engine-agnostic link list; transport choice does not affect links.
            var links = _configurator.BuildLinks();

            if (links.Count == 0)
            {
                if (linksContainer != null)
                    linksContainer.style.display = DisplayStyle.None;
                return;
            }

            // Link[0] → download slot, Link[1] → tutorial slot (mirrors the original two-link header).
            var download = links.Count > 0 ? links[0] : null;
            var tutorial = links.Count > 1 ? links[1] : null;

            if (downloadLink != null)
            {
                if (download != null && !string.IsNullOrEmpty(download.Url))
                {
                    downloadLink.text = download.Text;
                    downloadLink.style.display = DisplayStyle.Flex;
                    var url = download.Url!;
                    downloadLink.RegisterCallback<ClickEvent>(_ => Application.OpenURL(url));
                }
                else
                {
                    downloadLink.style.display = DisplayStyle.None;
                }
            }

            if (tutorial != null && !string.IsNullOrEmpty(tutorial.Url))
            {
                if (tutorialLink != null)
                {
                    tutorialLink.text = tutorial.Text;
                    tutorialLink.style.display = DisplayStyle.Flex;
                    var url = tutorial.Url!;
                    tutorialLink.RegisterCallback<ClickEvent>(_ => Application.OpenURL(url));
                }
                if (tutorialSeparator != null)
                    tutorialSeparator.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (tutorialLink != null) tutorialLink.style.display = DisplayStyle.None;
                if (tutorialSeparator != null) tutorialSeparator.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Builds both transport containers (stdio + http) from the shared DTO. Each transport's
        /// <see cref="AgentConfig.AiAgentConfigurator.Describe"/> yields ordered sections; the
        /// configure/remove status row is prepended so the user can write/remove the MCP entry.
        /// </summary>
        private void BuildTransportSections()
        {
            BuildTransportContainer(ContainerStdio!, TransportMethod.stdio);
            BuildTransportContainer(ContainerHttp!, TransportMethod.streamableHttp);
        }

        private void BuildTransportContainer(VisualElement container, TransportMethod transport)
        {
            container.Clear();

            var settings = CurrentSettings();
            var description = _configurator.Describe(settings, transport);

            // Configure/Remove status row — only for agents that have a detectable config
            // (the Custom configurator's GetStatus is always NotConfigured and it has no
            // writable config file, so it gets no status row, matching the old behaviour).
            if (HasDetectableConfig)
                container.Add(BuildConfigureStatusRow(transport));

            foreach (var section in description.Sections)
                container.Add(BuildSection(section, transport));
        }

        /// <summary>
        /// True when the configurator exposes a real, writable config file. The Custom agent
        /// throws from its config builders (no detectable file), so it is excluded from the
        /// Configure/Remove status row and reconfigure detection.
        /// </summary>
        private bool HasDetectableConfig => _configurator is not AgentConfig.Impl.CustomConfigurator;

        private VisualElement BuildSection(AgentConfig.ConfigurationSection section, TransportMethod transport)
        {
            var foldout = section.ExpandedFirst ? TemplateFoldoutFirst(section.Heading) : TemplateFoldout(section.Heading);
            foreach (var item in section.Items)
            {
                // The Custom agent's editable skills path is owned by the dedicated skills section
                // (SetupCustomSkillsUI — it adds the auto-generate toggle + Generate button the DTO
                // EditableField can't), matching the old single-field behaviour. The shared
                // CustomConfigurator.BuildSections still emits the editable field (+ its "Skills
                // output path (editable):" label) for engines that have no separate skills UI, so
                // skip that pair here to avoid rendering the field twice.
                if (_configurator is AgentConfig.Impl.CustomConfigurator && IsCustomSkillsPathItem(item))
                    continue;

                var element = BuildItem(item);
                if (element != null)
                    foldout.Add(element);
            }
            return foldout;
        }

        /// <summary>
        /// True for the shared <see cref="AgentConfig.Impl.CustomConfigurator"/>'s editable
        /// skills-path items — the <see cref="AgentConfig.ConfigurationItemKind.EditableField"/>
        /// and its preceding "Skills output path (editable):" description. These are rendered by
        /// the dedicated <see cref="SetupCustomSkillsUI"/> instead, so the section walk skips them.
        /// </summary>
        private static bool IsCustomSkillsPathItem(AgentConfig.ConfigurationItem item)
            => item.Kind == AgentConfig.ConfigurationItemKind.EditableField
                || (item.Kind == AgentConfig.ConfigurationItemKind.Description
                    && item.Text == "Skills output path (editable):");

        /// <summary>
        /// Maps a single shared <see cref="AgentConfig.ConfigurationItem"/> onto a Unity element.
        /// The kind vocabulary matches the Unity Template* elements 1:1; Link and EditableField are
        /// the two additions made for DTO parity.
        /// </summary>
        private VisualElement? BuildItem(AgentConfig.ConfigurationItem item)
        {
            switch (item.Kind)
            {
                case AgentConfig.ConfigurationItemKind.Description:
                    return TemplateLabelDescription(item.Text);
                case AgentConfig.ConfigurationItemKind.Warning:
                    return TemplateWarningLabel(item.Text);
                case AgentConfig.ConfigurationItemKind.Alert:
                    return TemplateAlertLabel(item.Text);
                case AgentConfig.ConfigurationItemKind.ReadOnlyField:
                    return TemplateTextFieldReadOnly(item.Text);
                case AgentConfig.ConfigurationItemKind.EditableField:
                    return BuildEditableField(item.Text);
                case AgentConfig.ConfigurationItemKind.Link:
                    return BuildLinkElement(item);
                default:
                    return TemplateLabelDescription(item.Text);
            }
        }

        /// <summary>
        /// An editable value field. Used by the Custom agent's editable skills path: edits are
        /// normalised + persisted through the same <see cref="UnityMcpPluginEditor.SkillsPath"/>
        /// setter the dedicated skills section uses, and reflected back onto the shared
        /// configurator so a later <c>Describe()</c> renders the persisted value.
        /// </summary>
        private VisualElement BuildEditableField(string value)
        {
            var field = new TextField { value = value };
            field.style.flexGrow = 1;
            field.style.flexShrink = 1;
            field.style.minWidth = 0;
            field.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.SkillsPath = evt.newValue;
                UnityMcpPluginEditor.Instance.Save();

                var normalized = UnityMcpPluginEditor.SkillsPath;
                if (_configurator is AgentConfig.Impl.CustomConfigurator custom)
                    custom.EditableSkillsPath = normalized;

                if (normalized != evt.newValue)
                    field.SetValueWithoutNotify(normalized);
            });
            return field;
        }

        /// <summary>
        /// A clickable open-URL link rendered inside a section (the DTO Link kind). Reuses the
        /// same link-label styling as the header links.
        /// </summary>
        private VisualElement BuildLinkElement(AgentConfig.ConfigurationItem item)
        {
            var label = new Label(item.Text);
            label.AddToClassList("section-desc");
            label.AddToClassList("link-label");
            if (!string.IsNullOrEmpty(item.Url))
            {
                var url = item.Url!;
                label.RegisterCallback<ClickEvent>(_ => Application.OpenURL(url));
            }
            return label;
        }

        #endregion

        #region Configure / Remove status row

        private VisualElement BuildConfigureStatusRow(TransportMethod transport)
        {
            var root = new UITemplate<VisualElement>("Editor/UI/uxml/agents/elements/TemplateConfigureStatus.uxml").Value;
            var statusText = root.Q<Label>("configureStatusText") ?? throw new NullReferenceException("Label 'configureStatusText' not found in UI.");
            var btnConfigure = root.Q<Button>("btnConfigure") ?? throw new NullReferenceException("Button 'btnConfigure' not found in UI.");
            var btnRemove = root.Q<Button>("btnRemoveConfig") ?? throw new NullReferenceException("Button 'btnRemoveConfig' not found in UI.");

            var settings = CurrentSettings();
            var config = GetConfig(settings, transport);

            var pathLabel = root.Q<Label>("labelConfigPath");
            if (pathLabel != null)
            {
                pathLabel.text = ToDisplayPath(config.ConfigPath);
                pathLabel.tooltip = config.ConfigPath;
            }

            UpdateStatusRow(statusText, btnConfigure, btnRemove, transport);

            btnConfigure.RegisterCallback<ClickEvent>(_ =>
            {
                var freshConfig = GetConfig(CurrentSettings(), transport);
                freshConfig.Configure();
                RefreshConfigurationUI();
            });
            btnRemove.RegisterCallback<ClickEvent>(_ =>
            {
                var freshConfig = GetConfig(CurrentSettings(), transport);
                freshConfig.Unconfigure();
                RefreshConfigurationUI();
            });

            return root;
        }

        private AgentConfig.AiAgentConfig GetConfig(AgentConfig.AgentConfiguratorSettings settings, TransportMethod transport)
            => transport == TransportMethod.stdio
                ? _configurator.GetStdioConfig(settings)
                : _configurator.GetHttpConfig(settings);

        private void UpdateStatusRow(Label statusText, Button btnConfigure, Button btnRemove, TransportMethod transport)
        {
            var settings = CurrentSettings();
            var isConfigured = _configurator.IsConfigured(settings, transport);
            var anyConfigured = _configurator.IsDetected(settings);
            var transportText = transport switch
            {
                TransportMethod.stdio => "stdio",
                TransportMethod.streamableHttp => "http",
                _ => "unknown"
            };

            statusText.text = isConfigured ? $"Configured ({transportText})" : "Not configured";
            btnConfigure.text = isConfigured ? "Reconfigure" : "Configure";
            btnConfigure.EnableInClassList("btn-primary", !isConfigured);
            btnRemove.style.display = anyConfigured ? DisplayStyle.Flex : DisplayStyle.None;
        }

        #endregion

        #region Alert panel

        private void SetupAlertPanel()
        {
            if (ContainerAlert == null)
                return;

            ContainerAlert.Clear();

            if (!HasDetectableConfig)
            {
                // Custom configurator: skills-only alert (mirrors the old CustomConfigurator override).
                _alertPanel = new AlertPanel(
                    "Setup Required",
                    "Skills should be configured for AI agents to work properly:"
                );
                _alertPanel.AddItem("• Enable Auto-generate Skills below", "alert-frame-item-recommended");
                _alertPanel.SetButton("Enable Skills", EnableAutoGenerateSkills);
                ContainerAlert.Add(_alertPanel.Root);
                UpdateAlertPanel();
                return;
            }

            _alertPanel = new AlertPanel(
                "Setup Required",
                "At least one of the following must be configured:"
            );

            if (SupportsSkills)
            {
                _alertPanel.AddItem("• Skills (Recommended)", "alert-frame-item-recommended");
                _alertPanel.AddItem("• MCP Configuration");
                _alertPanel.SetButton("Enable Skills", EnableAutoGenerateSkills);
            }
            else
            {
                _alertPanel.AddItem("• MCP Configuration");
                _alertPanel.SetButton("Configure", ConfigureActiveTransport);
            }

            ContainerAlert.Add(_alertPanel.Root);

            _reconfigureAlertPanel = new AlertPanel(
                "Reconfiguration Required",
                "Connection settings have changed. The existing MCP configuration is outdated and needs to be updated."
            );
            _reconfigureAlertPanel.SetButton("Reconfigure", ReconfigureActiveTransport);
            ContainerAlert.Add(_reconfigureAlertPanel.Root);

            UpdateAlertPanel();
        }

        private void UpdateAlertPanel()
        {
            if (_alertPanel == null)
                return;

            if (!HasDetectableConfig)
            {
                var hasCustomSkills = SupportsSkills && UnityMcpPluginEditor.IsAutoGenerateSkills(AgentId);
                _alertPanel.SetVisible(!hasCustomSkills);
                if (ContainerAlert != null)
                    ContainerAlert.style.display = !hasCustomSkills ? DisplayStyle.Flex : DisplayStyle.None;
                return;
            }

            var settings = CurrentSettings();
            var isMcpConfigured = _configurator.IsDetected(settings);
            var hasSkills = SupportsSkills && UnityMcpPluginEditor.IsAutoGenerateSkills(AgentId);
            var isSetupComplete = isMcpConfigured || hasSkills;
            var needsReconfigure = _configurator.GetStatus(settings, ActiveTransport) == AgentConfig.ConfiguratorStatus.ReconfigureNeeded;

            _alertPanel.SetVisible(!isSetupComplete);
            _reconfigureAlertPanel?.SetVisible(needsReconfigure);

            if (ContainerAlert != null)
                ContainerAlert.style.display = (!isSetupComplete || needsReconfigure)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        private void ConfigureActiveTransport()
        {
            var config = GetConfig(CurrentSettings(), ActiveTransport);
            config.Configure();
            RefreshConfigurationUI();
        }

        private void ReconfigureActiveTransport()
        {
            var settings = CurrentSettings();
            var config = GetConfig(settings, ActiveTransport);
            if (config.IsDetected())
                config.Configure();
            RefreshConfigurationUI();
        }

        /// <summary>
        /// Rebuilds the transport containers (so the Configure/Remove status rows and any
        /// reconfigure-alert section re-evaluate against current on-disk state) and refreshes
        /// the alert panel. Called after any configure/remove/reconfigure action.
        /// </summary>
        private void RefreshConfigurationUI()
        {
            if (ContainerStdio != null) BuildTransportContainer(ContainerStdio, TransportMethod.stdio);
            if (ContainerHttp != null) BuildTransportContainer(ContainerHttp, TransportMethod.streamableHttp);
            SetTransportMethod(UnityMcpPluginEditor.TransportMethod);
            UpdateAlertPanel();
        }

        #endregion

        #region Skills UI

        private void SetupSkillsUI()
        {
            if (ContainerSkills == null)
                return;

            // The Custom configurator keeps its editable-path skills layout (parity with the
            // former CustomConfigurator.SetupSkillsUI override).
            if (!HasDetectableConfig)
            {
                SetupCustomSkillsUI();
                return;
            }

            var section = TemplateSkillsSection();
            var pathLabel = section.Q<Label>("labelSkillsPath");
            var toggleAutoGenerate = section.Q<Toggle>("toggleAutoGenerateSkills");
            var btnGenerate = section.Q<Button>("btnGenerateSkills");
            var unsupportedLabel = section.Q<Label>("labelSkillsUnsupported");

            if (!SupportsSkills)
            {
                pathLabel.parent.style.display = DisplayStyle.None;
                toggleAutoGenerate.parent.parent.style.display = DisplayStyle.None;
                unsupportedLabel.style.display = DisplayStyle.Flex;
                unsupportedLabel.SetEnabled(false);
                ContainerSkills.Add(section);
                return;
            }

            unsupportedLabel.style.display = DisplayStyle.None;

            var absoluteSkillsPath = ResolveAbsoluteSkillsPath(SkillsPath!);
            pathLabel.text = ToDisplayPath(absoluteSkillsPath);
            pathLabel.tooltip = absoluteSkillsPath;

            toggleAutoGenerate.SetValueWithoutNotify(UnityMcpPluginEditor.IsAutoGenerateSkills(AgentId));
            toggleAutoGenerate.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.SetAutoGenerateSkills(AgentId, evt.newValue);
                UnityMcpPluginEditor.SkillsPath = SkillsPath!;
                UnityMcpPluginEditor.Instance.Save();

                if (evt.newValue)
                    UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);

                UpdateAlertPanel();
            });

            btnGenerate.RegisterCallback<ClickEvent>(_ =>
            {
                UnityMcpPluginEditor.SkillsPath = SkillsPath!;
                UnityMcpPluginEditor.Instance.Save();
                UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
            });

            ContainerSkills.Add(section);
        }

        private void SetupCustomSkillsUI()
        {
            ContainerUnderHeader!.style.marginBottom = 4;

            var section = TemplateSkillsSection();
            var pathLabel = section.Q<Label>("labelSkillsPath");
            var toggleAutoGenerate = section.Q<Toggle>("toggleAutoGenerateSkills");
            var btnGenerate = section.Q<Button>("btnGenerateSkills");
            var unsupportedLabel = section.Q<Label>("labelSkillsUnsupported");

            unsupportedLabel.style.display = DisplayStyle.None;

            // Editable path field (custom agent's editable skills path), persisted through the
            // SkillsPath setter so the on-disk config stays portable, mirroring the shared
            // CustomConfigurator's EditableSkillsPath.
            var headerRow = pathLabel.parent;
            var inputPath = new TextField { value = UnityMcpPluginEditor.SkillsPath };
            inputPath.style.flexGrow = 1;
            inputPath.style.flexShrink = 1;
            inputPath.style.minWidth = 0;
            inputPath.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.SkillsPath = evt.newValue;
                UnityMcpPluginEditor.Instance.Save();

                var normalized = UnityMcpPluginEditor.SkillsPath;
                if (_configurator is AgentConfig.Impl.CustomConfigurator custom)
                    custom.EditableSkillsPath = normalized;
                if (normalized != evt.newValue)
                    inputPath.SetValueWithoutNotify(normalized);
            });
            headerRow.Remove(pathLabel);
            headerRow.Add(inputPath);

            toggleAutoGenerate.SetValueWithoutNotify(UnityMcpPluginEditor.IsAutoGenerateSkills(AgentId));
            toggleAutoGenerate.RegisterValueChangedCallback(evt =>
            {
                UnityMcpPluginEditor.SetAutoGenerateSkills(AgentId, evt.newValue);
                UnityMcpPluginEditor.Instance.Save();

                if (evt.newValue)
                    UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);

                UpdateAlertPanel();
            });

            btnGenerate.RegisterCallback<ClickEvent>(_ =>
            {
                UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
            });

            ContainerSkills!.Add(section);
        }

        private void EnableAutoGenerateSkills()
        {
            UnityMcpPluginEditor.SetAutoGenerateSkills(AgentId, true);
            UnityMcpPluginEditor.SkillsPath = SkillsPath!;
            UnityMcpPluginEditor.Instance.Save();

            var mcpPluginInstance = UnityMcpPluginEditor.Instance.McpPluginInstance;
            if (mcpPluginInstance != null)
                mcpPluginInstance.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);

            var toggleAutoGenerate = Root?.Q<Toggle>("toggleAutoGenerateSkills");
            toggleAutoGenerate?.SetValueWithoutNotify(true);

            RefreshConfigurationUI();
        }

        #endregion

        #region Transport switching

        /// <summary>
        /// Shows the container matching the active transport and hides the other, mirroring the
        /// former configurator's <c>SetTransportMethod</c>.
        /// </summary>
        public void SetTransportMethod(TransportMethod transportMethod)
        {
            if (ContainerStdio == null || ContainerHttp == null)
                return;

            ContainerStdio.style.display = transportMethod == TransportMethod.stdio
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            ContainerHttp.style.display = transportMethod != TransportMethod.stdio
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        #endregion
    }
}
