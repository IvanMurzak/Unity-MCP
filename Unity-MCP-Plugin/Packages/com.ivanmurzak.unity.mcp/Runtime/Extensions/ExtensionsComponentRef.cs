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
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Runtime.Data;

namespace com.IvanMurzak.Unity.MCP.Runtime.Extensions
{
    public static class ExtensionsComponentRef
    {
        public static bool Matches(this ComponentRef componentRef, UnityEngine.Component component, int? index = null)
        {
            if (componentRef.InstanceID != UnityEngine.EntityId.None)
            {
                return componentRef.InstanceID == component?.GetEntityId();
            }
            if (componentRef.Index >= 0 && index != null)
            {
                return componentRef.Index == index.Value;
            }
            if (!StringUtils.IsNullOrEmpty(componentRef.TypeName))
            {
                var type = component?.GetType() ?? typeof(UnityEngine.Component);
                return type.IsMatch(componentRef.TypeName);
            }
            if (componentRef.InstanceID == UnityEngine.EntityId.None && component == null)
            {
                return true; // Matches null component
            }
            return false;
        }
    }
}
#endif
