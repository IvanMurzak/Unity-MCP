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
    public interface IResponseData<T> : IResponseData
    {
        T? Value { get; set; }
    }
    public interface IResponseData : IRequestID
    {
        ResponseStatus Status { get; set; }
        string? Message { get; set; }
    }
}
