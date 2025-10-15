/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System;
using System.Reflection;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class ToolMethodData
    {
        public string Name => Attribute.Name;
        public Type ClassType { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public McpPluginToolAttribute Attribute { get; set; }

        public ToolMethodData(Type classType, MethodInfo methodInfo, McpPluginToolAttribute attribute)
        {
            ClassType = classType;
            MethodInfo = methodInfo;
            Attribute = attribute;
        }
    }
}
