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

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("42", result.GetMessage(), "Should return int as string");
            Assert.IsNull(result.StructuredContent, "Primitive types should not have structured content");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsString_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnString));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("Hello World", result.GetMessage(), "Should return string value");
            Assert.IsNull(result.StructuredContent, "String is primitive and should not have structured content");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsFloat_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnFloat));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("3.14", result.GetMessage(), "Should return float as string");
            Assert.IsNull(result.StructuredContent, "Primitive types should not have structured content");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsBool_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnBool));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("True", result.GetMessage(), "Should return bool as string");
            Assert.IsNull(result.StructuredContent, "Primitive types should not have structured content");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsEnum_ShouldReturnAsStringWithoutStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnEnum));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("Success", result.GetMessage(), "Should return enum as string");
            Assert.IsNull(result.StructuredContent, "Enum is primitive and should not have structured content");
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

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("Information", result.GetMessage(), "Should return Microsoft.Extensions.Logging.LogLevel as string");
            Assert.IsNull(result.StructuredContent, "Enum is primitive and should not have structured content");
        }

        #endregion

        #region Custom Class Return Types

        [UnityTest]
        public IEnumerator RunTool_ReturnsCustomClass_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnCustomClass));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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
            Assert.AreEqual("John Doe", nameNode.GetValue<string>(), "Name should match");
            Assert.AreEqual(30, ageNode.GetValue<int>(), "Age should match");

            // Message should contain JSON representation
            var message = result.GetMessage();
            Assert.IsNotNull(message, "Message should not be null");
            Assert.IsTrue(message.Contains("John Doe"), "Message should contain serialized data");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsNestedClass_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnNestedClass));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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

            var nameNode = employeeNode["name"] ?? employeeNode["Name"];
            Assert.IsNotNull(nameNode, "Employee should have name/Name property");
            Assert.AreEqual("Jane Smith", nameNode.GetValue<string>(), "Employee name should match");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsList_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnList));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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
            Assert.AreEqual(3, structuredContent.Count, "List should have 3 items");
            Assert.AreEqual(1, structuredContent[0].GetValue<int>(), "First item should be 1");
            Assert.AreEqual(2, structuredContent[1].GetValue<int>(), "Second item should be 2");
            Assert.AreEqual(3, structuredContent[2].GetValue<int>(), "Third item should be 3");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsDictionary_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnDictionary));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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
            Assert.IsNotNull(structuredContent["key1"], "Should have key1");
            Assert.IsNotNull(structuredContent["key2"], "Should have key2");
            Assert.AreEqual("value1", structuredContent["key1"].GetValue<string>(), "key1 value should match");
            Assert.AreEqual("value2", structuredContent["key2"].GetValue<string>(), "key2 value should match");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsArray_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnArray));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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
            Assert.AreEqual(4, structuredContent.Count, "Array should have 4 items");
            Assert.AreEqual(10, structuredContent[0].GetValue<int>(), "First item should be 10");
            Assert.AreEqual(40, structuredContent[3].GetValue<int>(), "Last item should be 40");
        }

        #endregion

        #region Unity-Specific Return Types

        [UnityTest]
        public IEnumerator RunTool_ReturnsVector3_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnVector3));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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
            Assert.AreEqual(1.0f, structuredContent["x"].GetValue<float>(), 0.001f, "X should match");
            Assert.AreEqual(2.0f, structuredContent["y"].GetValue<float>(), 0.001f, "Y should match");
            Assert.AreEqual(3.0f, structuredContent["z"].GetValue<float>(), 0.001f, "Z should match");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsColor_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnColor));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsQuaternion_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnQuaternion));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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
            Assert.AreEqual(2, structuredContent.Count, "List should have 2 items");

            var firstNameNode = structuredContent[0]["name"] ?? structuredContent[0]["Name"];
            var firstAgeNode = structuredContent[0]["age"] ?? structuredContent[0]["Age"];
            Assert.AreEqual("Alice", firstNameNode.GetValue<string>(), "First person name should match");
            Assert.AreEqual(25, firstAgeNode.GetValue<int>(), "First person age should match");

            var secondNameNode = structuredContent[1]["name"] ?? structuredContent[1]["Name"];
            var secondAgeNode = structuredContent[1]["age"] ?? structuredContent[1]["Age"];
            Assert.AreEqual("Bob", secondNameNode.GetValue<string>(), "Second person name should match");
            Assert.AreEqual(35, secondAgeNode.GetValue<int>(), "Second person age should match");
        }

        [UnityTest]
        public IEnumerator RunTool_ReturnsDictionaryWithComplexValues_ShouldReturnStructuredContent()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnDictionaryWithComplexValues));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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
            Assert.IsNotNull(structuredContent["person1"], "Should have person1 key");

            var nameNode = structuredContent["person1"]["name"] ?? structuredContent["person1"]["Name"];
            var ageNode = structuredContent["person1"]["age"] ?? structuredContent["person1"]["Age"];
            Assert.AreEqual("Charlie", nameNode.GetValue<string>(), "person1 name should match");
            Assert.AreEqual(40, ageNode.GetValue<int>(), "person1 age should match");
        }

        #endregion

        #region JSON Serialization Validation

        [UnityTest]
        public IEnumerator RunTool_StructuredContent_ShouldBeValidJson()
        {
            // Arrange
            var methodInfo = typeof(TestReturnTypeMethods).GetMethod(nameof(TestReturnTypeMethods.ReturnCustomClass));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

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
            Assert.IsTrue(jsonString.Contains("John Doe"), "JSON should contain the name value");

            // Verify it can be deserialized back
            var deserializedNode = JsonNode.Parse(jsonString);
            Assert.IsNotNull(deserializedNode, "Should deserialize back to JsonNode");
            var deserializedNameNode = deserializedNode["name"] ?? deserializedNode["Name"];
            Assert.AreEqual("John Doe", deserializedNameNode.GetValue<string>(), "Deserialized value should match");
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
