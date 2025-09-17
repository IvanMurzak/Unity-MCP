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

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public class ResponsePromptArgument
    {
        public string Name { get; set; } = string.Empty;

        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets a human-readable description of the argument's purpose and expected values.
        /// </summary>
        /// <remarks>
        /// This description helps developers understand what information should be provided
        /// for this argument and how it will affect the generated prompt.
        /// </remarks>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets an indication as to whether this argument must be provided when requesting the prompt.
        /// </summary>
        /// <remarks>
        /// When set to <see langword="true"/>, the client must include this argument when making a <see cref="RequestMethods.PromptsGet"/> request.
        /// If a required argument is missing, the server should respond with an error.
        /// </remarks>
        public bool? Required { get; set; }

        public ResponsePromptArgument() { }
    }
}
