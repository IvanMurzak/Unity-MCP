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
using System.Collections;
using System.Linq;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.Unity.MCP.Editor.Tests.Utils;
using com.IvanMurzak.Unity.MCP.Reflection.Converter;
using com.IvanMurzak.Unity.MCP.TestFiles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    /// <summary>
    /// Tests to verify that blacklisted types are excluded from serialization.
    /// </summary>
    public class BlacklistTypeTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp() => base.SetUp();

        [UnityTearDown]
        public override IEnumerator TearDown() => base.TearDown();

        /// <summary>
        /// Creates a new Reflector configured similarly to UnityMcpPlugin.CreateDefaultReflector()
        /// but with custom blacklisted types for testing.
        /// </summary>
        private Reflector CreateTestReflector()
        {
            var reflector = new Reflector();
            reflector.JsonSerializerOptions.NumberHandling =
                System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;

            // Remove default converters and add Unity-specific ones
            reflector.Converters.Remove<GenericReflectionConverter<object>>();
            reflector.Converters.Remove<ArrayReflectionConverter>();
            reflector.Converters.Add(new UnityGenericReflectionConverter<object>());
            reflector.Converters.Add(new UnityArrayReflectionConverter());

            // Add Unity struct converters
            reflector.Converters.Add(new UnityEngine_Vector3_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Color_ReflectionConverter());

            // Add Unity object converters
            reflector.Converters.Add(new UnityEngine_Object_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_GameObject_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Component_ReflectionConverter());
            reflector.Converters.Add(new UnityEngine_Material_ReflectionConverter());

            return reflector;
        }

        [UnityTest]
        public IEnumerator Serialize_Component_WithBlacklistedType_Field_ShouldNotContainBlacklistedField()
        {
            // Arrange
            var reflector = CreateTestReflector();

            // Blacklist the test types
            reflector.Converters.BlacklistType(typeof(BlacklistedType));

            // Create a GameObject with the test component
            var go = new GameObject("TestGameObject_Blacklist");
            var component = go.AddComponent<BlacklistTestScript>();

            // Set values on the component
            component.normalIntField = 42;
            component.normalStringField = "Hello World";
            component.normalVectorField = new Vector3(1, 2, 3);
            component.blacklistedTypeField = new BlacklistedType { value = 100, name = "ShouldNotAppear" };

            yield return null;

            // Act
            var serialized = reflector.Serialize(
                component,
                recursive: true,
                logger: _logger);

            var json = serialized.ToJson(reflector) ?? string.Empty;
            Debug.Log($"[BlacklistTypeTests] Serialized component:\n{json}");

            // Assert
            Assert.IsNotNull(serialized, "Serialized result should not be null.");
            Assert.IsNotNull(serialized.fields, "Serialized fields should not be null.");

            // Verify normal fields are present
            var normalIntFieldExists = serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalIntField));
            var normalStringFieldExists = serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalStringField));
            var normalVectorFieldExists = serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalVectorField));

            Assert.IsTrue(normalIntFieldExists, "Normal int field should be serialized.");
            Assert.IsTrue(normalStringFieldExists, "Normal string field should be serialized.");
            Assert.IsTrue(normalVectorFieldExists, "Normal vector field should be serialized.");

            // Verify blacklisted field is NOT present
            var blacklistedFieldExists = serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.blacklistedTypeField));
            Assert.IsFalse(blacklistedFieldExists, "Blacklisted type field should NOT be serialized.");

            // Also verify the JSON string does not contain the blacklisted field name
            Assert.IsFalse(json.Contains("blacklistedTypeField"), "JSON should not contain 'blacklistedTypeField'.");
            Assert.IsFalse(json.Contains("ShouldNotAppear"), "JSON should not contain the value from blacklisted type.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Serialize_Component_WithBlacklistedStruct_Field_ShouldNotContainBlacklistedField()
        {
            // Arrange
            var reflector = CreateTestReflector();

            // Blacklist the test struct type
            reflector.Converters.BlacklistType(typeof(BlacklistedStruct));

            // Create a GameObject with the test component
            var go = new GameObject("TestGameObject_BlacklistStruct");
            var component = go.AddComponent<BlacklistTestScript>();

            // Set values on the component
            component.normalIntField = 123;
            component.blacklistedStructField = new BlacklistedStruct { x = 10.5f, y = 20.5f };

            yield return null;

            // Act
            var serialized = reflector.Serialize(
                component,
                recursive: true,
                logger: _logger);

            var json = serialized.ToJson(reflector) ?? string.Empty;
            Debug.Log($"[BlacklistTypeTests] Serialized component with struct:\n{json}");

            // Assert
            Assert.IsNotNull(serialized, "Serialized result should not be null.");
            Assert.IsNotNull(serialized.fields, "Serialized fields should not be null.");

            // Verify normal field is present
            var normalIntFieldExists = serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalIntField));
            Assert.IsTrue(normalIntFieldExists, "Normal int field should be serialized.");

            // Verify blacklisted struct field is NOT present
            var blacklistedStructFieldExists = serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.blacklistedStructField));
            Assert.IsFalse(blacklistedStructFieldExists, "Blacklisted struct field should NOT be serialized.");

            // Also verify the JSON string does not contain the blacklisted field name
            Assert.IsFalse(json.Contains("blacklistedStructField"), "JSON should not contain 'blacklistedStructField'.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Serialize_Component_WithMultipleBlacklistedTypes_ShouldNotContainAnyBlacklistedFields()
        {
            // Arrange
            var reflector = CreateTestReflector();

            // Blacklist both test types
            reflector.Converters.BlacklistType(typeof(BlacklistedType));
            reflector.Converters.BlacklistType(typeof(BlacklistedStruct));

            // Create a GameObject with the test component
            var go = new GameObject("TestGameObject_MultiBlacklist");
            var component = go.AddComponent<BlacklistTestScript>();

            // Set values on the component
            component.normalIntField = 999;
            component.normalStringField = "This should appear";
            component.normalVectorField = Vector3.one;
            component.blacklistedTypeField = new BlacklistedType { value = 1, name = "Hidden1" };
            component.blacklistedStructField = new BlacklistedStruct { x = 1, y = 2 };

            yield return null;

            // Act
            var serialized = reflector.Serialize(
                component,
                recursive: true,
                logger: _logger);

            var json = serialized.ToJson(reflector) ?? string.Empty;
            Debug.Log($"[BlacklistTypeTests] Serialized component with multiple blacklisted types:\n{json}");

            // Assert
            Assert.IsNotNull(serialized, "Serialized result should not be null.");
            Assert.IsNotNull(serialized.fields, "Serialized fields should not be null.");

            // Verify normal fields are present
            Assert.IsTrue(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalIntField)),
                "Normal int field should be serialized.");
            Assert.IsTrue(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalStringField)),
                "Normal string field should be serialized.");
            Assert.IsTrue(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalVectorField)),
                "Normal vector field should be serialized.");

            // Verify blacklisted fields are NOT present
            Assert.IsFalse(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.blacklistedTypeField)),
                "Blacklisted type field should NOT be serialized.");
            Assert.IsFalse(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.blacklistedStructField)),
                "Blacklisted struct field should NOT be serialized.");

            // Also verify the JSON string does not contain the blacklisted field names
            Assert.IsFalse(json.Contains("blacklistedTypeField"), "JSON should not contain 'blacklistedTypeField'.");
            Assert.IsFalse(json.Contains("blacklistedStructField"), "JSON should not contain 'blacklistedStructField'.");
            Assert.IsFalse(json.Contains("Hidden1"), "JSON should not contain values from blacklisted types.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Serialize_Component_WithoutBlacklist_ShouldContainAllFields()
        {
            // Arrange
            var reflector = CreateTestReflector();
            // NOTE: We do NOT blacklist any types here

            // Create a GameObject with the test component
            var go = new GameObject("TestGameObject_NoBlacklist");
            var component = go.AddComponent<BlacklistTestScript>();

            // Set values on the component
            component.normalIntField = 42;
            component.normalStringField = "Test";
            component.blacklistedTypeField = new BlacklistedType { value = 100, name = "ShouldAppear" };
            component.blacklistedStructField = new BlacklistedStruct { x = 5, y = 10 };

            yield return null;

            // Act
            var serialized = reflector.Serialize(
                component,
                recursive: true,
                logger: _logger);

            var json = serialized.ToJson(reflector) ?? string.Empty;
            Debug.Log($"[BlacklistTypeTests] Serialized component without blacklist:\n{json}");

            // Assert
            Assert.IsNotNull(serialized, "Serialized result should not be null.");
            Assert.IsNotNull(serialized.fields, "Serialized fields should not be null.");

            // Verify ALL fields are present (since nothing is blacklisted)
            Assert.IsTrue(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalIntField)),
                "Normal int field should be serialized.");
            Assert.IsTrue(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalStringField)),
                "Normal string field should be serialized.");
            Assert.IsTrue(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.blacklistedTypeField)),
                "BlacklistedType field should be serialized when NOT blacklisted.");
            Assert.IsTrue(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.blacklistedStructField)),
                "BlacklistedStruct field should be serialized when NOT blacklisted.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Serialize_Material_WithBlacklistedColor_ShouldNotContainColorMembers()
        {
            // Arrange
            var reflector = CreateTestReflector();

            // Blacklist the UnityEngine.Color type
            reflector.Converters.BlacklistType(typeof(Color));

            // Create a Material instance using executor (handles cleanup automatically)
            var materialEx = new CreateMaterialExecutor("TestMaterial_BlacklistColor.mat", "Standard", "Assets", "Unity-MCP-Test", "BlacklistTypeTests");

            // Validation executor
            var validateEx = new LazyNodeExecutor();
            validateEx.SetAction<object?>((input) =>
            {
                var material = materialEx.Asset;
                Assert.IsNotNull(material, "Material should be created.");

                // Set color on the material
                material!.color = new Color(1f, 0.5f, 0.25f, 1f);
                material.SetColor("_EmissionColor", new Color(0.1f, 0.2f, 0.3f, 1f));

                // Act
                var serialized = reflector.Serialize(
                    material,
                    recursive: true,
                    logger: _logger);

                var json = serialized.ToJson(reflector) ?? string.Empty;
                Debug.Log($"[BlacklistTypeTests] Serialized Material with blacklisted Color:\n{json}");

                // Assert
                Assert.IsNotNull(serialized, "Serialized result should not be null.");

                // Helper function to recursively check all fields for Color type
                void AssertNoColorFields(SerializedMember member, string path)
                {
                    if (member.fields != null)
                    {
                        foreach (var field in member.fields)
                        {
                            // Check that no field is of Color type
                            var isColorType = field.typeName?.Contains("Color") == true &&
                                              field.typeName?.Contains("UnityEngine") == true;
                            Assert.IsFalse(isColorType,
                                $"Field '{path}.{field.name}' of type '{field.typeName}' should not be serialized when Color is blacklisted.");

                            // Recursively check nested fields
                            AssertNoColorFields(field, $"{path}.{field.name}");
                        }
                    }
                }

                // Check all fields recursively
                AssertNoColorFields(serialized, "root");

                // Also check that the JSON does not contain UnityEngine.Color type references
                Assert.IsFalse(json.Contains("UnityEngine.Color"),
                    "JSON should not contain 'UnityEngine.Color' type reference.");
            });

            materialEx.AddChild(validateEx);
            materialEx.Execute();

            yield return null;
        }

        [UnityTest]
        public IEnumerator Serialize_Component_BlacklistByTypeName_ShouldNotContainBlacklistedField()
        {
            // Arrange
            var reflector = CreateTestReflector();

            // Blacklist using full type name string (similar to how TMPro types are blacklisted)
            var fullTypeName = typeof(BlacklistedType).FullName!;
            var type = ReflectorNet.Utils.TypeUtils.GetType(fullTypeName);
            Assert.IsNotNull(type, $"Type '{fullTypeName}' should be resolvable.");
            reflector.Converters.BlacklistType(type!);

            // Create a GameObject with the test component
            var go = new GameObject("TestGameObject_BlacklistByName");
            var component = go.AddComponent<BlacklistTestScript>();

            // Set values on the component
            component.normalIntField = 77;
            component.blacklistedTypeField = new BlacklistedType { value = 200, name = "HiddenByName" };

            yield return null;

            // Act
            var serialized = reflector.Serialize(
                component,
                recursive: true,
                logger: _logger);

            var json = serialized.ToJson(reflector) ?? string.Empty;
            Debug.Log($"[BlacklistTypeTests] Serialized component with type blacklisted by name:\n{json}");

            // Assert
            Assert.IsNotNull(serialized, "Serialized result should not be null.");
            Assert.IsNotNull(serialized.fields, "Serialized fields should not be null.");

            // Verify normal field is present
            Assert.IsTrue(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.normalIntField)),
                "Normal int field should be serialized.");

            // Verify blacklisted field is NOT present
            Assert.IsFalse(serialized.fields.Any(f => f.name == nameof(BlacklistTestScript.blacklistedTypeField)),
                "Blacklisted type field should NOT be serialized when blacklisted by type name.");

            // Also verify the JSON string does not contain the blacklisted field name
            Assert.IsFalse(json.Contains("blacklistedTypeField"), "JSON should not contain 'blacklistedTypeField'.");
            Assert.IsFalse(json.Contains("HiddenByName"), "JSON should not contain values from blacklisted types.");

            yield return null;
        }
    }
}
