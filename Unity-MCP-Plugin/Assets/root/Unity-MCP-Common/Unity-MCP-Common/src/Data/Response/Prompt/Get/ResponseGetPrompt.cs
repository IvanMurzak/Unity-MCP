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

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class ResponseGetPrompt : IResponseGetPrompt, IRequestID
    {
        public string RequestID { get; set; } = string.Empty;
        public virtual ResponseStatus Status { get; set; } = ResponseStatus.Error;
        public virtual string? Description { get; set; }
        public virtual List<ResponsePromptMessage> Messages { get; set; } = new List<ResponsePromptMessage>();

        public ResponseGetPrompt() { }
        public ResponseGetPrompt(ResponseStatus status, List<ResponsePromptMessage> messages, string? description = null) : this(string.Empty, status, messages, description)
        {
            // none
        }
        public ResponseGetPrompt(string requestId, ResponseStatus status, List<ResponsePromptMessage> messages, string? description = null)
        {
            RequestID = requestId;
            Status = status;
            Messages = messages;
            Description = description;
        }

        public ResponseGetPrompt SetRequestID(string requestId)
        {
            RequestID = requestId;
            return this;
        }

        public string? GetMessage() => Description;

        public static ResponseGetPrompt Error(Exception exception)
            => Error($"[Error] {exception?.Message}\n{exception?.StackTrace}");

        public static ResponseGetPrompt Error(string? description = null)
            => new ResponseGetPrompt(
                status: ResponseStatus.Error,
                description: description,
                messages: new List<ResponsePromptMessage>());

        public static ResponseGetPrompt Success(string message, Role role, string? description = null)
            => Success(
                description: description,
                messages: new List<ResponsePromptMessage> { new ResponsePromptMessage(message, role) });

        public static ResponseGetPrompt Success(ResponsePromptMessage message, string? description = null)
            => Success(
                description: description,
                messages: new List<ResponsePromptMessage> { message });

        public static ResponseGetPrompt Success(List<ResponsePromptMessage> messages, string? description = null)
            => new ResponseGetPrompt(
                status: ResponseStatus.Success,
                description: description,
                messages: messages ?? new List<ResponsePromptMessage>());

        public static ResponseGetPrompt Processing(List<ResponsePromptMessage>? messages = null, string? description = null)
            => new ResponseGetPrompt(
                status: ResponseStatus.Processing,
                description: description,
                messages: messages ?? new List<ResponsePromptMessage>());
    }
}
