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
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class ResponseCallTool : IResponseCallTool, IRequestID
    {
        public string RequestID { get; set; } = string.Empty;
        public virtual ResponseStatus Status { get; set; } = ResponseStatus.Error;
        public virtual List<ContentBlock> Content { get; set; } = new List<ContentBlock>();
        public virtual string? StructuredContent { get; set; } = null;

        public ResponseCallTool() { }
        public ResponseCallTool(ResponseStatus status, List<ContentBlock> content) : this(
            requestId: string.Empty,
            structuredContent: null,
            status: status,
            content: content)
        {
            // none
        }
        public ResponseCallTool(string structuredContent, ResponseStatus status, List<ContentBlock> content) : this(
            requestId: string.Empty,
            structuredContent: structuredContent,
            status: status,
            content: content)
        {
            // none
        }
        public ResponseCallTool(string requestId, string? structuredContent, ResponseStatus status, List<ContentBlock> content)
        {
            RequestID = requestId;
            Status = status;
            Content = content;
            StructuredContent = structuredContent;
        }

        public ResponseCallTool SetRequestID(string requestId)
        {
            RequestID = requestId;
            return this;
        }

        public string? GetMessage() => Content
            ?.FirstOrDefault(item => item.Type == "text" && !string.IsNullOrEmpty(item.Text))
            ?.Text;

        public static ResponseCallTool Error(Exception exception)
            => Error($"[Error] {exception?.Message}\n{exception?.StackTrace}");

        public static ResponseCallTool Error(string? message = null)
            => new ResponseCallTool(status: ResponseStatus.Error, new List<ContentBlock>
            {
                new ContentBlock()
                {
                    Type = "text",
                    Text = message,
                    MimeType = Consts.MimeType.TextPlain
                }
            });

        public static ResponseCallTool Success(string? message = null)
            => new ResponseCallTool(status: ResponseStatus.Success, new List<ContentBlock>
            {
                new ContentBlock()
                {
                    Type = "text",
                    Text = message,
                    MimeType = Consts.MimeType.TextPlain
                }
            });

        public static ResponseCallTool SuccessStructured(string structuredContent)
            => new ResponseCallTool(
                structuredContent: structuredContent,
                status: ResponseStatus.Success,
                new List<ContentBlock>
                {
                    new ContentBlock()
                    {
                        Type = "text",
                        Text = structuredContent, // needed for MCP backward compatibility: https://modelcontextprotocol.io/specification/2025-06-18/server/tools#structured-content
                        MimeType = Consts.MimeType.TextJson
                    }
                });

        public static ResponseCallTool Processing(string? message = null)
            => new ResponseCallTool(status: ResponseStatus.Processing, new List<ContentBlock>
            {
                new ContentBlock()
                {
                    Type = "text",
                    Text = message,
                    MimeType = Consts.MimeType.TextPlain
                }
            });
    }
}
