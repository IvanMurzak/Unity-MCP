/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using com.IvanMurzak.Unity.MCP.Common;
using Microsoft.Extensions.DependencyInjection;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static class ExtensionsMcpServerBuilder
    {
        public static IMcpPluginBuilder WithServerFeatures(this IMcpPluginBuilder builder, DataArguments dataArguments)
        {
            builder.Services.AddRouting();
            if (dataArguments.ClientTransport == Consts.MCP.Server.TransportMethod.stdio)
                builder.Services.AddHostedService<McpServerService>();

            builder.Services.AddSingleton<HubEventToolsChange>();
            builder.Services.AddSingleton<HubEventPromptsChange>();
            builder.Services.AddSingleton<HubEventResourcesChange>();
            builder.Services.AddSingleton<IRequestTrackingService, RequestTrackingService>();
            builder.Services.AddSingleton<IToolRunner, RemoteToolRunner>();
            builder.Services.AddSingleton<IPromptRunner, RemotePromptRunner>();
            builder.Services.AddSingleton<IResourceRunner, RemoteResourceRunner>();

            builder.AddMcpRunner();

            return builder;
        }
    }
}
