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
using R3;
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public class ConfigurationElements : IDisposable
    {
        public VisualElement Root { get; }
        public Label StatusText { get; }
        public Button BtnConfigure { get; }
        public Button BtnRemove { get; }

        private readonly Subject<bool> onConfigured = new();
        public Observable<bool> OnConfigured => onConfigured;

        private readonly AiAgentConfig _config;
        private readonly TransportMethod _transportMode;
        private readonly EventCallback<ClickEvent> _configureCallback;
        private readonly EventCallback<ClickEvent> _removeCallback;

        public ConfigurationElements(AiAgentConfig config, TransportMethod transportMode)
        {
            _config = config;
            _transportMode = transportMode;

            Root = new UITemplate<VisualElement>("Editor/UI/uxml/agents/elements/TemplateConfigureStatus.uxml").Value;
            StatusText = Root.Q<Label>("configureStatusText") ?? throw new NullReferenceException("Label 'configureStatusText' not found in UI.");
            BtnConfigure = Root.Q<Button>("btnConfigure") ?? throw new NullReferenceException("Button 'btnConfigure' not found in UI.");
            BtnRemove = Root.Q<Button>("btnRemoveConfig") ?? throw new NullReferenceException("Button 'btnRemoveConfig' not found in UI.");

            var pathLabel = Root.Q<Label>("labelConfigPath");
            if (pathLabel != null)
            {
                pathLabel.text = AiAgentConfigurator.ToDisplayPath(_config.ConfigPath);
                pathLabel.tooltip = _config.ConfigPath;
            }

            UpdateStatus();

            _configureCallback = new EventCallback<ClickEvent>(evt =>
            {
                var result = _config.Configure();
                UpdateStatus(result);
                onConfigured.OnNext(result);
            });
            BtnConfigure.RegisterCallback(_configureCallback);

            _removeCallback = new EventCallback<ClickEvent>(evt =>
            {
                _config.Unconfigure();
                UpdateStatus(false);
                onConfigured.OnNext(false);
            });
            BtnRemove.RegisterCallback(_removeCallback);
        }

        public void UpdateStatus(bool? isConfigured = null, bool? isAnyConfigured = null)
        {
            var isConfiguredValue = isConfigured ?? _config.IsConfigured();
            var showRemove = isAnyConfigured ?? isConfiguredValue;
            var transportText = _transportMode switch
            {
                TransportMethod.stdio => "stdio",
                TransportMethod.streamableHttp => "http",
                _ => "unknown"
            };

            StatusText.text = isConfiguredValue ? $"Configured ({transportText})" : "Not configured";

            BtnConfigure.text = isConfiguredValue ? "Reconfigure" : "Configure";
            BtnConfigure.EnableInClassList("btn-primary", !isConfiguredValue);
            BtnRemove.style.display = showRemove ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            BtnConfigure.UnregisterCallback(_configureCallback);
            BtnRemove.UnregisterCallback(_removeCallback);
            onConfigured.OnCompleted();
            onConfigured.Dispose();
        }
    }
}
