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
using UnityEngine.UIElements;
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    public class ConfigurationElements
    {
        public VisualElement Root { get; }
        public VisualElement StatusCircle { get; }
        public Label StatusText { get; }
        public Button BtnConfigure { get; }

        public ConfigurationElements(AiAgentConfig config, TransportMethod transportMode)
        {
            Root = new UITemplate<VisualElement>("Editor/UI/uxml/agents/elements/TemplateConfigureStatus.uxml").Value;
            StatusCircle = Root.Q<VisualElement>("configureStatusCircle") ?? throw new NullReferenceException("VisualElement 'configureStatusCircle' not found in UI.");
            StatusText = Root.Q<Label>("configureStatusText") ?? throw new NullReferenceException("Label 'configureStatusText' not found in UI.");
            BtnConfigure = Root.Q<Button>("btnConfigure") ?? throw new NullReferenceException("Button 'btnConfigure' not found in UI.");

            var isConfiguredResult = config.IsConfigured();
            var transportText = transportMode switch
            {
                TransportMethod.stdio => "stdio",
                TransportMethod.streamableHttp => "http",
                _ => "unknown"
            };

            StatusCircle.RemoveFromClassList(MainWindowEditor.USS_Connected);
            StatusCircle.RemoveFromClassList(MainWindowEditor.USS_Connecting);
            StatusCircle.RemoveFromClassList(MainWindowEditor.USS_Disconnected);

            StatusCircle.AddToClassList(isConfiguredResult
                ? MainWindowEditor.USS_Connected
                : MainWindowEditor.USS_Disconnected);
            StatusText.text = isConfiguredResult ? $"Configured ({transportText})" : "Not Configured";
            BtnConfigure.text = isConfiguredResult ? "Reconfigure" : "Configure";

            BtnConfigure.RegisterCallback<ClickEvent>(evt =>
            {
                var configureResult = config.Configure();

                StatusText.text = configureResult ? $"Configured ({transportText})" : "Not Configured";

                StatusCircle.RemoveFromClassList(MainWindowEditor.USS_Connected);
                StatusCircle.RemoveFromClassList(MainWindowEditor.USS_Connecting);
                StatusCircle.RemoveFromClassList(MainWindowEditor.USS_Disconnected);
                StatusCircle.AddToClassList(configureResult
                    ? MainWindowEditor.USS_Connected
                    : MainWindowEditor.USS_Disconnected);

                BtnConfigure.text = configureResult ? "Reconfigure" : "Configure";
            });
        }
    }
}