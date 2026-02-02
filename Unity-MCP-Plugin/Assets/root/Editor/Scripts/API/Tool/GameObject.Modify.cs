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
using System;
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
    public partial class Tool_GameObject
    {
        public const string GameObjectModifyToolId = "gameobject-modify";
        [McpPluginTool
        (
            GameObjectModifyToolId,
            Title = "GameObject / Modify"
        )]
        [Description("Modify GameObject fields and properties in opened Prefab or in a Scene. " +
            "You can modify multiple GameObjects at once. Just provide the same number of GameObject references and SerializedMember objects.")]
        public Logs? Modify
        (
            GameObjectRefList gameObjectRefs,
            [Description("Each item in the array represents a GameObject modification of the 'gameObjectRefs' at the same index. " +
                "Usually a GameObject is a container for components. Each component may have fields and properties for modification. " +
                "If you need to modify components of a GameObject, please use '" + GameObjectComponentModifyToolId + "' tool. " +
                "Ignore values that should not be modified. " +
                "Any unknown or wrong located fields and properties will be ignored. " +
                "Check the result of this command to see what was changed. The ignored fields and properties will be listed.")]
            SerializedMemberList gameObjectDiffs
        )
        {
            return MainThread.Instance.Run(() =>
            {
                if (gameObjectRefs.Count == 0)
                    throw new Exception("No GameObject references provided. Please provide at least one GameObject reference.");

                if (gameObjectDiffs.Count != gameObjectRefs.Count)
                    throw new Exception($"The number of {nameof(gameObjectDiffs)} and {nameof(gameObjectRefs)} should be the same. " +
                        $"{nameof(gameObjectDiffs)}: {gameObjectDiffs.Count}, {nameof(gameObjectRefs)}: {gameObjectRefs.Count}");

                var logs = new Logs();

                for (int i = 0; i < gameObjectRefs.Count; i++)
                {
                    var go = gameObjectRefs[i].FindGameObject(out var error);
                    if (error != null)
                    {
                        logs.Error(error);
                        continue;
                    }
                    if (go == null)
                    {
                        logs.Error($"GameObject by {nameof(gameObjectRefs)}[{i}] not found.");
                        continue;
                    }

                    var objToModify = (object)go;

                    var modified = McpPlugin.McpPlugin.Instance!.McpManager.Reflector.TryPopulate(
                        ref objToModify,
                        data: gameObjectDiffs[i],
                        logs: logs,
                        logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>());

                    if (modified)
                        UnityEditor.EditorUtility.SetDirty(go);
                }

                UnityMcpPlugin.NotifyToolExecuted();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

                if (logs.Count == 0)
                    logs.Warning("No modifications were made.");

                return logs;
            });
        }
    }
}
