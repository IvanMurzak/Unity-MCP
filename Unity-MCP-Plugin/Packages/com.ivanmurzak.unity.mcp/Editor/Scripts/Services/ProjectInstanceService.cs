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
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.AgentConfig;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    /// <summary>
    /// Editor-side owner of THIS project's connection identity (mcp-authorize design 04/06, PR 3). It is the
    /// single Unity-side seam that:
    /// <list type="bullet">
    ///   <item><b>Instance-metadata handshake:</b> builds the non-secret
    ///   <see cref="ConnectionInstanceMetadata"/> payload
    ///   (<c>{engine:"unity", projectName, projectPathHash, machineName, instanceId}</c>) and attaches it to
    ///   the <see cref="UnityMcpPlugin.UnityConnectionConfig"/> so McpPlugin's
    ///   <c>HubConnectionProvider</c> sends it as hub-connection query params on every (re)connect. The token
    ///   always travels separately in the <c>Authorization</c> header — never here.</item>
    ///   <item><b>ProjectIdentity + ProjectMarker wiring:</b> resolves the routing <see cref="ProjectIdentity.Pin"/>
    ///   (and the derived port, honouring any user override) via the shared
    ///   <see cref="ProjectIdentity"/> derivation, and reads/writes the non-secret, committable project marker
    ///   <c>&lt;project&gt;/.ai-game-dev/project.json</c> (<see cref="ProjectMarker"/>) so the enrolled
    ///   server target + pin are resolved from it. Credentials NEVER go in the marker.</item>
    /// </list>
    /// The derivation matches the committed <c>ProjectIdentity.GoldenVectors.json</c> byte-for-byte (trim
    /// trailing separators, do NOT convert separators, <see cref="string.ToLowerInvariant"/> only). This PR
    /// wires the pin + server-target resolution only; it does not change the runtime local-port default
    /// (Unity already ships the deterministic derived port via
    /// <see cref="UnityMcpPlugin.GeneratePortFromDirectory"/>).
    /// </summary>
    public static class ProjectInstanceService
    {
        /// <summary>The engine identifier this plugin reports in its instance-metadata handshake.</summary>
        public const string EngineName = "unity";

        /// <summary>
        /// <see cref="SessionState"/> key holding the per-editor-session instance id. <see cref="SessionState"/>
        /// survives domain reloads but is cleared when the Editor is closed — exactly the "one id per editor
        /// session" lifetime the account+instance router expects (reconnects of the same Editor reuse it; a
        /// fresh Editor launch mints a new one).
        /// </summary>
        internal const string SessionInstanceIdKey = "com.IvanMurzak.Unity.MCP.ProjectInstance.InstanceId";

        /// <summary>
        /// The instance id for the current editor session: a GUID minted once per Editor launch and stable
        /// across domain reloads (persisted in <see cref="SessionState"/>). Reconnects of the same Editor
        /// present the same id so the server replaces the connection entry instead of orphaning it.
        /// </summary>
        public static string SessionInstanceId
        {
            get
            {
                var existing = SessionState.GetString(SessionInstanceIdKey, string.Empty);
                if (!string.IsNullOrEmpty(existing))
                    return existing;

                var minted = Guid.NewGuid().ToString();
                SessionState.SetString(SessionInstanceIdKey, minted);
                return minted;
            }
        }

        /// <summary>
        /// Build the instance-metadata handshake payload for <paramref name="projectRoot"/>. The
        /// <see cref="ConnectionInstanceMetadata.ProjectPathHash"/> is derived from the same normalized root
        /// whose first 8 hex chars are the routing pin, so the server pin-matches this instance by prefix.
        /// <paramref name="instanceId"/> defaults to <see cref="SessionInstanceId"/>.
        /// </summary>
        public static ConnectionInstanceMetadata BuildMetadata(string projectRoot, string projectName, string? instanceId = null)
        {
            if (projectRoot == null)
                throw new ArgumentNullException(nameof(projectRoot));

            return ConnectionInstanceMetadata.Create(
                engine: EngineName,
                projectName: projectName ?? string.Empty,
                projectRootPath: projectRoot,
                instanceId: string.IsNullOrEmpty(instanceId) ? SessionInstanceId : instanceId);
        }

        /// <summary>
        /// Populate <paramref name="config"/>'s <see cref="ConnectionConfig.InstanceMetadata"/> from the host
        /// Unity project (<see cref="UnityMcpPluginEditor.ProjectRootPath"/> + <see cref="Application.productName"/>)
        /// so the handshake is sent on the next (re)connect. Called from the plugin build path; idempotent and
        /// safe on every editor boot / domain reload (the session instance id is stable across reloads).
        /// </summary>
        public static void AttachInstanceMetadata(UnityMcpPlugin.UnityConnectionConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.InstanceMetadata = BuildMetadata(
                projectRoot: UnityMcpPluginEditor.ProjectRootPath,
                projectName: Application.productName);
        }

        /// <summary>
        /// Read the project marker <c>&lt;projectRoot&gt;/.ai-game-dev/project.json</c>, or <c>null</c> when it
        /// does not exist (a project that has never been enrolled / configured).
        /// </summary>
        public static ProjectMarker? ReadMarker(string projectRoot)
        {
            if (projectRoot == null)
                throw new ArgumentNullException(nameof(projectRoot));
            return ProjectMarker.Read(projectRoot);
        }

        /// <summary>
        /// Write the non-secret project marker into <paramref name="projectRoot"/> (creating the
        /// <c>.ai-game-dev</c> directory if needed). The marker is committable; credentials NEVER go here.
        /// </summary>
        public static void WriteMarker(string projectRoot, ProjectMarker marker)
        {
            if (projectRoot == null)
                throw new ArgumentNullException(nameof(projectRoot));
            if (marker == null)
                throw new ArgumentNullException(nameof(marker));
            marker.Write(projectRoot);
        }

        /// <summary>
        /// The enrolled server target (hosted vs local) recorded in the project marker, or <c>null</c> when no
        /// marker exists or it records no target. This is how the plugin resolves which hub to point at.
        /// </summary>
        public static string? ResolveServerTarget(string projectRoot)
            => ReadMarker(projectRoot)?.ServerTarget;

        /// <summary>
        /// Resolve the project's <see cref="ProjectIdentity"/> (routing pin + effective port) for
        /// <paramref name="projectRoot"/>, honouring the marker's user port override when present. The pin is
        /// always hash-derived (never affected by the override).
        /// </summary>
        public static ProjectIdentity ResolveIdentity(string projectRoot)
        {
            if (projectRoot == null)
                throw new ArgumentNullException(nameof(projectRoot));
            return ProjectIdentity.Derive(projectRoot, ReadMarker(projectRoot));
        }

        /// <summary>The routing pin (first 8 lowercase hex chars of the SHA-256 of the normalized project root).</summary>
        public static string ResolvePin(string projectRoot)
        {
            if (projectRoot == null)
                throw new ArgumentNullException(nameof(projectRoot));
            return ProjectIdentity.DerivePin(projectRoot);
        }
    }
}
