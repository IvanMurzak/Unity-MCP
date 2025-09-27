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
using System.Linq;
using ModelContextProtocol.Protocol;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static class ExtensionsPrompt
    {
        public static GetPromptResult SetError(this GetPromptResult target, string message)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.Description = message;
            target.Messages = new List<PromptMessage>();

            return target;
        }

        public static ListPromptsResult SetError(this ListPromptsResult target, string message)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.Prompts = new List<Prompt>()
            {
                new Prompt()
                {
                    Name = "Error",
                    Description = message
                }
            };

            return target;
        }

        public static Prompt ToPrompt(this Common.Model.ResponsePrompt response) => new Prompt()
        {
            Name = response.Name,
            Title = response.Title,
            Description = response.Description,
            Arguments = response.Arguments?.Select(x => x.ToPromptArgument()).ToList()
        };

        public static PromptMessage ToPromptMessage(this Common.Model.ResponsePromptMessage promptMessage) => new PromptMessage()
        {
            Role = promptMessage.Role switch
            {
                Common.Model.Role.User => Role.User,
                Common.Model.Role.Assistant => Role.Assistant,
                _ => throw new ArgumentOutOfRangeException(nameof(promptMessage.Role), $"Invalid role value: {promptMessage.Role}")
            },
            Content = promptMessage.Content.ToTextContent()
        };

        public static PromptArgument ToPromptArgument(this Common.Model.ResponsePromptArgument promptArgument) => new PromptArgument()
        {
            Name = promptArgument.Name,
            Title = promptArgument.Title,
            Description = promptArgument.Description,
            Required = promptArgument.Required,
        };

        public static GetPromptResult ToGetPromptResult(this Common.Model.IResponseGetPrompt response) => new GetPromptResult()
        {
            Description = response.Description,
            Messages = response.Messages
                .Select(x => x.ToPromptMessage())
                .ToList()
        };
    }
}
