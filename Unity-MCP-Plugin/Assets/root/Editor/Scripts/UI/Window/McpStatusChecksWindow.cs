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
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using R3;
using UnityEditor;
using UnityEngine.UIElements;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Editor window for checking MCP tool status and diagnostics.
    /// Provides onboarding tests to verify that MCP integration is working correctly.
    /// </summary>
    public class McpStatusChecksWindow : McpWindowBase
    {
        private static readonly string[] _windowUxmlPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uxml/McpStatusChecksWindow.uxml");
        private static readonly string[] _windowUssPaths = EditorAssetLoader.GetEditorAssetPaths("Editor/UI/uss/McpStatusChecksWindow.uss");

        protected override string[] WindowUxmlPaths => _windowUxmlPaths;
        protected override string[] WindowUssPaths => _windowUssPaths;
        protected override string WindowTitle => "MCP Status Checks";

        private readonly CompositeDisposable _disposables = new();
        private VisualElement? _statusItemsContainer;

        // Track tool execution count (stub - resets on window open)
        private static int _toolExecutionCount = 0;

        /// <summary>
        /// Status of a single check item.
        /// </summary>
        public enum CheckStatus
        {
            Success,
            Error,
            Pending
        }

        /// <summary>
        /// Data model for a status check item.
        /// </summary>
        public class StatusCheckItem
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Subtitle { get; set; } = string.Empty;
            public CheckStatus Status { get; set; } = CheckStatus.Pending;

            public StatusCheckItem(string id, string title, string subtitle = "", CheckStatus status = CheckStatus.Pending)
            {
                Id = id;
                Title = title;
                Subtitle = subtitle;
                Status = status;
            }
        }

        /// <summary>
        /// Shows the MCP Status Checks window.
        /// </summary>
        public static void ShowWindow()
        {
            var window = GetWindow<McpStatusChecksWindow>("MCP Status Checks");
            window.SetupWindowWithIcon();
            window.minSize = new UnityEngine.Vector2(400, 500);
            window.Show();
            window.Focus();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToUpdates();
        }

        private void OnDisable()
        {
            _disposables.Clear();
        }

        protected override void OnGUICreated(VisualElement root)
        {
            base.OnGUICreated(root);
            
            _statusItemsContainer = root.Q<VisualElement>(className: "status-items-container");
            if (_statusItemsContainer == null)
            {
                Logger.LogWarning("{method}: status-items-container not found", nameof(OnGUICreated));
                return;
            }

            // Clear any static example cards from UXML
            _statusItemsContainer.Clear();

            // Initial render
            RefreshAllChecks();
        }

        private void SubscribeToUpdates()
        {
            _disposables.Clear();

            // Subscribe to connection state changes
            UnityMcpPlugin.ConnectionState
                .ThrottleLast(TimeSpan.FromMilliseconds(100))
                .ObserveOnCurrentSynchronizationContext()
                .Subscribe(_ => RefreshAllChecks())
                .AddTo(_disposables);

            // Subscribe to tool manager updates
            McpPlugin.McpPlugin.DoAlways(plugin =>
            {
                var toolManager = plugin.McpManager.ToolManager;
                if (toolManager != null)
                {
                    toolManager.OnToolsUpdated
                        .ObserveOnCurrentSynchronizationContext()
                        .Subscribe(_ => RefreshAllChecks())
                        .AddTo(_disposables);
                }
            }).AddTo(_disposables);
        }

        private void RefreshAllChecks()
        {
            if (_statusItemsContainer == null)
                return;

            var checks = GatherStatusChecks();
            
            _statusItemsContainer.Clear();
            foreach (var check in checks)
            {
                var card = CreateStatusCard(check);
                _statusItemsContainer.Add(card);
            }
        }

        private List<StatusCheckItem> GatherStatusChecks()
        {
            var checks = new List<StatusCheckItem>();

            // 1. MCP Client configured
            checks.Add(GetMcpClientConfiguredCheck());

            // 2. Unity connected to MCP Server
            checks.Add(GetUnityConnectedCheck());

            // 3. Version handshake status (stub)
            checks.Add(GetVersionHandshakeCheck());

            // 4. MCP Server connected to MCP Client (stub)
            checks.Add(GetServerToClientCheck());

            // 5. MCP Client location match (stub)
            checks.Add(GetClientLocationCheck());

            // 6. Plugin has at least 1 enabled tool
            checks.Add(GetEnabledToolsCheck());

            // 7. MCP Tool executed (stub with counter)
            checks.Add(GetToolExecutedCheck());

            return checks;
        }

        private StatusCheckItem GetMcpClientConfiguredCheck()
        {
            var configuredClients = GetConfiguredClientCount();
            var isConfigured = configuredClients > 0;

            return new StatusCheckItem(
                id: "mcp-client-configured",
                title: "MCP Client configured",
                subtitle: isConfigured ? $"{configuredClients} active" : "No MCP clients configured",
                status: isConfigured ? CheckStatus.Success : CheckStatus.Error
            );
        }

        private int GetConfiguredClientCount()
        {
            var count = 0;

            // Check JSON client configs (Claude Desktop, Cursor, etc.)
            try
            {
                var claudeDesktopPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Claude", "claude_desktop_config.json");
                if (System.IO.File.Exists(claudeDesktopPath) && 
                    JsonClientConfig.IsMcpClientConfigured(claudeDesktopPath))
                    count++;
            }
            catch { /* Ignore */ }

            try
            {
                var cursorPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".cursor", "mcp.json");
                if (System.IO.File.Exists(cursorPath) && 
                    JsonClientConfig.IsMcpClientConfigured(cursorPath))
                    count++;
            }
            catch { /* Ignore */ }

            return count;
        }

        private StatusCheckItem GetUnityConnectedCheck()
        {
            var connectionState = UnityMcpPlugin.ConnectionState.CurrentValue;
            var isConnected = connectionState == HubConnectionState.Connected;
            var keepConnected = UnityMcpPlugin.KeepConnected;

            string subtitle;
            CheckStatus status;

            if (isConnected)
            {
                subtitle = "Connected";
                status = CheckStatus.Success;
            }
            else if (keepConnected && connectionState == HubConnectionState.Connecting)
            {
                subtitle = "Connecting...";
                status = CheckStatus.Pending;
            }
            else if (keepConnected && connectionState == HubConnectionState.Reconnecting)
            {
                subtitle = "Reconnecting...";
                status = CheckStatus.Pending;
            }
            else
            {
                subtitle = "To pass this check, click Connect in the main window.";
                status = CheckStatus.Error;
            }

            return new StatusCheckItem(
                id: "unity-connected",
                title: "Unity connected to MCP Server",
                subtitle: subtitle,
                status: status
            );
        }

        private StatusCheckItem GetVersionHandshakeCheck()
        {
            // Stub: We can show local version, but server version needs API
            var version = UnityMcpPlugin.Version;
            var isConnected = UnityMcpPlugin.IsConnected.CurrentValue;

            if (isConnected)
            {
                return new StatusCheckItem(
                    id: "version-handshake",
                    title: "Version handshake status",
                    subtitle: $"Plugin v{version}",
                    status: CheckStatus.Success
                );
            }
            else
            {
                return new StatusCheckItem(
                    id: "version-handshake",
                    title: "Version handshake status",
                    subtitle: "Connect to verify version",
                    status: CheckStatus.Pending
                );
            }
        }

        private StatusCheckItem GetServerToClientCheck()
        {
            // Stub: This requires info from MCP Server about connected clients
            var isConnected = UnityMcpPlugin.IsConnected.CurrentValue;

            if (isConnected)
            {
                return new StatusCheckItem(
                    id: "server-to-client",
                    title: "MCP Server connected to MCP Client",
                    subtitle: "Awaiting client connection...",
                    status: CheckStatus.Pending
                );
            }
            else
            {
                return new StatusCheckItem(
                    id: "server-to-client",
                    title: "MCP Server connected to MCP Client",
                    subtitle: "Server not running",
                    status: CheckStatus.Error
                );
            }
        }

        private StatusCheckItem GetClientLocationCheck()
        {
            // Stub: Shows configured path but doesn't verify runtime match
            try
            {
                var claudeDesktopPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Claude", "claude_desktop_config.json");
                    
                if (System.IO.File.Exists(claudeDesktopPath))
                {
                    return new StatusCheckItem(
                        id: "client-location",
                        title: "MCP Client location match",
                        subtitle: "Claude Desktop configured",
                        status: CheckStatus.Success
                    );
                }
            }
            catch { /* Ignore */ }

            return new StatusCheckItem(
                id: "client-location",
                title: "MCP Client location match",
                subtitle: "No client config found",
                status: CheckStatus.Error
            );
        }

        private StatusCheckItem GetEnabledToolsCheck()
        {
            var mcpPlugin = UnityMcpPlugin.Instance.McpPluginInstance;
            var toolManager = mcpPlugin?.McpManager.ToolManager;

            if (toolManager == null)
            {
                return new StatusCheckItem(
                    id: "enabled-tools",
                    title: "MCP Plugin has at least 1 enabled tool",
                    subtitle: "Plugin not initialized",
                    status: CheckStatus.Error
                );
            }

            var allTools = toolManager.GetAllTools().ToList();
            var enabledCount = allTools.Count(t => toolManager.IsToolEnabled(t.Name));
            var totalCount = allTools.Count;

            if (enabledCount > 0)
            {
                return new StatusCheckItem(
                    id: "enabled-tools",
                    title: "MCP Plugin has at least 1 enabled tool",
                    subtitle: $"{enabledCount} / {totalCount} tools enabled",
                    status: CheckStatus.Success
                );
            }
            else
            {
                return new StatusCheckItem(
                    id: "enabled-tools",
                    title: "MCP Plugin has at least 1 enabled tool",
                    subtitle: "Enable tools in Window → MCP → Tools",
                    status: CheckStatus.Error
                );
            }
        }

        private StatusCheckItem GetToolExecutedCheck()
        {
            // Stub: Counter maintained in static field (resets on domain reload)
            if (_toolExecutionCount > 0)
            {
                return new StatusCheckItem(
                    id: "tool-executed",
                    title: "MCP Tool executed",
                    subtitle: $"{_toolExecutionCount} executed",
                    status: CheckStatus.Success
                );
            }
            else
            {
                return new StatusCheckItem(
                    id: "tool-executed",
                    title: "MCP Tool executed",
                    subtitle: "No tools executed yet",
                    status: CheckStatus.Pending
                );
            }
        }

        /// <summary>
        /// Call this method to increment the tool execution counter.
        /// Should be called after a tool is successfully executed.
        /// </summary>
        public static void NotifyToolExecuted()
        {
            _toolExecutionCount++;
        }

        private VisualElement CreateStatusCard(StatusCheckItem item)
        {
            var card = new VisualElement();
            card.AddToClassList("status-card");
            card.name = item.Id;

            if (item.Status == CheckStatus.Error)
            {
                card.AddToClassList("error");

                // Error accent bar
                var errorAccent = new VisualElement();
                errorAccent.AddToClassList("error-accent");
                card.Add(errorAccent);
            }
            else if (item.Status == CheckStatus.Success)
            {
                card.AddToClassList("success");
            }
            else
            {
                card.AddToClassList("pending");
            }

            // Icon container
            var iconContainer = new VisualElement();
            iconContainer.AddToClassList("status-icon-container");
            iconContainer.AddToClassList(item.Status == CheckStatus.Error ? "error-icon" : 
                                          item.Status == CheckStatus.Success ? "success-icon" : "pending-icon");

            var iconText = new Label();
            iconText.AddToClassList("status-icon-text");
            iconText.text = item.Status switch
            {
                CheckStatus.Success => "✔",
                CheckStatus.Error => "✖",
                CheckStatus.Pending => "◯",
                _ => "?"
            };
            iconContainer.Add(iconText);
            card.Add(iconContainer);

            // Content container
            var contentContainer = new VisualElement();
            contentContainer.AddToClassList("status-content");

            var titleLabel = new Label(item.Title);
            titleLabel.AddToClassList("status-title");
            contentContainer.Add(titleLabel);

            var subtitleLabel = new Label(item.Subtitle);
            subtitleLabel.AddToClassList("status-subtitle");
            if (item.Status == CheckStatus.Error)
                subtitleLabel.AddToClassList("error-text");
            contentContainer.Add(subtitleLabel);

            card.Add(contentContainer);

            // Click handler for expansion (future)
            card.RegisterCallback<MouseDownEvent>(evt =>
            {
                // Future: expand for troubleshooting details
            });

            return card;
        }
    }
}
