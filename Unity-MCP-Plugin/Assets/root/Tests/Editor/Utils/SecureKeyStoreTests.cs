#nullable enable

using System;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class SecureKeyStoreTests
    {
        [Test]
        public void EmptyKey_NoThrow()
        {
            Assert.DoesNotThrow(() => SecureKeyStore.Set(string.Empty, "value"));
            Assert.DoesNotThrow(() => SecureKeyStore.Set("   ", "value"));
            Assert.DoesNotThrow(() => SecureKeyStore.Delete(string.Empty));
            Assert.IsNull(SecureKeyStore.Get(string.Empty));
            Assert.IsNull(SecureKeyStore.Get("   "));
        }

        [Test]
        public void RoundTrip_CurrentPlatform()
        {
            if (!TryGetPlatformName(out var platformName, out var warning))
            {
                Debug.LogWarning(warning);
                return;
            }

            RunRoundTrip(platformName);
        }

        static void RunRoundTrip(string platformName)
        {
            var key = $"unity-mcp-test-{Guid.NewGuid():N}";
            const string value = "secure-key-store-test";

            try
            {
                SecureKeyStore.Delete(key);
                SecureKeyStore.Set(key, value);

                var read = SecureKeyStore.Get(key);
                if (string.IsNullOrWhiteSpace(read))
                    WarnInconclusive($"SMOKE: {platformName} secure store unavailable or blocked. Run with appropriate OS access to validate SecureKeyStore.");

                Assert.AreEqual(value, read);

                SecureKeyStore.Set(key, null);
                var removed = SecureKeyStore.Get(key);
                Assert.IsTrue(string.IsNullOrWhiteSpace(removed), "Expected null value to remove stored key.");
            }
            finally
            {
                SecureKeyStore.Delete(key);
            }
        }

        static bool TryGetPlatformName(out string platformName, out string warning)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    platformName = "Windows Credential Manager";
                    warning = string.Empty;
                    return true;
                case RuntimePlatform.OSXEditor:
                    platformName = "macOS Keychain";
                    warning = string.Empty;
                    return true;
                case RuntimePlatform.LinuxEditor:
                    platformName = "Linux Secret Service";
                    warning = string.Empty;
                    return true;
                default:
                    platformName = string.Empty;
                    warning = $"SMOKE: SecureKeyStore test skipped on {Application.platform}. Run on Windows/macOS/Linux to validate OS credential manager integration.";
                    return false;
            }
        }
    }
}
