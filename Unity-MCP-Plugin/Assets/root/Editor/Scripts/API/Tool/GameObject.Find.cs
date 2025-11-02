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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        [McpPluginTool
        (
            "GameObject_Find",
            Title = "Find GameObject in opened Prefab or in a Scene"
        )]
        [Description(@"Finds specific GameObject by provided information.
First it looks for the opened Prefab, if any Prefab is opened it looks only there ignoring a scene.
If no opened Prefab it looks into current active scene.
Returns GameObject information and its children.
Also, it returns Components preview just for the target GameObject.")]
        public string Find
        (
            GameObjectRef gameObjectRef,
            [Description("Determines the depth of the hierarchy to include. 0 - means only the target GameObject. 1 - means to include one layer below.")]
            int includeChildrenDepth = 0,
            [Description("If true, it will print only brief data of the target GameObject.")]
            bool briefData = false
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var go = gameObjectRef.FindGameObject(out var error);
                if (error != null)
                    return $"[Error] {error}";

                if (go == null)
                    return $"[Error] GameObject by {nameof(gameObjectRef)} not found.";

                var reflector = McpPlugin.McpPlugin.Instance!.McpManager.Reflector;

                var serializedGo = reflector.Serialize(
                    obj: go,
                    name: go.name,
                    recursive: !briefData,
                    logger: McpPlugin.McpPlugin.Instance.Logger
                );
                var json = serializedGo.ToJson(reflector);
                return @$"[Success] Found GameObject.
# Data:
```json
{json}
```

# Bounds:
```json
{go.CalculateBounds().ToJson(reflector)}
```

# Hierarchy:
{go.ToMetadata(includeChildrenDepth)?.Print() ?? "null"}
";
            });
        }
    }
}
