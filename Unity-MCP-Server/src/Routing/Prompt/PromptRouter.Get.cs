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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NLog;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static partial class PromptRouter
    {
        public static async ValueTask<GetPromptResult> Get(RequestContext<GetPromptRequestParams> request, CancellationToken cancellationToken)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Trace("{0}.Get", nameof(PromptRouter));

            if (request == null)
                return new GetPromptResult().SetError("[Error] Request is null");

            if (request.Params == null)
                return new GetPromptResult().SetError("[Error] Request.Params is null");

            if (request.Params.Arguments == null)
                return new GetPromptResult().SetError("[Error] Request.Params.Arguments is null");

            var mcpServerService = McpServerService.Instance;
            if (mcpServerService == null)
                return new GetPromptResult().SetError($"[Error] '{nameof(mcpServerService)}' instance is null");

            var promptRunner = mcpServerService.PromptRunner;
            if (promptRunner == null)
                return new GetPromptResult().SetError($"[Error] '{nameof(promptRunner)}' is null");

            logger.Trace("Using PromptRunner: {0}", promptRunner?.GetType().GetTypeShortName());

            if (promptRunner == null)
                return new GetPromptResult().SetError($"[Error] '{nameof(promptRunner)}' is null");

            var requestData = new RequestGetPrompt(request.Params.Name, request.Params.Arguments);
            if (logger.IsTraceEnabled)
                logger.Trace("Get remote prompt '{0}':\n{1}", request.Params.Name, requestData.ToJsonOrEmptyJsonObject(McpPlugin.Instance?.McpRunner.Reflector));

            var response = await promptRunner.RunGetPrompt(requestData, cancellationToken: cancellationToken);
            if (response == null)
                return new GetPromptResult().SetError($"[Error] '{nameof(response)}' is null");

            if (logger.IsTraceEnabled)
                logger.Trace("Get prompt response:\n{0}", response.ToJsonOrEmptyJsonObject(McpPlugin.Instance?.McpRunner.Reflector));

            if (response.Status == ResponseStatus.Error)
                return new GetPromptResult().SetError(response.Message ?? "[Error] Got an error during running tool");

            if (response.Value == null)
                return new GetPromptResult().SetError("[Error] Prompt returned null value");

            return response.Value.ToGetPromptResult();
        }

        public static ValueTask<GetPromptResult> Get(string name, Action<Dictionary<string, object>>? configureArguments = null)
        {
            var arguments = new Dictionary<string, object>();
            configureArguments?.Invoke(arguments);

            return GetWithJson(name, args =>
            {
                foreach (var kvp in arguments)
                    args[kvp.Key] = kvp.Value.ToJsonElement(McpPlugin.Instance?.McpRunner.Reflector);
            });
        }

        public static ValueTask<GetPromptResult> GetWithJson(string name, Action<Dictionary<string, JsonElement>>? configureArguments = null)
        {
            var mcpServer = McpServerService.Instance?.McpServer;
            if (mcpServer == null)
                throw new InvalidOperationException("[Error] 'McpServer' is null");

            var arguments = new Dictionary<string, JsonElement>();
            configureArguments?.Invoke(arguments);

            var request = new RequestContext<GetPromptRequestParams>(mcpServer)
            {
                Params = new GetPromptRequestParams()
                {
                    Name = name,
                    Arguments = arguments
                }
            };
            return Get(request, default);
        }
    }
}
