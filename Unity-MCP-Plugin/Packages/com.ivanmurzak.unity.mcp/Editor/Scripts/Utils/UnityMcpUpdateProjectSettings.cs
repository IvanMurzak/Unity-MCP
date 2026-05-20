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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Team-shared settings for the Unity-MCP update checker, persisted under
    /// <c>ProjectSettings/AI-Game-Developer-UpdateSettings.asset</c>.
    /// </summary>
    /// <remarks>
    /// Uses Unity's <see cref="ScriptableSingleton{T}"/> + <see cref="FilePathAttribute"/> pattern
    /// for project-scoped editor settings. The asset file is plain YAML, diff-friendly, and
    /// lives in the folder teams commit to VCS — so flipping <see cref="DisableUpdateNotificationsForTeam"/>
    /// once and committing the resulting asset disables the update popup for every team member
    /// who pulls that commit.
    ///
    /// This complements (does not replace) the per-user "Do not show again" flag stored via
    /// <see cref="EditorPrefs"/>. Precedence is enforced in
    /// <see cref="UpdateChecker.ShouldCheckForUpdates"/>: the project flag short-circuits
    /// the check before the per-user flag is consulted.
    ///
    /// See <see href="https://github.com/IvanMurzak/Unity-MCP/issues/768"/>.
    /// </remarks>
    [FilePath("ProjectSettings/AI-Game-Developer-UpdateSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class UnityMcpUpdateProjectSettings : ScriptableSingleton<UnityMcpUpdateProjectSettings>
    {
        [SerializeField] private bool disableUpdateNotificationsForTeam;

        /// <summary>
        /// When <c>true</c>, the update popup is suppressed for every team member who has this
        /// asset in their checkout — regardless of any per-user <see cref="EditorPrefs"/> state.
        /// </summary>
        /// <remarks>
        /// Setter persists immediately via <see cref="ScriptableSingleton{T}.Save"/> with
        /// <c>saveAsText: true</c> so the resulting asset is YAML (diff-friendly).
        /// </remarks>
        public bool DisableUpdateNotificationsForTeam
        {
            get => disableUpdateNotificationsForTeam;
            set
            {
                if (disableUpdateNotificationsForTeam == value)
                    return;
                disableUpdateNotificationsForTeam = value;
                Save(true);
            }
        }
    }
}
