#if UNITY_EDITOR_WIN

#nullable enable

using System;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using System.Text;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public static partial class SecureKeyStore
    {
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

        private static string BuildTargetName(string key)
        {
            return $"{ServiceName}:{key}";
        }

        private static string? Read(string targetName)
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

        private static void Write(string targetName, string value)
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

        private static void DeleteInternal(string targetName)
        {
            if (!CredDelete(targetName, CredTypeGeneric, 0))
            {
                var error = Marshal.GetLastWin32Error();
                if (error != ErrorNotFound)
                    Debug.LogWarning($"[Warning] Credential delete failed for {targetName} (error {error}).");
            }
        }
    }
}

#endif
