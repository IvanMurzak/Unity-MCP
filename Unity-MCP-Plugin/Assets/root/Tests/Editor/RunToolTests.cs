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
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin.Common;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class RunToolTests
    {
        private Reflector _reflector;
        private ILogger _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _reflector = new Reflector();
            _mockLogger = new MockLogger<RunTool>();
        }

        [Test]
        public void RunTool_CreateFromStaticMethod_ShouldInitializeCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestStaticMethods).GetMethod(nameof(TestStaticMethods.SimpleStaticMethod));

            // Act
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo, "Test Tool");

            // Assert
            Assert.IsNotNull(runTool, "RunTool should be created successfully");
            Assert.AreEqual("Test Tool", runTool.Title, "Title should be set correctly");
            Assert.IsNotNull(runTool.Method, "MethodInfo should be available");
        }

        [Test]
        public void RunTool_CreateFromInstanceMethod_ShouldInitializeCorrectly()
        {
            // Arrange
            var testInstance = new TestInstanceMethods();
            var methodInfo = typeof(TestInstanceMethods).GetMethod(nameof(TestInstanceMethods.SimpleInstanceMethod));

            // Act
            var runTool = RunTool.CreateFromInstanceMethod(_reflector, _mockLogger, testInstance, methodInfo, "Instance Tool");

            // Assert
            Assert.IsNotNull(runTool, "RunTool should be created successfully");
            Assert.AreEqual("Instance Tool", runTool.Title, "Title should be set correctly");
            Assert.IsNotNull(runTool.Method, "MethodInfo should be available");
        }

        [UnityTest]
        public IEnumerator RunTool_Run_WithValidParameters_ShouldReturnSuccess()
        {
            // Arrange
            var methodInfo = typeof(TestStaticMethods).GetMethod(nameof(TestStaticMethods.AddNumbers));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None, 5, 3);

            while (!task.IsCompleted)
                yield return null; // Wait for task to complete

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("8", result.GetMessage(), "Should return correct calculation result");
        }

        [UnityTest]
        public IEnumerator RunTool_Run_WithNamedParameters_ShouldReturnSuccess()
        {
            // Arrange
            var methodInfo = typeof(TestStaticMethods).GetMethod(nameof(TestStaticMethods.AddNumbers));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            var namedParams = new Dictionary<string, JsonElement>
            {
                {"a", JsonSerializer.SerializeToElement(10)},
                {"b", JsonSerializer.SerializeToElement(15)}
            };

            // Act
            var task = runTool.Run("test-request-id", namedParams, CancellationToken.None);
            while (!task.IsCompleted)
                yield return null; // Wait for task to complete

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("25", result.GetMessage(), "Should return correct calculation result");
        }

        [UnityTest]
        public IEnumerator RunTool_Run_WithRequestIDInjection_ShouldInjectCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestStaticMethods).GetMethod(nameof(TestStaticMethods.MethodWithRequestID));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);
            const string expectedRequestId = "test-request-123";

            // Act
            var task = runTool.Run(expectedRequestId, CancellationToken.None);
            while (!task.IsCompleted)
                yield return null; // Wait for task to complete

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual(expectedRequestId, result.GetMessage(), "Should return injected request ID");
        }

        [UnityTest]
        public IEnumerator RunTool_Run_WithException_ShouldReturnError()
        {
            // Arrange
            var methodInfo = typeof(TestStaticMethods).GetMethod(nameof(TestStaticMethods.ThrowingMethod));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None);

            while (!task.IsCompleted)
                yield return null; // Wait for task to complete

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Error, result.Status, "Should return error status");

            var message = result.GetMessage();
            Assert.IsTrue(message.Contains(TestStaticMethods.TestExceptionMessage),
                $"Error message should contain original exception message '{TestStaticMethods.TestExceptionMessage}'. Actual: {message}");
        }

        [UnityTest]
        public IEnumerator RunTool_Run_WithNullParameters_ShouldHandleGracefully()
        {
            // Arrange
            var methodInfo = typeof(TestStaticMethods).GetMethod(nameof(TestStaticMethods.SimpleStaticMethod));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", (Dictionary<string, JsonElement>)null, CancellationToken.None);
            while (!task.IsCompleted)
                yield return null; // Wait for task to complete

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should handle null parameters gracefully");
        }

        [Test]
        public void RunTool_Constructor_WithNullMethodInfo_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RunTool(_reflector, _mockLogger, (MethodInfo)null),
                "Constructor should throw ArgumentNullException for null MethodInfo");
        }

        [UnityTest]
        public IEnumerator RunTool_Run_WithAsyncMethod_ShouldReturnCorrectly()
        {
            // Arrange
            var methodInfo = typeof(TestStaticMethods).GetMethod(nameof(TestStaticMethods.AsyncMethod));
            var runTool = RunTool.CreateFromStaticMethod(_reflector, _mockLogger, methodInfo);

            // Act
            var task = runTool.Run("test-request-id", CancellationToken.None, "Hello");
            while (!task.IsCompleted)
                yield return null; // Wait for task to complete

            var result = task.Result;

            // Assert
            Assert.IsNotNull(result, "Result should not be null");
            Assert.AreEqual(ResponseStatus.Success, result.Status, "Should return success status");
            Assert.AreEqual("Hello Async", result.GetMessage(), "Should return async method result");
        }
    }

    // Test helper classes
    public static class TestStaticMethods
    {
        public const string TestExceptionMessage = "Test exception";

        public static string SimpleStaticMethod()
        {
            return "Static method called";
        }

        public static int AddNumbers(int a, int b)
        {
            return a + b;
        }

        public static string MethodWithRequestID([RequestID] string requestId)
        {
            return requestId;
        }

        public static void ThrowingMethod()
        {
            throw new InvalidOperationException(TestExceptionMessage);
        }

        public static async Task<string> AsyncMethod(string input)
        {
            await Task.Delay(10);
            return input + " Async";
        }
    }

    public class TestInstanceMethods
    {
        public string SimpleInstanceMethod()
        {
            return "Instance method called";
        }

        public int Multiply(int a, int b)
        {
            return a * b;
        }
    }
}