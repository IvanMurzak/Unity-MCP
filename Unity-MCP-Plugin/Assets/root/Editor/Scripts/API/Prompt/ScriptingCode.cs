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
using System.ComponentModel;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginPromptType]
    public partial class Prompt_ScriptingCode
    {
        [McpPluginPrompt(Name = "generate-monobehaviour-template", Role = Role.User)]
        [Description("Create a MonoBehaviour script template with common Unity lifecycle methods and patterns.")]
        public string GenerateMonoBehaviourTemplate()
        {
            return "Create new MonoBehaviour script with Start, Update, and other common Unity lifecycle methods, including proper using statements and namespace.";
        }

        [McpPluginPrompt(Name = "add-event-system", Role = Role.User)]
        [Description("Implement UnityEvent-based communication system between GameObjects.")]
        public string AddEventSystem()
        {
            return "Create event system using UnityEvents, UnityActions, or custom event delegates for decoupled communication between game systems and components.";
        }

        [McpPluginPrompt(Name = "create-singleton-manager", Role = Role.User)]
        [Description("Generate a singleton manager class following Unity best practices.")]
        public string CreateSingletonManager()
        {
            return "Create singleton manager class with proper initialization, thread-safe implementation, and DontDestroyOnLoad setup for persistent game managers.";
        }

        [McpPluginPrompt(Name = "setup-coroutine-framework", Role = Role.User)]
        [Description("Add coroutine-based asynchronous operations and utility methods.")]
        public string SetupCoroutineFramework()
        {
            return "Create coroutine utilities for async operations like delayed execution, gradual value changes, and time-based operations with proper cleanup.";
        }

        [McpPluginPrompt(Name = "create-scriptableobject-data", Role = Role.User)]
        [Description("Generate ScriptableObject classes for data storage and configuration.")]
        public string CreateScriptableObjectData()
        {
            return "Create ScriptableObject classes for game data, settings, and configuration with proper CreateAssetMenu attributes and serialization.";
        }

        [McpPluginPrompt(Name = "implement-object-pooling", Role = Role.User)]
        [Description("Create object pooling system for performance optimization.")]
        public string ImplementObjectPooling()
        {
            return "Implement object pooling system for frequently instantiated/destroyed objects like bullets, enemies, or particles to improve performance.";
        }

        [McpPluginPrompt(Name = "add-state-machine", Role = Role.User)]
        [Description("Create a finite state machine for character or game state management.")]
        public string AddStateMachine()
        {
            return "Create finite state machine implementation for managing character states, game states, or AI behavior with proper state transitions.";
        }

        [McpPluginPrompt(Name = "setup-dependency-injection", Role = Role.User)]
        [Description("Implement simple dependency injection pattern for better code organization.")]
        public string SetupDependencyInjection()
        {
            return "Create simple dependency injection system using constructor injection or service locator pattern for better testability and loose coupling.";
        }
    }
}