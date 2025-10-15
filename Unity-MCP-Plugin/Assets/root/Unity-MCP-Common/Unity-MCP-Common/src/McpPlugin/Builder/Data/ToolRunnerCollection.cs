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
    public class ToolRunnerCollection : Dictionary<string, IRunTool>
    {
        readonly Reflector reflector;
        readonly ILogger? _logger;

        public ToolRunnerCollection(Reflector reflector, ILogger? logger)
        {
            this.reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _logger = logger;
            _logger?.LogTrace("Ctor.");
        }
        public ToolRunnerCollection Add(IEnumerable<ToolMethodData> methods)
        {
            foreach (var method in methods.Where(resource => !string.IsNullOrEmpty(resource.Attribute?.Name)))
            {
                this[method.Attribute.Name!] = method.MethodInfo.IsStatic
                    ? RunTool.CreateFromStaticMethod(reflector, _logger, method.MethodInfo, method.Attribute.Title) as IRunTool
                    : RunTool.CreateFromClassMethod(reflector, _logger, method.ClassType, method.MethodInfo, method.Attribute.Title);
            }
            return this;
        }
        public ToolRunnerCollection Add(IDictionary<string, IRunTool> runners)
        {
            if (runners == null)
                throw new ArgumentNullException(nameof(runners));

            foreach (var runner in runners)
                Add(runner.Key, runner.Value);

            return this;
        }
    }
}
