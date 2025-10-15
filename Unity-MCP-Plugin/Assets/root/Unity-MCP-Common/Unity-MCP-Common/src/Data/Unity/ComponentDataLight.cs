/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
namespace com.IvanMurzak.Unity.MCP.Common.Model.Unity
{
    [System.Serializable]
    public class ComponentDataLight
    {
        public string typeName { get; set; } = string.Empty;
        public Enabled isEnabled { get; set; }
        public int instanceID { get; set; }

        public ComponentDataLight() { }

        public enum Enabled
        {
            NA = -1,
            False = 0,
            True = 1
        }
    }
    public static class ComponentDataLightExtension
    {
        public static bool IsEnabled(this ComponentDataLight componentData)
            => componentData.isEnabled == ComponentDataLight.Enabled.True;
    }
    public static class ComponentDataLightEnabledExtension
    {
        public static bool ToBool(this ComponentDataLight.Enabled enabled)
            => enabled == ComponentDataLight.Enabled.True;
    }
}
