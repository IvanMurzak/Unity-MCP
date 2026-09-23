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
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.AgentConfig;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace com.IvanMurzak.Unity.MCP.Editor.Services
{
    /// <summary>
    /// Unity's seam onto the shared <see cref="ProjectKeyProvider"/> (project-keys contract §6/§7): the Cloud
    /// "Configure" action writes <c>Authorization: Bearer agd_pk_…</c> for every agent, where the key is a
    /// non-expiring credential strictly bound to this project's pin.
    ///
    /// <para><b>Threading.</b> <see cref="GetOrMintAsync"/> / <see cref="RegenerateAsync"/> may block for a long
    /// time (HTTP + the cross-process <c>credentials.lock</c>, up to ~75 s), so both run on the thread pool and
    /// callers must marshal the result back to the main thread (<c>MainThread.Instance.RunAsync</c>). Neither ever
    /// throws: any failure (not signed in, 404 while the mint flag is off, network) yields <c>null</c>, and the
    /// caller writes the URL-only config.</para>
    ///
    /// <para><see cref="KnownKey"/> is the key the configurators compare against and write: set by the last
    /// get-or-mint / regenerate in this domain, otherwise peeked once from the local cache (a cheap file read,
    /// no lock, no network) so the status row reflects a key minted in an earlier session.</para>
    /// </summary>
    internal static class ProjectKeyService
    {
        public const string Engine = "unity";

        static readonly object _gate = new object();
        static readonly ILogger _logger = UnityLoggerFactory.LoggerFactory.CreateLogger(nameof(ProjectKeyService));

        static ProjectKeyProvider? _provider;
        static string? _providerIssuer;

        // Last resolved key per "<issuer>#<pin>" in this domain; a null value is a resolved "no key".
        static string? _knownEntry;
        static string? _knownKey;

        /// <summary>The issuer every mint/cache entry is keyed on: the origin of the Cloud server the configs point at.</summary>
        public static string Issuer => ProjectKeyStore.NormalizeIssuerOrigin(UnityMcpPlugin.UnityConnectionConfig.CloudServerBaseUrl);

        /// <summary>
        /// One provider per issuer, so its single-flight gate serializes "Configure" clicks across agents (they
        /// must not each mint their own key). The access token and subject resolve the CURRENT
        /// <see cref="AccountCredentialService.Provider"/> on every call — that instance is rebuilt (and the old
        /// one disposed) on login / sign-out, so it must never be captured.
        /// </summary>
        static ProjectKeyProvider Provider
        {
            get
            {
                var issuer = Issuer;
                lock (_gate)
                {
                    if (_provider == null || _providerIssuer != issuer)
                    {
                        _provider = new ProjectKeyProvider(
                            ct => AccountCredentialService.Provider.GetAccessTokenAsync(ct),
                            issuer,
                            subjectFallback: () => AccountCredentialService.Provider.Subject,
                            logger: _logger);
                        _providerIssuer = issuer;
                    }
                    return _provider;
                }
            }
        }

        /// <summary>
        /// The project key the agent configs for <paramref name="pin"/> are expected to carry, or <c>null</c> for
        /// the URL-only config. Never touches the network; signed out ⇒ <c>null</c> (contract §6: no login ⇒ URL-only).
        /// </summary>
        public static string? KnownKey(string pin)
        {
            if (!AccountCredentialService.IsSignedIn)
                return null;

            var entry = SafeEntryName(pin);
            if (entry == null)
                return null;

            lock (_gate)
            {
                if (_knownEntry == entry)
                    return _knownKey;
            }

            string? cached = null;
            try
            {
                cached = Provider.Store.Get(Issuer, pin)?.Key;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Project-key cache peek failed: {Message}", ex.Message);
            }
            Remember(entry, cached);
            return cached;
        }

        /// <summary>Get-or-mint off the main thread (contract §6). <c>null</c> ⇒ write the URL-only config.</summary>
        public static Task<string?> GetOrMintAsync(string pin, string projectRootPath)
            => RunOffMainThread(pin, p => p.GetOrMintAsync(pin, Engine, Environment.MachineName, projectRootPath), "get or mint", rememberNull: true);

        /// <summary>
        /// "Regenerate key" off the main thread (contract §7): mints a fresh key, overwrites the cache entry and
        /// revokes the replaced key. <c>null</c> ⇒ nothing changed (the previous key stays cached and valid).
        /// </summary>
        public static Task<string?> RegenerateAsync(string pin, string projectRootPath)
            => RunOffMainThread(pin, p => p.RegenerateAsync(pin, Engine, Environment.MachineName, projectRootPath), "regenerate", rememberNull: false);

        // A failed get-or-mint means the URL-only config is written, so "no key" is remembered; a failed regenerate
        // changed nothing, so the previous key stays the known one.
        static Task<string?> RunOffMainThread(string pin, Func<ProjectKeyProvider, Task<string?>> call, string action, bool rememberNull)
            => Task.Run(async () =>
            {
                string? key = null;
                try
                {
                    key = await call(Provider).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not {Action} the project key; the agent config stays URL-only: {Message}", action, ex.Message);
                }

                var entry = SafeEntryName(pin);
                if (entry != null && (key != null || rememberNull))
                    Remember(entry, key);
                return key;
            });

        static void Remember(string entry, string? key)
        {
            lock (_gate)
            {
                _knownEntry = entry;
                _knownKey = key;
            }
        }

        static string? SafeEntryName(string pin)
        {
            try
            {
                return ProjectKeyStore.EntryName(Issuer, pin);
            }
            catch (ArgumentException)
            {
                return null; // malformed pin or issuer — no key can exist for it.
            }
        }

        /// <summary>
        /// The status-row line saying which credential a Cloud HTTP config carries. Never renders key material.
        /// </summary>
        internal static string DescribeKeyState(bool isSignedIn, bool hasKey)
        {
            if (hasKey)
                return "Project key in use — agent configs carry this project's key.";
            return isSignedIn
                ? "No project key — configs are URL-only (the agent signs in itself)."
                : "No project key — configs are URL-only. Sign in to write a project key.";
        }
    }
}
