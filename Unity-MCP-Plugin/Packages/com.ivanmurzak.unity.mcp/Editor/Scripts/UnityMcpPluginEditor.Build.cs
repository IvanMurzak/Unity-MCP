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
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP
{
    public partial class UnityMcpPluginEditor
    {
        public UnityMcpPluginEditor BuildMcpPluginIfNeeded()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var loggerProvider = BuildLoggerProvider();
            var built = _plugin.BuildOnce(() => BuildMcpPlugin(
                version: BuildVersion(),
                reflector: CreateDefaultReflector(),
                loggerProvider: loggerProvider,
                configure: builder =>
                {
                    builder.WithSkillFileGenerator(new UnitySkillFileGenerator(loggerProvider?.CreateLogger(nameof(UnitySkillFileGenerator))));
                }
            ));
            stopwatch.Stop();

            if (built == null)
                return this; // already built, nothing to wire up

            _logger.LogDebug("MCP Plugin built in {elapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);

            SetCurrentPlugin(built);
            ApplyConfigToMcpPlugin(built);

            return this;
        }

        public void DisposeMcpPluginInstance()
        {
            var oldInstance = _plugin.TakeInstance();
            if (oldInstance == null)
                return;

            SetCurrentPlugin(null);

            // Dispose on a background thread to avoid blocking Unity's main thread.
            // The dispose path calls ConnectionManager.DisconnectImmediate() which
            // internally blocks on a semaphore (_gate) held by a pending Connect()
            // task whose continuation is queued on the main thread SynchronizationContext —
            // a deadlock if we block the main thread here.
            _ = System.Threading.Tasks.Task.Run(() => oldInstance.Dispose());
        }
    }
}
