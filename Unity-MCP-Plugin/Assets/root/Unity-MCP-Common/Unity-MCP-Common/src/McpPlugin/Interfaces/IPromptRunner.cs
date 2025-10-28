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
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Model;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public interface IPromptRunner
    {
        Observable<Unit> OnPromptsUpdated { get; }
        int EnabledPromptsCount { get; }
        int TotalPromptsCount { get; }
        bool HasPrompt(string name);
        bool AddPrompt(IRunPrompt runner);
        bool RemovePrompt(string name);
        bool IsPromptEnabled(string name);
        bool SetPromptEnabled(string name, bool enabled);
        Task<IResponseData<ResponseGetPrompt>> RunGetPrompt(IRequestGetPrompt request, CancellationToken cancellationToken = default);
        Task<IResponseData<ResponseListPrompts>> RunListPrompts(IRequestListPrompts request, CancellationToken cancellationToken = default);
    }
}
