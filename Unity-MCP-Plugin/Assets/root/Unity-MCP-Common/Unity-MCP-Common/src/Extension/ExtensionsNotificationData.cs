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
using com.IvanMurzak.Unity.MCP.Common.Model;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static class ExtensionsNotificationData
    {
        public static IRequestNotification SetName(this IRequestNotification data, string name)
        {
            data.Name = name;
            return data;
        }
        public static IRequestNotification SetOrAddParameter(this IRequestNotification data, string name, object? value)
        {
            data.Parameters ??= new Dictionary<string, object?>();
            data.Parameters[name] = value;
            return data;
        }
        // public static IRequestCallTool Build(this IRequestNotification data)
        //     => new RequestData(data as RequestNotification ?? throw new System.InvalidOperationException("NotificationData is null"));
    }
}
