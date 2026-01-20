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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Object
    {
        public const string ObjectGetDataToolId = "object-get-data";
        [McpPluginTool
        (
            ObjectGetDataToolId,
            Title = "Object / Get Data"
        )]
        [Description("Get data of the specified Unity Object.")]
        public SerializedMember? GetData
        (
            ObjectRef objectRef
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var obj = objectRef.FindObject();
                if (obj == null)
                    return null;

                return McpPlugin.McpPlugin.Instance!.McpManager.Reflector.Serialize(
                    obj,
                    name: obj?.name,
                    recursive: true,
                    flags: System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Object>()
                );
            });
        }
    }
}
