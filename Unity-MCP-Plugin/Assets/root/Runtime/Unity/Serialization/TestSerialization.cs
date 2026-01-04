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
using com.IvanMurzak.McpPlugin.Common.Utils;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public class TestSerialization : MonoBehaviour
    {
        [SerializeField] UnityEngine.Object? target;
        [SerializeField] bool recursive = false;

        [Header("Automatic trigger")]
        [SerializeField] bool onAwake = false;
        [SerializeField] bool onEnable = false;
        [SerializeField] bool onStart = false;

        private void Awake()
        {
            if (onAwake)
            {
                SerializeTarget();
            }
        }
        private void OnEnable()
        {
            if (onEnable)
            {
                SerializeTarget();
            }
        }
        private void Start()
        {
            if (onStart)
            {
                SerializeTarget();
            }
        }

        public void SerializeTarget()
        {
            var logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(TestSerialization));
            var reflector = UnityMcpPlugin.Instance.Reflector ?? throw new InvalidOperationException("Reflector is null");

            logger.LogInformation($"Serializing target '{target?.name}' of type '{target?.GetType().GetTypeId()}' with recursive={recursive}");

            var serialized = reflector.Serialize(
                obj: target,
                fallbackType: null,
                name: target?.name,
                recursive: recursive,
                context: null,
                logger: logger);

            logger.LogInformation(serialized.ToPrettyJson());
        }
    }
}
