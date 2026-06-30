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
using UnityEditor.PackageManager;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageManagerEvents = UnityEditor.PackageManager.Events;

namespace com.IvanMurzak.Unity.MCP.Editor
{
    /// <summary>
    /// Re-checks (and, if needed, re-downloads) the pinned GameDev-MCP-Server binary whenever the Unity-MCP
    /// UPM package itself is added/updated — issue #845: "re-download/version-check on package update".
    ///
    /// <para>Why a dedicated <c>registeredPackages</c> watcher in addition to
    /// <see cref="McpServerManager"/>'s own <c>[InitializeOnLoad]</c> check: the server-version pin
    /// (<see cref="McpServerManager.ServerVersion"/>) is bumped IN this package's code, so updating the package
    /// can change which server release should be on disk. <c>PackageManager.Events.registeredPackages</c> fires
    /// from the still-alive previous AppDomain right after UPM writes the new package files — i.e. BEFORE the
    /// recompile/domain reload that would re-run the manager's static constructor — so the binary re-check is
    /// triggered promptly on a package upgrade even when the subsequent reload is delayed or blocked. The
    /// underlying <see cref="McpServerManager.DownloadServerBinaryIfNeeded"/> is a no-op when the cached binary
    /// already matches the pin, so this watcher is safe to fire on any package change. Mirrors the
    /// subscription pattern in <c>NuGetDependencyResolver</c> (see Unity-MCP#707).</para>
    /// </summary>
    [InitializeOnLoad]
    static class ServerBinaryUpdateWatcher
    {
        const string Tag = "[Unity-MCP ServerBinaryUpdateWatcher]";

        /// <summary>The UPM package id whose version bumps may change the pinned server release.</summary>
        internal const string UnityMcpPackageName = "com.ivanmurzak.unity.mcp";

        /// <summary>
        /// Guards against the multiple <c>registeredPackages</c> events UPM can fire for a single user action
        /// kicking off overlapping re-checks.
        /// </summary>
        static bool _isRechecking;

        static ServerBinaryUpdateWatcher()
        {
            // -= before += guards against double-subscription if this constructor ever re-runs without a full
            // domain swap (mirrors NuGetDependencyResolver).
            PackageManagerEvents.registeredPackages -= OnRegisteredPackages;
            PackageManagerEvents.registeredPackages += OnRegisteredPackages;
        }

        static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            if (!AffectsUnityMcpPackage(args))
                return;

            if (_isRechecking)
                return;

            _isRechecking = true;
            try
            {
                Debug.Log($"{Tag} Unity-MCP package change detected — re-checking server binary version.");
                // Fire-and-forget: unattended path (no blocking modal, no result popup; failures surface via
                // McpServerManager.LastDownloadError -> the AI Game Developer window's error + retry button).
                _ = McpServerManager.DownloadServerBinaryIfNeeded(unattended: true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Server binary re-check failed: {ex}");
            }
            finally
            {
                _isRechecking = false;
            }
        }

        /// <summary>
        /// True when the UPM package-registration event involves the Unity-MCP package (added, or changed
        /// in either direction). Extracts the package names and defers to the pure overload so the decision is
        /// unit-testable without constructing UnityEditor PackageManager types.
        /// </summary>
        internal static bool AffectsUnityMcpPackage(PackageRegistrationEventArgs args)
        {
            if (args == null)
                return false;

            return ContainsUnityMcp(Names(args.added))
                || ContainsUnityMcp(Names(args.changedTo))
                || ContainsUnityMcp(Names(args.changedFrom));

            static IEnumerable<string?> Names(IEnumerable<PackageInfo>? packages)
            {
                if (packages == null)
                    yield break;
                foreach (var p in packages)
                    yield return p?.name;
            }
        }

        /// <summary>
        /// Pure decision: does any of the supplied package-name collections name the Unity-MCP package
        /// (case-insensitive)? Unit-testable with plain strings.
        /// </summary>
        internal static bool AffectsUnityMcpPackage(
            IEnumerable<string?>? added,
            IEnumerable<string?>? changedTo,
            IEnumerable<string?>? changedFrom)
            => ContainsUnityMcp(added) || ContainsUnityMcp(changedTo) || ContainsUnityMcp(changedFrom);

        /// <summary>True when <paramref name="packageNames"/> contains the Unity-MCP package id (case-insensitive).</summary>
        internal static bool ContainsUnityMcp(IEnumerable<string?>? packageNames)
        {
            if (packageNames == null)
                return false;

            foreach (var name in packageNames)
            {
                if (string.Equals(name, UnityMcpPackageName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
