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
using System.Collections.Generic;
using System.Linq;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Registry for all MCP client configurators.
    /// Provides access to configurators by client ID or name.
    /// </summary>
    public static class McpClientConfiguratorRegistry
    {
        private static readonly List<McpClientConfiguratorBase> _configurators = new()
        {
            new ClaudeCodeConfigurator(),
            new ClaudeDesktopConfigurator(),
            new VSCodeCopilotConfigurator(),
            new VisualStudioCopilotConfigurator(),
            new CursorConfigurator(),
            new GeminiConfigurator(),
            new AntigravityConfigurator(),
            new CodexConfigurator()
        };

        /// <summary>
        /// Gets all registered configurators.
        /// </summary>
        public static IReadOnlyList<McpClientConfiguratorBase> All => _configurators;

        /// <summary>
        /// Gets all client names for use in dropdown.
        /// </summary>
        public static List<string> GetClientNames() => _configurators.Select(c => c.ClientName).ToList();

        /// <summary>
        /// Gets all client IDs.
        /// </summary>
        public static List<string> GetClientIds() => _configurators.Select(c => c.ClientId).ToList();

        /// <summary>
        /// Gets a configurator by its client ID.
        /// </summary>
        /// <param name="clientId">The client ID to search for.</param>
        /// <returns>The configurator if found, null otherwise.</returns>
        public static McpClientConfiguratorBase? GetByClientId(string? clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return null;

            return _configurators.FirstOrDefault(c => c.ClientId == clientId);
        }

        /// <summary>
        /// Gets a configurator by its client name.
        /// </summary>
        /// <param name="clientName">The client name to search for.</param>
        /// <returns>The configurator if found, null otherwise.</returns>
        public static McpClientConfiguratorBase? GetByClientName(string? clientName)
        {
            if (string.IsNullOrEmpty(clientName))
                return null;

            return _configurators.FirstOrDefault(c => c.ClientName == clientName);
        }

        /// <summary>
        /// Gets the index of a configurator by its client ID.
        /// </summary>
        /// <param name="clientId">The client ID to search for.</param>
        /// <returns>The index if found, -1 otherwise.</returns>
        public static int GetIndexByClientId(string? clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return -1;

            for (int i = 0; i < _configurators.Count; i++)
            {
                if (_configurators[i].ClientId == clientId)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Gets the index of a configurator by its client name.
        /// </summary>
        /// <param name="clientName">The client name to search for.</param>
        /// <returns>The index if found, -1 otherwise.</returns>
        public static int GetIndexByClientName(string? clientName)
        {
            if (string.IsNullOrEmpty(clientName))
                return -1;

            for (int i = 0; i < _configurators.Count; i++)
            {
                if (_configurators[i].ClientName == clientName)
                    return i;
            }
            return -1;
        }
    }
}
