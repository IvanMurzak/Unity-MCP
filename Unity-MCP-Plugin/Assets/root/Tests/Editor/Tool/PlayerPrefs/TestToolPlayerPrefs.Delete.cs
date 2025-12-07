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
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolPlayerPrefs
    {
        #region DeleteKey Tests

        [Test]
        public void DeleteKey_ExistingKey_DeletesSuccessfully()
        {
            // Arrange
            PlayerPrefs.SetInt(TestKeyInt, 42);
            Assert.IsTrue(PlayerPrefs.HasKey(TestKeyInt), "Key should exist before deletion.");

            // Act
            var result = _tool.DeleteKey(TestKeyInt);

            // Assert
            ResultValidation(result);
            Assert.IsFalse(PlayerPrefs.HasKey(TestKeyInt), "Key should not exist after deletion.");
        }

        [Test]
        public void DeleteKey_NonExistentKey_ReturnsError()
        {
            // Arrange
            var nonExistentKey = TestKeyPrefix + "NonExistent_Delete";

            // Act
            var result = _tool.DeleteKey(nonExistentKey);

            // Assert
            ErrorValidation(result, "does not exist");
        }

        [Test]
        public void DeleteKey_NullKey_ReturnsError()
        {
            // Act
            var result = _tool.DeleteKey(null!);

            // Assert
            ErrorValidation(result, "cannot be null or empty");
        }

        [Test]
        public void DeleteKey_EmptyKey_ReturnsError()
        {
            // Act
            var result = _tool.DeleteKey(string.Empty);

            // Assert
            ErrorValidation(result, "cannot be null or empty");
        }

        [Test]
        public void DeleteKey_IntKey_DeletesSuccessfully()
        {
            // Arrange
            PlayerPrefs.SetInt(TestKeyInt, 999);

            // Act
            var result = _tool.DeleteKey(TestKeyInt);

            // Assert
            ResultValidation(result);
            Assert.IsFalse(PlayerPrefs.HasKey(TestKeyInt), "Int key should be deleted.");
        }

        [Test]
        public void DeleteKey_FloatKey_DeletesSuccessfully()
        {
            // Arrange
            PlayerPrefs.SetFloat(TestKeyFloat, 3.14f);

            // Act
            var result = _tool.DeleteKey(TestKeyFloat);

            // Assert
            ResultValidation(result);
            Assert.IsFalse(PlayerPrefs.HasKey(TestKeyFloat), "Float key should be deleted.");
        }

        [Test]
        public void DeleteKey_StringKey_DeletesSuccessfully()
        {
            // Arrange
            PlayerPrefs.SetString(TestKeyString, "Hello");

            // Act
            var result = _tool.DeleteKey(TestKeyString);

            // Assert
            ResultValidation(result);
            Assert.IsFalse(PlayerPrefs.HasKey(TestKeyString), "String key should be deleted.");
        }

        #endregion

        #region DeleteAllKeys Tests

        [Test]
        public void DeleteAllKeys_WithExistingKeys_DeletesAll()
        {
            // Arrange
            PlayerPrefs.SetInt(TestKeyInt, 42);
            PlayerPrefs.SetFloat(TestKeyFloat, 3.14f);
            PlayerPrefs.SetString(TestKeyString, "Hello");

            // Act
            var result = _tool.DeleteAllKeys();

            // Assert
            ResultValidation(result);
            Assert.IsFalse(PlayerPrefs.HasKey(TestKeyInt), "Int key should be deleted.");
            Assert.IsFalse(PlayerPrefs.HasKey(TestKeyFloat), "Float key should be deleted.");
            Assert.IsFalse(PlayerPrefs.HasKey(TestKeyString), "String key should be deleted.");
        }

        [Test]
        public void DeleteAllKeys_NoExistingKeys_SucceedsWithoutError()
        {
            // Arrange - make sure no test keys exist
            CleanupTestKeys();

            // Act
            var result = _tool.DeleteAllKeys();

            // Assert
            ResultValidation(result);
        }

        #endregion

        #region Save Tests

        [Test]
        public void Save_AfterWriting_SavesSuccessfully()
        {
            // Arrange
            PlayerPrefs.SetInt(TestKeyInt, 42);

            // Act
            var result = _tool.Save();

            // Assert
            ResultValidation(result);
        }

        [Test]
        public void Save_WithNoChanges_SucceedsWithoutError()
        {
            // Act
            var result = _tool.Save();

            // Assert
            ResultValidation(result);
        }

        #endregion
    }
}
