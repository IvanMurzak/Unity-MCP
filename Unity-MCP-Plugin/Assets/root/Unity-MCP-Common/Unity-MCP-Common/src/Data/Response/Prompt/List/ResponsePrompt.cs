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
    public class ResponsePrompt
    {
        public string Name { get; set; } = string.Empty;

        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets an optional description of what this prompt provides.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This description helps developers understand the purpose and use cases for the prompt.
        /// It should explain what the prompt is designed to accomplish and any important context.
        /// </para>
        /// <para>
        /// The description is typically used in documentation, UI displays, and for providing context
        /// to client applications that may need to choose between multiple available prompts.
        /// </para>
        /// </remarks>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a list of arguments that this prompt accepts for templating and customization.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This list defines the arguments that can be provided when requesting the prompt.
        /// Each argument specifies metadata like name, description, and whether it's required.
        /// </para>
        /// <para>
        /// When a client makes a <see cref="RequestMethods.PromptsGet"/> request, it can provide values for these arguments
        /// which will be substituted into the prompt template or otherwise used to render the prompt.
        /// </para>
        /// </remarks>
        public List<ResponsePromptArgument>? Arguments { get; set; }

        public ResponsePrompt() { }

        public ResponsePrompt(string name, string? title, string? description, List<ResponsePromptArgument>? arguments)
        {
            Name = name;
            Title = title;
            Description = description;
            Arguments = arguments;
        }
    }
}
