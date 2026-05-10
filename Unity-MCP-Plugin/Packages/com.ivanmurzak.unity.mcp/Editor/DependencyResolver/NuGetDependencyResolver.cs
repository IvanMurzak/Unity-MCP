/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.   |
+------------------------------------------------------------------+
*/

#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using PackageManagerEvents = UnityEditor.PackageManager.Events;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Entry point for NuGet dependency management. Runs on every domain reload via [InitializeOnLoad]
    /// AND in response to UPM package upgrades via PackageManager.Events.registeredPackages.
    ///
    /// This assembly has ZERO external dependencies — it always compiles, even when the main plugin
    /// fails due to missing or conflicting DLLs. It downloads NuGet packages directly from nuget.org,
    /// extracts DLLs, skips assemblies Unity already provides, and sets the UNITY_MCP_READY define
    /// so the main plugin assemblies can compile.
    ///
    /// Why both triggers are needed:
    ///   [InitializeOnLoad] fires after a *successful* project-wide recompile. When a user upgrades
    ///   the Unity package and the new code references newer NuGet APIs whose DLL is still on disk
    ///   at the old version, the post-import recompile fails, Unity blocks the domain reload, and
    ///   the new resolver's static constructor never runs. The previous AppDomain is still alive
    ///   though — and so are its event subscriptions. registeredPackages fires from the still-alive
    ///   AppDomain after UPM writes new package files but before the failed recompile, giving us a
    ///   chance to clean up stale DLLs and unblock the next compile attempt. See Unity-MCP#707.
    ///
    /// Flow on domain reload:
    ///   1. [InitializeOnLoad] fires
    ///   2. Deferred via EditorApplication.update (runs without editor focus, unlike delayCall)
    ///   3. NuGetPackageRestorer checks if all packages are installed
    ///   4. Downloads and installs any missing packages, removes stale-version siblings
    ///   5. Sets UNITY_MCP_READY scripting define
    ///   6. If packages were installed: triggers AssetDatabase.Refresh() → domain reload
    ///   7. On next reload: everything is in place, main plugin compiles
    ///
    /// Flow on UPM package change (no domain reload required to invoke):
    ///   1. registeredPackages fires from the previous AppDomain
    ///   2. NuGetPackageRestorer.Restore() runs the same install + cleanup logic
    ///   3. AssetDatabase.Refresh() prompts Unity to retry compilation with the corrected DLL set
    ///   4. On the resulting successful reload, [InitializeOnLoad] resumes the normal path
    /// </summary>
    [InitializeOnLoad]
    static class NuGetDependencyResolver
    {
        const string Tag = "[Unity-MCP DependencyResolver]";

        static NuGetDependencyResolver()
        {
            // In CI, skip runtime resolution — DLLs are committed to git
            // and UNITY_MCP_READY should already be in ProjectSettings.
            // Setting defines at runtime in batch mode causes "Error building Player
            // because scripts are compiling" when the test runner races the recompilation.
            if (IsCi())
            {
                RecompileGate.EnsureReadyDefine();
                return;
            }

            EditorApplication.update += ResolveOnce;

            // Subscribe to UPM package-change events so that when the user upgrades a package
            // whose new code references a newer NuGet DLL than the one currently on disk, we
            // can run the cleanup + restore from the still-alive AppDomain. The compile that
            // would otherwise fail (and block [InitializeOnLoad] in the next domain) gets
            // retried by Unity once AssetDatabase.Refresh() picks up the corrected DLL set.
            // -= before += guards against accidental double-subscription if a hot-reload path
            // ever re-runs this constructor without a full domain swap.
            PackageManagerEvents.registeredPackages -= OnRegisteredPackages;
            PackageManagerEvents.registeredPackages += OnRegisteredPackages;
        }

        /// <summary>
        /// Tracks whether a UPM-event-triggered restore is currently in flight, so a second
        /// event burst (UPM can fire multiple registeredPackages events for a single user
        /// action) doesn't kick off a parallel restore.
        /// </summary>
        static bool isRestoringFromPackageEvent;

        static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            if (isRestoringFromPackageEvent)
                return;

            // Cheap pre-filter: if no packages were added, removed, or upgraded, there is no
            // possibility of a NuGet-dep version change. Skip the heavier AllPackagesInstalled()
            // probe to avoid touching disk on every UPM ping.
            if ((args.added == null || args.added.Count == 0)
                && (args.removed == null || args.removed.Count == 0)
                && (args.changedFrom == null || args.changedFrom.Count == 0)
                && (args.changedTo == null || args.changedTo.Count == 0))
            {
                return;
            }

            isRestoringFromPackageEvent = true;
            try
            {
                Debug.Log($"{Tag} UPM package change detected — running NuGet restore.");
                RunRestoreAndRefresh();
            }
            catch (Exception ex)
            {
                // Same rationale as ResolveOnce: do NOT set UNITY_MCP_READY on a failed restore.
                Debug.LogError($"{Tag} Restore from UPM event failed: {ex}");
            }
            finally
            {
                isRestoringFromPackageEvent = false;
            }
        }

        /// <summary>
        /// Shared core used by both ResolveOnce (post-domain-reload) and OnRegisteredPackages
        /// (no-domain-reload UPM event path). AllPackagesInstalled() short-circuits the common
        /// no-op case without touching the network.
        /// </summary>
        static void RunRestoreAndRefresh()
        {
            if (NuGetPackageRestorer.AllPackagesInstalled())
            {
                NuGetPluginConfigurator.ConfigureAll();
                RecompileGate.EnsureReadyDefine();
                return;
            }

            Debug.Log($"{Tag} Restoring NuGet packages...");
            var changed = NuGetPackageRestorer.Restore();

            NuGetPluginConfigurator.ConfigureAll();

            RecompileGate.EnsureReadyDefine();

            if (changed)
            {
                Debug.Log($"{Tag} Packages restored. Refreshing AssetDatabase...");
                AssetDatabase.Refresh();
            }
        }

        static void ResolveOnce()
        {
            EditorApplication.update -= ResolveOnce;
            try
            {
                RunRestoreAndRefresh();
            }
            catch (Exception ex)
            {
                // Do NOT set UNITY_MCP_READY here: if restore/configuration failed, the DLL
                // layout is unknown/inconsistent, and letting main-plugin assemblies compile
                // against a partial/mismatched set (via defineConstraints) produces hard-to-
                // diagnose MissingMethodException / TypeLoadException at runtime. Surface the
                // failure loud and clear instead, so the user fixes the underlying problem
                // and retries (the next domain reload will run Restore again).
                Debug.LogError($"{Tag} Failed: {ex}");
            }
        }

        /// <summary>
        /// Checks if the current environment is a CI environment.
        /// Mirrors EnvironmentUtils.IsCi() but without external dependencies,
        /// since this assembly must compile standalone.
        /// Checks both command-line arguments and environment variables for
        /// CI, GITHUB_ACTIONS, and TF_BUILD (Azure Pipelines).
        /// </summary>
        static bool IsCi()
        {
            var args = ParseCommandLineArguments();

            var ci = GetArgOrEnv(args, "CI");
            var gha = GetArgOrEnv(args, "GITHUB_ACTIONS");
            var az = GetArgOrEnv(args, "TF_BUILD");

            return IsTrue(ci) || IsTrue(gha) || IsTrue(az);

            static string? GetArgOrEnv(Dictionary<string, string?> args, string key)
                => args.TryGetValue(key, out var v) ? v : Environment.GetEnvironmentVariable(key);

            static bool IsTrue(string? value)
                => string.Equals(value?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parses Unity command-line arguments into a dictionary.
        /// Handles both "-key value" and "-key=value" forms, plus bare flags like "-batchmode".
        /// Keys are stored WITHOUT the leading dash.
        /// </summary>
        static Dictionary<string, string?> ParseCommandLineArguments()
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var rawArgs = Environment.GetCommandLineArgs();

            for (var i = 0; i < rawArgs.Length; i++)
            {
                var arg = rawArgs[i];
                if (!arg.StartsWith("-"))
                    continue;

                var key = arg.TrimStart('-');

                // Handle -key=value form
                var eqIndex = key.IndexOf('=');
                if (eqIndex >= 0)
                {
                    result[key.Substring(0, eqIndex)] = key.Substring(eqIndex + 1);
                    continue;
                }

                // Handle -key value form (next arg is value if it doesn't start with -)
                if (i + 1 < rawArgs.Length && !rawArgs[i + 1].StartsWith("-"))
                {
                    result[key] = rawArgs[++i];
                }
                else
                {
                    // Bare flag like -batchmode
                    result[key] = null;
                }
            }

            return result;
        }

    }
}
