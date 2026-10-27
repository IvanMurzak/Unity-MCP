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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Project-level (team-shared) settings of the NuGet dependency resolver. Stored under
    /// <c>ProjectSettings/</c> so it is committed to VCS and applies to everyone who clones the
    /// project, mirroring <c>AI-Game-Developer-UpdateSettings.asset</c>.
    ///
    /// <para>Lives in the resolver assembly (not the main Editor assembly) because the resolver
    /// must read it before the gated plugin assemblies exist.</para>
    /// </summary>
    [FilePath("ProjectSettings/AI-Game-Developer-NuGetSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class NuGetProjectSettings : ScriptableSingleton<NuGetProjectSettings>
    {
        [SerializeField] private string[] skipPackages = Array.Empty<string>();

        /// <summary>
        /// The skip list the resolver actually applies: <see cref="NuGetConfig.SkipPackages"/>
        /// (shipped with the plugin) merged with <see cref="SkipPackages"/> (this project).
        /// Re-evaluated on every access so a settings change is picked up by the next restore.
        /// Lives here, not in <see cref="NuGetConfig"/>, because that file must stay engine-free.
        /// </summary>
        public static IReadOnlyCollection<string> EffectiveSkipPackages =>
            NuGetSkipList.Merge(NuGetConfig.SkipPackages, instance.SkipPackages);

        /// <summary>
        /// NuGet package IDs the resolver must NOT install (nor their transitive deps), because
        /// the project supplies those assemblies itself — for example through NuGetForUnity.
        /// Matched case-insensitively against the package ID.
        /// </summary>
        public string[] SkipPackages
        {
            get => skipPackages;
            set
            {
                skipPackages = value ?? Array.Empty<string>();
                Save(true);
            }
        }
    }
}
