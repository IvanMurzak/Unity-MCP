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

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Registry for all AI agent configurators.
    /// Provides access to configurators by agent ID or name.
    /// </summary>
    public static class AiAgentConfiguratorRegistry
    {
        private static readonly List<AiAgentConfigurator> _configurators = new AiAgentConfigurator[]
            {
                new ClaudeCodeConfigurator(),
                new ClaudeDesktopConfigurator(),
                new VisualStudioCodeCopilotConfigurator(),
                new VisualStudioCopilotConfigurator(),
                new CursorConfigurator(),
                new GeminiConfigurator(),
                new AntigravityConfigurator(),
                new OpenCodeConfigurator(),
                new CodexConfigurator()
            }
            .OrderBy(c => c.AgentName)
            .Append(new CustomConfigurator()) // Ensure CustomConfigurator is always last
            .ToList();

        /// <summary>
        /// Gets all registered configurators.
        /// </summary>
        public static IReadOnlyList<AiAgentConfigurator> All => _configurators;

        /// <summary>
        /// Gets all agent names for use in dropdown.
        /// </summary>
        public static List<string> GetAgentNames() => _configurators.Select(c => c.AgentName).ToList();

        /// <summary>
        /// Gets all agent IDs.
        /// </summary>
        public static List<string> GetAgentIds() => _configurators.Select(c => c.AgentId).ToList();

        /// <summary>
        /// Gets a configurator by its agent ID.
        /// </summary>
        /// <param name="agentId">The agent ID to search for.</param>
        /// <returns>The configurator if found, null otherwise.</returns>
        public static AiAgentConfigurator? GetByAgentId(string? agentId)
        {
            if (string.IsNullOrEmpty(agentId))
                return null;

            return _configurators.FirstOrDefault(c => c.AgentId == agentId);
        }

        /// <summary>
        /// Gets a configurator by its agent name.
        /// </summary>
        /// <param name="agentName">The agent name to search for.</param>
        /// <returns>The configurator if found, null otherwise.</returns>
        public static AiAgentConfigurator? GetByAgentName(string? agentName)
        {
            if (string.IsNullOrEmpty(agentName))
                return null;

            return _configurators.FirstOrDefault(c => c.AgentName == agentName);
        }

        /// <summary>
        /// Gets the index of a configurator by its agent ID.
        /// </summary>
        /// <param name="agentId">The agent ID to search for.</param>
        /// <returns>The index if found, -1 otherwise.</returns>
        public static int GetIndexByAgentId(string? agentId)
        {
            if (string.IsNullOrEmpty(agentId))
                return -1;

            for (int i = 0; i < _configurators.Count; i++)
            {
                if (_configurators[i].AgentId == agentId)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Gets the index of a configurator by its agent name.
        /// </summary>
        /// <param name="agentName">The agent name to search for.</param>
        /// <returns>The index if found, -1 otherwise.</returns>
        public static int GetIndexByAgentName(string? agentName)
        {
            if (string.IsNullOrEmpty(agentName))
                return -1;

            for (int i = 0; i < _configurators.Count; i++)
            {
                if (_configurators[i].AgentName == agentName)
                    return i;
            }
            return -1;
        }
    }
}
