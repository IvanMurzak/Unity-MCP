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
using com.IvanMurzak.ReflectorNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public interface IMcpPluginBuilder
    {
        IServiceCollection Services { get; }
        IMcpPluginBuilder WithTool(Type classType, MethodInfo method);
        IMcpPluginBuilder WithTool(McpPluginToolAttribute attribute, Type classType, MethodInfo method);
        IMcpPluginBuilder WithTool(string name, string? title, Type classType, MethodInfo method);
        IMcpPluginBuilder AddTool(string name, IRunTool runner);

        IMcpPluginBuilder WithPrompt(string name, Type classType, MethodInfo method);
        IMcpPluginBuilder AddPrompt(string name, IRunPrompt runner);

        IMcpPluginBuilder WithResource(Type classType, MethodInfo getContentMethod);
        IMcpPluginBuilder AddResource(IRunResource resourceParams);

        IMcpPluginBuilder AddLogging(Action<ILoggingBuilder> loggingBuilder);
        IMcpPluginBuilder WithConfig(Action<ConnectionConfig> config);
        IMcpPlugin Build(Reflector reflector);
    }
}
