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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
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

        private AiAgentConfig? _configStdio;
        private AiAgentConfig? _configHttp;

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
        protected VisualElement? ContainerHttp { get; private set; }
        protected VisualElement? ContainerStdio { get; private set; }
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
                }
                return _configHttp;
            }
        }

        /// <summary>
        /// Gets the Unity project root path (without /Assets suffix).
        /// </summary>
        protected static string ProjectRootPath => Application.dataPath.EndsWith("/Assets")
            ? Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length)
            : Application.dataPath;

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
            ContainerHttp = root.Q<VisualElement>("containerHttp") ?? throw new NullReferenceException("VisualElement 'containerHttp' not found in UI.");
            ContainerStdio = root.Q<VisualElement>("containerStdio") ?? throw new NullReferenceException("VisualElement 'containerStdio' not found in UI.");
            ToggleOptionHttp = root.Q<Toggle>("toggleOptionHttp") ?? throw new NullReferenceException("Toggle 'toggleOptionHttp' not found in UI.");
            ToggleOptionStdio = root.Q<Toggle>("toggleOptionStdio") ?? throw new NullReferenceException("Toggle 'toggleOptionStdio' not found in UI.");

            OnUICreated(root);
            McpWindowBase.EnableSmoothFoldoutTransitions(root);
            return root;
        }

        /// <summary>
        /// Called after the UI is created. Override to add custom behavior or bindings.
        /// </summary>
        /// <param name="root">The root visual element of the created UI.</param>
        protected virtual void OnUICreated(VisualElement root)
        {
            SetAgentIcon();
            SetAgentDownloadUrl(DownloadUrl);
            SetTutorialUrl(TutorialUrl);
            SetConfigureStatusIndicator();
        }

        protected virtual AiAgentConfigurator SetAgentDownloadUrl(string url)
        {
            ThrowIfRootNotSet();
            var downloadLink = Root!.Q<Label>("downloadLink");
            if (downloadLink != null)
                downloadLink.RegisterCallback<ClickEvent>(evt => Application.OpenURL(DownloadUrl));
            return this;
        }

        protected virtual AiAgentConfigurator SetTutorialUrl(string url, string label = "YouTube")
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
            if (icon != null)
            {
                agentIcon.style.backgroundImage = new StyleBackground(icon);
            }
            else
            {
                agentIcon.style.display = DisplayStyle.None;
            }
            return this;
        }

        protected virtual AiAgentConfigurator SetConfigureStatusIndicator()
        {
            ThrowIfRootNotSet();

            ContainerStdio!.Add(TemplateConfigurationElements(ConfigStdio, TransportMethod.stdio).Root);
            ContainerHttp!.Add(TemplateConfigurationElements(ConfigHttp, TransportMethod.streamableHttp).Root);

            return this;
        }

        #endregion

        #region Helpers

        protected void ThrowIfRootNotSet()
        {
            if (Root == null)
                throw new InvalidOperationException("Root visual element is not set. Ensure CreateUI has been called.");
        }

        #endregion
    }
}
