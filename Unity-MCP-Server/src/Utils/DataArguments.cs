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
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Common;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public class DataArguments
    {
        public int Port { get; private set; } = 8080;
        public int PluginTimeoutMs { get; private set; }
        public Consts.MCP.Server.TransportMethod ClientTransport { get; private set; }

        public DataArguments(string[] args)
        {
            Port = Consts.Hub.DefaultPort;
            PluginTimeoutMs = Consts.Hub.DefaultTimeoutMs;
            ClientTransport = Consts.MCP.Server.TransportMethod.http;

            ParseEnvironmentVariables(); // env variables - second priority
            ParseCommandLineArguments(args); // command line args - first priority (override previous values)
        }

        void ParseEnvironmentVariables()
        {
            // --- Global variables ---

            var envPort = Environment.GetEnvironmentVariable(Consts.MCP.Server.Env.Port);
            if (envPort != null && int.TryParse(envPort, out var parsedEnvPort))
                Port = parsedEnvPort;

            // --- Plugin variables ---

            var envPluginTimeout = Environment.GetEnvironmentVariable(Consts.MCP.Server.Env.PluginTimeout);
            if (envPluginTimeout != null && int.TryParse(envPluginTimeout, out var parsedEnvTimeoutMs))
                PluginTimeoutMs = parsedEnvTimeoutMs;

            // --- Client variables ---

            var envClientTransport = Environment.GetEnvironmentVariable(Consts.MCP.Server.Env.ClientTransportMethod);
            if (envClientTransport != null && Enum.TryParse(envClientTransport, out Consts.MCP.Server.TransportMethod parsedEnvTransport))
                ClientTransport = parsedEnvTransport;
        }
        void ParseCommandLineArguments(string[] args)
        {
            var commandLineArgs = ArgsUtils.ParseLineArguments(args);

            // --- Global variables ---

            var argPort = commandLineArgs.GetValueOrDefault(Consts.MCP.Server.Args.Port.TrimStart('-'));
            if (argPort != null && int.TryParse(argPort, out var port))
                Port = port;

            // --- Plugin variables ---

            var argPluginTimeout = commandLineArgs.GetValueOrDefault(Consts.MCP.Server.Args.PluginTimeout.TrimStart('-'));
            if (argPluginTimeout != null && int.TryParse(argPluginTimeout, out var timeoutMs))
                PluginTimeoutMs = timeoutMs;

            // --- Client variables ---

            var argClientTransport = commandLineArgs.GetValueOrDefault(Consts.MCP.Server.Args.ClientTransportMethod.TrimStart('-'));
            if (argClientTransport != null && Enum.TryParse(argClientTransport, out Consts.MCP.Server.TransportMethod parsedArgTransport))
                ClientTransport = parsedArgTransport;
        }
    }
}