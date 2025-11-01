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
        [McpPluginTool
        (
            "Assets_Shader_ListAll",
            Title = "List all shader names"
        )]
        [Description(@"Scans the project assets to find all shaders and to get the name from each of them. Returns the list of shader names.")]
        public string ListAll() => MainThread.Instance.Run(() =>
        {
            var shaderNames = ShaderUtils.GetAllShaders()
                .Where(shader => shader != null)
                .Select(shader => shader.name)
                .OrderBy(name => name)
                .ToList();

            return "[Success] List of all shader names in the project:\n" + string.Join("\n", shaderNames);
        });
    }
}