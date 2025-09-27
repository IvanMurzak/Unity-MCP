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
    public partial class Prompt_AnimationTimeline
    {
        [McpPluginPrompt(Name = "setup-animator-controller", Role = Role.User)]
        [Description("Create Animator Controller with state machine for character or object animation.")]
        public string SetupAnimatorController()
        {
            return "Create Animator Controller with basic state machine, setup animation states, transitions, parameters, and blend trees for character or object animation.";
        }

        [McpPluginPrompt(Name = "create-simple-tweening", Role = Role.User)]
        [Description("Add simple tweening animations for UI elements or object movements.")]
        public string CreateSimpleTweening()
        {
            return "Create simple tweening animations using Animation curves, Coroutines, or tweening libraries like DOTween for smooth object movements and UI transitions.";
        }

        [McpPluginPrompt(Name = "setup-timeline-sequence", Role = Role.User)]
        [Description("Create Timeline sequence for cinematic scenes or complex gameplay sequences.")]
        public string SetupTimelineSequence()
        {
            return "Create Timeline asset with multiple tracks, setup Animation, Audio, and Activation tracks for cinematic sequences or complex gameplay events.";
        }

        [McpPluginPrompt(Name = "add-animation-events", Role = Role.User)]
        [Description("Add animation events to trigger code execution at specific animation frames.")]
        public string AddAnimationEvents()
        {
            return "Add Animation Events to animation clips, create event handler methods, and setup communication between animation system and game logic.";
        }

        [McpPluginPrompt(Name = "create-procedural-animation", Role = Role.User)]
        [Description("Implement procedural animation systems for dynamic object movement.")]
        public string CreateProceduralAnimation()
        {
            return "Create procedural animation systems using code-driven movement, physics-based animation, or mathematical functions for dynamic object behavior.";
        }

        [McpPluginPrompt(Name = "setup-sprite-animation", Role = Role.User)]
        [Description("Create 2D sprite animations with proper frame timing and looping.")]
        public string SetupSpriteAnimation()
        {
            return "Create 2D sprite animations using Animation window, setup sprite sequences, configure frame timing, looping, and animation events for 2D characters.";
        }

        [McpPluginPrompt(Name = "add-ik-system", Role = Role.User)]
        [Description("Implement Inverse Kinematics (IK) system for realistic character poses.")]
        public string AddIKSystem()
        {
            return "Setup IK system using Unity's built-in IK or custom IK solutions for realistic character posing, foot placement, and hand interactions.";
        }

        [McpPluginPrompt(Name = "create-animation-blending", Role = Role.User)]
        [Description("Setup animation blending and layering for complex character animations.")]
        public string CreateAnimationBlending()
        {
            return "Create animation blending using Blend Trees, Animation Layers, and Avatar Masks for complex character animations with smooth transitions.";
        }
    }
}