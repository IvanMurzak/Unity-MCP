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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    public partial class Tool_EditorSession
    {
        public const string QuitToolId = "editor-session-quit";

        /// <summary>Lets the answer reach the CLI before the editor goes away.</summary>
        const double QuitDelaySeconds = 0.5;

        [AiTool
        (
            QuitToolId,
            Title = "Editor Session / Quit",
            DestructiveHint = true,
            ToolType = McpToolType.System
        )]
        [AiSkillDescription("Save and quit an editor that a CLI session started (`unity-mcp-cli exec`, `open --headless`). " +
            "Needs the session lease nonce; an editor the user opened is never closed. Used by `unity-mcp-cli close`.")]
        [AiSkillBody("Save and quit an editor that a CLI session started.\n\n" +
            "## Inputs\n\n" +
            "- `nonce` — the `nonce` of the project's session lease (`.ai-game-dev/session.json`), which the CLI also " +
            "passed to the editor as `AIGD_SESSION_NONCE`.\n\n" +
            "## Behavior\n\n" +
            "Refuses unless this editor was started by a CLI session (`AIGD_SESSION_OWNER=cli`) with the same nonce. " +
            "Otherwise answers right away, then saves changed scenes that have a file and changed assets and " +
            "exits — as the idle shutdown does. Unlike the OS polite-quit signal it also reaches a headless " +
            "(`-batchmode`) editor. Prefer `unity-mcp-cli close --owned-only` over calling it directly.")]
        [Description("Save and quit this editor if a CLI session started it and the nonce matches its session lease.")]
        public string Quit
        (
            [Description("The nonce of the project's CLI session lease (.ai-game-dev/session.json).")]
            string nonce
        )
        {
            var own = Startup.CliSessionNonce;
            if (own == null)
                throw new InvalidOperationException("This editor was not started by a CLI session; it is not closed this way.");
            if (!string.Equals(nonce, own, StringComparison.Ordinal))
                throw new ArgumentException("The nonce does not match this editor's CLI session lease.", nameof(nonce));

            MainThread.Instance.Run(() =>
            {
                var quitAt = EditorApplication.timeSinceStartup + QuitDelaySeconds;
                void QuitWhenDue()
                {
                    if (EditorApplication.timeSinceStartup < quitAt)
                        return;
                    EditorApplication.update -= QuitWhenDue;
                    Startup.SaveAndQuit();
                }
                EditorApplication.update += QuitWhenDue;
            });
            return "Saving and quitting.";
        }
    }
}
