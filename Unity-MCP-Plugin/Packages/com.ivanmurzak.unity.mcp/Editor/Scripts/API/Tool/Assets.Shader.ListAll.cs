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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Assets_Shader
    {
        public const string AssetsShaderListAllToolId = "assets-shader-list-all";
        [AiTool
        (
            AssetsShaderListAllToolId,
            Title = "Assets / List Shaders",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [AiSkillDescription("List all shaders available in the project assets and packages, sorted by name. " +
            "Use this to discover a valid `shaderName` for '" + Tool_Assets.AssetsMaterialCreateToolId + "'.")]
        [AiSkillBody("List all available shaders in the project assets and packages. " +
            "Returns their names. " +
            "Use this to find a shader name for '" + Tool_Assets.AssetsMaterialCreateToolId + "' tool.\n\n" +
            "## Behavior\n\n" +
            "Enumerates shaders via `ShaderUtils.GetAllShaders`, filters out nulls, and returns the names alphabetically sorted.")]
        [Description("List all available shaders in the project assets and packages. " +
            "Returns their names. " +
            "Use this to find a shader name for '" + Tool_Assets.AssetsMaterialCreateToolId + "' tool.")]
        public string[] ListAll(string? nothing = null) => MainThread.Instance.Run(() =>
        {
            return ShaderUtils.GetAllShaders()
                .Where(shader => shader != null)
                .Select(shader => shader.name)
                .OrderBy(name => name)
                .ToArray();
        });
    }
}