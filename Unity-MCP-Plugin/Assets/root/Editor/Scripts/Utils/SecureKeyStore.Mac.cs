#if UNITY_EDITOR_OSX

#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Text;
using Debug = UnityEngine.Debug;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    public static partial class SecureKeyStore
    {
        private const int ErrSecDuplicateItem = -25299;
        private const int ErrSecItemNotFound = -25300;

        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        private static extern int SecKeychainFindGenericPassword(
            IntPtr keychainOrArray,
            uint serviceNameLength,
            string serviceName,
            uint accountNameLength,
            string accountName,
            out uint passwordLength,
            out IntPtr passwordData,
            out IntPtr itemRef);

        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        private static extern int SecKeychainAddGenericPassword(
            IntPtr keychain,
            uint serviceNameLength,
            string serviceName,
            uint accountNameLength,
            string accountName,
            uint passwordLength,
            byte[] passwordData,
            out IntPtr itemRef);

        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        private static extern int SecKeychainItemDelete(IntPtr itemRef);

        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        private static extern int SecKeychainItemFreeContent(IntPtr attrList, IntPtr data);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr cf);

        private static string BuildTargetName(string key)
        {
            return key;
        }

        private static string? Read(string targetName)
        {
            var serviceLength = (uint)Encoding.UTF8.GetByteCount(ServiceName);
            var accountLength = (uint)Encoding.UTF8.GetByteCount(targetName);

            var result = SecKeychainFindGenericPassword(
                IntPtr.Zero,
                serviceLength,
                ServiceName,
                accountLength,
                targetName,
                out var passwordLength,
                out var passwordData,
                out var itemRef);

            try
            {
                if (result != 0)
                {
                    if (result != ErrSecItemNotFound)
                        Debug.LogWarning($"[Warning] Keychain read failed for {targetName} (error {result}).");
                    return null;
                }

                if (passwordData == IntPtr.Zero || passwordLength == 0)
                    return null;

                var bytes = new byte[passwordLength];
                Marshal.Copy(passwordData, bytes, 0, bytes.Length);
                var value = Encoding.UTF8.GetString(bytes);
                Array.Clear(bytes, 0, bytes.Length);
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            finally
            {
                if (passwordData != IntPtr.Zero)
                    SecKeychainItemFreeContent(IntPtr.Zero, passwordData);

                if (itemRef != IntPtr.Zero)
                    CFRelease(itemRef);
            }
        }

        private static void Write(string targetName, string value)
        {
            DeleteInternal(targetName);

            var serviceLength = (uint)Encoding.UTF8.GetByteCount(ServiceName);
            var accountLength = (uint)Encoding.UTF8.GetByteCount(targetName);
            var passwordBytes = Encoding.UTF8.GetBytes(value);

            try
            {
                var result = SecKeychainAddGenericPassword(
                    IntPtr.Zero,
                    serviceLength,
                    ServiceName,
                    accountLength,
                    targetName,
                    (uint)passwordBytes.Length,
                    passwordBytes,
                    out var itemRef);

                if (itemRef != IntPtr.Zero)
                    CFRelease(itemRef);

                if (result != 0 && result != ErrSecDuplicateItem)
                    Debug.LogWarning($"[Warning] Keychain write failed for {targetName} (error {result}).");
            }
            finally
            {
                Array.Clear(passwordBytes, 0, passwordBytes.Length);
            }
        }

        private static void DeleteInternal(string targetName)
        {
            var serviceLength = (uint)Encoding.UTF8.GetByteCount(ServiceName);
            var accountLength = (uint)Encoding.UTF8.GetByteCount(targetName);

            var result = SecKeychainFindGenericPassword(
                IntPtr.Zero,
                serviceLength,
                ServiceName,
                accountLength,
                targetName,
                out var passwordLength,
                out var passwordData,
                out var itemRef);

            try
            {
                if (result != 0)
                {
                    if (result != ErrSecItemNotFound)
                        Debug.LogWarning($"[Warning] Keychain lookup failed for {targetName} (error {result}).");
                    return;
                }

                var deleteResult = SecKeychainItemDelete(itemRef);
                if (deleteResult != 0 && deleteResult != ErrSecItemNotFound)
                    Debug.LogWarning($"[Warning] Keychain delete failed for {targetName} (error {deleteResult}).");
            }
            finally
            {
                if (passwordData != IntPtr.Zero)
                    SecKeychainItemFreeContent(IntPtr.Zero, passwordData);

                if (itemRef != IntPtr.Zero)
                    CFRelease(itemRef);
            }
        }
    }
}

#endif
