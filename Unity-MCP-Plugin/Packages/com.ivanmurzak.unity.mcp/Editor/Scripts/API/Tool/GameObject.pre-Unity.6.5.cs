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
#if !UNITY_6000_5_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using AIGD;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    public partial class Tool_GameObject
    {
        public static class Error
        {
            public static string NotFoundComponent(int componentInstanceID, IEnumerable<UnityEngine.Component> allComponents)
            {
                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var availableComponentsPreview = allComponents
                    .Select((c, i) => reflector.Serialize(
                        c,
                        name: $"[{i}]",
                        recursive: false,
                        logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>()
                    ))
                    .ToList();
                var previewJson = availableComponentsPreview.ToJson(reflector);

                var instanceIdSample = new { componentData = availableComponentsPreview[0] }.ToJson(reflector);
                var helpMessage = $"Use 'name=[index]' to specify the component. Or use 'instanceID' to specify the component.\n{instanceIdSample}";

                return $"No component with instanceID '{componentInstanceID}' found in GameObject.\n{helpMessage}\nAvailable components preview:\n{previewJson}";
            }
            public static string NotFoundComponents(ComponentRefList componentRefs, IEnumerable<UnityEngine.Component> allComponents)
            {
                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector is not available.");
                var componentInstanceIDsString = string.Join(", ", componentRefs.Select(cr => cr.ToString()));
                var availableComponentsPreview = allComponents
                    .Select((c, i) => reflector.Serialize(
                        obj: c,
                        fallbackType: typeof(UnityEngine.Component),
                        name: $"[{i}]",
                        recursive: false,
                        logger: UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_GameObject>()
                    ))
                    .ToList();
                var previewJson = availableComponentsPreview.ToJson(reflector);

                return $"No components with instanceIDs [{componentInstanceIDsString}] found in GameObject.\nAvailable components preview:\n{previewJson}";
            }
            public static string InvalidInstanceID(Type holderType, string fieldName)
                => $"Invalid instanceID '{fieldName}' for '{holderType.GetTypeId()}'. It should be a valid field name.";
        }

        // Shared skill metadata for `gameobject-destroy`. Mirrors GameObject.cs so the SKILL.md is identical
        // across both Unity builds (only the underlying EntityId/InstanceID code path differs).
        internal static class DestroySkill
        {
            public const string Description =
                "Destroy a GameObject (and all nested children) in the currently opened Prefab or active Scene. " +
                "Returns the destroyed GameObject's name, path, and instance ID for confirmation. " +
                "Use 'gameobject-find' to locate the target first.";

            public const string Body =
                "Destroy GameObject and all nested GameObjects recursively in opened Prefab or in a Scene. " +
                "Use 'gameobject-find' tool to find the target GameObject first.\n\n" +
                "## Behavior\n\n" +
                "Validates the `gameObjectRef`, resolves it on the main thread, then calls `Object.DestroyImmediate` " +
                "(the immediate variant is required for Editor-mode operations). Returns a `DestroyGameObjectResult` " +
                "containing `DestroyedName`, `DestroyedPath`, and `DestroyedInstanceId` so the caller has a record of " +
                "what was removed.";
        }

        // Shared skill metadata for `gameobject-component-get`. Mirrors GameObject.cs.
        internal static class ComponentGetSkill
        {
            public const string Description =
                "Get detailed information about a specific Component on a GameObject — type, enabled state, and " +
                "(optionally) serialized fields and properties. Supports token-saving path-scoped reads via `paths` or " +
                "`viewQuery`. Use 'gameobject-find' to list components first.";

            public const string Body =
                "Get detailed information about a specific Component on a GameObject. " +
                "Returns component type, enabled state, and optionally serialized fields and properties. " +
                "Use this to inspect component data before modifying it. " +
                "Use 'gameobject-find' tool to get the list of all components on the GameObject.\n\n" +
                "## Inputs\n\n" +
                "- `gameObjectRef` — the host GameObject.\n" +
                "- `componentRef` — the specific component to inspect (matched by index or instance ID).\n" +
                "- `includeFields` (default `true`) — populate the legacy `Fields` list.\n" +
                "- `includeProperties` (default `true`) — populate the legacy `Properties` list.\n" +
                "- `deepSerialization` (default `false`) — when populating legacy lists, recurse into nested members.\n\n" +
                "## Path-scoped reads (token-saving)\n\n" +
                "Supply `paths` (a list of paths) to read only the listed fields/elements via `Reflector.TryReadAt`, " +
                "or `viewQuery` (a `ViewQuery`) to navigate to a subtree and/or filter by name regex / max depth / type " +
                "via `Reflector.View`. The result is returned in the `View` field of the response, and the legacy " +
                "`Fields`/`Properties` lists are skipped. These two parameters are mutually exclusive — supply at most one.\n\n" +
                "## Path syntax\n\n" +
                "`fieldName`, `nested/field`, `arrayField/[i]`, `dictField/[key]`. Leading `#/` is stripped.";
        }
    }
}
#endif
