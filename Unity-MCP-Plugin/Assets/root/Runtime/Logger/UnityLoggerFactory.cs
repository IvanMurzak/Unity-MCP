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
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Utils
{
    public static class UnityLoggerFactory
    {
        private static ILoggerFactory? _loggerFactory;

        public static ILoggerFactory LoggerFactory
        {
            get
            {
                if (_loggerFactory == null)
                {
                    _loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                    {
                        builder.ClearProviders();
                        builder.AddProvider(new UnityLoggerProvider());
                    });
                }
                return _loggerFactory;
            }
        }
    }
}
