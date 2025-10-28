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
    public interface IResourceRunner
    {
        Observable<Unit> OnResourcesUpdated { get; }
        int EnabledResourcesCount { get; }
        int TotalResourcesCount { get; }
        bool HasResource(string name);
        bool AddResource(IRunResource resourceParams);
        bool RemoveResource(string name);
        bool IsResourceEnabled(string name);
        bool SetResourceEnabled(string name, bool enabled);
        Task<IResponseData<ResponseResourceContent[]>> RunResourceContent(IRequestResourceContent request, CancellationToken cancellationToken = default);
        Task<IResponseData<ResponseListResource[]>> RunListResources(IRequestListResources request, CancellationToken cancellationToken = default);
        Task<IResponseData<ResponseResourceTemplate[]>> RunResourceTemplates(IRequestListResourceTemplates request, CancellationToken cancellationToken = default);
    }
}
