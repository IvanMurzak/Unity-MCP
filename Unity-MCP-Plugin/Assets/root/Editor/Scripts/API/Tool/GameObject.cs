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
using System.Reflection;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
using com.IvanMurzak.Unity.MCP.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [McpPluginToolType]
    public partial class Tool_GameObject
    {
        public static class Error
        {
            static string RootGOsPrinted => GameObjectUtils.FindRootGameObjects().Print();

            public static string GameObjectPathIsEmpty()
                => $"[Error] GameObject path is empty. Root GameObjects in the active scene:\n{RootGOsPrinted}";
            public static string NotFoundGameObjectAtPath(string path)
                => $"[Error] GameObject '{path}' not found. Root GameObjects in the active scene:\n{RootGOsPrinted}";

            public static string GameObjectInstanceIDIsEmpty()
                => $"[Error] GameObject InstanceID is empty. Root GameObjects in the active scene:\n{RootGOsPrinted}";
            public static string GameObjectNameIsEmpty()
                => $"[Error] GameObject name is empty. Root GameObjects in the active scene:\n{RootGOsPrinted}";
            public static string NotFoundGameObjectWithName(string name)
                => $"[Error] GameObject with name '{name}' not found. Root GameObjects in the active scene:\n{RootGOsPrinted}";
            public static string NotFoundGameObjectWithInstanceID(int instanceID)
                => $"[Error] GameObject with InstanceID '{instanceID}' not found. Root GameObjects in the active scene:\n{RootGOsPrinted}";

            public static string TypeMismatch(string typeName, string expectedTypeName)
                => $"[Error] Type mismatch. Expected '{expectedTypeName}', but got '{typeName}'.";
            public static string InvalidComponentPropertyType(SerializedMember serializedProperty, PropertyInfo propertyInfo)
                => $"[Error] Invalid component property type '{serializedProperty.typeName}' for '{propertyInfo.Name}'. Expected '{propertyInfo.PropertyType.GetTypeId()}'.";
            public static string InvalidComponentFieldType(SerializedMember serializedProperty, FieldInfo propertyInfo)
                => $"[Error] Invalid component field type '{serializedProperty.typeName}' for '{propertyInfo.Name}'. Expected '{propertyInfo.FieldType.GetTypeId()}'.";
            public static string InvalidComponentType(string typeName)
                => $"[Error] Invalid component type '{typeName}'. It should be a valid Component Type.";
            public static string NotFoundComponent(int componentInstanceID, IEnumerable<UnityEngine.Component> allComponents)
            {
                var availableComponentsPreview = allComponents
                    .Select((c, i) => McpPlugin.McpPlugin.Instance!.McpManager.Reflector.Serialize(
                        c,
                        name: $"[{i}]",
                        recursive: false,
                        logger: McpPlugin.McpPlugin.Instance.Logger
                    ))
                    .ToList();
                var previewJson = availableComponentsPreview.ToJson(McpPlugin.McpPlugin.Instance!.McpManager.Reflector);

                var instanceIdSample = new { componentData = availableComponentsPreview[0] }.ToJson(McpPlugin.McpPlugin.Instance!.McpManager.Reflector);
                var helpMessage = $"Use 'name=[index]' to specify the component. Or use 'instanceID' to specify the component.\n{instanceIdSample}";

                return $"[Error] No component with instanceID '{componentInstanceID}' found in GameObject.\n{helpMessage}\nAvailable components preview:\n{previewJson}";
            }
            public static string NotFoundComponents(ComponentRefList componentRefs, IEnumerable<UnityEngine.Component> allComponents)
            {
                var componentInstanceIDsString = string.Join(", ", componentRefs.Select(cr => cr.ToString()));
                var availableComponentsPreview = allComponents
                    .Select((c, i) => McpPlugin.McpPlugin.Instance!.McpManager.Reflector.Serialize(
                        obj: c,
                        fallbackType: typeof(UnityEngine.Component),
                        name: $"[{i}]",
                        recursive: false,
                        logger: McpPlugin.McpPlugin.Instance.Logger
                    ))
                    .ToList();
                var previewJson = availableComponentsPreview.ToJson(McpPlugin.McpPlugin.Instance!.McpManager.Reflector);

                return $"[Error] No components with instanceIDs [{componentInstanceIDsString}] found in GameObject.\nAvailable components preview:\n{previewJson}";
            }
            public static string ComponentFieldNameIsEmpty()
                => $"[Error] Component field name is empty. It should be a valid field name.";
            public static string ComponentFieldTypeIsEmpty()
                => $"[Error] Component field type is empty. It should be a valid field type.";
            public static string ComponentPropertyNameIsEmpty()
                => $"[Error] Component property name is empty. It should be a valid property name.";
            public static string ComponentPropertyTypeIsEmpty()
                => $"[Error] Component property type is empty. It should be a valid property type.";

            public static string InvalidInstanceID(Type holderType, string fieldName)
                => $"[Error] Invalid instanceID '{fieldName}' for '{holderType.GetTypeId()}'. It should be a valid field name.";
        }
    }
}
