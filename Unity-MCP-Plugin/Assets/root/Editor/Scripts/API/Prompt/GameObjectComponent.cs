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
    public partial class Prompt_GameObjectComponent
    {
        [McpPluginPrompt(Name = "add-standard-components", Role = Role.User)]
        [Description("Add common Unity components like Rigidbody, Collider, and Renderer to selected GameObjects.")]
        public string AddStandardComponents()
        {
            return "Add standard Unity components to selected GameObjects: MeshRenderer, MeshFilter, Collider, and Rigidbody as appropriate for the object type.";
        }

        [McpPluginPrompt(Name = "setup-player-controller", Role = Role.User)]
        [Description("Create a basic player controller with movement scripts and necessary components.")]
        public string SetupPlayerController()
        {
            return "Create player GameObject with CharacterController or Rigidbody, add basic movement script with WASD controls, camera follow, and jump mechanics.";
        }

        [McpPluginPrompt(Name = "create-ui-canvas", Role = Role.User)]
        [Description("Setup Canvas with common UI elements and proper scaling configuration.")]
        public string CreateUICanvas()
        {
            return "Create UI Canvas with Canvas Scaler set to Scale With Screen Size, add EventSystem, and create common UI elements like buttons, text, and panels.";
        }

        [McpPluginPrompt(Name = "add-physics-interactions", Role = Role.User)]
        [Description("Configure colliders, rigidbodies, and physics materials for realistic physics interactions.")]
        public string AddPhysicsInteractions()
        {
            return "Setup physics interactions by adding appropriate Colliders, configure Rigidbody properties, create Physics Materials, and setup collision layers.";
        }

        [McpPluginPrompt(Name = "create-interactive-object", Role = Role.User)]
        [Description("Create an interactive GameObject that responds to player input or triggers.")]
        public string CreateInteractiveObject()
        {
            return "Create interactive GameObject with appropriate collider setup, add interaction script with OnTriggerEnter/OnCollisionEnter events, and visual feedback.";
        }

        [McpPluginPrompt(Name = "setup-audio-source", Role = Role.User)]
        [Description("Add and configure AudioSource component with common settings.")]
        public string SetupAudioSource()
        {
            return "Add AudioSource component to selected GameObjects, configure 3D spatial settings, volume, pitch, and loop options as appropriate.";
        }

        [McpPluginPrompt(Name = "create-particle-effects", Role = Role.User)]
        [Description("Add particle systems for visual effects like explosions, fire, or magic.")]
        public string CreateParticleEffects()
        {
            return "Create GameObject with Particle System component, configure emission, shape, velocity, and rendering settings for common visual effects.";
        }

        [McpPluginPrompt(Name = "setup-animator-component", Role = Role.User)]
        [Description("Add Animator component and basic animation setup to GameObjects.")]
        public string SetupAnimatorComponent()
        {
            return "Add Animator component to selected GameObjects, create basic Animator Controller, and setup simple animation states for common actions.";
        }
    }
}