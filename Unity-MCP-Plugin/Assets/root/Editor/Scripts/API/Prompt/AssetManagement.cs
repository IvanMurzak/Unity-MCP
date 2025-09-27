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
    public partial class Prompt_AssetManagement
    {
        [McpPluginPrompt(Name = "organize-project-structure", Role = Role.User)]
        [Description("Create and organize standard project folder hierarchy with proper naming conventions.")]
        public string OrganizeProjectStructure()
        {
            return "Create standard project folder structure including Scripts, Prefabs, Materials, Textures, Audio, Scenes, and Resources folders with proper organization.";
        }

        [McpPluginPrompt(Name = "import-setup-sprites", Role = Role.User)]
        [Description("Import sprite assets and configure sprite import settings with automatic slicing.")]
        public string ImportSetupSprites()
        {
            return "Import sprite assets, configure Texture Import Settings for sprites, setup sprite slicing for spritesheets, and organize sprites in appropriate folders.";
        }

        [McpPluginPrompt(Name = "setup-audio-manager", Role = Role.User)]
        [Description("Create audio management system with sound pools and volume controls.")]
        public string SetupAudioManager()
        {
            return "Create audio manager system with AudioSource pooling, volume controls, sound categories (SFX, Music, Voice), and easy-to-use audio playback methods.";
        }

        [McpPluginPrompt(Name = "configure-build-settings", Role = Role.User)]
        [Description("Setup build settings including scenes, player settings, and platform configurations.")]
        public string ConfigureBuildSettings()
        {
            return "Configure Build Settings with proper scene order, setup Player Settings including company name, product name, icons, and platform-specific configurations.";
        }

        [McpPluginPrompt(Name = "create-material-library", Role = Role.User)]
        [Description("Create a library of common materials with proper naming and organization.")]
        public string CreateMaterialLibrary()
        {
            return "Create collection of common materials (Standard, Unlit, UI, etc.) with proper naming conventions, organize in Materials folder, and setup material variants.";
        }

        [McpPluginPrompt(Name = "setup-asset-bundles", Role = Role.User)]
        [Description("Configure AssetBundles for content streaming and modular loading.")]
        public string SetupAssetBundles()
        {
            return "Setup AssetBundle system for streaming assets, configure bundle naming and dependencies, create build pipeline for asset bundle generation.";
        }

        [McpPluginPrompt(Name = "optimize-texture-settings", Role = Role.User)]
        [Description("Optimize texture import settings for better performance and memory usage.")]
        public string OptimizeTextureSettings()
        {
            return "Review and optimize texture import settings including compression, max size, format selection, and platform-specific overrides for better performance.";
        }

        [McpPluginPrompt(Name = "setup-addressables", Role = Role.User)]
        [Description("Configure Unity Addressables system for efficient asset loading and management.")]
        public string SetupAddressables()
        {
            return "Setup Unity Addressables system, configure groups and schemas, mark assets as addressable, and create efficient loading/unloading patterns.";
        }
    }
}