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
using Extensions.Unity.PlayerPrefsEx;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public partial class TestToolPlayerPrefs
    {
        [Test]
        public void ExistsKey_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            var nonExistentKey = TestKeyPrefix + "NonExistent_" + System.Guid.NewGuid().ToString("N")[..8];

            // Act
            var result = _tool.ExistsKey(nonExistentKey);

            // Assert
            ResultValidationExpected(result, "Exists: False");
        }

        [Test]
        public void ExistsKey_ExistingIntKey_ReturnsTrue()
        {
            // Arrange
            PlayerPrefsEx.SetInt(TestKeyInt, 42);

            // Act
            var result = _tool.ExistsKey(TestKeyInt);

            // Assert
            ResultValidationExpected(result, "Exists: True");
        }

        [Test]
        public void ExistsKey_ExistingFloatKey_ReturnsTrue()
        {
            // Arrange
            PlayerPrefsEx.SetFloat(TestKeyFloat, 3.14f);

            // Act
            var result = _tool.ExistsKey(TestKeyFloat);

            // Assert
            ResultValidationExpected(result, "Exists: True");
        }

        [Test]
        public void ExistsKey_ExistingStringKey_ReturnsTrue()
        {
            // Arrange
            PlayerPrefsEx.SetString(TestKeyString, "Hello");

            // Act
            var result = _tool.ExistsKey(TestKeyString);

            // Assert
            ResultValidationExpected(result, "Exists: True");
        }

        [Test]
        public void ExistsKey_NullKey_ReturnsError()
        {
            // Act
            var result = _tool.ExistsKey(null!);

            // Assert
            ErrorValidation(result, "cannot be null or empty");
        }

        [Test]
        public void ExistsKey_EmptyKey_ReturnsError()
        {
            // Act
            var result = _tool.ExistsKey(string.Empty);

            // Assert
            ErrorValidation(result, "cannot be null or empty");
        }

        [Test]
        public void GetKeyType_IntKey_ReturnsInt()
        {
            // Arrange
            PlayerPrefsEx.SetInt(TestKeyInt, 42);

            // Act
            var result = _tool.GetKeyType(TestKeyInt);

            // Assert
            ResultValidationExpected(result, "type: int");
        }

        [Test]
        public void GetKeyType_FloatKey_ReturnsFloat()
        {
            // Arrange
            PlayerPrefsEx.SetFloat(TestKeyFloat, 3.14f);

            // Act
            var result = _tool.GetKeyType(TestKeyFloat);

            // Assert
            ResultValidationExpected(result, "type: float");
        }

        [Test]
        public void GetKeyType_StringKey_ReturnsString()
        {
            // Arrange
            PlayerPrefsEx.SetString(TestKeyString, "Hello World");

            // Act
            var result = _tool.GetKeyType(TestKeyString);

            // Assert
            ResultValidationExpected(result, "type: string");
        }

        [Test]
        public void GetKeyType_NonExistentKey_ReturnsError()
        {
            // Arrange
            var nonExistentKey = TestKeyPrefix + "NonExistent";

            // Act
            var result = _tool.GetKeyType(nonExistentKey);

            // Assert
            ErrorValidation(result, "does not exist");
        }

        [Test]
        public void GetKeyType_NullKey_ReturnsError()
        {
            // Act
            var result = _tool.GetKeyType(null!);

            // Assert
            ErrorValidation(result, "cannot be null or empty");
        }
    }
}
