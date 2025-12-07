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
    public partial class TestToolPlayerPrefs
    {
        #region ReadKey Tests

        [Test]
        public void ReadKey_IntValue_ReturnsCorrectValueAndType()
        {
            // Arrange
            const int expectedValue = 12345;
            PlayerPrefsEx.SetInt(TestKeyInt, expectedValue);

            // Act
            var result = _tool.ReadKey(TestKeyInt);

            // Assert
            ResultValidationExpected(result, expectedValue.ToString(), "type: int");
        }

        [Test]
        public void ReadKey_FloatValue_ReturnsCorrectValueAndType()
        {
            // Arrange
            const float expectedValue = 2.71828f;
            PlayerPrefsEx.SetFloat(TestKeyFloat, expectedValue);

            // Act
            var result = _tool.ReadKey(TestKeyFloat);

            // Assert
            ResultValidationExpected(result, "type: float");
        }

        [Test]
        public void ReadKey_StringValue_ReturnsCorrectValueAndType()
        {
            // Arrange
            const string expectedValue = "Test String Value";
            PlayerPrefsEx.SetString(TestKeyString, expectedValue);

            // Act
            var result = _tool.ReadKey(TestKeyString);

            // Assert
            ResultValidationExpected(result, expectedValue, "type: string");
        }

        [Test]
        public void ReadKey_NonExistentKey_ReturnsError()
        {
            // Arrange
            var nonExistentKey = TestKeyPrefix + "NonExistent";

            // Act
            var result = _tool.ReadKey(nonExistentKey);

            // Assert
            ErrorValidation(result, "does not exist");
        }

        [Test]
        public void ReadKey_NullKey_ReturnsError()
        {
            // Act
            var result = _tool.ReadKey(null!);

            // Assert
            ErrorValidation(result, "cannot be null or empty");
        }

        [Test]
        public void ReadKey_EmptyKey_ReturnsError()
        {
            // Act
            var result = _tool.ReadKey(string.Empty);

            // Assert
            ErrorValidation(result, "cannot be null or empty");
        }

        #endregion

        #region WriteKey Tests

        [Test]
        public void WriteKey_IntValue_WritesCorrectly()
        {
            // Arrange
            const string key = TestKeyInt;
            const int expectedValue = 999;

            // Act
            var result = _tool.WriteKey(key, expectedValue.ToString(), Tool_PlayerPrefs.PlayerPrefsValueType.Int);

            // Assert
            ResultValidation(result);
            Assert.AreEqual(expectedValue, PlayerPrefsEx.GetInt(key, 0), "Value should be written correctly.");
        }

        [Test]
        public void WriteKey_FloatValue_WritesCorrectly()
        {
            // Arrange
            const string key = TestKeyFloat;
            const float expectedValue = 1.618f;

            // Act
            var result = _tool.WriteKey(key, expectedValue.ToString(), Tool_PlayerPrefs.PlayerPrefsValueType.Float);

            // Assert
            ResultValidation(result);
            Assert.AreEqual(expectedValue, PlayerPrefsEx.GetFloat(key, 0f), 0.0001f, "Value should be written correctly.");
        }

        [Test]
        public void WriteKey_StringValue_WritesCorrectly()
        {
            // Arrange
            const string key = TestKeyString;
            const string expectedValue = "Hello PlayerPrefs!";

            // Act
            var result = _tool.WriteKey(key, expectedValue, Tool_PlayerPrefs.PlayerPrefsValueType.String);

            // Assert
            ResultValidation(result);
            Assert.AreEqual(expectedValue, PlayerPrefsEx.GetString(key, string.Empty), "Value should be written correctly.");
        }

        [Test]
        public void WriteKey_NullKey_ReturnsError()
        {
            // Act
            var result = _tool.WriteKey(null!, "value", Tool_PlayerPrefs.PlayerPrefsValueType.String);

            // Assert
            ErrorValidation(result, "cannot be null or empty");
        }

        [Test]
        public void WriteKey_EmptyKey_ReturnsError()
        {
            // Act
            var result = _tool.WriteKey(string.Empty, "value", Tool_PlayerPrefs.PlayerPrefsValueType.String);

            // Assert
            ErrorValidation(result, "cannot be null or empty");
        }

        [Test]
        public void WriteKey_NullValue_ReturnsError()
        {
            // Act
            var result = _tool.WriteKey(TestKeyString, null!, Tool_PlayerPrefs.PlayerPrefsValueType.String);

            // Assert
            ErrorValidation(result, "cannot be null");
        }

        [Test]
        public void WriteKey_InvalidIntValue_ReturnsError()
        {
            // Act
            var result = _tool.WriteKey(TestKeyInt, "not_a_number", Tool_PlayerPrefs.PlayerPrefsValueType.Int);

            // Assert
            ErrorValidation(result, "Cannot parse");
        }

        [Test]
        public void WriteKey_InvalidFloatValue_ReturnsError()
        {
            // Act
            var result = _tool.WriteKey(TestKeyFloat, "not_a_float", Tool_PlayerPrefs.PlayerPrefsValueType.Float);

            // Assert
            ErrorValidation(result, "Cannot parse");
        }

        [Test]
        public void WriteKey_NegativeInt_WritesCorrectly()
        {
            // Arrange
            const string key = TestKeyInt;
            const int expectedValue = -12345;

            // Act
            var result = _tool.WriteKey(key, expectedValue.ToString(), Tool_PlayerPrefs.PlayerPrefsValueType.Int);

            // Assert
            ResultValidation(result);
            Assert.AreEqual(expectedValue, PlayerPrefsEx.GetInt(key, 0), "Negative value should be written correctly.");
        }

        [Test]
        public void WriteKey_NegativeFloat_WritesCorrectly()
        {
            // Arrange
            const string key = TestKeyFloat;
            const float expectedValue = -3.14159f;

            // Act
            var result = _tool.WriteKey(key, expectedValue.ToString(), Tool_PlayerPrefs.PlayerPrefsValueType.Float);

            // Assert
            ResultValidation(result);
            Assert.AreEqual(expectedValue, PlayerPrefsEx.GetFloat(key, 0f), 0.0001f, "Negative float should be written correctly.");
        }

        [Test]
        public void WriteKey_EmptyString_WritesCorrectly()
        {
            // Arrange
            const string key = TestKeyString;
            const string expectedValue = "";

            // Act
            var result = _tool.WriteKey(key, expectedValue, Tool_PlayerPrefs.PlayerPrefsValueType.String);

            // Assert
            ResultValidation(result);
            Assert.AreEqual(expectedValue, PlayerPrefsEx.GetString(key, string.Empty), "Empty string should be written correctly.");
        }

        [Test]
        public void WriteKey_OverwriteExistingValue_UpdatesValue()
        {
            // Arrange
            const string key = TestKeyInt;
            PlayerPrefsEx.SetInt(key, 100);

            // Act
            var result = _tool.WriteKey(key, "200", Tool_PlayerPrefs.PlayerPrefsValueType.Int);

            // Assert
            ResultValidation(result);
            Assert.AreEqual(200, PlayerPrefsEx.GetInt(key, 0), "Value should be overwritten.");
        }

        #endregion
    }
}
