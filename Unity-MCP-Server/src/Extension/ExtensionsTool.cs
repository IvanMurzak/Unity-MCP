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
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Common.Model;
using ModelContextProtocol.Protocol;

namespace com.IvanMurzak.Unity.MCP.Server
{
    public static class ExtensionsTool
    {
        static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public static CallToolResult SetError(this CallToolResult target, string message)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.IsError = true;
            target.Content ??= new List<ModelContextProtocol.Protocol.ContentBlock>(1);

            var content = new TextContentBlock()
            {
                Type = "text",
                Text = message
            };

            if (target.Content.Count == 0)
                target.Content.Add(content);
            else
                target.Content[0] = content;

            return target;
        }

        public static ListToolsResult SetError(this ListToolsResult target, string message)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.Tools = new List<Tool>();

            return target;
        }

        public static Tool ToTool(this IResponseListTool response)
        {
            if (_logger.IsTraceEnabled)
            {
                _logger.Trace("Converting IResponseListTool to Tool: {Name}", response.Name);
                _logger.Trace(JsonSerializer.Serialize(response));
            }
            return new Tool()
            {
                Name = response.Name,
                Description = response.Description,
                InputSchema = response.InputSchema,
                OutputSchema = response.OutputSchema,
                Annotations = new()
                {
                    Title = response.Title
                },
            };
        }

        public static CallToolResult ToCallToolResult(this IResponseCallTool response) => new CallToolResult()
        {
            IsError = response.Status == ResponseStatus.Error,
            Content = response.Content
                .Select(x => x.ToTextContent())
                .ToList(),
            StructuredContent = string.IsNullOrEmpty(response.StructuredContent)
                ? null
                : JsonNode.Parse(response.StructuredContent)
        };
    }
}
