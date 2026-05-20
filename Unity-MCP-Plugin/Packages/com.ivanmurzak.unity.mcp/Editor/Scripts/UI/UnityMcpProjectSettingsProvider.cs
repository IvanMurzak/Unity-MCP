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
#if UNITY_EDITOR
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.UI
{
    /// <summary>
    /// Exposes Unity-MCP's team-shared update settings under
    /// <c>Edit ▸ Project Settings ▸ AI Game Developer</c>.
    /// </summary>
    /// <remarks>
    /// Mutations write through to <see cref="UnityMcpUpdateProjectSettings"/>, whose backing
    /// asset (<c>ProjectSettings/AI-Game-Developer-UpdateSettings.asset</c>) is intended to be
    /// committed to VCS. See https://github.com/IvanMurzak/Unity-MCP/issues/768.
    /// </remarks>
    internal static class UnityMcpProjectSettingsProvider
    {
        private const string SettingsPath = "Project/AI Game Developer";

        [SettingsProvider]
        public static SettingsProvider Create() => new SettingsProvider(SettingsPath, SettingsScope.Project)
        {
            label = "AI Game Developer",
            guiHandler = _ =>
            {
                var settings = UnityMcpUpdateProjectSettings.instance;

                EditorGUILayout.LabelField("Update Notifications", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.ToggleLeft(
                    new GUIContent(
                        "Disable update notifications for the entire team",
                        "Stored in ProjectSettings/ and shared with everyone who clones this project. " +
                        "When enabled, the update popup is suppressed for every team member, regardless " +
                        "of their per-user 'Do not show again' state."),
                    settings.DisableUpdateNotificationsForTeam);
                if (EditorGUI.EndChangeCheck())
                    settings.DisableUpdateNotificationsForTeam = newValue;

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "This setting is stored in ProjectSettings/AI-Game-Developer-UpdateSettings.asset " +
                    "and is shared with everyone who clones this project. The per-user 'Do not show again' " +
                    "button on the update popup is unaffected by this setting.",
                    MessageType.Info);
            },
            keywords = new HashSet<string> { "AI", "MCP", "Update", "Notifications", "Team", "Game", "Developer" }
        };
    }
}
#endif
