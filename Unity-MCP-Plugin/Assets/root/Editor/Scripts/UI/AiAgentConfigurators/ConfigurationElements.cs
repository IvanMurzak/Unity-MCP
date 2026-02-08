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
        public VisualElement StatusCircle { get; }
        public Label StatusText { get; }
        public Button BtnConfigure { get; }

        private readonly Subject<bool> onConfigured = new();
        public Observable<bool> OnConfigured => onConfigured;

        private readonly AiAgentConfig _config;
        private readonly TransportMethod _transportMode;
        private readonly EventCallback<ClickEvent> _clickCallback;

        public ConfigurationElements(AiAgentConfig config, TransportMethod transportMode)
        {
            _config = config;
            _transportMode = transportMode;

            Root = new UITemplate<VisualElement>("Editor/UI/uxml/agents/elements/TemplateConfigureStatus.uxml").Value;
            StatusCircle = Root.Q<VisualElement>("configureStatusCircle") ?? throw new NullReferenceException("VisualElement 'configureStatusCircle' not found in UI.");
            StatusText = Root.Q<Label>("configureStatusText") ?? throw new NullReferenceException("Label 'configureStatusText' not found in UI.");
            BtnConfigure = Root.Q<Button>("btnConfigure") ?? throw new NullReferenceException("Button 'btnConfigure' not found in UI.");

            UpdateStatus();

            _clickCallback = new EventCallback<ClickEvent>(evt =>
            {
                var result = _config.Configure();
                UpdateStatus(result);
                onConfigured.OnNext(result);
            });
            BtnConfigure.RegisterCallback(_clickCallback);
        }

        public void UpdateStatus(bool? isConfigured = null)
        {
            var isConfiguredValue = isConfigured ?? _config.IsConfigured();
            var transportText = _transportMode switch
            {
                TransportMethod.stdio => "stdio",
                TransportMethod.streamableHttp => "http",
                _ => "unknown"
            };

            StatusText.text = isConfiguredValue ? $"Configured ({transportText})" : "Not Configured";

            StatusCircle.RemoveFromClassList(MainWindowEditor.USS_Connected);
            StatusCircle.RemoveFromClassList(MainWindowEditor.USS_Connecting);
            StatusCircle.RemoveFromClassList(MainWindowEditor.USS_Disconnected);
            StatusCircle.AddToClassList(isConfiguredValue
                ? MainWindowEditor.USS_Connected
                : MainWindowEditor.USS_Disconnected);

            BtnConfigure.text = isConfiguredValue ? "Reconfigure" : "Configure";
        }

        public void Dispose()
        {
            BtnConfigure.UnregisterCallback(_clickCallback);
            onConfigured.OnCompleted();
            onConfigured.Dispose();
        }
    }
}