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
using System.Text;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        [McpPluginTool
        (
            "GameObject_SetParent",
            Title = "GameObject / Set Parent"
        )]
        [Description(@"Set parent GameObject to list of GameObjects in opened Prefab or in a Scene.")]
        public string SetParent
        (
            [Description("List of references to the GameObjects to set new parent.")]
            GameObjectRefList gameObjectRefs,
            [Description("Reference to the parent GameObject.")]
            GameObjectRef parentGameObjectRef,
            [Description("A boolean flag indicating whether the GameObject's world position should remain unchanged when setting its parent.")]
            bool worldPositionStays = true
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var stringBuilder = new StringBuilder();
                int changedCount = 0;

                var parentGo = parentGameObjectRef.FindGameObject(out var error);
                if (error != null)
                {
                    stringBuilder.AppendLine(error);
                    return stringBuilder.ToString();
                }
                if (parentGo == null)
                {
                    stringBuilder.AppendLine($"[Error] GameObject by {nameof(parentGameObjectRef)} not found.");
                    return stringBuilder.ToString();
                }

                for (var i = 0; i < gameObjectRefs.Count; i++)
                {
                    var targetGo = gameObjectRefs[i].FindGameObject(out error);
                    if (error != null)
                    {
                        stringBuilder.AppendLine(error);
                        continue;
                    }
                    if (targetGo == null)
                    {
                        stringBuilder.AppendLine($"[Error] GameObject by {nameof(gameObjectRefs)}[{i}] not found.");
                        continue;
                    }

                    targetGo.transform.SetParent(parentGo.transform, worldPositionStays: worldPositionStays);
                    changedCount++;

                    stringBuilder.AppendLine(@$"[Success] Set parent of {gameObjectRefs[i]} to {parentGameObjectRef}.");
                }

                if (changedCount > 0)
                {
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    UnityEditor.EditorApplication.RepaintProjectWindow();
                    UnityEditor.EditorApplication.RepaintHierarchyWindow();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }

                return stringBuilder.ToString();
            });
        }
    }
}
