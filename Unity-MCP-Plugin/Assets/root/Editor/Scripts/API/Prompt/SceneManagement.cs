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
    public partial class Prompt_SceneManagement
    {
        [McpPluginPrompt(Name = "setup-basic-scene", Role = Role.User)]
        [Description("Setup a basic scene with camera, lighting, and basic environment.")]
        public string SetupBasicScene()
        {
            return "Create a basic Unity scene with Main Camera, Directional Light, and basic environment setup.";
        }

        [McpPluginPrompt(Name = "organize-scene-hierarchy", Role = Role.User)]
        [Description("Clean up and organize the scene hierarchy logically with proper naming and grouping.")]
        public string OrganizeSceneHierarchy()
        {
            return "Organize the scene hierarchy by grouping related GameObjects, using proper naming conventions, and creating empty parent objects for logical structure.";
        }

        [McpPluginPrompt(Name = "add-lighting-setup", Role = Role.User)]
        [Description("Configure proper lighting setup including directional light, skybox, and ambient lighting.")]
        public string AddLightingSetup()
        {
            return "Setup comprehensive lighting including Directional Light, configure Skybox, adjust ambient lighting settings, and add Light Probes if needed.";
        }

        [McpPluginPrompt(Name = "create-prefab-from-selection", Role = Role.User)]
        [Description("Convert selected GameObjects into reusable prefabs with proper naming and folder organization.")]
        public string CreatePrefabFromSelection()
        {
            return "Create prefab from currently selected GameObjects, place it in appropriate Prefabs folder, and replace the original with prefab instance.";
        }

        [McpPluginPrompt(Name = "setup-scene-camera", Role = Role.User)]
        [Description("Configure scene camera with appropriate settings for the project type.")]
        public string SetupSceneCamera()
        {
            return "Setup Main Camera with appropriate Field of View, Clipping Planes, Clear Flags, and positioning for the current scene type.";
        }

        [McpPluginPrompt(Name = "create-environment-template", Role = Role.User)]
        [Description("Create a basic environment template with ground, sky, and common environmental elements.")]
        public string CreateEnvironmentTemplate()
        {
            return "Create basic environment template including ground plane, skybox setup, basic terrain or platform, and common environmental GameObjects.";
        }
    }
}