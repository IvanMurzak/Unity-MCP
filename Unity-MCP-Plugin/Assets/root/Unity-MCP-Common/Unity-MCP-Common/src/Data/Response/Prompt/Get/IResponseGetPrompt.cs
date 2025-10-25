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

using System.Collections.Generic;

namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public interface IResponseGetPrompt : IRequestID
    {
        ResponseStatus Status { get; set; }
        string? Description { get; set; }
        List<ResponsePromptMessage> Messages { get; set; }
    }
}
