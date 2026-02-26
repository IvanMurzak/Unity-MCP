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
using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
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

                return $"[Error] No component with instanceID '{componentInstanceID}' found in GameObject.\n{helpMessage}\nAvailable components preview:\n{previewJson}";
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

                return $"[Error] No components with instanceIDs [{componentInstanceIDsString}] found in GameObject.\nAvailable components preview:\n{previewJson}";
            }
            public static string InvalidInstanceID(Type holderType, string fieldName)
                => $"[Error] Invalid instanceID '{fieldName}' for '{holderType.GetTypeId()}'. It should be a valid field name.";
        }
    }
}
