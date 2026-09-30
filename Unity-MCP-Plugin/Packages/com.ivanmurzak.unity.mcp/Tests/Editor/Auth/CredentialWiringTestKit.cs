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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Shared plumbing for the credential-wiring integration tests (unified-machine-auth d1):
    /// an <see cref="HttpMessageHandler"/> that captures every form POST the REAL
    /// <c>HttpTokenRefresher</c> sends (so the tests assert the actual wire bytes, not a mock's
    /// echo), and a raw on-disk store seeder that produces platform-correct bytes — plaintext on
    /// POSIX, DPAPI (CurrentUser, null entropy, UI_FORBIDDEN — the exact design-D2 parameters the
    /// store itself uses) on Windows — so a seeded legacy v1 file is indistinguishable from one a
    /// real v1 writer left behind.
    /// </summary>
    internal static class CredentialWiringTestKit
    {
        /// <summary>One captured token-endpoint request: the URI and the decoded form fields.</summary>
        internal sealed class CapturedRequest
        {
            public Uri? Uri;
            public Dictionary<string, string> Form = new Dictionary<string, string>();
        }

        /// <summary>
        /// Captures every request and answers with canned JSON. The response factory receives the
        /// 1-based request index so multi-round tests can vary answers.
        /// </summary>
        internal sealed class CapturingHandler : HttpMessageHandler
        {
            readonly Func<int, HttpResponseMessage> _respond;
            public readonly List<CapturedRequest> Requests = new List<CapturedRequest>();

            public CapturingHandler(Func<int, HttpResponseMessage> respond) => _respond = respond;

            public CapturingHandler(string json, HttpStatusCode status = HttpStatusCode.OK)
                : this(_ => Json(json, status)) { }

            public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
                => new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var captured = new CapturedRequest { Uri = request.RequestUri };
                if (request.Content != null)
                {
                    var body = await request.Content.ReadAsStringAsync();
                    foreach (var pair in body.Split('&'))
                    {
                        if (pair.Length == 0)
                            continue;
                        var eq = pair.IndexOf('=');
                        var key = Uri.UnescapeDataString(eq < 0 ? pair : pair.Substring(0, eq));
                        var value = eq < 0 ? "" : Uri.UnescapeDataString(pair.Substring(eq + 1));
                        captured.Form[key] = value;
                    }
                }
                lock (Requests)
                    Requests.Add(captured);
                return _respond(Requests.Count);
            }
        }

        /// <summary>A standard successful token-endpoint response body.</summary>
        internal static string TokenJson(string accessToken, string? refreshToken, int expiresIn = 3600)
            => refreshToken == null
                ? $@"{{ ""access_token"": ""{accessToken}"", ""token_type"": ""Bearer"", ""expires_in"": {expiresIn} }}"
                : $@"{{ ""access_token"": ""{accessToken}"", ""refresh_token"": ""{refreshToken}"", ""token_type"": ""Bearer"", ""expires_in"": {expiresIn} }}";

        // ── Raw store seeding / reading (platform-correct at-rest bytes) ─────────────────────────

        /// <summary>
        /// Write <paramref name="json"/> as the store's <c>credentials.json</c> exactly as a real
        /// writer would have: DPAPI-protected on Windows, plaintext on POSIX.
        /// </summary>
        internal static void SeedRawStoreJson(string baseDirectory, string json)
        {
            Directory.CreateDirectory(baseDirectory);
            var bytes = Encoding.UTF8.GetBytes(json);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                bytes = Protect(bytes);
            File.WriteAllBytes(Path.Combine(baseDirectory, "credentials.json"), bytes);
        }

        /// <summary>Read the store's on-disk <c>credentials.json</c> back to plaintext JSON.</summary>
        internal static string ReadRawStoreJson(string baseDirectory)
        {
            var bytes = File.ReadAllBytes(Path.Combine(baseDirectory, "credentials.json"));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                bytes = Unprotect(bytes);
            return Encoding.UTF8.GetString(bytes);
        }

        // The exact DPAPI parameters the store pins (design D2): CurrentUser scope, null entropy,
        // CRYPTPROTECT_UI_FORBIDDEN. DPAPI round-trips across processes of the same user, so bytes
        // protected here are readable by the real MachineCredentialStore and vice versa.

        const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

        static byte[] Protect(byte[] data)
        {
            var inBlob = new DATA_BLOB();
            var outBlob = new DATA_BLOB();
            try
            {
                inBlob.pbData = Marshal.AllocHGlobal(data.Length);
                inBlob.cbData = data.Length;
                Marshal.Copy(data, 0, inBlob.pbData, data.Length);

                if (!CryptProtectData(ref inBlob, "ai-game-dev credentials", IntPtr.Zero, IntPtr.Zero,
                        IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref outBlob))
                    throw new CryptographicException(Marshal.GetLastWin32Error());

                var result = new byte[outBlob.cbData];
                Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                return result;
            }
            finally
            {
                if (inBlob.pbData != IntPtr.Zero) Marshal.FreeHGlobal(inBlob.pbData);
                if (outBlob.pbData != IntPtr.Zero) LocalFree(outBlob.pbData);
            }
        }

        static byte[] Unprotect(byte[] data)
        {
            var inBlob = new DATA_BLOB();
            var outBlob = new DATA_BLOB();
            try
            {
                inBlob.pbData = Marshal.AllocHGlobal(data.Length);
                inBlob.cbData = data.Length;
                Marshal.Copy(data, 0, inBlob.pbData, data.Length);

                if (!CryptUnprotectData(ref inBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                        IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref outBlob))
                    throw new CryptographicException(Marshal.GetLastWin32Error());

                var result = new byte[outBlob.cbData];
                Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
                return result;
            }
            finally
            {
                if (inBlob.pbData != IntPtr.Zero) Marshal.FreeHGlobal(inBlob.pbData);
                if (outBlob.pbData != IntPtr.Zero) LocalFree(outBlob.pbData);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CryptProtectData(ref DATA_BLOB pDataIn, string? szDataDescr,
            IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, IntPtr ppszDataDescr,
            IntPtr pOptionalEntropy, IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LocalFree(IntPtr hMem);
    }
}
