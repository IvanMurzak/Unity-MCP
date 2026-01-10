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
        private VisualElement? _miniViewContainer;
        private VisualElement? _fullViewContainer;
        private bool _isExpanded = true;

        // Track tool execution count (resets on domain reload)
        private static int _toolExecutionCount = 0;

        /// <summary>
        /// Status of a single check item.
        /// </summary>
        public enum CheckStatus
        {
            Success,    // Green - check passed
            Error,      // Red - check failed, action required
            Pending     // Gray - optional or awaiting
        }

        /// <summary>
        /// Data model for a status check item.
        /// </summary>
        public class StatusCheckItem
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Subtitle { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public CheckStatus Status { get; set; } = CheckStatus.Pending;

            public StatusCheckItem(string id, string title, string subtitle = "", string description = "", CheckStatus status = CheckStatus.Pending)
            {
                Id = id;
                Title = title;
                Subtitle = subtitle;
                Description = description;
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

            _miniViewContainer = root.Q<VisualElement>("mini-view-container");
            _fullViewContainer = root.Q<VisualElement>("full-view-container");
            _statusItemsContainer = root.Q<VisualElement>(className: "status-items-container");

            if (_statusItemsContainer == null)
            {
                Logger.LogWarning("{method}: status-items-container not found", nameof(OnGUICreated));
                return;
            }

            // Setup mini view click handler
            if (_miniViewContainer != null)
            {
                _miniViewContainer.RegisterCallback<MouseDownEvent>(evt => ToggleView());
            }

            // Clear any static cards
            _statusItemsContainer.Clear();

            // Initial render
            RefreshAllChecks();
            UpdateViewState();
        }

        private void ToggleView()
        {
            _isExpanded = !_isExpanded;
            UpdateViewState();
        }

        private void UpdateViewState()
        {
            if (_miniViewContainer == null || _fullViewContainer == null)
                return;

            _miniViewContainer.style.display = _isExpanded ? DisplayStyle.None : DisplayStyle.Flex;
            _fullViewContainer.style.display = _isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
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

            // Update mini view
            UpdateMiniView(checks);

            // Update stats label in full view
            var statsLabel = _fullViewContainer?.Q<Label>("stats-label");
            if (statsLabel != null)
            {
                var passedCount = checks.Count(c => c.Status == CheckStatus.Success);
                statsLabel.text = $"({passedCount}/{checks.Count})";
            }

            // Update full view
            _statusItemsContainer.Clear();
            foreach (var check in checks)
            {
                var card = CreateStatusCard(check);
                _statusItemsContainer.Add(card);
            }
        }

        private void UpdateMiniView(List<StatusCheckItem> checks)
        {
            var miniDotsContainer = _miniViewContainer?.Q<VisualElement>("mini-dots");
            var miniLabel = _miniViewContainer?.Q<Label>("mini-label");

            if (miniDotsContainer == null || miniLabel == null)
                return;

            var passedCount = checks.Count(c => c.Status == CheckStatus.Success);
            miniLabel.text = $"Status ({passedCount}/{checks.Count})";

            miniDotsContainer.Clear();
            foreach (var check in checks)
            {
                var dot = new VisualElement();
                dot.AddToClassList("mini-dot");
                dot.AddToClassList(check.Status switch
                {
                    CheckStatus.Success => "dot-success",
                    CheckStatus.Error => "dot-error",
                    _ => "dot-pending"
                });
                miniDotsContainer.Add(dot);
            }
        }

        private List<StatusCheckItem> GatherStatusChecks()
        {
            var checks = new List<StatusCheckItem>
            {
                GetMcpClientConfiguredCheck(),
                GetUnityConnectedCheck(),
                GetVersionHandshakeCheck(),
                GetServerToClientCheck(),
                GetClientLocationCheck(),
                GetEnabledToolsCheck(),
                GetToolExecutedCheck()
            };
            return checks;
        }

        #region Status Check Methods

        private StatusCheckItem GetMcpClientConfiguredCheck()
        {
            var configuredClients = GetConfiguredClientNames();
            var count = configuredClients.Count;
            var isConfigured = count > 0;

            var clientList = isConfigured ? string.Join(", ", configuredClients) : "";

            return new StatusCheckItem(
                id: "mcp-client-configured",
                title: "MCP Client configured",
                subtitle: isConfigured ? $"{count} configured: {clientList}" : "No MCP clients configured",
                description: isConfigured
                    ? $"Configured clients: {clientList}. You can configure more clients in the main window using the Configure button."
                    : "No MCP clients are configured. Open the main MCP window and click the 'Configure' button next to your preferred MCP client (Claude Desktop, Cursor, etc.) to set up the connection.",
                status: isConfigured ? CheckStatus.Success : CheckStatus.Error
            );
        }

        private List<string> GetConfiguredClientNames()
        {
            var clients = new List<string>();

            try
            {
                var claudeDesktopPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Claude", "claude_desktop_config.json");
                if (System.IO.File.Exists(claudeDesktopPath) &&
                    JsonClientConfig.IsMcpClientConfigured(claudeDesktopPath))
                    clients.Add("Claude Desktop");
            }
            catch { /* Ignore */ }

            try
            {
                var cursorPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".cursor", "mcp.json");
                if (System.IO.File.Exists(cursorPath) &&
                    JsonClientConfig.IsMcpClientConfigured(cursorPath))
                    clients.Add("Cursor");
            }
            catch { /* Ignore */ }

            return clients;
        }

        private StatusCheckItem GetUnityConnectedCheck()
        {
            var connectionState = UnityMcpPlugin.ConnectionState.CurrentValue;
            var isConnected = connectionState == HubConnectionState.Connected;
            var keepConnected = UnityMcpPlugin.KeepConnected;
            var version = UnityMcpPlugin.Version;

            if (isConnected)
            {
                return new StatusCheckItem(
                    id: "unity-connected",
                    title: "Unity connected to MCP Server",
                    subtitle: $"Connected (Plugin v{version})",
                    description: $"Unity is successfully connected to the MCP Server. Plugin version: {version}",
                    status: CheckStatus.Success
                );
            }
            else if (keepConnected && (connectionState == HubConnectionState.Connecting || connectionState == HubConnectionState.Reconnecting))
            {
                return new StatusCheckItem(
                    id: "unity-connected",
                    title: "Unity connected to MCP Server",
                    subtitle: connectionState == HubConnectionState.Connecting ? "Connecting..." : "Reconnecting...",
                    description: "Unity is attempting to connect to the MCP Server. Please wait...",
                    status: CheckStatus.Pending
                );
            }
            else
            {
                return new StatusCheckItem(
                    id: "unity-connected",
                    title: "Unity connected to MCP Server",
                    subtitle: "Not connected",
                    description: "Unity is not connected to the MCP Server.\n\n" +
                        "To fix this:\n" +
                        "1. Open the main MCP window (Window → MCP → Main)\n" +
                        "2. Click the 'Connect' button\n" +
                        "3. Make sure your configured MCP client is running and has this Unity project folder open",
                    status: CheckStatus.Error
                );
            }
        }

        private StatusCheckItem GetVersionHandshakeCheck()
        {
            var version = UnityMcpPlugin.Version;
            var isConnected = UnityMcpPlugin.IsConnected.CurrentValue;

            // Stub: Server version would come from RemoteMcpManagerHub
            var serverVersion = isConnected ? version : "Unknown";

            if (isConnected)
            {
                return new StatusCheckItem(
                    id: "version-handshake",
                    title: "Version handshake status",
                    subtitle: $"Plugin v{version}, Server v{serverVersion}",
                    description: $"Plugin and Server versions are compatible.\n\nPlugin version: {version}\nServer version: {serverVersion}",
                    status: CheckStatus.Success
                );
            }
            else
            {
                return new StatusCheckItem(
                    id: "version-handshake",
                    title: "Version handshake status",
                    subtitle: "Connect to verify version",
                    description: "Cannot verify version compatibility until connected to the MCP Server.\n\n" +
                        "If you have issues after connecting, make sure your MCP Server (mcp-tool-unity) is up to date. " +
                        "You may need to update it if the plugin was recently updated.",
                    status: CheckStatus.Pending
                );
            }
        }

        private StatusCheckItem GetServerToClientCheck()
        {
            var isConnected = UnityMcpPlugin.IsConnected.CurrentValue;

            // Stub: Client name would come from MCP Server tracking
            var clientName = "Unknown";

            if (isConnected)
            {
                return new StatusCheckItem(
                    id: "server-to-client",
                    title: "MCP Server connected to MCP Client",
                    subtitle: $"Awaiting client: {clientName}",
                    description: "The MCP Server is running and waiting for an MCP Client connection.\n\n" +
                        "Make sure your MCP client (Claude Desktop, Cursor, etc.) is:\n" +
                        "1. Running\n" +
                        "2. Configured with the correct Unity-MCP server settings\n" +
                        "3. Has this Unity project folder open or selected",
                    status: CheckStatus.Pending
                );
            }
            else
            {
                return new StatusCheckItem(
                    id: "server-to-client",
                    title: "MCP Server connected to MCP Client",
                    subtitle: "Server not running",
                    description: "The MCP Server is not running, so no MCP Client can connect.\n\n" +
                        "First, click 'Connect' in the main MCP window to start the server.",
                    status: CheckStatus.Error
                );
            }
        }

        private StatusCheckItem GetClientLocationCheck()
        {
            var unityProjectPath = Environment.CurrentDirectory;

            // Stub: Would need to parse client config to get actual path
            var isConnected = UnityMcpPlugin.IsConnected.CurrentValue;

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
                        subtitle: $"Unity: {unityProjectPath}",
                        description: $"Unity project path: {unityProjectPath}\n\n" +
                            "Make sure your MCP client is running in the same directory. " +
                            "Each Unity project generates a unique port based on its folder path, " +
                            "so the client must be launched from the correct location.",
                        status: isConnected ? CheckStatus.Success : CheckStatus.Pending
                    );
                }
            }
            catch { /* Ignore */ }

            return new StatusCheckItem(
                id: "client-location",
                title: "MCP Client location match",
                subtitle: "No client config found",
                description: "Could not find any MCP client configuration.\n\n" +
                    $"Unity project path: {unityProjectPath}\n\n" +
                    "Configure an MCP client first using the 'Configure' button in the main MCP window.",
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
                    description: "The MCP Plugin is not initialized. Try connecting to the MCP Server first.",
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
                    description: $"You have {enabledCount} out of {totalCount} tools enabled.\n\n" +
                        "You can manage enabled tools in Window → MCP → Tools. " +
                        "Disabling unused tools saves LLM context tokens.",
                    status: CheckStatus.Success
                );
            }
            else
            {
                return new StatusCheckItem(
                    id: "enabled-tools",
                    title: "MCP Plugin has at least 1 enabled tool",
                    subtitle: "0 tools enabled",
                    description: "No tools are currently enabled. The MCP client won't be able to perform any actions.\n\n" +
                        "To fix this:\n" +
                        "1. Go to Window → MCP → Tools\n" +
                        "2. Enable at least one tool by toggling it on",
                    status: CheckStatus.Error
                );
            }
        }

        private StatusCheckItem GetToolExecutedCheck()
        {
            // This is always gray/optional - just an indicator
            return new StatusCheckItem(
                id: "tool-executed",
                title: "MCP Tool executed",
                subtitle: _toolExecutionCount > 0 ? $"{_toolExecutionCount} executed this session" : "No tools executed yet",
                description: _toolExecutionCount > 0
                    ? $"MCP tools have been executed {_toolExecutionCount} times this session. This confirms that the connection is working correctly."
                    : "No MCP tools have been executed yet this session. This counter increments each time an MCP client successfully calls a tool.",
                status: CheckStatus.Pending // Always pending/gray - optional indicator
            );
        }

        #endregion

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

            // Status-based styling
            card.AddToClassList(item.Status switch
            {
                CheckStatus.Success => "success",
                CheckStatus.Error => "error",
                _ => "pending"
            });

            if (item.Status == CheckStatus.Error)
            {
                var errorAccent = new VisualElement();
                errorAccent.AddToClassList("error-accent");
                card.Add(errorAccent);
            }

            // Icon container
            var iconContainer = new VisualElement();
            iconContainer.AddToClassList("status-icon-container");
            iconContainer.AddToClassList(item.Status switch
            {
                CheckStatus.Success => "success-icon",
                CheckStatus.Error => "error-icon",
                _ => "pending-icon"
            });

            var iconText = new Label
            {
                text = item.Status switch
                {
                    CheckStatus.Success => "✔",
                    CheckStatus.Error => "✖",
                    _ => "◯"
                }
            };
            iconText.AddToClassList("status-icon-text");
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

            // Description (expandable, shown for errors)
            if (!string.IsNullOrEmpty(item.Description))
            {
                var descriptionLabel = new Label(item.Description);
                descriptionLabel.AddToClassList("status-description");
                if (item.Status == CheckStatus.Error)
                {
                    descriptionLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    descriptionLabel.style.display = DisplayStyle.None;
                }
                contentContainer.Add(descriptionLabel);

                // Click to toggle description
                card.RegisterCallback<MouseDownEvent>(evt =>
                {
                    var isVisible = descriptionLabel.style.display == DisplayStyle.Flex;
                    descriptionLabel.style.display = isVisible ? DisplayStyle.None : DisplayStyle.Flex;
                });
            }

            card.Add(contentContainer);

            return card;
        }
    }
}
