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
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Base class for AI agent configurator UI components.
    /// Each AI agent has its own configurator that provides specific configuration instructions.
    /// </summary>
    public abstract class AiAgentConfigurator
    {
        #region Properties

        protected AiAgentConfig? _configStdio;
        protected AiAgentConfig? _configHttp;
        protected ConfigurationElements? _configElementStdio;
        protected ConfigurationElements? _configElementHttp;
        protected IDisposable? _subscriptionStdio;
        protected IDisposable? _subscriptionHttp;
        protected AlertPanel? _alertPanel;
        protected AlertPanel? _reconfigureAlertPanel;

        /// <summary>
        /// The display name of the AI agent.
        /// </summary>
        public abstract string AgentName { get; }

        /// <summary>
        /// The unique identifier for this agent (used for dropdown values and PlayerPrefs).
        /// </summary>
        public abstract string AgentId { get; }

        /// <summary>
        /// The download URL for the AI agent.
        /// </summary>
        public abstract string DownloadUrl { get; }

        /// <summary>
        /// The relative (or absolute) path where skill files should be generated for this agent.
        /// Return null if the agent does not support skills.
        /// </summary>
        public virtual string? SkillsPath => null;

        /// <summary>
        /// Whether this agent supports skill file generation.
        /// </summary>
        public bool SupportsSkills => SkillsPath != null;

        /// <summary>
        /// The tutorial URL for configuring the AI agent.
        /// </summary>
        public virtual string TutorialUrl => string.Empty;

        /// <summary>
        /// Gets the icon file name for this agent (e.g., "claude-64.png").
        /// Return null if no icon should be displayed.
        /// </summary>
        protected abstract string? IconFileName { get; }

        protected VisualElement? Root { get; private set; }
        protected VisualElement? ContainerUnderHeader { get; private set; }
        protected VisualElement? ContainerAlert { get; private set; }
        protected VisualElement? ContainerHttp { get; private set; }
        protected VisualElement? ContainerStdio { get; private set; }
        protected VisualElement? ContainerSkills { get; private set; }
        protected Toggle? ToggleOptionHttp { get; private set; }
        protected Toggle? ToggleOptionStdio { get; private set; }
        /// <summary>
        /// Gets the icon paths for this agent.
        /// </summary>
        protected string[]? IconPaths => IconFileName != null
            ? EditorAssetLoader.GetEditorAssetPaths($"Editor/Gizmos/ai-agents/{IconFileName}")
            : null;

        /// <summary>
        /// Gets the agent configuration for the current platform.
        /// </summary>
        public AiAgentConfig ConfigStdio
        {
            get
            {
                if (_configStdio == null)
                {
#if UNITY_EDITOR_WIN
                    _configStdio = CreateConfigStdioWindows();
#else
                    _configStdio = CreateConfigStdioMacLinux();
#endif
                    ApplyStdioAuthorizationConfig(_configStdio);
                }
                return _configStdio;
            }
        }

        /// <summary>
        /// Gets the agent configuration for the current platform.
        /// </summary>
        public AiAgentConfig ConfigHttp
        {
            get
            {
                if (_configHttp == null)
                {
#if UNITY_EDITOR_WIN
                    _configHttp = CreateConfigHttpWindows();
#else
                    _configHttp = CreateConfigHttpMacLinux();
#endif
                    ApplyHttpAuthorizationConfig(_configHttp);
                }
                return _configHttp;
            }
        }

        /// <summary>
        /// Gets the Unity project root path (without /Assets suffix).
        /// </summary>
        protected static string ProjectRootPath
        {
            get
            {
                var dataPath = Application.dataPath.Replace('\\', Path.DirectorySeparatorChar)
                                                   .Replace('/', Path.DirectorySeparatorChar);
                var suffix = $"{Path.DirectorySeparatorChar}Assets";
                return dataPath.EndsWith(suffix)
                    ? dataPath.Substring(0, dataPath.Length - suffix.Length)
                    : dataPath;
            }
        }

        /// <summary>
        /// Returns a shortened display path relative to the project root.
        /// </summary>
        public static string ToDisplayPath(string fullPath)
        {
            var root = ProjectRootPath.Replace('\\', '/').TrimEnd('/') + '/';
            var normalized = fullPath.Replace('\\', '/');
            return normalized.StartsWith(root) ? normalized[root.Length..] : normalized;
        }

        #endregion

        #region Abstract

        /// <summary>
        /// Creates the AI agent STDIO configuration for Windows platform.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigStdioWindows();

        /// <summary>
        /// Creates the AI agent STDIO configuration for Mac and Linux platforms.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigStdioMacLinux();

        /// <summary>
        /// Creates the AI agent HTTP configuration for Windows platform.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigHttpWindows();

        /// <summary>
        /// Creates the AI agent HTTP configuration for Mac and Linux platforms.
        /// </summary>
        protected abstract AiAgentConfig CreateConfigHttpMacLinux();

        #endregion

        #region UI Templates

        protected Label TemplateLabelDescription(string? text = null)
        {
            var result = new UITemplate<Label>("Editor/UI/uxml/agents/elements/TemplateLabelDescription.uxml").Value;
            if (text != null)
                result.text = text;
            return result;
        }
        protected Label TemplateWarningLabel(string? text = null)
        {
            var result = new UITemplate<Label>("Editor/UI/uxml/agents/elements/TemplateWarningLabel.uxml").Value;
            if (text != null)
                result.text = text;
            return result;
        }
        protected Label TemplateAlertLabel(string? text = null)
        {
            var result = new UITemplate<Label>("Editor/UI/uxml/agents/elements/TemplateAlertLabel.uxml").Value;
            if (text != null)
                result.text = text;
            return result;
        }
        protected TextField TemplateTextFieldReadOnly(string? value = null)
        {
            var result = new UITemplate<TextField>("Editor/UI/uxml/agents/elements/TemplateTextFieldReadOnly.uxml").Value;
            if (value != null)
                result.value = value;
            return result;
        }
        protected Foldout TemplateFoldoutFirst(string? text = null)
        {
            var result = new UITemplate<Foldout>("Editor/UI/uxml/agents/elements/TemplateFoldoutFirst.uxml").Value;
            if (text != null)
                result.text = text;
            return result;
        }
        protected Foldout TemplateFoldout(string? text = null)
        {
            var result = new UITemplate<Foldout>("Editor/UI/uxml/agents/elements/TemplateFoldout.uxml").Value;
            if (text != null)
                result.text = text;
            return result;
        }
        protected ConfigurationElements TemplateConfigurationElements(AiAgentConfig config, TransportMethod transportMode) => new ConfigurationElements(config, transportMode);

        #endregion

        #region UI Creation

        /// <summary>
        /// Creates and returns the visual element containing the configuration UI for this client.
        /// </summary>
        /// <param name="container">The parent container where the UI will be added.</param>
        /// <returns>The created visual element, or null if the template couldn't be loaded.</returns>
        public virtual VisualElement? CreateUI(VisualElement container)
        {
            var root = new UITemplate<VisualElement>("Editor/UI/uxml/agents/AiAgentTemplateConfig.uxml").Value;

            Root = root;
            ContainerUnderHeader = root.Q<VisualElement>("containerUnderHeader") ?? throw new NullReferenceException("VisualElement 'containerUnderHeader' not found in UI.");
            ContainerAlert = root.Q<VisualElement>("containerAlert") ?? throw new NullReferenceException("VisualElement 'containerAlert' not found in UI.");
            ContainerHttp = root.Q<VisualElement>("containerHttp") ?? throw new NullReferenceException("VisualElement 'containerHttp' not found in UI.");
            ContainerStdio = root.Q<VisualElement>("containerStdio") ?? throw new NullReferenceException("VisualElement 'containerStdio' not found in UI.");
            ContainerSkills = root.Q<VisualElement>("containerSkills") ?? throw new NullReferenceException("VisualElement 'containerSkills' not found in UI.");

            OnUICreated(root);
            SetupSkillsUI();
            SetupAlertPanel();
            McpWindowBase.EnableSmoothFoldoutTransitions(root);
            return root;
        }

        /// <summary>
        /// Called after the UI is created. Override to add custom behavior or bindings.
        /// </summary>
        /// <param name="root">The root visual element of the created UI.</param>
        protected virtual void OnUICreated(VisualElement root)
        {
            SetAgentName(AgentName);
            SetAgentIcon();
            SetAgentDownloadUrl(DownloadUrl);
            SetTutorialUrl(TutorialUrl);
            SetConfigureStatusIndicator();
            SetTransportMethod(UnityMcpPluginEditor.TransportMethod);
        }

        protected virtual AiAgentConfigurator SetAgentName(string name)
        {
            ThrowIfRootNotSet();
            var nameLabel = Root!.Q<Label>("agentName") ?? throw new NullReferenceException("Label 'agentName' not found in UI.");
            nameLabel.text = name;
            return this;
        }

        protected virtual AiAgentConfigurator SetAgentDownloadUrl(string url)
        {
            ThrowIfRootNotSet();
            var downloadLink = Root!.Q<Label>("downloadLink");
            if (downloadLink != null)
                downloadLink.RegisterCallback<ClickEvent>(evt => Application.OpenURL(DownloadUrl));
            return this;
        }

        protected virtual AiAgentConfigurator DisableLinksContainer()
        {
            ThrowIfRootNotSet();
            var linksContainer = Root!.Q<VisualElement>("linksContainer");
            if (linksContainer != null)
                linksContainer.style.display = DisplayStyle.None;
            return this;
        }

        protected virtual AiAgentConfigurator SetTutorialUrl(string url, string label = "YouTube Tutorial")
        {
            ThrowIfRootNotSet();
            var tutorialLink = Root!.Q<Label>("tutorialLink");
            if (tutorialLink != null)
            {
                tutorialLink.text = label;

                if (TutorialUrl == string.Empty)
                {
                    tutorialLink.style.display = DisplayStyle.None;
                    var tutorialSeparator = Root!.Q<Label>("tutorialSeparator");
                    if (tutorialSeparator != null)
                        tutorialSeparator.style.display = DisplayStyle.None;
                }
                else
                {
                    tutorialLink.RegisterCallback<ClickEvent>(evt => Application.OpenURL(TutorialUrl));
                }
            }
            return this;
        }

        /// <summary>
        /// Sets the agent icon on the agentIcon element.
        /// </summary>
        /// <param name="root">The root visual element containing the agentIcon element.</param>
        protected virtual AiAgentConfigurator SetAgentIcon()
        {
            ThrowIfRootNotSet();
            var agentIcon = Root!.Q<VisualElement>("agentIcon") ?? throw new NullReferenceException("VisualElement 'agentIcon' not found in UI.");

            if (IconPaths == null)
            {
                agentIcon.style.display = DisplayStyle.None;
                return this;
            }

            var icon = EditorAssetLoader.LoadAssetAtPath<Texture2D>(IconPaths);

            agentIcon.style.backgroundImage = icon == null ? null : new StyleBackground(icon);
            agentIcon.style.display = icon == null ? DisplayStyle.None : DisplayStyle.Flex;
            return this;
        }

        protected virtual AiAgentConfigurator SetConfigureStatusIndicator()
        {
            ThrowIfRootNotSet();
            DisposeConfigurationElements();

            _configElementStdio = TemplateConfigurationElements(ConfigStdio, TransportMethod.stdio);
            _configElementHttp = TemplateConfigurationElements(ConfigHttp, TransportMethod.streamableHttp);

            _subscriptionStdio = _configElementStdio.OnConfigured.Subscribe(_ => RefreshConfigurationUI());
            _subscriptionHttp = _configElementHttp.OnConfigured.Subscribe(_ => RefreshConfigurationUI());

            ContainerStdio!.Add(_configElementStdio.Root);
            ContainerHttp!.Add(_configElementHttp.Root);

            // Initial status so Remove buttons reflect any config across both transports
            RefreshConfigurationUI();

            return this;
        }

        protected virtual void DisposeConfigurationElements()
        {
            _subscriptionStdio?.Dispose();
            _subscriptionHttp?.Dispose();
            _configElementStdio?.Dispose();
            _configElementHttp?.Dispose();

            _subscriptionStdio = null;
            _subscriptionHttp = null;
            _configElementStdio = null;
            _configElementHttp = null;
        }

        public virtual AiAgentConfigurator SetTransportMethod(TransportMethod transportMethod)
        {
            ThrowIfRootNotSet();

            ContainerStdio!.style.display = transportMethod == TransportMethod.stdio
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            ContainerHttp!.style.display = transportMethod != TransportMethod.stdio
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            return this;
        }

        protected VisualElement TemplateSkillsSection() =>
            new UITemplate<VisualElement>("Editor/UI/uxml/agents/elements/TemplateSkillsSection.uxml").Value;

        /// <summary>
        /// Builds the setup alert panel inside <see cref="ContainerAlert"/>.
        /// Shows a warning when neither skills nor MCP configuration is set up.
        /// Also adds a reconfiguration alert shown when a config exists but is outdated.
        /// Override in subclasses to suppress (e.g. Custom configurator).
        /// </summary>
        protected virtual void SetupAlertPanel()
        {
            if (ContainerAlert == null)
                return;

            ContainerAlert.Clear();

            _alertPanel = new AlertPanel(
                "Setup Required",
                "At least one of the following must be configured:"
            );

            if (SupportsSkills)
            {
                _alertPanel.AddItem("\u2022 Skills (Recommended)", "alert-frame-item-recommended");
                _alertPanel.AddItem("\u2022 MCP Configuration");
                _alertPanel.SetButton("Enable Skills", EnableAutoGenerateSkills);
            }
            else
            {
                _alertPanel.AddItem("\u2022 MCP Configuration");
                _alertPanel.SetButton("Configure", ConfigureActiveTransport);
            }

            ContainerAlert.Add(_alertPanel.Root);

            _reconfigureAlertPanel = new AlertPanel(
                "Reconfiguration Required",
                "Connection settings have changed. The existing MCP configuration is outdated and needs to be updated."
            );
            _reconfigureAlertPanel.SetButton("Reconfigure", ReconfigureDetectedConfigs);

            ContainerAlert.Add(_reconfigureAlertPanel.Root);

            UpdateAlertPanel();
        }

        /// <summary>
        /// Updates the visibility of the alert panel based on current configuration state.
        /// </summary>
        protected virtual void UpdateAlertPanel()
        {
            if (_alertPanel == null)
                return;

            var isMcpConfigured = ConfigStdio.IsDetected() || ConfigHttp.IsDetected();
            var hasSkills = SupportsSkills && UnityMcpPluginEditor.IsAutoGenerateSkills(AgentId);
            var isSetupComplete = isMcpConfigured || hasSkills;
            var needsReconfigure = IsReconfigureNeeded();

            _alertPanel.SetVisible(!isSetupComplete);
            _reconfigureAlertPanel?.SetVisible(needsReconfigure);

            if (ContainerAlert != null)
                ContainerAlert.style.display = (!isSetupComplete || needsReconfigure)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        /// <summary>
        /// Returns true when a detected MCP config exists but no longer matches the current settings.
        /// Only checks the transport config that matches the current transport method, because
        /// both configs share the same server name — a correctly configured HTTP entry would
        /// cause the STDIO check to false-positive (and vice versa).
        /// </summary>
        private bool IsReconfigureNeeded()
        {
            var isStdio = UnityMcpPluginEditor.TransportMethod == TransportMethod.stdio;

            var activeConfig = isStdio ? ConfigStdio : ConfigHttp;

            return activeConfig.IsDetected() && !activeConfig.IsConfigured();
        }

        /// <summary>
        /// Reconfigures the active transport MCP config by writing the current settings to its config file.
        /// Called when the user clicks the "Reconfigure" button on the reconfigure alert.
        /// </summary>
        protected virtual void ReconfigureDetectedConfigs()
        {
            var isStdio = UnityMcpPluginEditor.TransportMethod == TransportMethod.stdio;
            var activeConfig = isStdio ? ConfigStdio : ConfigHttp;

            if (activeConfig.IsDetected()) activeConfig.Configure();
            RefreshConfigurationUI();
        }

        /// <summary>
        /// Configures the MCP entry for the currently active transport method.
        /// Called from the "Configure" button on the setup alert when skills are not supported.
        /// </summary>
        protected void ConfigureActiveTransport()
        {
            var isStdio = UnityMcpPluginEditor.TransportMethod == TransportMethod.stdio;
            var activeConfig = isStdio ? ConfigStdio : ConfigHttp;
            activeConfig.Configure();
            RefreshConfigurationUI();
        }

        /// <summary>
        /// Enables auto-generate skills for this agent, generates skill files, and refreshes the UI.
        /// Called from the "Enable Skills" button on the setup alert.
        /// </summary>
        protected void EnableAutoGenerateSkills()
        {
            UnityMcpPluginEditor.SetAutoGenerateSkills(AgentId, true);
            UnityMcpPluginEditor.SkillsPath = SkillsPath!;
            UnityMcpPluginEditor.Instance.Save();

            var mcpPluginInstance = UnityMcpPluginEditor.Instance.McpPluginInstance;
            if (mcpPluginInstance != null)
                mcpPluginInstance.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);

            // Refresh the skills toggle checkbox in the UI
            var toggleAutoGenerate = Root?.Q<Toggle>("toggleAutoGenerateSkills");
            toggleAutoGenerate?.SetValueWithoutNotify(true);

            RefreshConfigurationUI();
        }

        /// <summary>
        /// Refreshes all configuration-related UI: alert panel visibility and
        /// configure button status indicators for both transport modes.
        /// Called after any configuration change (alert Reconfigure, small Configure, or Remove).
        /// </summary>
        protected void RefreshConfigurationUI()
        {
            var anyConfigured = ConfigStdio.IsDetected() || ConfigHttp.IsDetected();
            _configElementStdio?.UpdateStatus(isAnyConfigured: anyConfigured);
            _configElementHttp?.UpdateStatus(isAnyConfigured: anyConfigured);
            UpdateAlertPanel();
        }

        /// <summary>
        /// Builds the skills UI inside <see cref="ContainerSkills"/>.
        /// Override in subclasses that need a custom layout (e.g. editable path field).
        /// </summary>
        protected virtual void SetupSkillsUI()
        {
            if (ContainerSkills == null)
                return;

            var section = TemplateSkillsSection();
            var pathLabel = section.Q<Label>("labelSkillsPath");
            var toggleAutoGenerate = section.Q<Toggle>("toggleAutoGenerateSkills");
            var btnGenerate = section.Q<Button>("btnGenerateSkills");
            var unsupportedLabel = section.Q<Label>("labelSkillsUnsupported");

            if (!SupportsSkills)
            {
                // Hide the normal controls, show the unsupported label
                pathLabel.parent.style.display = DisplayStyle.None;
                toggleAutoGenerate.parent.parent.style.display = DisplayStyle.None;
                unsupportedLabel.style.display = DisplayStyle.Flex;
                unsupportedLabel.SetEnabled(false);
                ContainerSkills.Add(section);
                return;
            }

            // Hide the unsupported label
            unsupportedLabel.style.display = DisplayStyle.None;

            // Show the skills output path
            pathLabel.text = ToDisplayPath(SkillsPath!);
            pathLabel.tooltip = SkillsPath;

            // Configure toggle (per-agent)
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

            // Configure generate button
            btnGenerate.RegisterCallback<ClickEvent>(evt =>
            {
                UnityMcpPluginEditor.SkillsPath = SkillsPath!;
                UnityMcpPluginEditor.Instance.Save();
                UnityMcpPluginEditor.Instance.McpPluginInstance!.GenerateSkillFiles(UnityMcpPluginEditor.ProjectRootPath);
            });

            ContainerSkills.Add(section);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Invalidates cached configurations so they are recreated on next access.
        /// Call this when deployment mode or token changes.
        /// </summary>
        public virtual void Invalidate()
        {
            _configStdio = null;
            _configHttp = null;
        }

        /// <summary>
        /// Applies authorization to the STDIO config: injects the token into args when required,
        /// removes it otherwise, and marks HTTP headers for removal.
        /// Delegates to the config's own format-specific implementation.
        /// </summary>
        protected virtual void ApplyStdioAuthorizationConfig(AiAgentConfig config)
        {
            var isRequired = UnityMcpPluginEditor.AuthOption == AuthOption.required;
            config.ApplyStdioAuthorization(isRequired, UnityMcpPluginEditor.Token);
        }

        /// <summary>
        /// Injects MCP authorization into the HTTP config when remote deployment is active.
        /// Delegates to the config's own format-specific implementation.
        /// Only applies when transport is HTTP — no-op for stdio transport.
        /// </summary>
        protected virtual void ApplyHttpAuthorizationConfig(AiAgentConfig config)
        {
            // In Cloud mode, authorization is always required (cloud server enforces it).
            // In Local mode, it depends on the user's AuthOption setting.
            var isCloud = UnityMcpPluginEditor.ConnectionMode == ConnectionMode.Cloud;
            var isRequired = isCloud || UnityMcpPluginEditor.AuthOption == AuthOption.required;
            config.ApplyHttpAuthorization(isRequired, UnityMcpPluginEditor.Token);
        }

        protected void ThrowIfRootNotSet()
        {
            if (Root == null)
                throw new InvalidOperationException("Root visual element is not set. Ensure CreateUI has been called.");
        }

        #endregion
    }
}
