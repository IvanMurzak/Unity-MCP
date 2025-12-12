/*
┌────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                   │
│  Repository: GitHub (https://github.com/IvanMurzak/MCP-Plugin-dotnet)  │
│  Copyright (c) 2025 Ivan Murzak                                        │
│  Licensed under the Apache License, Version 2.0.                       │
│  See the LICENSE file in the project root for more information.        │
└────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable

using com.IvanMurzak.ReflectorNet;

namespace com.IvanMurzak.Unity.MCP.Runtime.Data
{
    [System.Serializable]
    public class ComponentDataShallow
    {
        public int instanceID { get; set; }
        public string typeName { get; set; } = string.Empty;
        public Enabled isEnabled { get; set; }

        public ComponentDataShallow() { }
        public ComponentDataShallow(UnityEngine.Component component)
        {
            instanceID = component.GetInstanceID();
            typeName = component.GetType().GetTypeName();
            isEnabled = component is UnityEngine.Behaviour behaviour
                ? (behaviour.enabled ? Enabled.True : Enabled.False)
                : Enabled.NA;
        }

        public enum Enabled
        {
            NA = -1,
            False = 0,
            True = 1
        }
    }
    public static class ComponentDataLightExtension
    {
        public static bool IsEnabled(this ComponentDataShallow componentData)
            => componentData.isEnabled == ComponentDataShallow.Enabled.True;
    }
    public static class ComponentDataLightEnabledExtension
    {
        public static bool ToBool(this ComponentDataShallow.Enabled enabled)
            => enabled == ComponentDataShallow.Enabled.True;
    }
}
