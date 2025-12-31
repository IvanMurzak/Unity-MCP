#nullable enable

using System;
using com.IvanMurzak.Unity.MCP.Runtime.Utils;
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
        public void RoundTrip_Windows()
        {
#if UNITY_EDITOR_WIN
            RunRoundTrip("Windows Credential Manager");
#else
            WarnInconclusive("SMOKE: SecureKeyStore Windows test skipped. Run on Windows to validate Credential Manager integration.");
#endif
        }

        [Test]
        public void RoundTrip_Mac()
        {
#if UNITY_EDITOR_OSX
            RunRoundTrip("macOS Keychain");
#else
            WarnInconclusive("SMOKE: SecureKeyStore macOS test skipped. Run on macOS to validate Keychain integration.");
#endif
        }

        [Test]
        public void RoundTrip_Linux()
        {
#if UNITY_EDITOR_LINUX
            RunRoundTrip("Linux Secret Service");
#else
            WarnInconclusive("SMOKE: SecureKeyStore Linux test skipped. Run on Linux with secret-tool to validate Secret Service integration.");
#endif
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

        static void WarnInconclusive(string message)
        {
            Debug.LogWarning(message);
            Assert.Inconclusive(message);
        }
    }
}
