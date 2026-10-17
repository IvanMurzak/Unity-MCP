/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using System;
using System.Net;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    /// <summary>
    /// Custom + <c>oauth</c> bearer rule, identical in every engine plugin: the ai-game.dev account token goes to a
    /// loopback host only (decided by parsing the URL host, never by DNS); any other host gets the explicit token or the
    /// connection fails with <see cref="CredentialScopeException"/>.
    /// TODO: switch to McpPlugin's <c>HostScope</c> / <c>CustomOAuthCredentialPolicy</c> once a McpPlugin release
    /// containing them (MCP-Plugin-dotnet PR #259) is pinned (the plugin package and Tests~/EngineFree), then delete this file (and its .meta).
    /// </summary>
    public static class LoopbackTokenScope
    {
        /// <summary>
        /// True for an absolute http(s) URL without user info whose host is <c>localhost</c> (any case),
        /// <c>127.0.0.0/8</c> or <c>::1</c> (also IPv4-mapped).
        /// </summary>
        public static bool IsLoopback(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url!.Trim(), UriKind.Absolute, out var uri))
                return false;
            if ((uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || !string.IsNullOrEmpty(uri.UserInfo))
                return false;
            switch (uri.HostNameType)
            {
                case UriHostNameType.Dns:
                    return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
                case UriHostNameType.IPv4:
                case UriHostNameType.IPv6:
                    if (!IPAddress.TryParse(uri.DnsSafeHost, out var address))
                        return false;
                    if (address.IsIPv4MappedToIPv6)
                        address = address.MapToIPv4();
                    return IPAddress.IsLoopback(address);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Loopback: the account token, else <paramref name="explicitToken"/>, else null. Other hosts:
        /// <paramref name="explicitToken"/> (the account token is never read), else <see cref="CredentialScopeException"/>.
        /// </summary>
        public static async Task<string?> ResolveAsync(string? host, Func<Task<string?>>? accountToken, string? explicitToken, string tokenSettingName)
        {
            var hasExplicit = !string.IsNullOrEmpty(explicitToken);
            if (!IsLoopback(host))
                return hasExplicit ? explicitToken : throw new CredentialScopeException(host, tokenSettingName);
            if (accountToken != null)
            {
                var token = await accountToken().ConfigureAwait(false);
                if (!string.IsNullOrEmpty(token))
                    return token;
            }
            return hasExplicit ? explicitToken : null;
        }
    }

    /// <summary>The account token would leave this machine and no explicit token is configured.</summary>
    public sealed class CredentialScopeException : InvalidOperationException
    {
        public CredentialScopeException(string? host, string tokenSettingName)
            : base($"Refusing to send your ai-game.dev account token to {Describe(host)}; set {tokenSettingName} or use a localhost server.") { }

        // Authority only: never echo user info, a path or a query.
        private static string Describe(string? host) =>
            string.IsNullOrWhiteSpace(host) ? "an empty host"
            : Uri.TryCreate(host!.Trim(), UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host)
                ? uri.Host + (uri.IsDefaultPort ? string.Empty : ":" + uri.Port)
                : "a non-loopback host";
    }
}
