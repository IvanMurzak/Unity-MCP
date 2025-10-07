/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common;
using com.IvanMurzak.Unity.MCP.Common.Model;
using com.IvanMurzak.Unity.MCP.Utils;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class RunToolStructuredContentTests
    {
        private Reflector _reflector;
        private Microsoft.Extensions.Logging.ILogger _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _reflector = new Reflector();

            // Register Unity type converters to avoid circular reference issues
            _reflector.JsonSerializer.AddConverter(new com.IvanMurzak.Unity.MCP.Common.Json.Converters.Vector3Converter());
            _reflector.JsonSerializer.AddConverter(new com.IvanMurzak.Unity.MCP.Common.Json.Converters.ColorConverter());
            _reflector.JsonSerializer.AddConverter(new com.IvanMurzak.Unity.MCP.Common.Json.Converters.QuaternionConverter());

            _mockLogger = UnityLoggerFactory.LoggerFactory.CreateLogger<RunToolStructuredContentTests>();
        }

        #region Primitive Return Types

        [UnityTest]
        public IEnumerator RunTool_ReturnsInt_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnInt));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnInt();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual(expectedValue.ToString(), result.GetMessage(), "Should return int as string");
            Assert.IsNull(result.StructuredContent, "Primitive types should not have structured content");

            // Verify the int value can be parsed back to the original value
            var parsedValue = int.Parse(result.GetMessage());
            Assert.AreEqual(expectedValue, parsedValue, "Parsed int should match original value");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsString_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnString));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnString();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual(expectedValue, result.GetMessage(), "Should return string value");
            Assert.IsNull(result.StructuredContent, "String is primitive and should not have structured content");

            // Verify the string value matches exactly (no corruption)
            Assert.AreEqual(expectedValue, result.GetMessage(), "String should match original value exactly");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsFloat_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnFloat));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnFloat();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual(expectedValue.ToString(), result.GetMessage(), "Should return float as string");
            Assert.IsNull(result.StructuredContent, "Primitive types should not have structured content");

            // Verify the float value can be parsed back to the original value
            var parsedValue = float.Parse(result.GetMessage());
            Assert.AreEqual(expectedValue, parsedValue, 0.001f, "Parsed float should match original value");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsBool_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnBool));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnBool();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual(expectedValue.ToString(), result.GetMessage(), "Should return bool as string");
            Assert.IsNull(result.StructuredContent, "Primitive types should not have structured content");

            // Verify the bool value can be parsed back to the original value
            var parsedValue = bool.Parse(result.GetMessage());
            Assert.AreEqual(expectedValue, parsedValue, "Parsed bool should match original value");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsEnum_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnEnum));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedEnumValue = TestReturnTypeMethods.ReturnEnum();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual(expectedEnumValue.ToString(), result.GetMessage(), "Should return enum as string");
            Assert.IsNull(result.StructuredContent, "Enum is primitive and should not have structured content");

            // Verify the enum value can be parsed back to the original value
            var parsedValue = Enum.Parse(typeof(ResponseStatus), result.GetMessage());
            Assert.AreEqual(expectedEnumValue, parsedValue, "Parsed enum should match original value");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsNull_ShouldReturnSuccessWithNullMessage()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnNull));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsTrue(result.GetMessage() == null || result.GetMessage() == "", "Message should be null or empty");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsMicrosoftLogLevel_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnMicrosoftLogLevel));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnMicrosoftLogLevel();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual(expectedValue.ToString(), result.GetMessage(), "Should return Microsoft.Extensions.Logging.LogLevel as string");
            Assert.IsNull(result.StructuredContent, "Enum is primitive and should not have structured content");

            // Verify the enum value can be parsed back to the original value
            var parsedValue = Enum.Parse(typeof(Microsoft.Extensions.Logging.LogLevel), result.GetMessage());
            Assert.AreEqual(expectedValue, parsedValue, "Parsed enum should match original value");
        }

        #endregion

        #region Custom Class Return Types

        [UnityTest]
        public IEnumerator RunTool_ReturnsCustomClass_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnCustomClass));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnCustomClass();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "Custom class should have structured content");

            var structuredContent = result.StructuredContent;

            // Check if properties are in camelCase (name, age) or PascalCase (Name, Age)
            var nameNode = structuredContent["name"] ?? structuredContent["Name"];
            var ageNode = structuredContent["age"] ?? structuredContent["Age"];

            Assert.IsNotNull(nameNode, "Should have name/Name property");
            Assert.IsNotNull(ageNode, "Should have age/Age property");
            Assert.AreEqual(expectedValue.Name, nameNode.GetValue<string>(), "Name should match");
            Assert.AreEqual(expectedValue.Age, ageNode.GetValue<int>(), "Age should match");

            // Message should contain JSON representation
            var message = result.GetMessage();
            Assert.IsNotNull(message, "Message should not be null");
            Assert.IsTrue(message.Contains(expectedValue.Name), "Message should contain serialized data");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsNestedClass_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnNestedClass));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnNestedClass();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "Nested class should have structured content");

            var structuredContent = result.StructuredContent;
            var companyNameNode = structuredContent["companyName"] ?? structuredContent["CompanyName"];
            var employeeNode = structuredContent["employee"] ?? structuredContent["Employee"];

            Assert.IsNotNull(companyNameNode, "Should have companyName/CompanyName property");
            Assert.IsNotNull(employeeNode, "Should have employee/Employee property");
            Assert.AreEqual(expectedValue.CompanyName, companyNameNode.GetValue<string>(), "Company name should match");

            var nameNode = employeeNode["name"] ?? employeeNode["Name"];
            Assert.IsNotNull(nameNode, "Employee should have name/Name property");
            Assert.AreEqual(expectedValue.Employee.Name, nameNode.GetValue<string>(), "Employee name should match");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsList_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnList));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnList();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "List should have structured content");

            var structuredContent = result.StructuredContent.AsArray();
            Assert.AreEqual(expectedValue.Count, structuredContent.Count, $"List should have {expectedValue.Count} items");
            for (int i = 0; i < expectedValue.Count; i++)
            {
                Assert.AreEqual(expectedValue[i], structuredContent[i].GetValue<int>(), $"Item at index {i} should match");
            }
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsDictionary_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnDictionary));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnDictionary();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "Dictionary should have structured content");

            var structuredContent = result.StructuredContent;
            foreach (var kvp in expectedValue)
            {
                Assert.IsNotNull(structuredContent[kvp.Key], $"Should have {kvp.Key}");
                Assert.AreEqual(kvp.Value, structuredContent[kvp.Key].GetValue<string>(), $"{kvp.Key} value should match");
            }
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsArray_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnArray));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnArray();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "Array should have structured content");

            var structuredContent = result.StructuredContent.AsArray();
            Assert.AreEqual(expectedValue.Length, structuredContent.Count, $"Array should have {expectedValue.Length} items");
            for (int i = 0; i < expectedValue.Length; i++)
            {
                Assert.AreEqual(expectedValue[i], structuredContent[i].GetValue<int>(), $"Item at index {i} should match expected value");
            }
        }

        #endregion

        #region Unity-Specific Return Types

        [UnityTest]
        public IEnumerator RunTool_ReturnsVector3_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnVector3));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnVector3();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "Vector3 should have structured content");

            var structuredContent = result.StructuredContent;
            Assert.IsNotNull(structuredContent["x"], "Should have x property");
            Assert.IsNotNull(structuredContent["y"], "Should have y property");
            Assert.IsNotNull(structuredContent["z"], "Should have z property");
            Assert.AreEqual(expectedValue.x, structuredContent["x"].GetValue<float>(), 0.001f, "X should match");
            Assert.AreEqual(expectedValue.y, structuredContent["y"].GetValue<float>(), 0.001f, "Y should match");
            Assert.AreEqual(expectedValue.z, structuredContent["z"].GetValue<float>(), 0.001f, "Z should match");

            // Verify the Vector3 can be reconstructed from structured content
            var reconstructedVector = new Vector3(
                structuredContent["x"].GetValue<float>(),
                structuredContent["y"].GetValue<float>(),
                structuredContent["z"].GetValue<float>()
            );
            Assert.AreEqual(expectedValue, reconstructedVector, "Reconstructed Vector3 should match original");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsColor_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnColor));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnColor();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "Color should have structured content");

            var structuredContent = result.StructuredContent;
            Assert.IsNotNull(structuredContent["r"], "Should have r property");
            Assert.IsNotNull(structuredContent["g"], "Should have g property");
            Assert.IsNotNull(structuredContent["b"], "Should have b property");
            Assert.IsNotNull(structuredContent["a"], "Should have a property");

            // Verify the Color can be reconstructed from structured content
            var reconstructedColor = new Color(
                structuredContent["r"].GetValue<float>(),
                structuredContent["g"].GetValue<float>(),
                structuredContent["b"].GetValue<float>(),
                structuredContent["a"].GetValue<float>()
            );
            Assert.AreEqual(expectedValue, reconstructedColor, "Reconstructed Color should match original");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsQuaternion_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnQuaternion));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnQuaternion();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "Quaternion should have structured content");

            var structuredContent = result.StructuredContent;
            Assert.IsNotNull(structuredContent["x"], "Should have x property");
            Assert.IsNotNull(structuredContent["y"], "Should have y property");
            Assert.IsNotNull(structuredContent["z"], "Should have z property");
            Assert.IsNotNull(structuredContent["w"], "Should have w property");

            // Verify the Quaternion can be reconstructed from structured content
            var reconstructedQuaternion = new Quaternion(
                structuredContent["x"].GetValue<float>(),
                structuredContent["y"].GetValue<float>(),
                structuredContent["z"].GetValue<float>(),
                structuredContent["w"].GetValue<float>()
            );
            Assert.AreEqual(expectedValue, reconstructedQuaternion, "Reconstructed Quaternion should match original");
        }

        #endregion

        #region ResponseCallTool Return Type

        [UnityTest]
        public IEnumerator RunTool_ReturnsResponseCallTool_ShouldPassThroughDirectly()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnResponseCallTool));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("Custom Response", result.GetMessage(), "Should return custom response message");
        }

        #endregion

        #region Complex Scenarios

        [UnityTest]
        public IEnumerator RunTool_ReturnsListOfCustomObjects_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnListOfCustomObjects));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnListOfCustomObjects();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "List of custom objects should have structured content");

            var structuredContent = result.StructuredContent.AsArray();
            Assert.AreEqual(expectedValue.Count, structuredContent.Count, $"List should have {expectedValue.Count} items");

            for (int i = 0; i < expectedValue.Count; i++)
            {
                var nameNode = structuredContent[i]["name"] ?? structuredContent[i]["Name"];
                var ageNode = structuredContent[i]["age"] ?? structuredContent[i]["Age"];
                Assert.AreEqual(expectedValue[i].Name, nameNode.GetValue<string>(), $"Item {i} name should match");
                Assert.AreEqual(expectedValue[i].Age, ageNode.GetValue<int>(), $"Item {i} age should match");
            }
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsDictionaryWithComplexValues_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnDictionaryWithComplexValues));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnDictionaryWithComplexValues();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.IsNotNull(result.StructuredContent, "Dictionary with complex values should have structured content");

            var structuredContent = result.StructuredContent;
            foreach (var kvp in expectedValue)
            {
                Assert.IsNotNull(structuredContent[kvp.Key], $"Should have {kvp.Key} key");

                var nameNode = structuredContent[kvp.Key]["name"] ?? structuredContent[kvp.Key]["Name"];
                var ageNode = structuredContent[kvp.Key]["age"] ?? structuredContent[kvp.Key]["Age"];
                Assert.AreEqual(kvp.Value.Name, nameNode.GetValue<string>(), $"{kvp.Key} name should match");
                Assert.AreEqual(kvp.Value.Age, ageNode.GetValue<int>(), $"{kvp.Key} age should match");
            }
        }

        #endregion

        #region JSON Serialization Validation

        [UnityTest]
        public IEnumerator RunTool_StructuredContent_ShouldBeValidJson()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnCustomClass));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var expectedValue = TestReturnTypeMethods.ReturnCustomClass();

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result.StructuredContent, "Should have structured content");

            // Verify it can be serialized to valid JSON
            var jsonString = result.StructuredContent.ToJsonString();
            Assert.IsNotNull(jsonString, "Should serialize to JSON string");
            Assert.IsTrue(jsonString.Contains("name") || jsonString.Contains("Name"), "JSON should contain name/Name property");
            Assert.IsTrue(jsonString.Contains(expectedValue.Name), $"JSON should contain the name value: {expectedValue.Name}");

            // Verify it can be deserialized back
            var deserializedNode = JsonNode.Parse(jsonString);
            Assert.IsNotNull(deserializedNode, "Should deserialize back to JsonNode");
            var deserializedNameNode = deserializedNode["name"] ?? deserializedNode["Name"];
            Assert.AreEqual(expectedValue.Name, deserializedNameNode.GetValue<string>(), "Deserialized value should match");
        }

        #endregion
    }

    #region Test Helper Classes

    public static class TestReturnTypeMethods
    {
        // Primitive types
        public static int ReturnInt() => 42;
        public static string ReturnString() => "Hello World";
        public static float ReturnFloat() => 3.14f;
        public static bool ReturnBool() => true;
        public static ResponseStatus ReturnEnum() => ResponseStatus.Success;
        public static Microsoft.Extensions.Logging.LogLevel ReturnMicrosoftLogLevel() => Microsoft.Extensions.Logging.LogLevel.Information;
        public static string ReturnNull() => null;

        // Custom classes
        public static Person ReturnCustomClass() => new Person { Name = "John Doe", Age = 30 };

        public static Company ReturnNestedClass() => new Company
        {
            CompanyName = "Acme Corp",
            Employee = new Person { Name = "Jane Smith", Age = 28 }
        };

        // Collections
        public static List<int> ReturnList() => new List<int> { 1, 2, 3 };

        public static Dictionary<string, string> ReturnDictionary() => new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        public static int[] ReturnArray() => new int[] { 10, 20, 30, 40 };

        // Unity types
        public static Vector3 ReturnVector3() => new Vector3(1.0f, 2.0f, 3.0f);
        public static Color ReturnColor() => Color.red;
        public static Quaternion ReturnQuaternion() => Quaternion.identity;

        // ResponseCallTool
        public static ResponseCallTool ReturnResponseCallTool() => ResponseCallTool.Success("Custom Response");

        // Complex scenarios
        public static List<Person> ReturnListOfCustomObjects() => new List<Person>
        {
            new Person { Name = "Alice", Age = 25 },
            new Person { Name = "Bob", Age = 35 }
        };

        public static Dictionary<string, Person> ReturnDictionaryWithComplexValues() => new Dictionary<string, Person>
        {
            { "person1", new Person { Name = "Charlie", Age = 40 } },
            { "person2", new Person { Name = "Diana", Age = 32 } }
        };
    }

    [Serializable]
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Serializable]
    public class Company
    {
        public string CompanyName { get; set; }
        public Person Employee { get; set; }
    }

    #endregion
}
