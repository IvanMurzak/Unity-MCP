/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
namespace com.IvanMurzak.Unity.MCP.Common.Model
{
    public enum ResponseStatus
    {
        Error, // request failed
        Success, // request completed successfully
        Processing // the request needs longer processing. It will be callback later, please wait
    }
}
