/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using System.Linq;
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
        public static async ValueTask<ListPromptsResult> List(RequestContext<ListPromptsRequestParams> request, CancellationToken cancellationToken)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Trace("List");

            var mcpServerService = McpServerService.Instance;
            if (mcpServerService == null)
                return new ListPromptsResult().SetError($"[Error] '{nameof(mcpServerService)}' is null");

            var promptRunner = mcpServerService.PromptRunner;
            if (promptRunner == null)
                return new ListPromptsResult().SetError($"[Error] '{nameof(promptRunner)}' is null");

            logger.Trace("Using PromptRunner: {0}", promptRunner.GetType().GetTypeShortName());

            var requestData = new RequestListPrompts();
            var response = await promptRunner.RunListPrompts(requestData, cancellationToken: cancellationToken);
            if (response == null)
                return new ListPromptsResult().SetError($"[Error] '{nameof(response)}' is null");

            if (response.Status == ResponseStatus.Error)
                return new ListPromptsResult().SetError(response.Message ?? "[Error] Got an error during reading resources");

            if (response.Value == null)
                return new ListPromptsResult().SetError("[Error] Resource value is null");

            var result = new ListPromptsResult()
            {
                Prompts = response.Value.Prompts
                    .Where(x => x != null)
                    .Select(x => x!.ToPrompt())
                    .ToList()
            };

            if (logger.IsTraceEnabled)
                logger.Trace("ListAll, result: {0}", result.ToJson(McpPlugin.Instance?.McpRunner.Reflector));

            return result;
        }
    }
}
