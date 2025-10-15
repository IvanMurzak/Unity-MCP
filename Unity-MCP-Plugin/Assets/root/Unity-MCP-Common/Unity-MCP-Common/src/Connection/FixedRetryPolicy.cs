/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class FixedRetryPolicy : IRetryPolicy
    {
        private readonly TimeSpan _delay;

        public FixedRetryPolicy(TimeSpan delay)
        {
            _delay = delay;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return _delay;
        }
    }
}
