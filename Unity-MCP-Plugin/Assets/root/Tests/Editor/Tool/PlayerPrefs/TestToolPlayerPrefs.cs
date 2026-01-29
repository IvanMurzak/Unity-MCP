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
using com.IvanMurzak.Unity.MCP.Editor.API;
using Extensions.Unity.PlayerPrefsEx;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolPlayerPrefs : BaseTest
    {
        const string TestKeyPrefix = "MCP_Test_";
        const string TestKeyInt = TestKeyPrefix + "Int";
        const string TestKeyFloat = TestKeyPrefix + "Float";
        const string TestKeyString = TestKeyPrefix + "String";

        Tool_PlayerPrefs _tool = null!;

        [SetUp]
        public void TestSetUp()
        {
            _tool = new Tool_PlayerPrefs();

            // Clean up any existing test keys
            CleanupTestKeys();
        }

        [TearDown]
        public void TestTearDown()
        {
            // Clean up test keys after each test
            CleanupTestKeys();
            PlayerPrefsEx.Save();
        }

        void CleanupTestKeys()
        {
            // Delete keys from PlayerPrefsEx for all possible types
            if (PlayerPrefsEx.HasKey<int>(TestKeyInt))
                PlayerPrefsEx.DeleteKey<int>(TestKeyInt);
            if (PlayerPrefsEx.HasKey<float>(TestKeyInt))
                PlayerPrefsEx.DeleteKey<float>(TestKeyInt);
            if (PlayerPrefsEx.HasKey<string>(TestKeyInt))
                PlayerPrefsEx.DeleteKey<string>(TestKeyInt);
            if (PlayerPrefsEx.HasKey<bool>(TestKeyInt))
                PlayerPrefsEx.DeleteKey<bool>(TestKeyInt);

            if (PlayerPrefsEx.HasKey<int>(TestKeyFloat))
                PlayerPrefsEx.DeleteKey<int>(TestKeyFloat);
            if (PlayerPrefsEx.HasKey<float>(TestKeyFloat))
                PlayerPrefsEx.DeleteKey<float>(TestKeyFloat);
            if (PlayerPrefsEx.HasKey<string>(TestKeyFloat))
                PlayerPrefsEx.DeleteKey<string>(TestKeyFloat);
            if (PlayerPrefsEx.HasKey<bool>(TestKeyFloat))
                PlayerPrefsEx.DeleteKey<bool>(TestKeyFloat);

            if (PlayerPrefsEx.HasKey<int>(TestKeyString))
                PlayerPrefsEx.DeleteKey<int>(TestKeyString);
            if (PlayerPrefsEx.HasKey<float>(TestKeyString))
                PlayerPrefsEx.DeleteKey<float>(TestKeyString);
            if (PlayerPrefsEx.HasKey<string>(TestKeyString))
                PlayerPrefsEx.DeleteKey<string>(TestKeyString);
            if (PlayerPrefsEx.HasKey<bool>(TestKeyString))
                PlayerPrefsEx.DeleteKey<bool>(TestKeyString);

            // Delete any keys that start with the test prefix
            // Note: PlayerPrefsEx doesn't have a way to enumerate keys,
            // so we just delete the known test keys
        }

        void ResultValidation(string result)
        {
            Debug.Log($"[{nameof(TestToolPlayerPrefs)}] Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsNotEmpty(result, "Result should not be empty.");
            Assert.IsTrue(result.Contains("[Success]"), $"Should contain success message. Result: {result}");
        }

        void ResultValidationExpected(string result, params string[] expectedSubstrings)
        {
            ResultValidation(result);

            foreach (var expected in expectedSubstrings)
                Assert.IsTrue(result.Contains(expected), $"Should contain expected substring: '{expected}'. Result: {result}");
        }

        void ErrorValidation(string result, string? expectedErrorSubstring = null)
        {
            Debug.Log($"[{nameof(TestToolPlayerPrefs)}] Error Result:\n{result}");
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsTrue(result.Contains("[Error]"), $"Should contain error message. Result: {result}");

            if (expectedErrorSubstring != null)
                Assert.IsTrue(result.Contains(expectedErrorSubstring), $"Should contain expected error substring: '{expectedErrorSubstring}'. Result: {result}");
        }
    }
}
