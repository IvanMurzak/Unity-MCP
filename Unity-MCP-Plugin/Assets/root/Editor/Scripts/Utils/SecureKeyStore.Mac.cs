/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Tristyn Mackay (https://github.com/InMetaTech-Tristyn)  │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

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
        private static extern int SecKeychainItemModifyAttributesAndData(
            IntPtr itemRef,
            IntPtr attrList,
            uint dataLength,
            byte[] data);

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
                        Debug.LogError($"Keychain read failed for {targetName} (error {result}).");
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
            var serviceLength = (uint)Encoding.UTF8.GetByteCount(ServiceName);
            var accountLength = (uint)Encoding.UTF8.GetByteCount(targetName);
            var passwordBytes = Encoding.UTF8.GetBytes(value);
            IntPtr itemRef = IntPtr.Zero;
            IntPtr passwordData = IntPtr.Zero;

            try
            {
                var lookupResult = SecKeychainFindGenericPassword(
                    IntPtr.Zero,
                    serviceLength,
                    ServiceName,
                    accountLength,
                    targetName,
                    out var passwordLength,
                    out passwordData,
                    out itemRef);

                if (lookupResult == 0 && itemRef != IntPtr.Zero)
                {
                    var modifyResult = SecKeychainItemModifyAttributesAndData(
                        itemRef,
                        IntPtr.Zero,
                        (uint)passwordBytes.Length,
                        passwordBytes);

                    if (modifyResult != 0)
                        Debug.LogError($"Keychain update failed for {targetName} (error {modifyResult}).");

                    return;
                }

                if (lookupResult != 0 && lookupResult != ErrSecItemNotFound)
                    Debug.LogError($"Keychain lookup failed for {targetName} (error {lookupResult}).");

                var addResult = SecKeychainAddGenericPassword(
                    IntPtr.Zero,
                    serviceLength,
                    ServiceName,
                    accountLength,
                    targetName,
                    (uint)passwordBytes.Length,
                    passwordBytes,
                    out var addItemRef);

                if (addItemRef != IntPtr.Zero)
                    CFRelease(addItemRef);

                if (addResult != 0 && addResult != ErrSecDuplicateItem)
                    Debug.LogError($"Keychain write failed for {targetName} (error {addResult}).");
            }
            finally
            {
                if (passwordData != IntPtr.Zero)
                    SecKeychainItemFreeContent(IntPtr.Zero, passwordData);

                if (itemRef != IntPtr.Zero)
                    CFRelease(itemRef);

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
                        Debug.LogError($"Keychain lookup failed for {targetName} (error {result}).");
                    return;
                }

                var deleteResult = SecKeychainItemDelete(itemRef);
                if (deleteResult != 0 && deleteResult != ErrSecItemNotFound)
                    Debug.LogError($"Keychain delete failed for {targetName} (error {deleteResult}).");
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
