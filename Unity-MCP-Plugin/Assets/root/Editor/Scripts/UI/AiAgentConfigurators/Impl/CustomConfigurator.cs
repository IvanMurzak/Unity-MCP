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
    public class CustomConfigurator : AiAgentConfiguratorBase
    {
        public override string AgentName => "Other - Custom";
        public override string AgentId => "other-custom";
        public override string DownloadUrl => "NA";

        protected override string[] UxmlPaths => EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/agents/CustomConfig.uxml");
        protected override string? IconFileName => null;

        protected override void CreateConfigureStatusIndicator(VisualElement root)
        {
            // empty
        }

        protected override AiAgentConfig CreateConfigMacLinux()
        {
            throw new System.NotImplementedException();
        }

        protected override AiAgentConfig CreateConfigWindows()
        {
            throw new System.NotImplementedException();
        }

        protected override void OnUICreated(VisualElement root)
        {
            base.OnUICreated(root);

            // Provide raw json configuration
            // -----------------------------------------------------------------

            var toggleOptionStdio = root.Query<Toggle>("toggleOptionStdio").First();
            var toggleOptionHttp = root.Query<Toggle>("toggleOptionHttp").First();

            var containerStdio = root.Query<VisualElement>("containerStdio").First();
            var containerHttp = root.Query<VisualElement>("containerHttp").First();

            var rawJsonFieldStdio = root.Query<TextField>("rawJsonConfigurationStdio").First();
            var rawJsonFieldHttp = root.Query<TextField>("rawJsonConfigurationHttp").First();
            var dockerCommand = root.Query<TextField>("dockerCommand").First();

            rawJsonFieldStdio.value = Startup.Server.RawJsonConfigurationStdio(UnityMcpPlugin.Port, "mcpServers", UnityMcpPlugin.TimeoutMs).ToString();
            rawJsonFieldHttp.value = Startup.Server.RawJsonConfigurationHttp(UnityMcpPlugin.Host, "mcpServers").ToString();
            dockerCommand.value = Startup.Server.DockerRunCommand();

            void UpdateConfigurationVisibility(bool isStdioSelected)
            {
                containerStdio.style.display = isStdioSelected ? DisplayStyle.Flex : DisplayStyle.None;
                containerHttp.style.display = isStdioSelected ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Initialize with STDIO selected by default
            toggleOptionStdio.value = true;
            toggleOptionHttp.value = false;
            UpdateConfigurationVisibility(true);

            toggleOptionStdio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    toggleOptionHttp.SetValueWithoutNotify(false);
                    UpdateConfigurationVisibility(true);
                }
                else if (!toggleOptionHttp.value)
                {
                    // Prevent both toggles from being unchecked
                    toggleOptionStdio.SetValueWithoutNotify(true);
                }
            });

            toggleOptionHttp.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    toggleOptionStdio.SetValueWithoutNotify(false);
                    UpdateConfigurationVisibility(false);
                }
                else if (!toggleOptionStdio.value)
                {
                    // Prevent both toggles from being unchecked
                    toggleOptionHttp.SetValueWithoutNotify(true);
                }
            });
        }
    }
}
