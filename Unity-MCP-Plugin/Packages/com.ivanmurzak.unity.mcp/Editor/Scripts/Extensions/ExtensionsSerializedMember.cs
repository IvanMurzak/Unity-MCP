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
#if UNITY_6000_5_OR_NEWER
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Runtime.Data;
using R3;

namespace com.IvanMurzak.Unity.MCP.Editor.Extensions
{
    public static class ExtensionsSerializedMember
    {
        public static bool TryGetInstanceID(this SerializedMember member, out UnityEngine.EntityId entityId)
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector;
            if (reflector == null)
            {
                entityId = UnityEngine.EntityId.None;
                return false;
            }

            try
            {
                var objectRef = member.GetValue<ObjectRef>(reflector);
                if (objectRef != null)
                {
                    entityId = objectRef.InstanceID;
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
                    entityId = fieldValue.GetValue<UnityEngine.EntityId>(reflector);
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions, fallback to instanceID field
            }

            entityId = UnityEngine.EntityId.None;
            return false;
        }
        public static bool TryGetGameObjectInstanceID(this SerializedMember member, out UnityEngine.EntityId entityId)
        {
            var reflector = UnityMcpPluginEditor.Instance.Reflector;
            if (reflector == null)
            {
                entityId = UnityEngine.EntityId.None;
                return false;
            }

            try
            {
                var objectRef = member.GetValue<GameObjectRef>(reflector);
                if (objectRef != null)
                {
                    entityId = objectRef.InstanceID;
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
                    entityId = fieldValue.GetValue<UnityEngine.EntityId>(reflector);
                    return true;
                }
            }
            catch
            {
                // Ignore exceptions, fallback to instanceID field
            }

            entityId = UnityEngine.EntityId.None;
            return false;
        }
    }
}
#endif
