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
    public class ResourceMethodData
    {
        public Type ClassType { get; set; }
        public MethodInfo GetContentMethod { get; set; }
        public MethodInfo ListResourcesMethod { get; set; }
        public McpPluginResourceAttribute Attribute { get; set; }

        public ResourceMethodData(Type classType, MethodInfo getContentMethod, MethodInfo listResourcesMethod, McpPluginResourceAttribute attribute)
        {
            ClassType = classType;
            GetContentMethod = getContentMethod;
            ListResourcesMethod = listResourcesMethod;
            Attribute = attribute;
        }
    }
}
