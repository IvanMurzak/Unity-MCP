#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Data.Unity;
using com.IvanMurzak.Unity.MCP.Common.Data.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_GameObject
    {
        [McpPluginTool
        (
            "GameObject_ModifyComponent",
            Title = "Modify Component at GameObject",
            Description = "Modify existed component at GameObject."
        )]
        public string ModifyComponent
        (
            [Description(@"Json Object with required readonly 'instanceId' and 'type' fields.
Any other field would be used for changing value in the target component.
Only required to modify properties and fields and with 'Type' field at the root.
It should respect the original structure of the component.
Nested 'instanceId' fields and properties are references to UnityEngine.Object types.
The target reference instance could be located in project assets, in the scene or in the prefabs.")]
            ComponentData data,
            [Description("GameObject by 'instanceId'. Priority: 1. (Recommended)")]
            int? instanceId = null,
            [Description("GameObject by 'path'. Priority: 2.")]
            string? path = null,
            [Description("GameObject by 'name'. Priority: 3.")]
            string? name = null
        )
        => MainThread.Run(() =>
        {
            if (string.IsNullOrEmpty(data?.type))
                return Error.InvalidComponentType(data?.type);

            var type = TypeUtils.GetType(data.type);
            if (type == null)
                return Error.InvalidComponentType(data.type);

            var go = GameObjectUtils.FindBy(instanceId, path, name, out var error);
            if (error != null)
                return error;

            var allComponents = go.GetComponents<UnityEngine.Component>();
            var component = allComponents.FirstOrDefault(c => c.GetInstanceID() == data.instanceId);
            if (component == null)
                return Error.NotFoundComponent(data.instanceId, allComponents);

            var castedComponent = TypeUtils.CastTo(component, data.type, out error);
            if (error != null)
                return error;

            if (!type.IsAssignableFrom(component.GetType()))
                return Error.TypeMismatch(data.type, component.GetType().FullName);

            // Enable/Disable component if needed
            if (castedComponent is UnityEngine.Behaviour bh)
            {
                var setEnabled = data.isEnabled.ToBool();
                if (bh.enabled != setEnabled)
                    bh.enabled = setEnabled;
            }

            var changedFields = new List<string>(data.fields?.Count ?? 0);
            var changedProperties = new List<string>(data.properties?.Count ?? 0);

            if ((data.fields?.Count ?? 0) > 0)
            {
                // Validate fields
                foreach (var field in data.fields)
                {
                    if (string.IsNullOrEmpty(field.name))
                        return Error.ComponentFieldNameIsEmpty();

                    if (string.IsNullOrEmpty(field.type))
                        return Error.ComponentFieldTypeIsEmpty();
                }

                // Modify fields
                foreach (var field in data.fields)
                {
                    var targetType = TypeUtils.GetType(field.type);
                    if (targetType == null)
                    {
                        if (McpPluginUnity.IsLogActive(LogLevel.Error))
                            Debug.LogError($"[Error] Type '{field.type}' not found. Can't modify field '{field.name}' in component '{data.instanceId}' at GameObject with 'instanceId'={go.GetInstanceID()}.", go);
                    }

                    var fieldInfo = type.GetField(field.name);
                    if (fieldInfo == null)
                    {
                        if (McpPluginUnity.IsLogActive(LogLevel.Warning))
                            Debug.LogWarning($"[Error] Field '{field.name}' not found. Can't modify field '{field.name}' in component '{data.instanceId}' at GameObject with 'instanceId'={go.GetInstanceID()}.", go);
                        continue;
                    }
                    if (targetType == null)
                        return Error.InvalidComponentFieldType(field, fieldInfo);

                    // The `targetType` is a UnityEngine.Object type, so it should be handled differently.
                    if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                    {
                        var referenceInstanceId = JsonUtils.Deserialize<InstanceId>(field.valueJsonElement.Value).instanceId;
                        if (referenceInstanceId == 0)
                            return Error.InvalidInstanceId(targetType, field.name);

                        // Find the object by instanceId
                        var referenceObject = UnityEditor.EditorUtility.InstanceIDToObject(referenceInstanceId);

                        if (typeof(Sprite).IsAssignableFrom(targetType) && referenceObject is Texture2D texture)
                        {
                            // Try to find the first Sprite sub-asset in the Texture2D asset
                            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(texture);
                            var sprite = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                .OfType<Sprite>()
                                .FirstOrDefault();
                            referenceObject = sprite;
                            fieldInfo.SetValue(component, referenceObject);
                        }
                        else
                        {
                            // Cast the object to the target type
                            var castedObject = TypeUtils.CastTo(referenceObject, targetType, out error);
                            if (error != null)
                                return error;

                            fieldInfo.SetValue(component, castedObject);
                        }
                    }
                    else
                    {
                        var fieldValue = JsonUtility.FromJson(field.valueJsonElement.Value.GetRawText(), targetType);
                        fieldInfo.SetValue(component, fieldValue);
                    }
                    changedFields.Add(field.name);
                }
            }

            if ((data.properties?.Count ?? 0) > 0)
            {
                // Validate properties
                foreach (var property in data.properties)
                {
                    if (string.IsNullOrEmpty(property.name))
                        return Error.ComponentPropertyNameIsEmpty();

                    if (string.IsNullOrEmpty(property.type))
                        return Error.ComponentPropertyTypeIsEmpty();
                }
                // Modify properties
                foreach (var property in data.properties)
                {
                    var targetType = TypeUtils.GetType(property.type);
                    if (targetType == null)
                    {
                        if (McpPluginUnity.IsLogActive(LogLevel.Warning))
                            Debug.LogWarning($"[Error] Type '{property.type}' not found. Can't modify property '{property.name}' in component '{data.instanceId}' at GameObject with 'instanceId'={go.GetInstanceID()}.", go);
                    }

                    var propInfo = type.GetProperty(property.name);
                    if (propInfo == null)
                    {
                        if (McpPluginUnity.IsLogActive(LogLevel.Warning))
                            Debug.LogWarning($"[Error] Property '{property.name}' not found. Can't modify property '{property.name}' in component '{data.instanceId}' at GameObject with 'instanceId'={go.GetInstanceID()}.", go);
                        continue;
                    }
                    if (!propInfo.CanWrite)
                    {
                        if (McpPluginUnity.IsLogActive(LogLevel.Warning))
                            Debug.LogWarning($"[Warning] Property '{property.name}' is not writable. Can't modify property '{property.name}' in component '{data.instanceId}' at GameObject with 'instanceId'={go.GetInstanceID()}.", go);
                        continue;
                    }

                    if (targetType == null)
                        return Error.InvalidComponentPropertyType(property, propInfo);

                    // The `targetType` is a UnityEngine.Object type, so it should be handled differently.
                    if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                    {
                        var referenceInstanceId = JsonUtils.Deserialize<InstanceId>(property.valueJsonElement.Value).instanceId;
                        if (referenceInstanceId == 0)
                            return Error.InvalidInstanceId(targetType, property.name);

                        // Find the object by instanceId
                        var referenceObject = UnityEditor.EditorUtility.InstanceIDToObject(referenceInstanceId);

                        if (typeof(Sprite).IsAssignableFrom(targetType) && referenceObject is Texture2D texture)
                        {
                            // Try to find the first Sprite sub-asset in the Texture2D asset
                            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(texture);
                            var sprite = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                .OfType<Sprite>()
                                .FirstOrDefault();
                            referenceObject = sprite;
                            propInfo.SetValue(component, referenceObject);
                        }
                        else
                        {
                            // Cast the object to the target type
                            var castedObject = TypeUtils.CastTo(referenceObject, targetType, out error);
                            if (error != null)
                                return error;

                            propInfo.SetValue(component, castedObject);
                        }
                    }
                    else
                    {
                        var propValue = JsonUtility.FromJson(property.valueJsonElement.Value.GetRawText(), targetType);
                        propInfo.SetValue(component, propValue);

                    }
                    changedProperties.Add(property.name);
                }
            }

            var changedFieldsString = string.Join(", ", changedFields);
            var changedPropertiesString = string.Join(", ", changedProperties);

            return @$"[Success] Modify fields:{changedFields.Count}, properties:{changedProperties.Count} at component '{data.instanceId}' with:
Modified fields:
[{changedFieldsString}]
----------------------------
Modified Properties:
[{changedPropertiesString}]
----------------------------
at GameObject.
{go.Print()}";
        });
    }
}