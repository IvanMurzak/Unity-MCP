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
        #region Integration Tests

        [Test]
        public void Integration_WriteReadDelete_IntValue()
        {
            // Arrange
            const string key = TestKeyInt;
            const int value = 42;

            // Act & Assert - Write
            var writeResult = _tool.WriteKey(key, value.ToString(), Tool_PlayerPrefs.PlayerPrefsValueType.Int);
            ResultValidation(writeResult);

            // Act & Assert - Exists
            var existsResult = _tool.ExistsKey(key);
            ResultValidationExpected(existsResult, "Exists: True");

            // Act & Assert - Read
            var readResult = _tool.ReadKey(key);
            ResultValidationExpected(readResult, value.ToString(), "type: int");

            // Act & Assert - GetType
            var typeResult = _tool.GetKeyType(key);
            ResultValidationExpected(typeResult, "type: int");

            // Act & Assert - Delete
            var deleteResult = _tool.DeleteKey(key);
            ResultValidation(deleteResult);

            // Verify deletion
            Assert.IsFalse(PlayerPrefsEx.HasKey<int>(key) || PlayerPrefsEx.HasKey<float>(key) || PlayerPrefsEx.HasKey<string>(key) || PlayerPrefsEx.HasKey<bool>(key), "Key should not exist after deletion.");
        }

        [Test]
        public void Integration_WriteReadDelete_FloatValue()
        {
            // Arrange
            const string key = TestKeyFloat;
            const float value = 3.14159f;

            // Act & Assert - Write
            var writeResult = _tool.WriteKey(key, value.ToString(), Tool_PlayerPrefs.PlayerPrefsValueType.Float);
            ResultValidation(writeResult);

            // Act & Assert - Exists
            var existsResult = _tool.ExistsKey(key);
            ResultValidationExpected(existsResult, "Exists: True");

            // Act & Assert - Read
            var readResult = _tool.ReadKey(key);
            ResultValidationExpected(readResult, "type: float");

            // Act & Assert - GetType
            var typeResult = _tool.GetKeyType(key);
            ResultValidationExpected(typeResult, "type: float");

            // Act & Assert - Delete
            var deleteResult = _tool.DeleteKey(key);
            ResultValidation(deleteResult);

            // Verify deletion
            Assert.IsFalse(PlayerPrefsEx.HasKey<int>(key) || PlayerPrefsEx.HasKey<float>(key) || PlayerPrefsEx.HasKey<string>(key) || PlayerPrefsEx.HasKey<bool>(key), "Key should not exist after deletion.");
        }

        [Test]
        public void Integration_WriteReadDelete_StringValue()
        {
            // Arrange
            const string key = TestKeyString;
            const string value = "Integration Test String";

            // Act & Assert - Write
            var writeResult = _tool.WriteKey(key, value, Tool_PlayerPrefs.PlayerPrefsValueType.String);
            ResultValidation(writeResult);

            // Act & Assert - Exists
            var existsResult = _tool.ExistsKey(key);
            ResultValidationExpected(existsResult, "Exists: True");

            // Act & Assert - Read
            var readResult = _tool.ReadKey(key);
            ResultValidationExpected(readResult, value, "type: string");

            // Act & Assert - GetType
            var typeResult = _tool.GetKeyType(key);
            ResultValidationExpected(typeResult, "type: string");

            // Act & Assert - Delete
            var deleteResult = _tool.DeleteKey(key);
            ResultValidation(deleteResult);

            // Verify deletion
            Assert.IsFalse(PlayerPrefsEx.HasKey<int>(key) || PlayerPrefsEx.HasKey<float>(key) || PlayerPrefsEx.HasKey<string>(key) || PlayerPrefsEx.HasKey<bool>(key), "Key should not exist after deletion.");
        }

        [Test]
        public void Integration_MultipleKeysAndDeleteAll()
        {
            // Arrange - Write multiple values
            _tool.WriteKey(TestKeyInt, "100", Tool_PlayerPrefs.PlayerPrefsValueType.Int);
            _tool.WriteKey(TestKeyFloat, "2.5", Tool_PlayerPrefs.PlayerPrefsValueType.Float);
            _tool.WriteKey(TestKeyString, "Test", Tool_PlayerPrefs.PlayerPrefsValueType.String);

            // Verify all keys exist
            Assert.IsTrue(PlayerPrefsEx.HasKey<int>(TestKeyInt), "Int key should exist.");
            Assert.IsTrue(PlayerPrefsEx.HasKey<float>(TestKeyFloat), "Float key should exist.");
            Assert.IsTrue(PlayerPrefsEx.HasKey<string>(TestKeyString), "String key should exist.");

            // Act - Delete all
            var result = _tool.DeleteAllKeys();

            // Assert
            ResultValidation(result);
            Assert.IsFalse(PlayerPrefsEx.HasKey<int>(TestKeyInt), "Int key should be deleted.");
            Assert.IsFalse(PlayerPrefsEx.HasKey<float>(TestKeyFloat), "Float key should be deleted.");
            Assert.IsFalse(PlayerPrefsEx.HasKey<string>(TestKeyString), "String key should be deleted.");
        }

        [Test]
        public void Integration_OverwriteValueWithDifferentType()
        {
            // Arrange - Write as int first
            _tool.WriteKey(TestKeyInt, "42", Tool_PlayerPrefs.PlayerPrefsValueType.Int);
            var typeResult1 = _tool.GetKeyType(TestKeyInt);
            ResultValidationExpected(typeResult1, "type: int");

            // Act - Overwrite with string
            _tool.WriteKey(TestKeyInt, "Hello", Tool_PlayerPrefs.PlayerPrefsValueType.String);

            // Assert - Type should now be string
            var typeResult2 = _tool.GetKeyType(TestKeyInt);
            ResultValidationExpected(typeResult2, "type: string");

            var readResult = _tool.ReadKey(TestKeyInt);
            ResultValidationExpected(readResult, "Hello", "type: string");
        }

        [Test]
        public void Integration_SaveAndVerify()
        {
            // Arrange
            _tool.WriteKey(TestKeyInt, "123", Tool_PlayerPrefs.PlayerPrefsValueType.Int);

            // Act
            var saveResult = _tool.Save();

            // Assert
            ResultValidation(saveResult);

            // Verify value is still accessible
            var readResult = _tool.ReadKey(TestKeyInt);
            ResultValidationExpected(readResult, "123");
        }

        [Test]
        public void Integration_SpecialCharactersInKey()
        {
            // Arrange
            const string specialKey = TestKeyPrefix + "Special_Key.With:Chars-123";
            const string value = "Special Value";

            try
            {
                // Act
                var writeResult = _tool.WriteKey(specialKey, value, Tool_PlayerPrefs.PlayerPrefsValueType.String);
                ResultValidation(writeResult);

                var readResult = _tool.ReadKey(specialKey);
                ResultValidationExpected(readResult, value);
            }
            finally
            {
                // Cleanup
                if (PlayerPrefsEx.HasKey<int>(specialKey))
                    PlayerPrefsEx.DeleteKey<int>(specialKey);
                if (PlayerPrefsEx.HasKey<float>(specialKey))
                    PlayerPrefsEx.DeleteKey<float>(specialKey);
                if (PlayerPrefsEx.HasKey<string>(specialKey))
                    PlayerPrefsEx.DeleteKey<string>(specialKey);
                if (PlayerPrefsEx.HasKey<bool>(specialKey))
                    PlayerPrefsEx.DeleteKey<bool>(specialKey);
            }
        }

        [Test]
        public void Integration_SpecialCharactersInStringValue()
        {
            // Arrange
            const string value = "Hello\nWorld\tWith\r\nSpecial \"Chars\" & <symbols>";

            // Act
            var writeResult = _tool.WriteKey(TestKeyString, value, Tool_PlayerPrefs.PlayerPrefsValueType.String);
            ResultValidation(writeResult);

            // Assert
            Assert.AreEqual(value, PlayerPrefsEx.GetString(TestKeyString, string.Empty), "Special characters should be preserved.");
        }

        [Test]
        public void Integration_UnicodeStringValue()
        {
            // Arrange
            const string value = "Привет мир! 你好世界! 🌍🚀";

            // Act
            var writeResult = _tool.WriteKey(TestKeyString, value, Tool_PlayerPrefs.PlayerPrefsValueType.String);
            ResultValidation(writeResult);

            // Assert
            Assert.AreEqual(value, PlayerPrefsEx.GetString(TestKeyString, string.Empty), "Unicode characters should be preserved.");
        }

        [Test]
        public void Integration_LargeIntValue()
        {
            // Arrange
            const int value = int.MaxValue;

            // Act
            var writeResult = _tool.WriteKey(TestKeyInt, value.ToString(), Tool_PlayerPrefs.PlayerPrefsValueType.Int);
            ResultValidation(writeResult);

            // Assert
            Assert.AreEqual(value, PlayerPrefsEx.GetInt(TestKeyInt, 0), "Large int value should be handled correctly.");
        }

        [Test]
        public void Integration_SmallIntValue()
        {
            // Arrange
            const int value = int.MinValue;

            // Act
            var writeResult = _tool.WriteKey(TestKeyInt, value.ToString(), Tool_PlayerPrefs.PlayerPrefsValueType.Int);
            ResultValidation(writeResult);

            // Assert
            Assert.AreEqual(value, PlayerPrefsEx.GetInt(TestKeyInt, 0), "Small int value should be handled correctly.");
        }

        #endregion

        #region Error Messages Tests

        [Test]
        public void Error_KeyIsNullOrEmpty_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_PlayerPrefs.Error.KeyIsNullOrEmpty();

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix.");
            Assert.IsTrue(result.Contains("cannot be null or empty"), "Should contain error description.");
        }

        [Test]
        public void Error_KeyDoesNotExist_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_PlayerPrefs.Error.KeyDoesNotExist("test_key");

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix.");
            Assert.IsTrue(result.Contains("test_key"), "Should contain the key name.");
            Assert.IsTrue(result.Contains("does not exist"), "Should contain error description.");
        }

        [Test]
        public void Error_ValueCannotBeNull_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_PlayerPrefs.Error.ValueCannotBeNull();

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix.");
            Assert.IsTrue(result.Contains("cannot be null"), "Should contain error description.");
        }

        [Test]
        public void Error_InvalidValueType_ReturnsCorrectMessage()
        {
            // Act
            var result = Tool_PlayerPrefs.Error.InvalidValueType("invalid");

            // Assert
            Assert.IsTrue(result.Contains("[Error]"), "Should contain error prefix.");
            Assert.IsTrue(result.Contains("invalid"), "Should contain the invalid type.");
            Assert.IsTrue(result.Contains("int") || result.Contains("float") || result.Contains("string"),
                "Should mention valid types.");
        }

        #endregion
    }
}
