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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Project Settings page for <see cref="NuGetProjectSettings"/>. Registered from the
    /// resolver assembly so it is available even while the gated plugin assemblies are not
    /// compiled — which is exactly when a wrong skip list needs fixing.
    /// </summary>
    internal static class NuGetProjectSettingsProvider
    {
        private const string SettingsPath = "Project/AI Game Developer/NuGet";

        [SettingsProvider]
        public static SettingsProvider Create() => new SettingsProvider(SettingsPath, SettingsScope.Project)
        {
            label = "NuGet",
            guiHandler = _ =>
            {
                EditorGUILayout.LabelField("Skip packages", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "NuGet package IDs the dependency resolver must not install, because this project " +
                    "supplies those assemblies itself (for example through NuGetForUnity). Skipped packages " +
                    "and their transitive dependencies are removed from " + NuGetConfig.InstallPath + ". " +
                    "Stored in ProjectSettings/AI-Game-Developer-NuGetSettings.asset and shared with everyone " +
                    "who clones this project. Applied by the next restore: " + NuGetResolverMenu.MenuPath + ".",
                    MessageType.Info);

                var settings = NuGetProjectSettings.instance;
                var packages = new List<string>(settings.SkipPackages);
                var changed = false;

                for (var i = 0; i < packages.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    var edited = EditorGUILayout.TextField(packages[i]);
                    if (edited != packages[i])
                    {
                        packages[i] = edited;
                        changed = true;
                    }
                    if (GUILayout.Button("-", GUILayout.Width(24)))
                    {
                        packages.RemoveAt(i);
                        changed = true;
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add package ID", GUILayout.Width(120)))
                {
                    packages.Add(string.Empty);
                    changed = true;
                }

                if (changed)
                    settings.SkipPackages = packages.ToArray();
            },
            keywords = new HashSet<string>
            {
                "AI", "MCP", "Unity-MCP", "NuGet", "Skip", "Packages", "Dependencies", "NuGetForUnity", "Game", "Developer"
            }
        };
    }
}
