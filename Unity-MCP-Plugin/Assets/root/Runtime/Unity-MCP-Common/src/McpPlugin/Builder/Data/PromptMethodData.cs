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
using System.Reflection;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class PromptMethodData
    {
        public string Name => Attribute.Name;
        public Type ClassType { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public McpPluginPromptAttribute Attribute { get; set; }

        public PromptMethodData(Type classType, MethodInfo methodInfo, McpPluginPromptAttribute attribute)
        {
            ClassType = classType;
            MethodInfo = methodInfo;
            Attribute = attribute;
        }
    }
}
