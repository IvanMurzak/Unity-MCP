/*
Copyright (c) 2025 Ivan Murzak
Licensed under the Apache License, Version 2.0.
See the LICENSE file in the project root for more information.
*/

#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    public static class SecureKeyStore
    {
        private const string ServiceName = "com.ivanmurzak.unity.mcp";
        private const string DisplayName = "Unity MCP";

        public static string? Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

#if UNITY_EDITOR_WIN
            return WindowsRead(BuildTargetName(key));
#elif UNITY_EDITOR_OSX
            return MacRead(key);
#elif UNITY_EDITOR_LINUX
            return LinuxRead(key);
#else
            return null;
#endif
        }

        public static void Set(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (string.IsNullOrWhiteSpace(value))
            {
                Delete(key);
                return;
            }

#if UNITY_EDITOR_WIN
            WindowsWrite(BuildTargetName(key), value);
#elif UNITY_EDITOR_OSX
            MacWrite(key, value);
#elif UNITY_EDITOR_LINUX
            LinuxWrite(key, value);
#endif
        }

        public static void Delete(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

#if UNITY_EDITOR_WIN
            WindowsDelete(BuildTargetName(key));
#elif UNITY_EDITOR_OSX
            MacDelete(key);
#elif UNITY_EDITOR_LINUX
            LinuxDelete(key);
#endif
        }

        private static string BuildTargetName(string key)
        {
            return $"{ServiceName}:{key}";
        }

#if UNITY_EDITOR_WIN
        private const uint CredTypeGeneric = 1;
        private const uint CredPersistLocalMachine = 2;
        private const int ErrorNotFound = 1168;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string? Comment;
            public FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string? TargetAlias;
            public string? UserName;
        }

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredRead(string target, uint type, uint reservedFlag, out IntPtr credentialPtr);

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredWrite([In] ref Credential userCredential, [In] uint flags);

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredDelete(string target, uint type, uint flags);

        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern void CredFree([In] IntPtr cred);

        private static string? WindowsRead(string targetName)
        {
            if (!CredRead(targetName, CredTypeGeneric, 0, out var credentialPtr))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ErrorNotFound)
                    Debug.LogWarning($"[Warning] Credential read failed for {targetName} (error {error}).");
                return null;
            }

            try
            {
                var credential = Marshal.PtrToStructure<Credential>(credentialPtr);
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                    return null;

                var value = Marshal.PtrToStringUni(
                    credential.CredentialBlob,
                    (int)credential.CredentialBlobSize / 2);

                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            finally
            {
                CredFree(credentialPtr);
            }
        }

        private static void WindowsWrite(string targetName, string value)
        {
            var valueBytes = Encoding.Unicode.GetBytes(value);
            var valuePtr = Marshal.AllocHGlobal(valueBytes.Length);

            try
            {
                Marshal.Copy(valueBytes, 0, valuePtr, valueBytes.Length);

                var credential = new Credential
                {
                    Type = CredTypeGeneric,
                    TargetName = targetName,
                    CredentialBlobSize = (uint)valueBytes.Length,
                    CredentialBlob = valuePtr,
                    Persist = CredPersistLocalMachine,
                    AttributeCount = 0,
                    Attributes = IntPtr.Zero,
                    UserName = null,
                    Comment = null,
                    TargetAlias = null,
                    Flags = 0
                };

                if (!CredWrite(ref credential, 0))
                {
                    var error = Marshal.GetLastWin32Error();
                    Debug.LogWarning($"[Warning] Credential write failed for {targetName} (error {error}).");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(valuePtr);
            }
        }

        private static void WindowsDelete(string targetName)
        {
            if (!CredDelete(targetName, CredTypeGeneric, 0))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ErrorNotFound)
                    Debug.LogWarning($"[Warning] Credential delete failed for {targetName} (error {error}).");
            }
        }
#endif

#if UNITY_EDITOR_OSX
        private static string? MacRead(string account)
        {
            return RunSecretCommand("security",
                new[] { "find-generic-password", "-a", account, "-s", ServiceName, "-w" },
                null,
                ignoreNotFound: true);
        }

        private static void MacWrite(string account, string value)
        {
            RunSecretCommand("security",
                new[] { "add-generic-password", "-a", account, "-s", ServiceName, "-w", value, "-U" },
                null,
                ignoreNotFound: false);
        }

        private static void MacDelete(string account)
        {
            RunSecretCommand("security",
                new[] { "delete-generic-password", "-a", account, "-s", ServiceName },
                null,
                ignoreNotFound: true);
        }
#endif

#if UNITY_EDITOR_LINUX
        private static string? LinuxRead(string account)
        {
            return RunSecretCommand("secret-tool",
                new[] { "lookup", "service", ServiceName, "account", account },
                null,
                ignoreNotFound: true);
        }

        private static void LinuxWrite(string account, string value)
        {
            RunSecretCommand("secret-tool",
                new[] { "store", "--label", $"{DisplayName} {account}", "service", ServiceName, "account", account },
                value,
                ignoreNotFound: false);
        }

        private static void LinuxDelete(string account)
        {
            RunSecretCommand("secret-tool",
                new[] { "clear", "service", ServiceName, "account", account },
                null,
                ignoreNotFound: true);
        }
#endif

#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
        private static string? RunSecretCommand(
            string fileName,
            string[] args,
            string? input,
            bool ignoreNotFound)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = BuildArguments(args),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = input != null,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                    return null;

                if (input != null)
                {
                    process.StandardInput.Write(input);
                    process.StandardInput.Close();
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    if (!ignoreNotFound && !string.IsNullOrWhiteSpace(error))
                        Debug.LogWarning($"[Warning] Secret store command failed: {error.Trim()}");
                    return null;
                }

                return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Warning] Secret store command failed: {ex.GetBaseException().Message}");
                return null;
            }
        }

        private static string BuildArguments(string[] args)
        {
            if (args.Length == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (var i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    builder.Append(' ');

                builder.Append(QuoteArgument(args[i]));
            }

            return builder.ToString();
        }

        private static string QuoteArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "\"\"";

            if (arg.IndexOfAny(new[] { ' ', '"', '\'' }) >= 0)
                return "\"" + arg.Replace("\"", "\\\"") + "\"";

            return arg;
        }
#endif
    }
}
