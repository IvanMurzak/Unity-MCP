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
using System.Linq;
using com.IvanMurzak.ReflectorNet;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class PromptRunnerCollection : Dictionary<string, IRunPrompt>
    {
        readonly Reflector reflector;
        readonly ILogger? _logger;

        public PromptRunnerCollection(Reflector reflector, ILogger? logger)
        {
            this.reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _logger = logger;
            _logger?.LogTrace("Ctor.");
        }
        public PromptRunnerCollection Add(IEnumerable<PromptMethodData> methods)
        {
            foreach (var method in methods.Where(resource => !string.IsNullOrEmpty(resource.Attribute?.Name)))
            {
                this[method.Attribute.Name!] = method.MethodInfo.IsStatic
                    ? RunPrompt.CreateFromStaticMethod(reflector, method.Attribute.Name, _logger, method.MethodInfo) as IRunPrompt
                    : RunPrompt.CreateFromClassMethod(reflector, method.Attribute.Name, _logger, method.ClassType, method.MethodInfo) as IRunPrompt;
            }
            return this;
        }
        public PromptRunnerCollection Add(IDictionary<string, IRunPrompt> runners)
        {
            if (runners == null)
                throw new ArgumentNullException(nameof(runners));

            foreach (var runner in runners)
                Add(runner.Key, runner.Value);

            return this;
        }
    }
}
