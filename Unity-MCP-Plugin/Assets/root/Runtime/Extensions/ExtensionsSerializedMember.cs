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
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Data;

namespace com.IvanMurzak.Unity.MCP.Runtime.Extensions
{
    public static class ExtensionsSerializedMember
    {
        public static bool TryGetInstanceID(this SerializedMember member, out int instanceID)
        {
            try
            {
                var objectRef = member.GetValue<ObjectRef>(McpPlugin.McpPlugin.Instance!.McpManager.Reflector);
                if (objectRef != null)
                {
                    instanceID = objectRef.InstanceID;
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions, fallback to instanceID field
            }

            try
            {
                var fieldValue = member.GetField(ObjectRef.ObjectRefProperty.InstanceID);
                if (fieldValue != null)
                {
                    instanceID = fieldValue.GetValue<int>(McpPlugin.McpPlugin.Instance!.McpManager.Reflector);
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions, fallback to instanceID field
            }

            instanceID = 0;
            return false;
        }
        public static bool TryGetGameObjectInstanceID(this SerializedMember member, out int instanceID)
        {
            try
            {
                var objectRef = member.GetValue<GameObjectRef>(McpPlugin.McpPlugin.Instance!.McpManager.Reflector);
                if (objectRef != null)
                {
                    instanceID = objectRef.InstanceID;
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions, fallback to instanceID field
            }

            try
            {
                var fieldValue = member.GetField(ObjectRef.ObjectRefProperty.InstanceID);
                if (fieldValue != null)
                {
                    instanceID = fieldValue.GetValue<int>(McpPlugin.McpPlugin.Instance!.McpManager.Reflector);
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions, fallback to instanceID field
            }

            instanceID = 0;
            return false;
        }
    }
}
